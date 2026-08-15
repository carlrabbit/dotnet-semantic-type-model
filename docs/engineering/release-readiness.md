# Release Readiness Engineering

## Purpose

Define the non-publishing validation gate before human-approved publication.

## Release gate

```sh
./eng/release-check.sh <version>
```

The gate must remain synchronized with `eng/release-check.sh` and include repository validation, release build,
package production, package smoke validation, samples, and public-documentation validation as applicable.

## Documentation prerequisite

Before final release validation:

- synchronize current specs/architecture/decisions with implementation;
- update `README.md` and `public-docs/usage.md` when the consumer flow changes;
- update `public-docs/configuration.md` for public generator/library options;
- update target guides for use/configure/diagnose changes;
- update `public-docs/diagnostics.md` or range pages for diagnostics;
- update the single shared `public-docs/nuget/SemanticTypeModel.md`;
- update compatibility and release notes for migration/version-specific changes;
- verify `public-docs/samples.md` still routes to current executable samples;
- run `./eng/public-docs.sh`.

## Suite version alignment

Every package produced for a release uses the same requested SemanticTypeModel suite version. Package smoke,
samples, and documentation must not mix SemanticTypeModel package versions.

## Package checks

Inspect `artifacts/nuget/` and verify:

- expected package IDs only;
- identical requested version across the SemanticTypeModel package suite;
- expected target-framework assets;
- shared README inclusion as `README.md` in every package;
- package metadata and dependencies;
- no unintended repository artifacts.

## Publication boundary

Passing release validation does not publish. Publication, tag creation, and GitHub release creation require
separate explicit approval.
