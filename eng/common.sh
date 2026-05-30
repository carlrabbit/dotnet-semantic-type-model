#!/usr/bin/env sh
set -eu

require_command() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "Required command not found: $1" >&2
    exit 1
  }
}


semantic_type_model_package_ids() {
  cat <<'PACKAGES'
SemanticTypeModel.Abstractions
SemanticTypeModel.Core
SemanticTypeModel.JsonSchema
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
SemanticTypeModel.DependencyInjection
SemanticTypeModel.PowerBI
SemanticTypeModel.EFCore
SemanticTypeModel.SystemTextJson
PACKAGES
}

semantic_type_model_package_projects() {
  cat <<'PROJECTS'
src/SemanticTypeModel.Abstractions/SemanticTypeModel.Abstractions.csproj
src/SemanticTypeModel.Core/SemanticTypeModel.Core.csproj
src/SemanticTypeModel.JsonSchema/SemanticTypeModel.JsonSchema.csproj
src/SemanticTypeModel.DotNet/SemanticTypeModel.DotNet.csproj
src/SemanticTypeModel.Generators/SemanticTypeModel.Generators.csproj
src/SemanticTypeModel.DependencyInjection/SemanticTypeModel.DependencyInjection.csproj
src/SemanticTypeModel.PowerBI/SemanticTypeModel.PowerBI.csproj
src/SemanticTypeModel.EFCore/SemanticTypeModel.EFCore.csproj
src/SemanticTypeModel.SystemTextJson/SemanticTypeModel.SystemTextJson.csproj
PROJECTS
}
