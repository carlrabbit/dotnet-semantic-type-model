# CI Workflow

## Goal

Validate every pull request and push to main by running the full canonical check.

## Constraints

- Must use `./eng/check.sh` instead of duplicating logic.
- Must support repositories with and without Bun tooling.
- Must not run benchmarks.
- Must not run long-running tests.

## Triggers

- Pull requests.
- Pushes to `main`.

## Validation

The workflow passes when `./eng/check.sh` exits with code 0.

## Authority

This document is authoritative for:
- CI workflow intent;
- CI workflow constraints.

## Document Contract

When CI behavior changes, review and update:
- .github/workflows/ci.yml
- docs/WORKFLOWS.md
- docs/ENGINEERING.md
