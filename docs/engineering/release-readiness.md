# Release Readiness Engineering

## Purpose

Define the pre-release validation gate that must pass before publishing actions are considered.

## Release Gate Command

```sh
./eng/release-check.sh <version>
```

## Required Order

`./eng/release-check.sh <version>` runs this gate sequence without publishing:

1. `./eng/check.sh`
2. `dotnet build --configuration Release`
3. `./eng/package.sh <version>` when available and packages are produced
4. `./eng/package-smoke.sh <version>` when local packages exist
5. `./eng/samples.sh` when the samples command exists
6. `./eng/public-api.sh`
7. `./eng/public-docs.sh`

## Release Documentation Checks

Before release, ensure:

- user-first `README.md` is current;
- package README source is current;
- getting started and installation docs are current;
- public API and compatibility docs are current;
- diagnostics documentation is current;
- sample docs are current;
- versioning policy and release notes are current.

## Document Contract

When release-readiness policy changes, review and update:
- `docs/workflows/release-check.md`
- `docs/workflows/release.md`
- `eng/release-check.sh`
- `.github/ISSUE_TEMPLATE/release.md`
