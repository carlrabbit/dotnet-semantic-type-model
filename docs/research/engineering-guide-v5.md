# Engineering Guide V5

## Status

Authoritative engineering guide for the default .NET repository profile.

## Purpose

This guide defines a context-sensitive, AI-agent-friendly engineering setup for professional .NET repositories.

Version 5 replaces the single broad validation gate with validation tiers and separates:

```text
focused implementation validation
standard local validation
PR integration validation
release validation
documentation validation
```

This guide assumes the companion project setup guide defines documentation maturity, task modes, milestones, and authority boundaries.

## Default stack

The default stack remains:

```text
.NET 10
Microsoft Testing Platform
TUnit
BenchmarkDotNet
Bun
Biome
```

Optional modules:

```text
Blazor
Playwright
TypeScript runtime/browser tooling
NuGet packaging
samples
GitHub Copilot
OpenAI Codex
GitHub Pages
public documentation
release readiness
```

---

# 1. Core principles

## 1.1 Canonical commands, not invented commands

Agents and workflows must use repository-provided `eng/` commands.

Do not ask agents to infer command lines from project layout.

## 1.2 Validation must be tiered

Not every task needs the same validation.

A narrow implementation task should not be forced to run release validation, package smoke tests, all samples, all public docs checks, and all long-running tests.

## 1.3 Local validation proves plausibility

Local validation should catch obvious mistakes and validate the affected area.

## 1.4 PR workflows prove integration

GitHub workflows are responsible for clean-checkout integration validation.

## 1.5 Release validation is explicit

Release validation is not part of ordinary implementation.

## 1.6 Documentation synchronization is explicit

Broad documentation synchronization is not part of every implementation task.

---

# 2. Repository layout

Base layout:

```text
/
├─ .config/
│  └─ dotnet-tools.json
├─ .github/
│  ├─ workflows/
│  └─ copilot-instructions.md
├─ artifacts/
│  └─ .gitkeep
├─ docs/
│  ├─ ENGINEERING.md
│  └─ engineering/
│     └─ command-contract.md
├─ eng/
│  ├─ restore.sh
│  ├─ build.sh
│  ├─ test.sh
│  ├─ test-project.sh
│  ├─ test-filter.sh
│  ├─ format.sh
│  ├─ check.sh
│  ├─ check-affected.sh
│  ├─ benchmark.sh
│  └─ common.sh
├─ src/
├─ tests/
├─ .editorconfig
├─ AGENTS.md
├─ Directory.Build.props
├─ Directory.Packages.props
├─ global.json
└─ README.md
```

Optional release/public package layout:

```text
eng/
  package.sh
  publish.sh
  package-smoke.sh
  public-api.sh
  public-docs.sh
  release-check.sh
  samples.sh

tests/
  package-smoke/

public-docs/
packages/
samples/
benchmarks/
site/
tools/
```

No non-root README files are allowed.

---

# 3. Validation tiers

## Tier 0 — Edit sanity

Use for documentation-only changes, formatting-only changes, or trivial local edits.

Typical commands:

```sh
./eng/format.sh --verify
```

or repository equivalent.

Done when:

```text
changed files are formatted
no obvious generated-file mismatch exists
```

## Tier 1 — Focused implementation validation

Use for narrow implementation tasks.

Typical commands:

```sh
./eng/build.sh
./eng/test-project.sh <project-or-test-project>
```

or:

```sh
./eng/test-filter.sh <filter>
```

Done when:

```text
affected project builds
affected tests pass
no broad release validation is required
```

## Tier 2 — Standard local validation

Use before completing non-trivial implementation or when no narrower validation is defined.

Typical command:

```sh
./eng/check.sh
```

Done when:

```text
restore, build, fast tests, and formatting pass
frontend checks pass when configured
```

## Tier 3 — PR integration validation

Implemented by GitHub Actions.

Typical behavior:

```text
clean restore
full build
fast tests
format verification
frontend check when configured
selected integration tests when configured
```

Done when:

```text
PR workflows pass
```

## Tier 4 — Release validation

Use only for release work or explicit release readiness tasks.

Typical command:

```sh
./eng/release-check.sh <version>
```

May include:

```text
release build
package generation
package smoke tests
public API validation
sample validation
public documentation validation
release notes validation
```

---

# 4. Required command contract

## `eng/restore.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet restore

if [ -f package.json ]; then
  bun install --frozen-lockfile
fi
```

## `eng/build.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet build --no-restore
```

## `eng/test.sh`

Runs fast tests only.

```sh
#!/usr/bin/env sh
set -eu

dotnet test --no-build --configuration Debug --filter "TestCategory!=Slow&TestCategory!=E2E&TestCategory!=PackageSmoke"
```

If the selected framework does not use `TestCategory`, implement and document the equivalent filter.

## `eng/test-project.sh`

```sh
#!/usr/bin/env sh
set -eu

PROJECT="${1:?project path is required}"

dotnet test "$PROJECT" --configuration Debug
```

## `eng/test-filter.sh`

```sh
#!/usr/bin/env sh
set -eu

FILTER="${1:?test filter is required}"

