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
if [ "$package_count" = "0" ]; then
  echo "No local .nupkg files found for version $version in $package_dir." >&2
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

packages="
SemanticTypeModel.Abstractions
SemanticTypeModel.Core
SemanticTypeModel.JsonSchema
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
SemanticTypeModel.JsonEditor
SemanticTypeModel.PowerBI
SemanticTypeModel.EFCore
"

for package_id in $packages; do
  dotnet add "$consumer_dir" package "$package_id" --version "$version" --source "$(pwd)/$package_dir" >/dev/null
done

cat > "$consumer_dir/Program.cs" <<'CS'
using Microsoft.Extensions.DependencyInjection;
using Legacy = SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Building;
using SemanticTypeModel.DotNet;
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

        _ = typeof(SemanticTypeAttribute);

        Console.WriteLine("Package smoke consumer succeeded.");
    }
}
CS

dotnet run --project "$consumer_dir" --configuration Release >/dev/null

dotnet test tests/package-smoke/SemanticTypeModel.PackageSmoke.Tests/SemanticTypeModel.PackageSmoke.Tests.csproj \
  --configuration Release \
  -p:PackageSmokeVersion="$version" \
  -p:PackageSmokeSource="$(pwd)/$package_dir"

echo "Package smoke validation passed for version $version."
