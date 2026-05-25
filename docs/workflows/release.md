# Release Workflow

## Goal

Define the high-level release workflow and required pre-publish gates.

## Constraints

- Publishing remains explicit and separate from release checks.
- Release execution must reference release-readiness command outputs.

## Required Pre-Publish Gate

- `./eng/release-check.sh <version>` must pass.

## Required Public Documentation Gate

- `README.md` and public documentation surfaces must be current.
- Package README source and release notes must be current.

## Authority

This document is authoritative for:
- release workflow intent;
- release pre-publish gating requirements.

## Document Contract

When this workflow changes, review and update:
- `docs/engineering/release-readiness.md`
- `docs/PUBLIC-DOCS.md`
- `.github/ISSUE_TEMPLATE/release.md`