dotnet test --configuration Debug --filter "$FILTER"
```

## `eng/format.sh`

```sh
#!/usr/bin/env sh
set -eu

VERIFY=0
if [ "${1:-}" = "--verify" ]; then
  VERIFY=1
fi

if [ "$VERIFY" -eq 1 ]; then
  dotnet format --verify-no-changes
else
  dotnet format
fi

if [ -f biome.json ]; then
  if [ "$VERIFY" -eq 1 ]; then
    bun run check
  else
    bun run format
  fi
fi
```

## `eng/check.sh`

Standard local validation.

```sh
#!/usr/bin/env sh
set -eu

./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
```

If `biome.json` exists, `eng/format.sh --verify` should include Biome check behavior.

## `eng/check-affected.sh`

Focused local validation helper.

This script is repository-specific.

Expected behavior:

```text
detect or accept affected project/test target
run build and focused tests
avoid release-only checks
avoid long-running checks
```

Minimal implementation may print guidance if automatic affected detection is not implemented.

## `eng/benchmark.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet run --configuration Release --project benchmarks/PROJECT_NAME.Benchmarks
```

Benchmarks are never part of Tier 1, Tier 2, or Tier 3 unless explicitly configured.

---

# 5. Optional release command contract

## `eng/package.sh`

```sh
#!/usr/bin/env sh
set -eu

VERSION="${1:?version is required}"

dotnet pack --configuration Release \
  -p:PackageVersion="$VERSION" \
  -p:ContinuousIntegrationBuild=true \
  --output ./artifacts/nuget
```

## `eng/package-smoke.sh`

```sh
#!/usr/bin/env sh
set -eu

VERSION="${1:?version is required}"

dotnet test tests/package-smoke --configuration Release \
  -p:PackageSmokeVersion="$VERSION"
```

## `eng/public-api.sh`

Repository-specific public API validation.

Acceptable strategies:

```text
PublicAPI.Shipped.txt / PublicAPI.Unshipped.txt
API snapshot comparison
generated API diff
package surface validation
```

## `eng/public-docs.sh`

Public documentation validation.

This should check structure first, then deeper consistency where practical.

Examples:

```text
required public-docs files exist
no public-docs README files exist
NuGet README sources exist
diagnostic pages exist for public diagnostics
sample documentation references existing samples
release notes file exists
versioning policy exists
```

## `eng/samples.sh`

Builds or validates runnable samples.

Samples are not part of normal Tier 1 implementation validation unless the task changes samples.

## `eng/release-check.sh`

```sh
#!/usr/bin/env sh
set -eu

VERSION="${1:?version is required}"

