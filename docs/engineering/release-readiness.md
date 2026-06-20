# Release Readiness Engineering

## Purpose

Define the non-publishing release validation gate that must pass before human-approved publication.

## Release Gate Command

```sh
./eng/release-check.sh <version>
```

## Required Order

`./eng/release-check.sh <version>` runs this sequence without publishing:

1. `./eng/check.sh`
2. `dotnet build --configuration Release`
3. `./eng/package.sh <version>`
4. `./eng/package-smoke.sh <version>`
5. `./eng/samples.sh` when the samples command exists
6. `./eng/public-docs.sh`

The documented sequence must remain synchronized with `eng/release-check.sh`.

## Documentation Synchronization Prerequisite

Before running the final release gate:

- resolve applicable `.guide-sync/pending/` items;
- synchronize authoritative specs and terminology with implemented behavior;
- synchronize package README sources and usage guides;
- synchronize getting-started, installation, package, sample, compatibility, versioning, and release-note docs;
- remove stale current-version guidance;
- validate public documentation.

Documentation synchronization is part of release readiness, not a substitute for package validation.

## Release Documentation Checks

Before release, ensure:

- the root `README.md` names the release target and current package versions correctly;
- per-package README sources are current and included in packages;
- getting-started and installation docs are current;
- package inventory and descriptions are current;
- public API and compatibility docs are current;
- diagnostics documentation is current;
- sample docs and commands are current;
- versioning policy and release notes are current;
- release notes include upgrade guidance and known limitations.

## Package Checks

Before publication, inspect `artifacts/nuget/` and verify:

- expected package IDs only;
- requested version on every package;
- expected target framework assets;
- package README inclusion;
- package description and repository/license metadata;
- expected dependencies;
- no unintended source-tree or build artifacts.

## Publication Boundary

Passing `./eng/release-check.sh <version>` does not publish packages.

Publication, tag creation, and GitHub release creation require separate explicit human approval and the repository's documented publish workflow or command.
