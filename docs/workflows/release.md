# Release Workflow

## Goal

Define the manual prerelease publish workflow.

## Constraints

- Publishing is manual-only (`workflow_dispatch`).
- Publishing must never run on pull requests or normal pushes.
- Publish must run release validation first.
- Publishing requires NuGet API key secret.

## Required Gate

- `./eng/release-check.sh <version>` must pass before `./eng/publish.sh <version>`.
