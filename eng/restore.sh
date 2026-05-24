#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

dotnet restore

if [ -f package.json ]; then
  require_command bun
  bun install --frozen-lockfile
fi
