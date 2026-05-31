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
- package README source files exist;
- package IDs in project files match `docs/PUBLIC-DOCS.md`;
- NuGet package README source mappings match project files;
- package installation versions are consistent across `README.md` and `public-docs/`;
- package lists do not drift between the README, public docs, and project files;
- public API and compatibility reference docs exist;
- sample documentation pages exist;
- non-root README files are rejected.

## Related Authority

- `docs/PUBLIC-DOCS.md`
- `README.md`
- `public-docs/`

## Done Criteria

- Public documentation surfaces required by `docs/PUBLIC-DOCS.md` exist.
- Public documentation validation command passes.
- No `README.md` files are added outside the repository root.

## Document Contract

When public documentation validation changes, review and update:
- `docs/PUBLIC-DOCS.md`
- `docs/workflows/public-docs.md`
- `eng/public-docs.sh`
