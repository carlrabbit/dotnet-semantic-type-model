# M0023: 1.0.0 Release Hardening and Final Release

## Status

Draft milestone content document.

## Goal

Execute the final `1.0.0` release process after M0022 has made the repository API-stable, documented, sample-backed, package-smoke-tested, and release-ready.

This milestone is the release execution gate. It should not add new features. It should validate, harden, publish a release candidate if needed, resolve release-blocking defects only, and publish `1.0.0`.

The governing rule is:

```text
M0023 may fix release blockers, but it must not expand product scope.
```

## Relationship to M0022

M0022 is the 1.0 readiness gate.

M0023 assumes M0022 has already:

- canonicalized the public API;
- removed pre-1.0 legacy public surface;
- cleaned up `SemanticTypeModel.JsonEditor` package references unless a real package exists;
- documented JSON Editor compatibility as part of `SemanticTypeModel.JsonSchema`;
- completed public docs and samples;
- stabilized diagnostics and annotation keys;
- validated package smoke tests;
- made release scripts and workflows reliable.

If M0022 is incomplete, do not use M0023 to bypass it. Fix M0022 first or explicitly move unfinished readiness work into M0023 with a documented milestone update.

## Scope

This milestone covers:

- final release-candidate validation;
- optional `1.0.0-rc.1` package production/publishing if desired;
- release-blocking defect fixes only;
- final public API diff review;
- final documentation freeze;
- final package artifact verification;
- final release notes;
- NuGet publishing of `1.0.0`;
- GitHub release creation if repository process requires it;
- post-release verification.

## Non-Goals

This milestone does not cover:

- new product features;
- new projection targets;
- new public APIs except for release-blocking fixes;
- large API redesign;
- compatibility shims for pre-1.0 prototype APIs;
- resurrecting `SemanticTypeModel.JsonEditor` as a package unless a real package exists and was accepted before M0023;
- changing package boundaries after the release candidate except to fix a release blocker.

## Required Reading

Before implementation, read:

- `docs/TERMINOLOGY.md`
- `docs/MILESTONES.md`
- `docs/ENGINEERING.md`
- `docs/PUBLIC-DOCS.md`
- `docs/WORKFLOWS.md`
- `docs/GUARDRAILS.md`
- `docs/workflows/release.md`
- `docs/workflows/release-check.md`
- `docs/workflows/package.md`
- `docs/engineering/packaging.md`
- `docs/engineering/release-readiness.md`
- `docs/engineering/command-contract.md`
- `public-docs/packages.md`
- `public-docs/versioning.md`
- `public-docs/release-notes.md`
- `public-docs/guides/projection-capabilities.md`
- `docs/milestones/m0022-1-0-public-api-compatibility-documentation-samples-and-release-readiness.md`
- `docs/research/project-setup-guide-v5.md`
- `docs/research/engineering-guide-v4.md`

If any document is missing or renamed, update this milestone according to current repository structure before implementation.

## Intended Final Package Set

Use the package set finalized by M0022.

Unless M0022 explicitly introduced a real `SemanticTypeModel.JsonEditor` package, the final 1.0 package set must exclude it.

Expected package set after M0022:

```text
SemanticTypeModel.Abstractions
SemanticTypeModel.Core
SemanticTypeModel.JsonSchema
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
SemanticTypeModel.SystemTextJson
SemanticTypeModel.PowerBI
SemanticTypeModel.EFCore
```

The release process must not publish `SemanticTypeModel.JsonEditor` unless a real package exists and M0022 changed the package policy.

## Release Candidate Phase

Produce and validate a release candidate version:

```text
1.0.0-rc.1
```

Required local validation:

```sh
./eng/check.sh
./eng/public-api.sh
./eng/public-docs.sh
./eng/release-check.sh 1.0.0-rc.1
```

If release-check already includes public API and public docs validation, running the separate commands is still useful for clearer failure isolation unless repository policy says otherwise.

Produce package artifacts:

```sh
./eng/package.sh 1.0.0-rc.1
```

Validate package artifacts:

```sh
./eng/package-smoke.sh 1.0.0-rc.1
```

If the repository uses GitHub workflows for release validation, run the equivalent manual workflows:

```text
release-check.yml
pack.yml
```

The release candidate may be published to NuGet only if the repository owner explicitly chooses to publish RC packages. If not published, it must at least be produced and package-smoke-tested locally or through workflow artifacts.

## Release Candidate Review

Review:

- package artifact names;
- package versions;
- package dependencies;
- package metadata;
- package README content;
- symbol package presence where configured;
- public API baseline diff;
- diagnostics reference;
- projection capability docs;
- release notes;
- root README;
- package smoke test results;
- sample validation results;
- workflow artifacts.

Any defect found must be classified as either:

```text
Release blocker
  Must be fixed before 1.0.0.

Post-1.0 follow-up
  Must be documented but does not block release.
```

Only release blockers may be fixed in M0023.

## Release Blocker Policy

A release blocker includes:

