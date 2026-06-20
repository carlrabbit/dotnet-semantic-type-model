# M0045: Documentation Synchronization and 2.3.0 Release Preparation

## Status

Implemented pending human publication review.

## Goal

Synchronize all authoritative and consumer-facing documentation with the implemented M0044 Configuration surface, then prepare and validate the complete `SemanticTypeModel.*` package set as a `2.3.0` release candidate without publishing it.

The milestone has two ordered phases:

```text
Phase 1 — documentation-sync
  Resolve deferred documentation work and reconcile all docs with shipped behavior.

Phase 2 — release-readiness
  Build, package, smoke-test, sample-test, and validate the 2.3.0 release candidate.
```

Release-readiness must not begin until documentation synchronization is complete enough that package READMEs, usage guides, compatibility notes, samples, and release notes describe the package contents being validated.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Post-2.2 stable public package set with implemented Configuration packages and M0044 explicit registration support |
| Release target | `2.3.0` |
| Capability-provider scope | The repository owns public package contracts, Configuration domain behavior, runtime adapters, source-generated consumer glue, diagnostics, samples, packaging, and public documentation. |
| Consumer/dogfood scope | Package-based samples validate bounded external-consumer usage; samples do not define or expand product behavior. |

## Execution Mode

This milestone intentionally combines two modes:

1. `documentation-sync` for Phase 1;
2. `release-readiness` for Phase 2.

The implementation agent may read `.guide-sync/pending/` during Phase 1. Ordinary implementation work must continue to use target-repository authority documents only.

Human review is required before any publication, tag, GitHub release, or NuGet push.

## Scope

### Phase 1 — Documentation Synchronization

- Read all current files under `.guide-sync/pending/`.
- Resolve the M0044 synchronization hint for:
  - explicit `AddSemanticOptions<TOptions>` registration;
  - selected-type derivation;
  - multiple explicit registrations from one complete semantic model;
  - proof that unselected configuration types remain unregistered;
  - `ConfigurationSectionPresence.Optional`;
  - `ConfigurationSectionPresence.Required`;
  - required section validation with `ValidateOnStart`;
  - named options;
  - call-site section-path overrides;
  - generated helper delegation to the runtime adapter;
  - registration-time failures versus options-validation failures.
- Reconcile target-repository authority documents with the implemented source and public API.
- Reconcile public documentation with actual package IDs, current API names, current namespaces, diagnostics, samples, and package behavior.
- Replace `2.2.0` installation/version examples with `2.3.0` where they describe the current release target.
- Replace the `2.3.0-preview M0040` release-note section with a coherent `2.3.0` release-candidate section covering M0040 through M0044.
- Fold the detached M0044 release-note fragment into the `2.3.0` release notes.
- Review and synchronize root docs, public docs, package README sources, usage guides, samples, compatibility, versioning, release notes, specifications, terminology, decisions, and milestone indexes.
- Preserve the M0042/M0043 documentation quality contracts: package READMEs remain concise; guides retain concrete supported-items, options/policies, diagnostics, mistakes, and limitations.

### Phase 2 — 2.3.0 Release Preparation

- Confirm the final packable project inventory.
- Confirm `SemanticTypeModel.Configuration` and `SemanticTypeModel.Configuration.Generators` are included when intended as public packages.
- Verify package metadata, package descriptions, package README inclusion, dependencies, target frameworks, repository metadata, and version values.
- Build the repository in Release configuration.
- Produce packages at version `2.3.0`.
- Run package smoke tests against the produced packages.
- Run all package-based samples against the local package set.
- Run public-documentation validation.
- Run the complete release gate with `2.3.0`.
- Inspect `artifacts/nuget/` and confirm the expected package set is present with no stale or unexpected packages.
- Record validation evidence and remaining human-review decisions.
- Prepare the repository for a later explicit publication step.

### Directly Allowed Script Corrections

Documentation or release scripts may be corrected when required to make documented release behavior match actual behavior, including:

