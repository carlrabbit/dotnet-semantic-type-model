#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

dotnet build --configuration Release --no-restore

projects="
SemanticTypeModel.Abstractions
SemanticTypeModel.Core
SemanticTypeModel.JsonSchema
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
SemanticTypeModel.DependencyInjection
SemanticTypeModel.PowerBI
SemanticTypeModel.EFCore
SemanticTypeModel.SystemTextJson
SemanticTypeModel.Configuration
SemanticTypeModel.Configuration.Generators
"

for project in $projects; do
  shipped_file="src/$project/PublicAPI.Shipped.txt"
  unshipped_file="src/$project/PublicAPI.Unshipped.txt"

  if [ ! -f "$shipped_file" ]; then
    echo "Missing public API baseline: $shipped_file" >&2
    exit 1
  fi

  if [ ! -f "$unshipped_file" ]; then
    echo "Missing public API baseline: $unshipped_file" >&2
    exit 1
  fi

  if ! grep -Ev '^[[:space:]]*(#|$)' "$shipped_file" | grep -q .; then
    echo "Public API shipped baseline is empty: $shipped_file" >&2
    exit 1
  fi

  if grep -Ev '^[[:space:]]*(#|$)' "$unshipped_file" | grep -q .; then
    echo "Public API unshipped baseline must be empty until reviewed: $unshipped_file" >&2
    exit 1
  fi
done

echo "Public API baseline validation passed."