- package cannot be restored;
- package cannot be installed in a clean project;
- package smoke tests fail;
- public API baseline is accidental or inconsistent with M0022 decisions;
- package dependency boundaries are wrong;
- package README is missing or misleading;
- root README contains obsolete package guidance;
- `SemanticTypeModel.JsonEditor` is still presented as a package without a real package project;
- documentation contradicts the final package set;
- release workflow would publish the wrong packages;
- diagnostics documentation is materially incomplete;
- source generator package fails in a clean consumer;
- JSON Schema, System.Text.Json, EF Core, or Power BI baseline samples fail.

A post-1.0 follow-up includes:

- minor documentation polish;
- non-blocking sample enhancement;
- future projection improvement;
- non-contract performance improvement;
- advanced feature not promised for 1.0.

## Final Version Phase

After release-candidate validation and release-blocker fixes, produce final version:

```text
1.0.0
```

Required validation:

```sh
./eng/check.sh
./eng/public-api.sh
./eng/public-docs.sh
./eng/release-check.sh 1.0.0
./eng/package.sh 1.0.0
./eng/package-smoke.sh 1.0.0
```

If samples validation is separate, run:

```sh
./eng/samples.sh
```

If workflow validation is required, run manual workflows:

```text
release-check.yml with version 1.0.0
pack.yml with version 1.0.0
```

## Publishing

Publish through the repository's manual publishing workflow or canonical command only.

Expected manual workflow:

```text
.github/workflows/publish-nuget.yml
```

Expected command used by the workflow:

```sh
./eng/publish.sh 1.0.0
```

Publishing requirements:

- manual trigger only;
- secret-based NuGet API key;
- release validation before publishing;
- duplicate-safe publishing;
- no publish on pull request;
- no publish on ordinary push;
- package set matches M0022 final package set;
- package artifacts match `1.0.0` exactly.

## GitHub Release

If the repository uses GitHub releases, create a `1.0.0` release after NuGet publish verification.

The release must link to or include:

- release notes;
- package list;
- known limitations;
- migration note from prerelease;
- public docs entry point;
- samples entry point;
- compatibility policy.

Do not duplicate large internal milestone content in the GitHub release body.

## Post-Release Verification

After publishing, verify:

- packages appear on NuGet;
- package versions are correct;
- package descriptions and README rendering are correct;
- packages can be installed into a clean consumer project;
- source generator package works in a clean consumer project;
- System.Text.Json sample works from packages;
- EF Core sample works from packages;
- Power BI sample works from packages;
- JSON Schema and JSON Editor compatibility docs point to the correct package;
- root README install commands are correct;
- release notes are published and accurate.

Create follow-up issues only for non-blocking post-release work.

## Documentation Freeze

Before final publish, freeze:

```text
README.md
public-docs/packages.md
public-docs/installation.md
public-docs/getting-started.md
public-docs/versioning.md
public-docs/release-notes.md
public-docs/guides/projection-capabilities.md
public-docs/diagnostics.md
public-docs/nuget/*.md
```

After final publish, update only for post-release corrections.

## Versioning Documentation

Ensure `public-docs/versioning.md` states:

- `1.0.0` is the first stable release;
- semantic versioning applies after `1.0.0`;
- documented public APIs are covered by compatibility policy;
- experimental APIs are explicitly marked;
- diagnostic IDs are stable unless policy says otherwise;
- annotation keys are stable unless policy says otherwise;
- prerelease APIs before 1.0 were not compatibility-stable.

## Release Notes Requirements

Update `public-docs/release-notes.md` with:

```text
1.0.0-rc.1
1.0.0
```

The `1.0.0` entry must include:

- final package set;
- major supported scenarios;
- package installation guidance;
- compatibility policy link;
- known limitations;
- migration notes from `0.1.0-alpha`;
- note that JSON Editor compatibility is provided through `SemanticTypeModel.JsonSchema`, not a standalone package, unless policy changed.

## Acceptance Criteria

The milestone is complete when:

- M0022 is complete or explicitly reconciled;
- final package set is confirmed;
- `SemanticTypeModel.JsonEditor` is not published unless a real package exists and is intentionally included;
- `1.0.0-rc.1` package artifacts are produced and validated, or an equivalent release-candidate validation is documented;
- release blockers from RC validation are fixed;
- public API baseline diff is reviewed and accepted;
- public docs are frozen for final release;
- release notes contain `1.0.0-rc.1` and `1.0.0` entries as appropriate;
- `./eng/release-check.sh 1.0.0` passes;
- `./eng/package-smoke.sh 1.0.0` passes using package artifacts;
- final packages are published through the canonical manual process;
- post-release package restore/install verification succeeds;
- GitHub release is created if repository policy requires it;
- follow-up issues are created only for non-blocking post-1.0 work.

## Completion Report

When closing this milestone, report:

- final package IDs published;
- final version published;
- release candidate version validated or published;
- commands run;
- workflows run;
- package artifact verification results;
- package smoke test results;
- sample validation results;
- public API diff result;
- documentation freeze result;
- NuGet publish result;
- post-release verification result;
- follow-up issues created.
