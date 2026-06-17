# Package Documentation

## Purpose

Package documentation keeps NuGet README content, public usage guides, and release validation aligned with the packages that are actually produced by this repository.

## Package README Expectations

Every packable `SemanticTypeModel.*` project must:

- set `PackageId` to the exact package name;
- set `PackageReadmeFile` to `README.md`;
- pack the matching `public-docs/nuget/<PackageId>.md` file as `README.md`;
- keep install commands, quick-start snippets, package dependencies, and links aligned with current shipped behavior.

Package README sources should be concise package entry points. Broader tutorials and projection guidance belong in the affected `public-docs/` usage guide and sample pages.

## Compatibility Review

Compatibility review is documented through smoke tests, runnable samples, public documentation, release notes, compatibility documentation, and human review. The repository does not maintain text API baseline files as release gates.

## Validation

Run `./eng/public-docs.sh` after changing package README sources or consumer-facing usage guides. Release candidates also run the package, package smoke, samples, public docs, and release-check commands described in `docs/engineering/release-readiness.md`.
