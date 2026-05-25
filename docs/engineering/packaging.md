# Packaging Engineering

## Purpose

Define package validation behavior for local package artifacts and consumer smoke testing.

## Package Smoke Command

```sh
./eng/package-smoke.sh <version>
```

## Behavior

- The command validates local package artifacts in `artifacts/packages/<version>/`.
- When no local package artifacts are present, the command exits successfully with an explicit skip message.
- When packages exist, the command creates a clean consumer project, adds packages from the local package source, and builds the consumer project.

## Constraints

- Package smoke testing must not publish packages.
- Package smoke testing must not require external secrets.
- Package smoke testing must verify consumer restore/build from local package output.

## Document Contract

When packaging validation behavior changes, review and update:
- `eng/package-smoke.sh`
- `docs/engineering/release-readiness.md`
- `docs/workflows/release-check.md`
