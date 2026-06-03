#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

require_command dotnet

projects="
samples/code-first-authoring/code-first-authoring.csproj
samples/json-schema-roundtrip/json-schema-roundtrip.csproj
samples/dotnet-generator-to-json-schema/dotnet-generator-to-json-schema.csproj
samples/runtime-di-usage/runtime-di-usage.csproj
samples/powerbi-projection/powerbi-projection.csproj
samples/ef-core-projection/ef-core-projection.csproj
samples/system-text-json-basic/system-text-json-basic.csproj
"

for project in $projects; do
  dotnet run --project "$project" --configuration Debug

done
