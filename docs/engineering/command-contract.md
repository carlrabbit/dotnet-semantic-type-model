# Command Contract

## Purpose

Define the stable set of repository commands used by humans, CI, and agents.

## Canonical Commands

| Command | Purpose |
|---|---|
| `./eng/restore.sh` | Restore all dependencies |
| `./eng/build.sh` | Build the solution |
| `./eng/test.sh` | Run short-running tests |
| `./eng/format.sh` | Format all code |
| `./eng/check.sh` | Full validation (restore + build + test + format check) |
| `./eng/benchmark.sh` | Run benchmarks in Release mode |
| `./eng/samples.sh` | Build and run runnable samples |
| `./eng/public-docs.sh` | Validate required public documentation surfaces |
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
- All required commands for the relevant task must succeed before work is considered complete.
