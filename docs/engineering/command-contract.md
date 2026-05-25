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
| `./eng/public-api.sh` | Validate public API documentation baseline artifacts |
| `./eng/package-smoke.sh <version>` | Validate locally packed packages from a clean consumer project |
| `./eng/release-check.sh <version>` | Run release-readiness gate without publishing |

## Rules

- Humans, agents, and CI must use these commands.
- Do not invent alternative commands.
- CI must call `./eng/check.sh` instead of duplicating logic.
- `./eng/release-check.sh <version>` must not publish artifacts.
- All required commands for the relevant task must succeed before work is considered complete.

## Authority

This document is authoritative for:
- canonical command names;
- command semantics;
- command ordering.

## Document Contract

When commands change, review and update:
- docs/ENGINEERING.md
- AGENTS.md
- .github/copilot-instructions.md
- docs/engineering/public-documentation.md
- docs/engineering/release-readiness.md
- docs/engineering/packaging.md
- docs/workflows/ci.md
