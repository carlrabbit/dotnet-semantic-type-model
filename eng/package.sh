#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

if [ "$#" -ne 1 ]; then
  echo "Usage: ./eng/package.sh <version>" >&2
  exit 1
fi

version="$1"
output_dir="artifacts/nuget"

rm -rf "$output_dir"
mkdir -p "$output_dir"

projects="
src/SemanticTypeModel.Abstractions/SemanticTypeModel.Abstractions.csproj
src/SemanticTypeModel.Core/SemanticTypeModel.Core.csproj
src/SemanticTypeModel.JsonSchema/SemanticTypeModel.JsonSchema.csproj
src/SemanticTypeModel.DotNet/SemanticTypeModel.DotNet.csproj
src/SemanticTypeModel.Generators/SemanticTypeModel.Generators.csproj
src/SemanticTypeModel.DependencyInjection/SemanticTypeModel.DependencyInjection.csproj
src/SemanticTypeModel.PowerBI/SemanticTypeModel.PowerBI.csproj
src/SemanticTypeModel.EFCore/SemanticTypeModel.EFCore.csproj
src/SemanticTypeModel.SystemTextJson/SemanticTypeModel.SystemTextJson.csproj
"

for project in $projects; do
  dotnet pack "$project" --configuration Release --output "$output_dir" -p:PackageVersion="$version"
done

echo "Packages produced in $output_dir for version $version."
