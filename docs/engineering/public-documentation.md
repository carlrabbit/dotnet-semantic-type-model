# Public Documentation Engineering

## Purpose

Define engineering validation expectations for public consumer documentation surfaces.

## Validation Command

```sh
./eng/public-docs.sh
```

## Required Checks

`./eng/public-docs.sh` validates:

- required consumer-facing files exist;
- package README source file exists;
- public API and compatibility reference docs exist;
- sample documentation pages exist;
- non-root README files are rejected under `public-docs/`.

## Related Authority

- `docs/PUBLIC-DOCS.md`
- `README.md`
- `public-docs/`

## Done Criteria

- Public documentation surfaces required by `docs/PUBLIC-DOCS.md` exist.
- Public documentation validation command passes.
- No `README.md` files are added under `public-docs/`.

## Document Contract

When public documentation validation changes, review and update:
- `docs/PUBLIC-DOCS.md`
- `docs/workflows/public-docs.md`
- `eng/public-docs.sh`
