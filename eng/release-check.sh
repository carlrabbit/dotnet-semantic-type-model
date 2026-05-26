#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

if [ "$#" -ne 1 ]; then
  echo "Usage: ./eng/release-check.sh <version>" >&2
  exit 1
fi

version="$1"
script_dir="$(dirname "$0")"

"$script_dir/check.sh"
dotnet build --configuration Release
"$script_dir/package.sh" "$version"
"$script_dir/package-smoke.sh" "$version"

if [ -x "$script_dir/samples.sh" ]; then
  "$script_dir/samples.sh"
fi

"$script_dir/public-api.sh"
"$script_dir/public-docs.sh"

echo "Release check passed for version $version."
