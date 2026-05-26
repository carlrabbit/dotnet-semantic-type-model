# Packaging Engineering

## Purpose

Define package production, smoke validation, and publish behavior.

## Package Commands

```sh
./eng/package.sh <version>
./eng/package-smoke.sh <version>
./eng/publish.sh <version>
```

## Behavior

- `./eng/package.sh <version>` packs Release packages into `artifacts/nuget`.
- `./eng/package-smoke.sh <version>` validates package consumption from `artifacts/nuget` using a clean consumer and `tests/package-smoke/`.
- `./eng/publish.sh <version>` publishes only existing package artifacts from `artifacts/nuget`.

## Constraints

- Publishing is manual-only and never part of normal CI.
- Publishing requires `NUGET_API_KEY`.
- `dotnet nuget push` must use duplicate-safe publishing (`--skip-duplicate`).
- Every published package must define package metadata and a package README source mapping.
