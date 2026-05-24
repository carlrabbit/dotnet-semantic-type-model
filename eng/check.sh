#!/usr/bin/env sh
set -eu

SCRIPT_DIR="$(dirname "$0")"

"$SCRIPT_DIR/restore.sh"
"$SCRIPT_DIR/build.sh"
"$SCRIPT_DIR/test.sh"

dotnet format --verify-no-changes

if [ -f biome.json ]; then
  bun run check
fi
