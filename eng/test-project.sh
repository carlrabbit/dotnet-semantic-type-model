#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

if [ "$#" -ne 1 ]; then
  echo "Usage: ./eng/test-project.sh <test-project.csproj>" >&2
  exit 2
fi

project="$1"

if [ ! -f "$project" ]; then
  echo "Test project not found: $project" >&2
  exit 1
fi

dotnet test "$project" --configuration Debug --treenode-filter "/**[(TestCategory!=Slow)&(TestCategory!=E2E)]"
