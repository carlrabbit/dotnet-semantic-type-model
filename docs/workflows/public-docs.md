# Public Documentation Workflow

## Goal

Validate public documentation before release or public-facing changes.

## Constraints

- Must not require secrets.
- Must not publish.
- Must validate required public documentation files.
- Must reject non-root README files under `public-docs/`.

## Inputs

- `README.md`
- `docs/PUBLIC-DOCS.md`
- `public-docs/`

## Outputs

- public documentation validation result.

## Authority

This document is authoritative for:
- public documentation validation workflow intent.

## Document Contract

When this workflow changes, review and update:
- `docs/PUBLIC-DOCS.md`
- `docs/ENGINEERING.md`
- `docs/engineering/public-documentation.md`
- `eng/public-docs.sh`
