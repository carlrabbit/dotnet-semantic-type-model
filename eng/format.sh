#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

dotnet format

if [ -f biome.json ]; then
  require_command bun
  bun run format
fi