- removing duplicate release-readiness steps;
- correcting stale package inventories;
- correcting docs validation coverage;
- correcting package smoke coverage;
- correcting release-check sequencing.

Such changes must remain narrowly scoped to documentation synchronization or release readiness.

## Non-Goals

- Do not add new product features.
- Do not redesign the explicit per-type registration API.
- Do not add a new public API baseline system.
- Do not perform broad unrelated documentation cleanup.
- Do not publish packages.
- Do not create tags.
- Do not create a GitHub release.
- Do not update or copy external guide documents.
- Do not add TBPs, issue templates, or non-root README files.

## Focus Areas

### Focus Area 1 — Consume Deferred Documentation Synchronization Hints

Read `.guide-sync/pending/`, verify every hinted item against current source/tests/specs/public APIs, and resolve or precisely defer each item. Pending hints are synchronization checklists, not behavioral authority.

M0044 topics that must be resolved:

```text
explicit AddSemanticOptions<TOptions> registration
selected-type derivation
multiple selected types from one complete model
unselected types remain unregistered
ConfigurationSectionPresence.Optional
ConfigurationSectionPresence.Required
required section plus ValidateOnStart
named options
call-site section override
generated helper delegation to runtime registration
registration-time versus options-validation failures
```

### Focus Area 2 — Synchronize Authoritative Project Documentation

Review and correct:

```text
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/specs/configuration-domain-model-and-options-projection.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-compile-time-generator.md
docs/engineering/packaging.md
docs/engineering/release-readiness.md
docs/engineering/public-documentation.md
```

Add or correct canonical terminology for configuration section presence, explicit per-type options registration, selected-type Configuration derivation, runtime registration adapter, and generated registration helper. Ensure release-readiness docs exactly match `eng/release-check.sh`.

### Focus Area 3 — Synchronize Consumer Documentation

Review and correct:

```text
README.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/guides/configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.Generators.md
public-docs/api/public-api.md
public-docs/api/compatibility.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/versioning.md
public-docs/release-notes.md
```

Configuration docs must accurately cover:

```text
AddSemanticOptions<TOptions>
explicit inclusion rather than model-wide automatic registration
DeriveConfigurationType<TOptions>
reuse of one complete semantic model across multiple services
unselected configuration types remain unregistered
ConfigurationSectionPresence.Optional as the compatibility default
ConfigurationSectionPresence.Required
provider-independent section existence semantics
required section validation timing
ValidateOnStart interaction
DataAnnotations validation
RequiredWhen validation
named options
call-site section override
runtime adapter as canonical behavior
generated helper as optional delegating convenience
registration-time model errors
runtime options-validation failures
model-wide registration compatibility status
```

Examples must compile against the current public API or be grounded in passing sample/test code.

### Focus Area 4 — Prepare Coherent 2.3.0 Release Notes and Compatibility Guidance

The `2.3.0` release notes must cover:

- Configuration domain model and packages introduced since `2.2.0`;
- projection-neutral `RequiredWhen`;
- Configuration-specific attributes and metadata;
- concrete usage-guide and package-documentation improvements;
- removal of stale fake public API baseline files;
- explicit per-type options registration;
- selected-type Configuration derivation;
- required section presence;
- runtime adapter and generated-helper delegation;
- compatibility status of model-wide registration;
- upgrade/migration guidance;
- known limitations;
- publication status.

Do not retain a detached M0044 note after the coherent `2.3.0` section is created.

### Focus Area 5 — Validate Package Metadata and Package Contents

Determine the expected package set from actual packable projects and canonical packaging scripts. Verify every produced package has the correct package ID, version `2.3.0`, target framework assets, description, package README, repository/license metadata, dependencies, and no unintended artifacts. Treat inventory mismatches as release blockers.

### Focus Area 6 — Run the 2.3.0 Release Candidate Gate

Run:

