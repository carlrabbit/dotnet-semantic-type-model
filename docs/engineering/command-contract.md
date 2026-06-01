# Command Contract

## Purpose

Define the stable set of repository commands used by humans, CI, and agents.

## Validation Tiers

| Tier | Command scope | Commands |
|---|---|---|
| Tier 0 | Static/documentation checks | `./eng/public-docs.sh` for public docs, format verification for touched code, shell syntax checks for scripts |
| Tier 1 | Focused affected-area validation | `./eng/test-project.sh <project>`, `./eng/test-filter.sh <filter>`, `./eng/check-affected.sh [paths...]` |
| Tier 2 | Full repository implementation check | `./eng/check.sh` |
| Tier 3 | Release candidate/package validation | `./eng/package.sh <version>`, `./eng/package-smoke.sh <version>`, `./eng/public-api.sh`, `./eng/public-docs.sh`, `./eng/samples.sh`, `./eng/release-check.sh <version>` |
| Tier 4 | Publish validation | `./eng/release-check.sh <version>`, then `./eng/publish.sh <version>` or the publish workflow |

`./eng/check.sh` is Tier 2. Use it before completing implementation work, but prefer Tier 1 commands for fast inner-loop validation when the affected area is known.

## Canonical Commands

| Command | Purpose |
|---|---|
| `./eng/restore.sh` | Restore all dependencies |
| `./eng/build.sh` | Build the solution |
| `./eng/test.sh` | Run all short-running tests |
| `./eng/test-project.sh <project>` | Run short-running tests for one test project |
| `./eng/test-filter.sh <filter>` | Run short-running tests matching an MTP tree-node filter |
| `./eng/check-affected.sh [paths...]` | Run focused validation guidance for changed paths, or Tier 2 when no focused mapping is available |
| `./eng/format.sh` | Format all code |
| `./eng/check.sh` | Tier 2 validation (restore + build + short-running tests + format check) |
| `./eng/benchmark.sh` | Run benchmarks in Release mode |
| `./eng/samples.sh` | Build and run runnable samples |
| `./eng/public-docs.sh` | Validate public documentation surfaces and package documentation consistency |
| `./eng/public-api.sh` | Validate public API baseline files |
| `./eng/package.sh <version>` | Pack release NuGet packages into `artifacts/nuget` |
| `./eng/package-smoke.sh <version>` | Validate local package consumption from `artifacts/nuget` |
| `./eng/release-check.sh <version>` | Run release-readiness gate without publishing |
| `./eng/publish.sh <version>` | Publish local `artifacts/nuget` packages to NuGet.org |

## Rules

- Humans, agents, and CI must use these commands.
- Do not invent alternative commands.
- CI must call `./eng/check.sh` instead of duplicating logic.
- `./eng/release-check.sh <version>` must not publish artifacts.
- `./eng/publish.sh <version>` requires `NUGET_API_KEY`.
- All required commands for the relevant validation tier must succeed before work is considered complete.
