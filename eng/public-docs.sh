#!/usr/bin/env sh
set -eu

. "$(dirname "$0")/common.sh"

required_files="
README.md
CONTRIBUTING.md
AGENTS.md
docs/PUBLIC-DOCS.md
public-docs/usage.md
public-docs/configuration.md
public-docs/troubleshooting.md
public-docs/diagnostics.md
public-docs/versioning.md
public-docs/release-notes.md
public-docs/samples.md
public-docs/api/compatibility.md
public-docs/guides/core-semantics.md
public-docs/guides/json-schema.md
public-docs/guides/json-editor-compatibility.md
public-docs/guides/ef-core.md
public-docs/guides/power-bi.md
public-docs/guides/system-text-json.md
public-docs/guides/configuration-options.md
public-docs/guides/projection-capabilities.md
public-docs/nuget/SemanticTypeModel.md
public-docs/diagnostics/stm0xxx.md
public-docs/diagnostics/stm1xxx.md
public-docs/diagnostics/stm3xxx.md
public-docs/diagnostics/stm5xxx.md
"

for file in $required_files; do
  if [ ! -f "$file" ]; then
    echo "Missing public documentation file: $file" >&2
    exit 1
  fi
done

forbidden_paths="
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/api/public-api.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/configuration.md
public-docs/diagnostics/preview-status.md
docs/engineering/building-blocks.md
"

for file in $forbidden_paths; do
  if [ -e "$file" ]; then
    echo "Superseded documentation path must not exist: $file" >&2
    exit 1
  fi
done

if [ -d public-docs/samples ] && find public-docs/samples -type f -name '*.md' -print -quit | grep -q .; then
  echo "Per-sample public Markdown pages are not allowed; use public-docs/samples.md and executable sample source." >&2
  find public-docs/samples -type f -name '*.md' -print >&2
  exit 1
fi

nuget_docs="$(find public-docs/nuget -maxdepth 1 -type f -name '*.md' -print | sort)"
if [ "$nuget_docs" != "public-docs/nuget/SemanticTypeModel.md" ]; then
  echo "Exactly one shared NuGet README source is allowed: public-docs/nuget/SemanticTypeModel.md" >&2
  echo "$nuget_docs" >&2
  exit 1
fi

non_root_readmes="$(find . -path './.git' -prune -o -type f -name 'README.md' ! -path './README.md' -print)"
if [ -n "$non_root_readmes" ]; then
  echo "Non-root README.md files are not allowed." >&2
  echo "$non_root_readmes" >&2
  exit 1
fi

export STM_PACKAGE_IDS="$(semantic_type_model_package_ids)"
export STM_PACKAGE_PROJECTS="$(semantic_type_model_package_projects)"

python3 - <<'PY'
from pathlib import Path
import os
import re
import sys
import xml.etree.ElementTree as ET

errors: list[str] = []
package_ids = [x for x in os.environ['STM_PACKAGE_IDS'].splitlines() if x]
package_projects = [x for x in os.environ['STM_PACKAGE_PROJECTS'].splitlines() if x]

if len(package_ids) != len(package_projects):
    errors.append('eng/common.sh package ID/project inventories have different lengths.')

shared_readme_path = Path('public-docs/nuget/SemanticTypeModel.md')
shared_readme = shared_readme_path.read_text(encoding='utf-8')
public_docs = Path('docs/PUBLIC-DOCS.md').read_text(encoding='utf-8')
root_readme = Path('README.md').read_text(encoding='utf-8')

if 'same exact version' not in shared_readme.lower():
    errors.append('Shared NuGet README must state the same-exact-version rule.')
if 'same exact version' not in root_readme.lower():
    errors.append('README.md must state the same-exact-version rule.')

expected_include = '../../public-docs/nuget/SemanticTypeModel.md'
for package_id, project_path in zip(package_ids, package_projects):
    if f'`{package_id}`' not in shared_readme:
        errors.append(f'Shared NuGet README is missing package ID {package_id}.')
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

    packed_readmes = [
        node.attrib.get('Include')
        for node in root.findall('.//None')
        if node.attrib.get('PackagePath') == 'README.md' and node.attrib.get('Pack', '').lower() == 'true'
    ]
    if packed_readmes != [expected_include]:
        errors.append(
            f'{project_path} must pack only {expected_include} as README.md; found {packed_readmes!r}.'
        )

