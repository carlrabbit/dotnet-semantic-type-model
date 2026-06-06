#!/usr/bin/env sh
set -eu

SCRIPT_DIR="$(dirname "$0")"
SAMPLE_PACKAGE_VERSION="${SEMANTIC_TYPE_MODEL_CHECK_AFFECTED_PACKAGE_VERSION:-0.0.0-check-affected}"

if [ "$#" -eq 0 ]; then
  echo "No affected paths were provided; running Tier 2 validation."
  "$SCRIPT_DIR/check.sh"
  exit 0
fi

run_public_docs=false
run_core_tests=false
run_json_schema_tests=false
run_dependency_injection_tests=false
run_ef_core_tests=false
run_generators_tests=false
run_powerbi_tests=false
run_samples=false
matched=false

for affected_path in "$@"; do
  case "$affected_path" in
    README.md|docs/*|public-docs/*)
      run_public_docs=true
      matched=true
      ;;
    tests/unit/SemanticTypeModel.Core.Tests.Unit/*)
      run_core_tests=true
      matched=true
      ;;
    tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/*)
      run_json_schema_tests=true
      matched=true
      ;;
    tests/unit/SemanticTypeModel.DependencyInjection.Tests.Unit/*)
      run_dependency_injection_tests=true
      matched=true
      ;;
    tests/unit/SemanticTypeModel.EFCore.Tests.Unit/*)
      run_ef_core_tests=true
      matched=true
      ;;
    tests/unit/SemanticTypeModel.Generators.Tests.Unit/*)
      run_generators_tests=true
      matched=true
      ;;
    tests/unit/SemanticTypeModel.PowerBI.Tests.Unit/*)
      run_powerbi_tests=true
      matched=true
      ;;
    samples/*)
      run_samples=true
      matched=true
      ;;
  esac
done

if [ "$matched" = false ]; then
  echo "No focused validation mapping matched; running Tier 2 validation."
  "$SCRIPT_DIR/check.sh"
  exit 0
fi

if [ "$run_public_docs" = true ]; then
  "$SCRIPT_DIR/public-docs.sh"
fi

if [ "$run_core_tests" = true ]; then
  "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.Core.Tests.Unit/SemanticTypeModel.Core.Tests.Unit.csproj
fi

if [ "$run_json_schema_tests" = true ]; then
  "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/SemanticTypeModel.JsonSchema.Tests.Unit.csproj
fi

if [ "$run_dependency_injection_tests" = true ]; then
  "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.DependencyInjection.Tests.Unit/SemanticTypeModel.DependencyInjection.Tests.Unit.csproj
fi

if [ "$run_ef_core_tests" = true ]; then
  "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.EFCore.Tests.Unit/SemanticTypeModel.EFCore.Tests.Unit.csproj
fi

if [ "$run_generators_tests" = true ]; then
  "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.Generators.Tests.Unit/SemanticTypeModel.Generators.Tests.Unit.csproj
fi

if [ "$run_powerbi_tests" = true ]; then
  "$SCRIPT_DIR/test-project.sh" tests/unit/SemanticTypeModel.PowerBI.Tests.Unit/SemanticTypeModel.PowerBI.Tests.Unit.csproj
fi

if [ "$run_samples" = true ]; then
  echo "Preparing local packages for affected sample validation using version $SAMPLE_PACKAGE_VERSION."
  "$SCRIPT_DIR/package.sh" "$SAMPLE_PACKAGE_VERSION"
  "$SCRIPT_DIR/samples.sh"
fi