```sh
./eng/public-docs.sh
./eng/samples.sh
./eng/check.sh
./eng/package.sh 2.3.0
./eng/package-smoke.sh 2.3.0
./eng/release-check.sh 2.3.0
```

`./eng/release-check.sh 2.3.0` is the authoritative final non-publishing gate. Do not run a publish command.

## Implementation Constraints

- Use canonical `eng/` scripts.
- Complete documentation synchronization before the final release gate.
- Base documentation claims on implemented source, tests, generated output, and package contents.
- Do not infer public APIs from planning documents alone.
- Do not make target docs reference external guides as authority.
- Keep package READMEs concise and usage guides concrete.
- Preserve provider-versus-consumer boundaries.
- Keep samples package-based and bounded.
- Do not silently skip failing validation.
- Do not weaken validation to obtain a green release check.
- Publication remains a separate human-approved action.

## Required Authority Documents

### Always Read

```text
AGENTS.md
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/packaging.md
docs/engineering/release-readiness.md
docs/engineering/public-documentation.md
docs/engineering/package-documentation.md
public-docs/versioning.md
public-docs/release-notes.md
public-docs/api/public-api.md
public-docs/api/compatibility.md
```

### Read for M0044 Synchronization

```text
docs/milestones/m0044-explicit-per-type-configuration-registration-and-required-section-presence.md
docs/decisions/configuration-registration-is-explicit-per-options-type.md
docs/specs/configuration-domain-model-and-options-projection.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-compile-time-generator.md
public-docs/guides/configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.Generators.md
```

### Read for Package and Release Validation

```text
eng/common.sh
eng/check.sh
eng/package.sh
eng/package-smoke.sh
eng/samples.sh
eng/public-docs.sh
eng/release-check.sh
src/*/*.csproj
samples/*/*.csproj
public-docs/packages.md
public-docs/installation.md
public-docs/samples.md
public-docs/samples/*.md
```

### Deferred Sync Metadata

During Phase 1, read `.guide-sync/pending/`. Do not treat it as behavioral authority.

## Files or Areas Likely Affected

```text
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/specs/configuration-domain-model-and-options-projection.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-compile-time-generator.md
docs/engineering/packaging.md
docs/engineering/release-readiness.md
docs/engineering/public-documentation.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/guides/configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.Generators.md
public-docs/api/public-api.md
public-docs/api/compatibility.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/versioning.md
public-docs/release-notes.md
eng/release-check.sh
eng/public-docs.sh
eng/package-smoke.sh
.guide-sync/pending/
```

Implementation source and tests are outside normal scope, except for narrowly scoped release-blocking corrections discovered by validation.

## Validation Tiers and Concrete Commands

### Phase 1 — Documentation Sync

```sh
./eng/public-docs.sh
```

Search for stale current guidance:

```sh
grep -R "2\.2\.0\|2\.3\.0-preview\|AddSemanticConfigurationOptions\|automatically registers every configuration type\|empty section is always valid" README.md docs public-docs samples --exclude-dir=.git
```

Review and justify retained historical references. Validate examples:

```sh
./eng/samples.sh
```

### Phase 2 — Release Readiness

```sh
./eng/check.sh
./eng/package.sh 2.3.0
./eng/package-smoke.sh 2.3.0
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.3.0
```

Inspect inventory:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

Archive inspection may use `unzip -l` or another installed archive tool for selected packages.

## Acceptance Criteria

### Documentation Synchronization

- All applicable `.guide-sync/pending/` items are resolved or precisely deferred.
- Root README installs and describes `2.3.0`.
- Getting-started, installation, package, sample, and compatibility docs use the current package set and API names.
- Configuration docs explain explicit per-type registration and do not recommend model-wide automatic registration.
- Configuration docs explain selected-type derivation and prove unselected types remain unregistered.
- Configuration docs explain optional versus required section presence.
- Configuration docs explain validation timing and failure categories.
- Package README sources remain concise and link to the detailed guide.
- Examples are grounded in compiling source, passing tests, generated code, or package-based samples.
- `public-docs/release-notes.md` contains a coherent `2.3.0` section rather than fragmented preview/milestone notes.
- `public-docs/versioning.md` and compatibility docs are consistent with the 2.3.0 changes.
- Authoritative specs, terminology, decisions, and public docs do not contradict one another.
- `docs/engineering/release-readiness.md` matches the actual release-check script and contains no duplicated steps.

