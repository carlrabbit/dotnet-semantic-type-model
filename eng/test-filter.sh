#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

if [ "$#" -ne 1 ]; then
  echo "Usage: ./eng/test-filter.sh <mtp-treenode-filter>" >&2
  exit 2
fi

dotnet test --no-build --configuration Debug --treenode-filter "$1"
