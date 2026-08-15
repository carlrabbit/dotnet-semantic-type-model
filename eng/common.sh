#!/usr/bin/env sh
set -eu

require_command() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "Required command not found: $1" >&2
    exit 1
  }
}

semantic_type_model_package_ids() {
  dotnet run --project eng/Engineering.Commands/Engineering.Commands.csproj -- package-ids
}

semantic_type_model_package_projects() {
  dotnet run --project eng/Engineering.Commands/Engineering.Commands.csproj -- package-projects
}
