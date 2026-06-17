# Release Readiness Engineering

## Purpose

Define the release validation gate that must pass before publishing.

## Release Gate Command

```sh
./eng/release-check.sh <version>
```

## Required Order

`./eng/release-check.sh <version>` runs this gate sequence without publishing:

1. `./eng/check.sh`
2. `dotnet build --configuration Release`
3. `./eng/package.sh <version>`
4. `./eng/package-smoke.sh <version>`
5. `./eng/samples.sh` when the samples command exists
6. `./eng/public-docs.sh`
7. `./eng/public-docs.sh`

## Release Documentation Checks

Before release, ensure:

- user-first `README.md` is current;
- per-package README sources are current;
- getting started and installation docs are current;
- public API and compatibility docs are current;
- diagnostics documentation is current;
- sample docs are current;
- versioning policy and release notes are current.
