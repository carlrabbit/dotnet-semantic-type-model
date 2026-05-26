# TBP - Public Documentation Update

## Purpose

Provide repeatable steps for keeping consumer-facing public documentation synchronized with shipped behavior.

## Required Reading

- `docs/PUBLIC-DOCS.md`
- `docs/TERMINOLOGY.md`
- `docs/engineering/public-documentation.md`
- `public-docs/`

## Procedure

1. Identify the consumer-visible change.
2. Update relevant `public-docs/` pages.
3. Update `README.md` summary sections for install/quick start/packages/samples as needed.
4. Update package README source (`public-docs/nuget/*.md`) when package usage changes.
5. Update diagnostics and compatibility pages when applicable.
6. Run `./eng/public-docs.sh`.

## Validation

- `./eng/public-docs.sh` passes.
- Links and file mappings in `docs/PUBLIC-DOCS.md` remain accurate.
