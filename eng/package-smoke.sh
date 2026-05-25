#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

if [ "$#" -ne 1 ]; then
  echo "Usage: ./eng/package-smoke.sh <version>" >&2
  exit 1
fi

version="$1"
package_dir="artifacts/packages/$version"

if [ ! -d "$package_dir" ]; then
  echo "No local package directory found at $package_dir. Skipping package smoke test."
  exit 0
fi

package_count="$(find "$package_dir" -maxdepth 1 -type f -name "*.$version.nupkg" ! -name "*.snupkg" | wc -l | tr -d ' ')"
if [ "$package_count" = "0" ]; then
  echo "No local .nupkg files found for version $version in $package_dir. Skipping package smoke test."
  exit 0
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

for package_file in "$package_dir"/*."$version".nupkg; do
  [ -f "$package_file" ] || continue
  case "$package_file" in
    *.snupkg)
      continue
      ;;
  esac

  file_name="$(basename "$package_file")"
  package_id="${file_name%.$version.nupkg}"
  dotnet add "$consumer_dir" package "$package_id" --version "$version" --source "$(pwd)/$package_dir" >/dev/null
done

dotnet build "$consumer_dir" --configuration Release >/dev/null

echo "Package smoke validation passed for version $version."
