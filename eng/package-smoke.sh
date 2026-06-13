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
using Legacy = SemanticTypeModel.Abstractions.Model;
using Canonical = SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Core.Building;
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

        var legacyBuilder = new TypeSchemaModelBuilder()
            .AddShape("Root", new Legacy.ScalarShape { Kind = Legacy.ScalarKind.String })
            .SetRoot("Root");
        _ = legacyBuilder.Build();
        Canonical.TypeSchemaModel canonicalModel = BuildCanonicalModel();
        var modelBuilder = new ModelBuilder(new ConventionSet());
        _ = modelBuilder.ApplySemanticTypeModel(canonicalModel, options => options.ProjectUnannotatedObjectsAsEntities = true);

        _ = typeof(SemanticTypeAttribute);

        Console.WriteLine("Package smoke consumer succeeded.");
    }

    private static Canonical.TypeSchemaModel BuildCanonicalModel()
    {
        Canonical.ScalarTypeDefinition scalar = new()
        {
            Id = new Canonical.TypeId("String"),
            Name = "String",
            Kind = Canonical.TypeKind.Scalar,
            Nullability = Canonical.Nullability.NonNullable,
            Annotations = new Canonical.AnnotationBag(),
            ScalarKind = Canonical.ScalarKind.String,
        };

        Canonical.ObjectTypeDefinition customer = new()
        {
            Id = new Canonical.TypeId("Customer"),
            Name = "Customer",
            Kind = Canonical.TypeKind.Object,
            Nullability = Canonical.Nullability.NonNullable,
            Annotations = new Canonical.AnnotationBag(),
            Semantics = new Canonical.EntitySemantics { Role = Canonical.EntityRole.Entity },
            Properties =
            [
                new Canonical.PropertyDefinition
                {
                    Id = new Canonical.PropertyId("CustomerId"),
                    Name = "id",
                    Type = new Canonical.TypeRef(scalar.Id),
                    Cardinality = new Canonical.Cardinality { IsRequired = true },
                    Mutability = Canonical.Mutability.Mutable,
                    Constraints = new Canonical.ConstraintSet(),
                    Annotations = new Canonical.AnnotationBag(),
                },
            ],
            Keys =
            [
                new Canonical.KeyDefinition
                {
                    Name = "PK_Customer",
                    Kind = Canonical.KeyKind.Primary,
                    Properties = [new Canonical.PropertyRef(new Canonical.PropertyId("CustomerId"))],
                    Annotations = new Canonical.AnnotationBag(),
                },
            ],
            Relationships = [],
        };

        System.Collections.Generic.Dictionary<Canonical.TypeId, Canonical.TypeDefinition> typesById = new()
        {
            [scalar.Id] = scalar,
            [customer.Id] = customer,
        };

        return new Canonical.TypeSchemaModel
        {
            Id = new Canonical.SchemaModelId("CustomerModel"),
            Types = [scalar, customer],
            TypesById = typesById,
            Annotations = new Canonical.AnnotationBag(),
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
