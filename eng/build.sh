#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

dotnet build --no-restore