./eng/check.sh
dotnet build --configuration Release
./eng/package.sh "$VERSION"
./eng/package-smoke.sh "$VERSION"
./eng/samples.sh
./eng/public-api.sh
./eng/public-docs.sh
```

Release check must not publish.

---

# 6. Test classification

| Category | Default local | PR | Release | Description |
|---|---:|---:|---:|---|
| Unit | Yes | Yes | Yes | Fast isolated tests. |
| Focused integration | Task-specific | Optional | Yes if relevant | Integration for affected area. |
| Broad integration | No | Optional | Yes if relevant | Heavier integration tests. |
| PackageSmoke | No | No or release branch | Yes | Tests packed packages as a consumer. |
| E2E | No | Optional | Optional/Yes | Browser/system tests. |
| Slow | No | No by default | Optional | Expensive tests. |
| Benchmark | Never as test | No | Optional | BenchmarkDotNet only. |

Rules:

- `eng/test.sh` runs fast tests only.
- `eng/test-project.sh` and `eng/test-filter.sh` support focused validation.
- package smoke tests are release validation;
- E2E tests are explicit;
- benchmarks are never tests;
- agents must not add sleeps, broad generated tests, or environment-dependent tests without justification.

---

# 7. Building blocks

## BB00 — Repository Base

Purpose:

```text
Create minimal repository skeleton and canonical command surface.
```

Files:

```text
README.md
AGENTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
eng/restore.sh
eng/build.sh
eng/test.sh
eng/test-project.sh
eng/test-filter.sh
eng/format.sh
eng/check.sh
eng/check-affected.sh
artifacts/.gitkeep
```

Validation:

```sh
./eng/check.sh
```

## BB01 — .NET Solution

Files:

```text
*.slnx
src/<Project>/<Project>.csproj
tests/<Project>.Tests/<Project>.Tests.csproj
```

Rules:

```text
production under src/
tests under tests/
package smoke tests under tests/package-smoke/ when release-ready
```

## BB02 — Shared Build Configuration

Files:

```text
global.json
Directory.Build.props
Directory.Packages.props
.config/dotnet-tools.json
```

Rules:

```text
pin SDK
centralize package versions
treat production warnings as errors where practical
generate XML docs for public API packages
```

## BB03 — EditorConfig and C# Style

File:

```text
.editorconfig
```

Validation:

```sh
./eng/format.sh --verify
```

## BB04 — MTP + TUnit Unit Tests

Rules:

```text
unit tests are fast, deterministic, isolated
no network, database, browser, sleep, or order dependence
```

## BB05 — Test Guardrails

Now implemented through validation tiers and test classification.

Do not create a separate guardrails layer unless repository-specific constraints are extensive.

## BB06 — BenchmarkDotNet

Optional/recommended for performance-sensitive libraries.

Command:

```sh
./eng/benchmark.sh
```

Not part of normal validation.

## BB07 — GitHub Actions CI

Recommended.

Workflows should call `eng/` scripts, not duplicate command logic.

Typical PR workflow:

```yaml
run: ./eng/check.sh
```

Optional workflows may run integration, E2E, package smoke, or release validation by trigger.

## BB08 — Agent Instructions

Files:

```text
AGENTS.md
.github/copilot-instructions.md
```

Rules:

```text
route by task mode
use validation tiers
do not require broad documentation reading for narrow work
do not require research guide reading
```

## BB09 — Bun + Biome

Apply only when TypeScript/JavaScript is present.

Rules:

```text
use Bun, not npm
use Biome, not ESLint/Prettier
commit bun.lock
```

## BB10 — Blazor Module

Apply for Blazor UI or component packages.

## BB11 — Playwright E2E Module

Apply only when real browser validation is required.

Command:

```sh
./eng/e2e.sh
```

Not part of default local validation.

## BB12 — TypeScript Runtime Tools

Apply for self-authored TypeScript runtime utilities, browser assets, graph/layout tooling, JSON/schema tooling, or build helpers.

## BB13 — Documentation Skeleton

Required minimal docs:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

The project setup guide controls specs, milestones, architecture, decisions, and public docs.

## BB14 — NuGet Packaging

Apply when producing NuGet packages.

Files:

```text
eng/package.sh
eng/publish.sh
docs/engineering/packaging.md
```

## BB15 — Samples

Recommended for public packages.

Command:

```sh
./eng/samples.sh
```

Samples are executable documentation but are not default implementation validation unless changed.

## BB16 — GitHub Copilot

`.github/copilot-instructions.md` should point to `AGENTS.md` and avoid duplicating the guide.

## BB17 — OpenAI Codex

`AGENTS.md` is the primary Codex instruction file.

Codex should receive:

```text
task mode
relevant milestone/focus area
relevant specs
validation tier
documentation expectations
```

## BB18 — GitHub Pages Website

Optional publication mechanism.

Website content should be generated from or synchronized with public docs where practical.

## BB19 — Public Documentation

Apply when public documentation is active or release-ready.

Validation:

```sh
./eng/public-docs.sh
```

Not part of ordinary implementation unless public behavior changes.

## BB20 — Release Readiness

Apply when publishing external artifacts.

Validation:

```sh
./eng/release-check.sh <version>
```

Release validation is Tier 4.

---

# 8. AGENTS.md engineering requirements

Recommended engineering section:

```md
## Validation

Use the validation tier specified by the task or milestone.

Tier 0:
- ./eng/format.sh --verify

Tier 1:
- ./eng/build.sh
- ./eng/test-project.sh <project-or-test-project>
- or ./eng/test-filter.sh <filter>

Tier 2:
- ./eng/check.sh

Tier 3:
- GitHub PR workflows

Tier 4:
- ./eng/release-check.sh <version>

Do not run benchmarks, E2E tests, package smoke tests, publish commands, or release-check unless the task requests them.
```

---

# 9. Documentation synchronization and engineering

Engineering commands support documentation synchronization, but implementation tasks do not automatically perform it.

Documentation synchronization tasks may use:

```sh
./eng/public-docs.sh
./eng/samples.sh
./eng/check.sh
```

Release documentation tasks may use:

```sh
./eng/release-check.sh <version>
```

---

# 10. Completion rules

## Focused implementation completion

A focused implementation task is complete when:

```text
code changes are scoped to the focus area
directly affected specs/docs are updated if needed
Tier 1 or specified validation passes
the agent reports what validation ran
deferred documentation impact is noted
```

## Standard implementation completion

A broader implementation task is complete when:

```text
Tier 2 validation passes
direct docs are updated
deferred docs are listed
```

## Documentation synchronization completion

A documentation synchronization task is complete when:

```text
affected docs are updated
indexes and cross-links are normalized
public docs are synchronized if public-facing behavior changed
public-docs validation passes when applicable
```

## Release completion

A release task is complete when:

```text
Tier 4 validation passes
package artifacts are produced
public API is intentional
package smoke tests pass
samples pass where applicable
public docs and release notes are current
nothing is published unless explicitly requested
```

---

# 11. Final V5 model

The engineering model is now:

```text
Tier 0:
  edit sanity

Tier 1:
  focused implementation validation

Tier 2:
  standard local validation

Tier 3:
  PR integration validation

Tier 4:
  release validation
```

`./eng/check.sh` remains valuable, but it is no longer the universal inner-loop command.

`./eng/release-check.sh` remains the release gate and is never part of ordinary implementation unless explicitly requested.

Agents should validate narrowly, report exactly what they ran, and leave broad repository synchronization to the correct task mode.
