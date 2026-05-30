#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

if [ "$#" -ne 1 ]; then
  echo "Usage: ./eng/publish.sh <version>" >&2
  exit 1
fi

version="$1"
package_dir="artifacts/nuget"
source_url="https://api.nuget.org/v3/index.json"
api_key="${NUGET_API_KEY:-}"

if [ -z "$api_key" ]; then
  echo "NUGET_API_KEY is required to publish packages." >&2
  exit 1
fi

if [ ! -d "$package_dir" ]; then
  echo "Package directory does not exist: $package_dir" >&2
  exit 1
fi

if find "$package_dir" -maxdepth 1 -type f -name "SemanticTypeModel.JsonEditor.$version.nupkg" | grep . >/dev/null; then
  echo "SemanticTypeModel.JsonEditor is not part of the 1.0 package set and will not be published." >&2
  exit 1
fi

for package_id in $(semantic_type_model_package_ids); do
  package_file="$package_dir/$package_id.$version.nupkg"
  if [ ! -f "$package_file" ]; then
    echo "Expected package is missing: $package_file" >&2
    exit 1
  fi

  dotnet nuget push "$package_file" --api-key "$api_key" --source "$source_url" --skip-duplicate
done

echo "Publish command completed for version $version."