### Release Readiness

- `./eng/check.sh` passes.
- `./eng/package.sh 2.3.0` produces the expected package set.
- `./eng/package-smoke.sh 2.3.0` passes.
- `./eng/samples.sh` passes against local packages.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.3.0` passes.
- Package IDs, versions, descriptions, READMEs, dependencies, and contents are reviewed.
- Configuration runtime and generator packages are present if intended for 2.3.0.
- No package is published.
- Remaining publication decisions are explicitly listed for human review.

## Direct Documentation Impact

This milestone directly affects the authority and public documentation surfaces listed above, especially Configuration specs/guides/package READMEs, package lists, compatibility, versioning, and release notes.

## Deferred Documentation Synchronization Hints

A post-preparation hint is included at:

```text
.guide-sync/pending/m0045-2-3-0-publication-follow-up.md
```

It tracks the later human-approved publication phase. Completed older sync hints should be resolved or removed according to repository convention.

## Human Review Requirements

Human review is required for final package inventory, package IDs/descriptions, compatibility wording, model-wide Configuration registration status, required-section wording, named-options and override documentation, generated-helper ergonomics, diagnostic compatibility, release notes, package archive contents, release evidence, and all publication actions.

M0045 stops before publication.

## Out-of-Scope Guide Migration Work

M0045 is not a guide migration. Do not update or copy external guide documents. Do not make target repository documentation reference external guides as operational authority.


## Implementation Evidence

M0045 consumed `m0045-documentation-synchronization-and-2-3-0-release-preparation.zip`, applied its repository files, and deleted the source zip after unpacking. Phase 1 read `.guide-sync/pending/` as deferred metadata, synchronized current installation guidance to `2.3.0`, folded the M0044 Configuration registration note into coherent 2.3.0 documentation, and retained historical `2.2.0` references only where they describe prior milestones, compatibility history, or release notes. Phase 2 confirmed the actual packable project inventory from `src/*/*.csproj` and `eng/common.sh`; the intended Configuration runtime and reserved generator packages are present in the package inventory.

Validation completed for the 2.3.0 release candidate without publishing, creating tags, or creating a GitHub release:

```sh
./eng/public-docs.sh
./eng/samples.sh
./eng/check.sh
./eng/package.sh 2.3.0
./eng/package-smoke.sh 2.3.0
./eng/release-check.sh 2.3.0
find artifacts/nuget -maxdepth 1 -type f -print | sort
unzip -l artifacts/nuget/SemanticTypeModel.Configuration.2.3.0.nupkg
unzip -l artifacts/nuget/SemanticTypeModel.Configuration.Generators.2.3.0.nupkg
unzip -p artifacts/nuget/SemanticTypeModel.Configuration.Generators.2.3.0.nupkg '*.nuspec'
```

The final package inventory contains the eleven expected publishable package IDs plus matching symbol packages for version `2.3.0`, including `SemanticTypeModel.Configuration` and `SemanticTypeModel.Configuration.Generators`. Selected package inspection confirmed README inclusion, repository/license metadata, target framework assets, and Configuration package dependencies. The Configuration.Generators package is documented and described as a reserved placeholder package for 2.3.0 unless generated helper output is present in a consuming build.

Remaining human-review decisions before publication: final package inventory, package IDs/descriptions, compatibility wording, model-wide Configuration registration status, required-section behavior wording, named options and call-site override documentation, generated-helper ergonomics, diagnostic compatibility, release notes, package archive contents, publication approval, tag creation, and GitHub release creation.
