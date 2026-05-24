#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

dotnet test --no-build --configuration Debug --treenode-filter "/**[(TestCategory!=Slow)&(TestCategory!=E2E)]"
