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

## Rules

- Humans, agents, and CI must use these commands.
- Do not invent alternative commands.
- CI must call `./eng/check.sh` instead of duplicating logic.
- All commands must succeed before work is considered complete.

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
- .github/workflows/ci.yml
