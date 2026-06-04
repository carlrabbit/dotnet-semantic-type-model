#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

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
public-docs/guides/json-schema.md
public-docs/guides/json-editor-compatibility.md
public-docs/api/compatibility.md
public-docs/nuget/SemanticTypeModel.Abstractions.md
public-docs/nuget/SemanticTypeModel.Core.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
public-docs/nuget/SemanticTypeModel.DotNet.md
public-docs/nuget/SemanticTypeModel.Generators.md
public-docs/nuget/SemanticTypeModel.SystemTextJson.md
public-docs/nuget/SemanticTypeModel.DependencyInjection.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/samples/getting-started.md
public-docs/samples/json-schema-roundtrip.md
public-docs/samples/code-first-json-schema.md
public-docs/samples/code-first-ef-core.md
public-docs/samples/code-first-powerbi.md
public-docs/samples/system-text-json-resolver.md
public-docs/samples/runtime-di.md
"

for file in $required_files; do
  if [ ! -f "$file" ]; then
    echo "Missing public documentation file: $file" >&2
    exit 1
  fi

done

non_root_readmes="$(find . -path './.git' -prune -o -type f -name 'README.md' ! -path './README.md' -print)"
if [ -n "$non_root_readmes" ]; then
  echo "Non-root README.md files are not allowed." >&2
  echo "$non_root_readmes" >&2
  exit 1
fi

python3 - <<'PY'
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

errors: list[str] = []

package_ids = [line.strip() for line in """
SemanticTypeModel.Abstractions
SemanticTypeModel.Core
SemanticTypeModel.JsonSchema
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
SemanticTypeModel.DependencyInjection
SemanticTypeModel.PowerBI
SemanticTypeModel.EFCore
SemanticTypeModel.SystemTextJson
""".splitlines() if line.strip()]

package_projects = [line.strip() for line in """
src/SemanticTypeModel.Abstractions/SemanticTypeModel.Abstractions.csproj
src/SemanticTypeModel.Core/SemanticTypeModel.Core.csproj
src/SemanticTypeModel.JsonSchema/SemanticTypeModel.JsonSchema.csproj
src/SemanticTypeModel.DotNet/SemanticTypeModel.DotNet.csproj
src/SemanticTypeModel.Generators/SemanticTypeModel.Generators.csproj
src/SemanticTypeModel.DependencyInjection/SemanticTypeModel.DependencyInjection.csproj
src/SemanticTypeModel.PowerBI/SemanticTypeModel.PowerBI.csproj
src/SemanticTypeModel.EFCore/SemanticTypeModel.EFCore.csproj
src/SemanticTypeModel.SystemTextJson/SemanticTypeModel.SystemTextJson.csproj
""".splitlines() if line.strip()]

public_docs = Path('docs/PUBLIC-DOCS.md').read_text(encoding='utf-8')
readme = Path('README.md').read_text(encoding='utf-8')
packages_doc = Path('public-docs/packages.md').read_text(encoding='utf-8')
release_notes = Path('public-docs/release-notes.md').read_text(encoding='utf-8')

for package_id, project_path in zip(package_ids, package_projects):
    nuget_doc = f'public-docs/nuget/{package_id}.md'
    if f'`{package_id}` -> `{nuget_doc}`' not in public_docs:
        errors.append(f'docs/PUBLIC-DOCS.md is missing README mapping for {package_id}.')
    if f'`{package_id}`' not in readme:
        errors.append(f'README.md package list is missing {package_id}.')
    if f'`{package_id}`' not in packages_doc:
        errors.append(f'public-docs/packages.md package list is missing {package_id}.')

    project = Path(project_path)
    if not project.exists():
        errors.append(f'Package project is missing: {project_path}.')
        continue

    root = ET.parse(project).getroot()
    package_id_values = [node.text for node in root.findall('.//PackageId')]
    if package_id_values != [package_id]:
        errors.append(f'{project_path} PackageId must be exactly {package_id}.')

    readme_values = [node.text for node in root.findall('.//PackageReadmeFile')]
    if readme_values != ['README.md']:
        errors.append(f'{project_path} must set PackageReadmeFile to README.md.')

    expected_include = f'../../{nuget_doc}'
    includes = [node.attrib.get('Include') for node in root.findall('.//None')]
    if expected_include not in includes:
        errors.append(f'{project_path} must pack {expected_include} as the NuGet README source.')

version_pattern = re.compile(r'dotnet add package\s+SemanticTypeModel\.[\w.]+\s+--version\s+([0-9]+\.[0-9]+\.[0-9]+(?:[-+][A-Za-z0-9.-]+)?)')
version_sources: dict[str, set[str]] = {}
for path in [Path('README.md'), *Path('public-docs').rglob('*.md')]:
    text = path.read_text(encoding='utf-8')
    for match in version_pattern.finditer(text):
        version_sources.setdefault(match.group(1), set()).add(str(path))

if not version_sources:
    errors.append('No documented dotnet add package --version commands were found.')
else:
    version = next(iter(version_sources))
    if f'## {version}' not in release_notes:
        errors.append(f'public-docs/release-notes.md must contain a release heading for {version}.')

active_public_docs = [path for path in Path('public-docs').rglob('*.md') if path.name != 'release-notes.md']
if '0.1.0-alpha' in readme or '0.1.0-alpha' in ''.join(path.read_text(encoding='utf-8') for path in active_public_docs):
    errors.append('Active public docs contain stale 0.1.0-alpha prerelease guidance.')

if errors:
    for error in errors:
        print(error, file=sys.stderr)
    sys.exit(1)
PY

echo "Public documentation validation passed."
