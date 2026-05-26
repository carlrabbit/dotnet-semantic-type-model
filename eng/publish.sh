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

found=0
for package_file in "$package_dir"/*."$version".nupkg; do
  [ -f "$package_file" ] || continue
  case "$package_file" in
    *.snupkg)
      continue
      ;;
  esac

  found=1
  dotnet nuget push "$package_file" --api-key "$api_key" --source "$source_url" --skip-duplicate
done

if [ "$found" -eq 0 ]; then
  echo "No publishable .nupkg files found for version $version in $package_dir." >&2
  exit 1
fi

echo "Publish command completed for version $version."