# Active/evergreen docs: release chronology and migration history may contain old versions/milestones.
active_docs = [Path('README.md')]
for path in Path('public-docs').rglob('*.md'):
    if path == Path('public-docs/release-notes.md'):
        continue
    if path == Path('public-docs/api/compatibility.md'):
        continue
    active_docs.append(path)

# Explicit package versions may be omitted. If present anywhere in evergreen docs, they must agree suite-wide.
patterns = [
    re.compile(r'dotnet add package\s+(SemanticTypeModel\.[\w.]+)\s+--version\s+([0-9]+\.[0-9]+\.[0-9]+(?:[-+][A-Za-z0-9.-]+)?)'),
    re.compile(r'<PackageReference\s+Include="(SemanticTypeModel\.[^"]+)"[^>]*\sVersion="([^"]+)"'),
    re.compile(r'<PackageVersion\s+Include="(SemanticTypeModel\.[^"]+)"[^>]*\sVersion="([^"]+)"'),
]
versions: dict[str, set[str]] = {}
for path in active_docs:
    text = path.read_text(encoding='utf-8')
    for pattern in patterns:
        for package_id, version in pattern.findall(text):
            versions.setdefault(version, set()).add(f'{path}:{package_id}')

if len(versions) > 1:
    details = '; '.join(f'{version} -> {sorted(paths)}' for version, paths in sorted(versions.items()))
    errors.append(f'Evergreen docs contain mixed SemanticTypeModel package versions: {details}')

# Milestone/release-candidate narration is not evergreen consumer documentation.
for path in active_docs:
    text = path.read_text(encoding='utf-8')
    if re.search(r'\bM\d{4}\b', text):
        errors.append(f'Evergreen public doc contains milestone narration: {path}')
    if re.search(r'\brelease[- ]preparation\b|\brelease candidate\b', text, re.IGNORECASE):
        errors.append(f'Evergreen public doc contains release-candidate narration: {path}')

# No stale path references in active docs/governance entry points.
stale_tokens = [
    'public-docs/getting-started.md',
    'public-docs/installation.md',
    'public-docs/concepts.md',
    'public-docs/packages.md',
    'public-docs/api/public-api.md',
    'guides/ef-core-projection.md',
    'guides/power-bi-projection.md',
    'guides/configuration.md',
    'public-docs/samples/',
]
reference_docs = active_docs + [Path('CONTRIBUTING.md'), Path('AGENTS.md'), Path('docs/PUBLIC-DOCS.md')]
for path in reference_docs:
    text = path.read_text(encoding='utf-8')
    for token in stale_tokens:
        if token in text:
            errors.append(f'{path} references superseded documentation path/token: {token}')

# Local Markdown link existence check (anchors and URLs excluded).
link_re = re.compile(r'(?<!!)\[[^\]]*\]\(([^)]+)\)')
link_docs = [Path('README.md'), Path('CONTRIBUTING.md'), Path('AGENTS.md')]
link_docs += [
    path for path in Path('public-docs').rglob('*.md')
    if path not in {Path('public-docs/release-notes.md'), Path('public-docs/api/compatibility.md')}
]
for path in link_docs:
    text = path.read_text(encoding='utf-8')
    for raw in link_re.findall(text):
        target = raw.strip().split()[0].strip('<>')
        if not target or target.startswith(('#', 'http://', 'https://', 'mailto:')):
            continue
        target = target.split('#', 1)[0]
        if not target:
            continue
        resolved = (path.parent / target).resolve()
        try:
            resolved.relative_to(Path.cwd().resolve())
        except ValueError:
            errors.append(f'{path} links outside repository: {raw}')
            continue
        if not resolved.exists():
            errors.append(f'{path} has broken local link: {raw}')

if errors:
    for error in errors:
        print(error, file=sys.stderr)
    sys.exit(1)
PY

echo "Public documentation validation passed."
