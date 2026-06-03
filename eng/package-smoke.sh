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
using Hardening = SemanticTypeModel.Abstractions.Hardening;
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
        Hardening.TypeSchemaModel hardeningModel = BuildHardeningModel();
        var modelBuilder = new ModelBuilder(new ConventionSet());
        _ = modelBuilder.ApplySemanticTypeModel(hardeningModel, options => options.ProjectUnannotatedObjectsAsEntities = true);

        _ = typeof(SemanticTypeAttribute);

        Console.WriteLine("Package smoke consumer succeeded.");
    }

    private static Hardening.TypeSchemaModel BuildHardeningModel()
    {
        Hardening.ScalarTypeDefinition scalar = new()
        {
            Id = new Hardening.TypeId("String"),
            Name = "String",
            Kind = Hardening.TypeKind.Scalar,
            Nullability = Hardening.Nullability.NonNullable,
            Annotations = new Hardening.AnnotationBag(),
            ScalarKind = Hardening.ScalarKind.String,
        };

        Hardening.ObjectTypeDefinition customer = new()
        {
            Id = new Hardening.TypeId("Customer"),
            Name = "Customer",
            Kind = Hardening.TypeKind.Object,
            Nullability = Hardening.Nullability.NonNullable,
            Annotations = new Hardening.AnnotationBag(),
            Semantics = new Hardening.EntitySemantics { Role = Hardening.EntityRole.Entity },
            Properties =
            [
                new Hardening.PropertyDefinition
                {
                    Id = new Hardening.PropertyId("CustomerId"),
                    Name = "id",
                    Type = new Hardening.TypeRef(scalar.Id),
                    Cardinality = new Hardening.Cardinality { IsRequired = true },
                    Mutability = Hardening.Mutability.Mutable,
                    Constraints = new Hardening.ConstraintSet(),
                    Annotations = new Hardening.AnnotationBag(),
                },
            ],
            Keys =
            [
                new Hardening.KeyDefinition
                {
                    Name = "PK_Customer",
                    Kind = Hardening.KeyKind.Primary,
                    Properties = [new Hardening.PropertyRef(new Hardening.PropertyId("CustomerId"))],
                    Annotations = new Hardening.AnnotationBag(),
                },
            ],
            Relationships = [],
        };

        System.Collections.Generic.Dictionary<Hardening.TypeId, Hardening.TypeDefinition> typesById = new()
        {
            [scalar.Id] = scalar,
            [customer.Id] = customer,
        };

        return new Hardening.TypeSchemaModel
        {
            Id = new Hardening.SchemaModelId("CustomerModel"),
            Types = [scalar, customer],
            TypesById = typesById,
            Annotations = new Hardening.AnnotationBag(),
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
