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

for project in $(semantic_type_model_package_projects); do
  dotnet pack "$project" --configuration Release --output "$output_dir" -p:PackageVersion="$version"
done

echo "Packages produced in $output_dir for version $version."
