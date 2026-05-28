#!/usr/bin/env sh
set -eu

required_files="
README.md
docs/PUBLIC-DOCS.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/samples.md
public-docs/diagnostics.md
public-docs/versioning.md
public-docs/release-notes.md
public-docs/api/public-api.md
public-docs/api/compatibility.md
public-docs/nuget/SemanticTypeModel.Abstractions.md
public-docs/nuget/SemanticTypeModel.Core.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
public-docs/nuget/SemanticTypeModel.DotNet.md
public-docs/nuget/SemanticTypeModel.Generators.md
public-docs/nuget/SemanticTypeModel.JsonEditor.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/samples/getting-started.md
public-docs/samples/code-first.md
public-docs/samples/dotnet-generator.md
public-docs/samples/json-schema-roundtrip.md
public-docs/samples/runtime-di.md
public-docs/samples/powerbi-projection.md
public-docs/samples/ef-core-projection.md
"

for file in $required_files; do
  if [ ! -f "$file" ]; then
    echo "Missing public documentation file: $file" >&2
    exit 1
  fi

done

if find public-docs -type f -name 'README.md' | grep -q .; then
  echo "Non-root README.md files are not allowed under public-docs/." >&2
  find public-docs -type f -name 'README.md' >&2
  exit 1
fi

echo "Public documentation validation passed."
