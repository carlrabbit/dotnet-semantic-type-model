# Release Check Workflow

## Goal

Run release-readiness validation without publishing artifacts.

## Constraints

- Must not publish.
- Must run canonical engineering commands.
- Must include public docs and public API validation.

## Inputs

- release version string
- repository source tree

## Outputs

- pass/fail release-readiness result.

## Validation Steps

1. `./eng/check.sh`
2. `dotnet build --configuration Release`
3. `./eng/package.sh <version>` when available and packages are produced
4. `./eng/package-smoke.sh <version>` when local packages are available
5. `./eng/samples.sh` when present
6. `./eng/public-api.sh`
7. `./eng/public-docs.sh`

## Authority

This document is authoritative for:
- release-check workflow intent and sequence.

## Document Contract

When this workflow changes, review and update:
- `docs/engineering/release-readiness.md`
- `eng/release-check.sh`
- `.github/ISSUE_TEMPLATE/release.md`
