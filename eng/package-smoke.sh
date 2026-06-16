#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

if [ "$#" -ne 1 ]; then
  echo "Usage: ./eng/package-smoke.sh <version>" >&2
  exit 1
fi

version="$1"
package_dir="artifacts/nuget"

if [ ! -d "$package_dir" ]; then
  echo "Package directory does not exist: $package_dir" >&2
  exit 1
fi

package_count="$(find "$package_dir" -maxdepth 1 -type f -name "*.$version.nupkg" ! -name "*.snupkg" | wc -l | tr -d ' ')"
expected_count="$(semantic_type_model_package_ids | wc -l | tr -d ' ')"
if [ "$package_count" = "0" ]; then
  echo "No local .nupkg files found for version $version in $package_dir." >&2
  exit 1
fi
if [ "$package_count" != "$expected_count" ]; then
  echo "Expected $expected_count publishable packages for version $version, found $package_count in $package_dir." >&2
  find "$package_dir" -maxdepth 1 -type f -name "*.$version.nupkg" ! -name "*.snupkg" -print | sort >&2
  exit 1
fi

for package_id in $(semantic_type_model_package_ids); do
  if [ ! -f "$package_dir/$package_id.$version.nupkg" ]; then
    echo "Expected package is missing: $package_dir/$package_id.$version.nupkg" >&2
    exit 1
  fi
done

if find "$package_dir" -maxdepth 1 -type f -name "SemanticTypeModel.JsonEditor.$version.nupkg" | grep . >/dev/null; then
  echo "SemanticTypeModel.JsonEditor is not part of the 1.0 package set." >&2
  exit 1
fi

tmp_root="$(mktemp -d)"
cleanup() {
  rm -rf "$tmp_root"
}
trap cleanup EXIT INT TERM

consumer_dir="$tmp_root/consumer"
mkdir -p "$consumer_dir"

dotnet new console --framework net10.0 --output "$consumer_dir" >/dev/null

cat > "$consumer_dir/NuGet.Config" <<NUGET
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$(pwd)/$package_dir" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
NUGET

dotnet add "$consumer_dir" package Microsoft.EntityFrameworkCore --version "10.0.0" >/dev/null

for package_id in $(semantic_type_model_package_ids); do
  dotnet add "$consumer_dir" package "$package_id" --version "$version" >/dev/null
done

cat > "$consumer_dir/Program.cs" <<'CS'
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Model = SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.JsonSchema.Import;

[SemanticType(Name = "SmokeCustomer")]
public sealed partial class SmokeCustomer
{
    public string Id { get; set; } = string.Empty;
}

internal static class Program
{
    private static void Main()
    {
        JsonSchemaImportResult imported = JsonSchemaImporter.Import("""
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "title": "Customer",
          "type": "object",
          "properties": {
            "id": { "type": "string" }
          }
        }
        """);

        if (imported.Model is null)
        {
            throw new InvalidOperationException("Expected imported model.");
        }

        _ = JsonSchemaExporter.Export(imported.Model);

        Model.TypeSchemaModel canonicalModel = BuildCanonicalModel();
        var modelBuilder = new ModelBuilder(new ConventionSet());
        _ = modelBuilder.ApplySemanticTypeModel(canonicalModel, options => options.ProjectUnannotatedObjectsAsEntities = true);

        _ = typeof(SemanticTypeAttribute);

        Console.WriteLine("Package smoke consumer succeeded.");
    }

    private static Model.TypeSchemaModel BuildCanonicalModel()
    {
        Model.ScalarTypeDefinition scalar = new()
        {
            Id = new Model.TypeId("String"),
            Name = "String",
            Kind = Model.TypeKind.Scalar,
            Nullability = Model.Nullability.NonNullable,
            Annotations = new Model.AnnotationBag(),
            ScalarKind = Model.ScalarKind.String,
        };

        Model.ObjectTypeDefinition customer = new()
        {
            Id = new Model.TypeId("Customer"),
            Name = "Customer",
            Kind = Model.TypeKind.Object,
            Nullability = Model.Nullability.NonNullable,
            Annotations = new Model.AnnotationBag(),
            Semantics = new Model.EntitySemantics { Role = Model.EntityRole.Entity },
            Properties =
            [
                new Model.PropertyDefinition
                {
                    Id = new Model.PropertyId("CustomerId"),
                    Name = "id",
                    Type = new Model.TypeRef(scalar.Id),
                    Cardinality = new Model.Cardinality { IsRequired = true },
                    Mutability = Model.Mutability.Mutable,
                    Constraints = new Model.ConstraintSet(),
                    Annotations = new Model.AnnotationBag(),
                },
            ],
            Keys =
            [
                new Model.KeyDefinition
                {
                    Name = "PK_Customer",
                    Kind = Model.KeyKind.Primary,
                    Properties = [new Model.PropertyRef(new Model.PropertyId("CustomerId"))],
                    Annotations = new Model.AnnotationBag(),
                },
            ],
            Relationships = [],
        };

        System.Collections.Generic.Dictionary<Model.TypeId, Model.TypeDefinition> typesById = new()
        {
            [scalar.Id] = scalar,
            [customer.Id] = customer,
        };

        return new Model.TypeSchemaModel
        {
            Id = new Model.SchemaModelId("CustomerModel"),
            Types = [scalar, customer],
            TypesById = typesById,
            Annotations = new Model.AnnotationBag(),
        };
    }
}
CS

dotnet run --project "$consumer_dir" --configuration Release >/dev/null

dotnet test tests/package-smoke/SemanticTypeModel.PackageSmoke.Tests/SemanticTypeModel.PackageSmoke.Tests.csproj \
  --configuration Release \
  -p:PackageSmokeVersion="$version" \
  -p:PackageSmokeSource="$(pwd)/$package_dir"

echo "Package smoke validation passed for version $version."
