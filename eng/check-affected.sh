#!/usr/bin/env sh
set -eu

SCRIPT_DIR="$(dirname "$0")"

if [ "$#" -eq 0 ]; then
  echo "No affected paths were provided; running Tier 2 validation."
  "$SCRIPT_DIR/check.sh"
  exit 0
fi

case " $* " in
  *" README.md "*|*" docs/"*|*" public-docs/"*)
    "$SCRIPT_DIR/public-docs.sh"
    ;;
  *" tests/unit/SemanticTypeModel.Core.Tests.Unit/"*)
    "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.Core.Tests.Unit/SemanticTypeModel.Core.Tests.Unit.csproj
    ;;
  *" tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/"*)
    "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/SemanticTypeModel.JsonSchema.Tests.Unit.csproj
    ;;
  *" tests/unit/SemanticTypeModel.DependencyInjection.Tests.Unit/"*)
    "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.DependencyInjection.Tests.Unit/SemanticTypeModel.DependencyInjection.Tests.Unit.csproj
    ;;
  *" tests/unit/SemanticTypeModel.EFCore.Tests.Unit/"*)
    "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.EFCore.Tests.Unit/SemanticTypeModel.EFCore.Tests.Unit.csproj
    ;;
  *" tests/unit/SemanticTypeModel.Generators.Tests.Unit/"*)
    "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.Generators.Tests.Unit/SemanticTypeModel.Generators.Tests.Unit.csproj
    ;;
  *" tests/unit/SemanticTypeModel.PowerBI.Tests.Unit/"*)
    "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.PowerBI.Tests.Unit/SemanticTypeModel.PowerBI.Tests.Unit.csproj
    ;;
  *)
    echo "No focused validation mapping matched; running Tier 2 validation."
    "$SCRIPT_DIR/check.sh"
    ;;
esac
