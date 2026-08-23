# Release Readiness Engineering

## Purpose

Define the non-publishing validation gate before separately approved publication.

## Release Gate

Use the platform-native launcher for the same logical command.

Windows:

```powershell
.\eng\release-check.ps1 <version>
```

Linux/macOS/CI:

```bash
./eng/release-check.sh <version>
```

The gate must remain synchronized with the platform launchers and include repository validation, Release build, package production, package smoke validation, samples, and public-documentation validation as applicable.

The aggregate gate must succeed as a whole. Partial child-command output from separate failed runs is not aggregate release evidence.

## Documentation Prerequisite

Before final release validation:

- synchronize current specs/architecture/decisions with implementation;
- resolve release-scope `.guide-sync/pending/` items;
- update `README.md` and `public-docs/usage.md` when the consumer flow changes;
- update `public-docs/configuration.md` for current public generator/library options and boundaries;
- update target guides for use/configure/diagnose changes;
- update `public-docs/diagnostics.md` or range pages for diagnostics;
- update the single shared `public-docs/nuget/SemanticTypeModel.md`;
- update compatibility and release notes for migration/version-specific changes;
- verify `public-docs/samples.md` still routes to current executable samples;
- run the platform-native `public-docs` command.

## Suite Version Alignment

Every package produced for a release uses the same requested SemanticTypeModel suite version. Package smoke, samples, generated manifests, and documentation must not mix SemanticTypeModel package versions.

## Publication-Channel Preflight

Repository source is not publication truth.

Before declaring a release candidate ready:

- verify the previous stable release/version assumptions used by compatibility and release notes against the actual package/release channel;
- verify the target version is not already published for any expected package ID.

If required publication-channel state cannot be verified, release readiness is not complete.

## Package Checks

Inspect `artifacts/nuget/` from the final aggregate candidate and verify:

- expected package IDs only;
- identical requested version across the SemanticTypeModel package suite;
- expected target-framework assets;
- shared README inclusion as `README.md` in every package;
- package metadata and dependencies;
- no unintended repository artifacts.

## CI Evidence

When a manual release-check workflow exists, run it against the final candidate commit with the exact intended stable version and retain the successful workflow run as independent release evidence.

## Publication Boundary

Passing release validation does not publish.

Package publication, Git tag creation, and GitHub Release creation require a separate explicit release operation and approval. Release-readiness work must not perform those actions unless publication is explicitly assigned.
