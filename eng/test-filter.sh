#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet
require_command rg

if [ "$#" -ne 1 ]; then
  echo "Usage: ./eng/test-filter.sh <search-term|mtp-treenode-filter>" >&2
  exit 2
fi

filter="$1"

case "$filter" in
  /*)
    dotnet test --no-build --configuration Debug --treenode-filter "$filter"
    ;;
  *)
    matched=false
    for project in tests/unit/*/*.csproj; do
      project_directory="$(dirname "$project")"
      if printf '%s\n' "$project" | rg --fixed-strings --quiet "$filter" \
        || rg --glob '*.cs' --fixed-strings --quiet "$filter" "$project_directory"; then
        matched=true
        "$(dirname "$0")/test-project.sh" "$project"
      fi
    done

    if [ "$matched" = false ]; then
      echo "No unit test project or C# test source matched focused test term '$filter'." >&2
      exit 1
    fi
    ;;
esac
