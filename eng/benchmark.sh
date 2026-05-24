#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

dotnet run --configuration Release --project benchmarks/SemanticTypeModel.Benchmarks
