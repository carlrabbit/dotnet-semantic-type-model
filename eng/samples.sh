#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

package_dir="artifacts/nuget"

if [ ! -d "$package_dir" ]; then
  echo "Sample validation requires local SemanticTypeModel packages in $package_dir." >&2
  echo "Run ./eng/package.sh <version> before ./eng/samples.sh." >&2
  exit 1
fi

versions=""
for package_id in $(semantic_type_model_package_ids); do
  matches="$(find "$package_dir" -maxdepth 1 -type f -name "$package_id.*.nupkg" ! -name "*.snupkg" -exec basename {} .nupkg \; | awk -v prefix="$package_id." 'index($0, prefix) == 1 { print substr($0, length(prefix) + 1) }' | sort -u)"
  if [ -z "$matches" ]; then
    echo "Missing local package for sample validation: $package_id" >&2
    exit 1
  fi
  versions="$(printf '%s\n%s\n' "$versions" "$matches")"
done

version="$(printf '%s\n' "$versions" | sed '/^$/d' | sort | uniq -c | awk -v expected="$(semantic_type_model_package_ids | wc -l | tr -d ' ')" '$1 == expected { print $2 }' | tail -n 1)"
if [ -z "$version" ]; then
  echo "Could not find one SemanticTypeModel package version shared by all packages in $package_dir." >&2
  exit 1
fi

projects="
samples/json-schema-roundtrip/json-schema-roundtrip.csproj
samples/code-first-json-schema/code-first-json-schema.csproj
samples/code-first-ef-core/code-first-ef-core.csproj
samples/code-first-powerbi/code-first-powerbi.csproj
samples/system-text-json-resolver/system-text-json-resolver.csproj
samples/runtime-di/runtime-di.csproj
samples/configuration-options/configuration-options.csproj
"

for project in $projects; do
  dotnet restore "$project" --no-cache --force-evaluate -p:SemanticTypeModelSamplePackageVersion="$version" >/dev/null
  dotnet run --no-restore --project "$project" --configuration Debug -p:SemanticTypeModelSamplePackageVersion="$version"
done

printf 'Package-based sample validation passed for SemanticTypeModel package version %s.\n' "$version"
