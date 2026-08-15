# Command Contract

## Purpose

Define the stable set of repository commands used by humans, CI, and agents.

## Validation Tiers

| Tier | Command scope | Commands |
|---|---|---|
| Tier 0 | Static/documentation checks | `./eng/public-docs.sh` for public docs, format verification for touched code, shell syntax checks for scripts |
| Tier 1 | Focused affected-area validation | `./eng/test-project.sh <project>`, `./eng/test-filter.sh <filter>`, `./eng/check-affected.sh [paths...]` |
| Tier 2 | Full repository implementation check | `./eng/check.sh` |
| Tier 3 | Release candidate/package validation | `./eng/package.sh <version>`, `./eng/package-smoke.sh <version>`, `./eng/public-docs.sh`, `./eng/samples.sh`, `./eng/release-check.sh <version>` |
| Tier 4 | Publish validation | `./eng/release-check.sh <version>`, then `./eng/publish.sh <version>` or the publish workflow |

`./eng/check.sh` is Tier 2. Use it before completing implementation work, but prefer Tier 1 commands for fast inner-loop validation when the affected area is known.

## Canonical Commands

| Command | Purpose |
|---|---|
| `./eng/restore.sh` | Restore all dependencies |
| `./eng/build.sh` | Build the solution |
| `./eng/test.sh` | Run all short-running tests |
| `./eng/test-project.sh <project>` | Run short-running tests for one test project |
| `./eng/test-filter.sh <filter>` | Run short-running tests in unit-test projects whose path or C# source contains a bare focused term; arguments beginning with `/` are passed through as MTP tree-node filters |
| `./eng/check-affected.sh [paths...]` | Run focused validation guidance for changed paths, or Tier 2 when no focused mapping is available; sample paths prepare local packages before running sample validation |
| `./eng/format.sh` | Format all code |
| `./eng/check.sh` | Tier 2 validation (restore + build + short-running tests + format check) |
| `./eng/benchmark.sh` | Run benchmarks in Release mode |
| `./eng/samples.sh` | Build and run runnable samples against locally prepared packages in `artifacts/nuget`; run `./eng/package.sh <version>` first |
| `./eng/public-docs.sh` | Validate public documentation surfaces and package documentation consistency |
| `./eng/package.sh <version>` | Pack release NuGet packages into `artifacts/nuget` |
| `./eng/package-smoke.sh <version>` | Validate local package consumption from `artifacts/nuget` |
| `./eng/release-check.sh <version>` | Run release-readiness gate without publishing |
| `./eng/publish.sh <version>` | Publish local `artifacts/nuget` packages to NuGet.org |

## Rules

- Humans, agents, and CI must use these commands.
- Bare focused terms are repository source/project selectors rather than MTP expressions; use a leading `/` when supplying a complete MTP tree-node filter.
- Do not invent alternative commands.
- CI must call `./eng/check.sh` instead of duplicating logic.
- `./eng/release-check.sh <version>` must not publish artifacts.
- `./eng/publish.sh <version>` requires `NUGET_API_KEY`.
- All required commands for the relevant validation tier must succeed before work is considered complete.

## Implementation Levels

The `eng/` filenames remain the stable command API. Direct `dotnet` launchers (`restore`, `build`, `test`, `test-project`, `format`, and `benchmark`) are Level 1. Small sequencing commands (`check`, `package`, `samples`, `release-check`, and `publish`) are Level 2. Repository policy commands (`check-affected` and `public-docs`) are Level 3: their shell files are thin launchers into the tested `eng/Engineering.Commands` host. `package-smoke` is also Level 3: its thin launcher delegates package policy, temporary consumer construction, and scenario orchestration to the tested command host.

Complex path classification, package inventory, and documentation policy belong in tested .NET engineering code. Shell remains responsible for direct process invocation and short, readable sequencing. Launchers forward arguments and return the host or subprocess exit status unchanged.
