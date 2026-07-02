# M0048: Documentation Synchronization and 2.4.0 Release Preparation

## Status

Planned.

## Goal

Synchronize authoritative and consumer-facing documentation with implemented M0046 and M0047 behavior, then prepare and validate the complete `SemanticTypeModel.*` package set as a non-publishing `2.4.0` release candidate.

The milestone has two ordered phases:

```text
Phase 1 — documentation-sync
Phase 2 — release-readiness
```

Release-readiness must not begin until documentation synchronization is complete.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Published 2.3.0 package set with implemented M0046 shared samples/nullability hardening and M0047 audience-specific descriptions |
| Release target | `2.4.0` |
| Consumer/dogfood scope | Package-based Order Fulfillment samples validate real use of one shared semantic model across projections |

## Execution Mode

This milestone combines:

1. `documentation-sync`;
2. `release-readiness`.

The implementation agent may read `.guide-sync/pending/` during Phase 1. Those files are synchronization metadata, not behavioral authority.

Human approval is required before publication, tagging, or GitHub release creation.

## Scope

### Phase 1 — Documentation Synchronization

Resolve applicable pending synchronization hints, especially M0046 and M0047.

Synchronize documentation for:

- the shared Order Fulfillment sample domain;
- cross-sample overlap and target-specific selection from one complete model;
- EF Core nullable value-type correction;
- scalar/nullability compatibility coverage;
- `UserDescription`;
- `TechnicalDescription`;
- removal of generic `Description`;
- removal of `SemanticDescriptionAttribute`;
- XML `<summary>` as technical-description fallback;
- removal of XML inclusion and XML requirement switches;
- `RequireTechnicalDescription`;
- JSON Schema user-description mapping;
- optional JSON Schema technical-description extension;
- EF Core table and column comments;
- Power BI user-description behavior;
- Configuration audience-specific output;
- query and inspection output exposing both descriptions;
- breaking 2.4.0 migration guidance.

Replace current 2.3.0 version guidance with 2.4.0 where it represents the release target.

Replace fragmented M0046 preview and M0047 migration-note fragments with one coherent 2.4.0 release section.

### Phase 2 — Release Readiness

- Confirm the packable project inventory.
- Produce every intended package at version `2.4.0`.
- Verify package IDs, descriptions, READMEs, dependencies, target frameworks, repository/license metadata, analyzer/generator assets, and archive contents.
- Run package smoke, shared samples, public-doc validation, and the full release gate.
- Record validation evidence and remaining human-review decisions.
- Prepare but do not execute publication.

## Non-Goals

- No new product features.
- No redesign of M0046 or M0047 behavior.
- No compatibility aliases for removed description APIs.
- No arbitrary XML-tag mapping.
- No broad unrelated cleanup.
- No package publication.
- No tag or GitHub release.
- No copied guide documents, TBPs, issue templates, workflow documents, or non-root READMEs.

## Focus Areas

### 1. Resolve Deferred Synchronization Metadata

Read all files under `.guide-sync/pending/`.

For each hint:

- verify claims against current source, tests, generated output, samples, and packages;
- resolve completed work;
- remove completed hints according to repository convention;
- retain only genuinely deferred work with a precise reason.

### 2. Synchronize Canonical Description Documentation

Authoritative documentation must state:

```text
canonical:
  UserDescription
  TechnicalDescription

removed:
  generic Description
  SemanticDescriptionAttribute
  IncludeXmlDocumentation
  SemanticTypeModelIncludeXmlDocumentation
  RequireXmlDocumentation

XML <summary>:
  automatic fallback for TechnicalDescription only

user-facing projections:
  no silent fallback to TechnicalDescription

technical projections:
  no silent fallback to UserDescription
```

Review all affected canonical, extraction, generator, query, JSON Schema, EF Core, Power BI, and Configuration specifications.

### 3. Synchronize Projection and Package Documentation

Review and update:

```text
public-docs/guides/*.md
public-docs/nuget/*.md
public-docs/api/public-api.md
public-docs/api/compatibility.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
```

Required cross-projection example:

```text
Customer.Name XML <summary>
  -> TechnicalDescription
  -> EF Core column comment

Customer.Name SemanticUserDescription
  -> UserDescription
  -> JSON Schema description
  -> Power BI description
```

### 4. Synchronize Shared Samples

Review:

```text
samples/OrderFulfillment.Domain/
samples/code-first-json-schema/
samples/code-first-ef-core/
samples/code-first-powerbi/
samples/system-text-json-resolver/
samples/runtime-di/
samples/configuration-options/
public-docs/samples.md
public-docs/samples/*.md
docs/engineering/samples.md
eng/samples.sh
```

Docs must explain one complete model, target-specific derivation, explicit Configuration selection, deliberate overlap, sample-canary versus exhaustive-test roles, audience-specific descriptions, and EF nullable value-type correction.

### 5. Produce Coherent 2.4.0 Migration and Release Notes

The 2.4.0 section must cover:

- shared Order Fulfillment samples;
- target selection from one complete model;
- EF nullable value-type fix;
- scalar/nullability audit;
- `UserDescription` and `TechnicalDescription`;
- XML summary technical fallback;
- removed general description APIs and XML switches;
- projection behavior across JSON Schema, EF Core, Power BI, Configuration, query, and inspection;
- manual migration requirements;
- package inventory;
- known limitations;
- publication status.

Old general descriptions cannot be migrated automatically because audience intent cannot be inferred safely.

### 6. Synchronize Version Guidance

Update current release guidance to `2.4.0` in:

```text
README.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/packages.md
public-docs/versioning.md
public-docs/release-notes.md
package README sources
sample package-version configuration
```

Historical release notes may retain earlier versions.

### 7. Validate Package Inventory and Contents

For every intended package verify:

- package ID and version;
- target frameworks;
- description and README;
- repository/license metadata;
- dependencies;
- analyzer/generator assets;
- absence of unintended artifacts.

Unexpected or missing packages are release blockers.

### 8. Run the 2.4.0 Release Gate

Run:

```sh
./eng/public-docs.sh
./eng/samples.sh
./eng/check.sh
./eng/package.sh 2.4.0
./eng/package-smoke.sh 2.4.0
./eng/release-check.sh 2.4.0
```

Do not invoke a publish command.

## Implementation Constraints

- Use target-repository authority documents only.
- Base claims on implemented source, tests, generated output, samples, and package contents.
- Do not infer APIs from planning documents alone.
- Complete documentation sync before final release validation.
- Keep package READMEs concise and guides detailed.
- Preserve provider-versus-consumer boundaries.
- Keep samples package-based.
- Do not weaken validation.
- Source fixes are allowed only for narrow demonstrated release blockers and require tests/docs.
- Publication remains separate.

## Required Authority Documents

### Always Read

```text
AGENTS.md
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/engineering/command-contract.md
docs/engineering/packaging.md
docs/engineering/release-readiness.md
docs/engineering/public-documentation.md
docs/engineering/package-documentation.md
docs/engineering/samples.md
public-docs/versioning.md
public-docs/release-notes.md
public-docs/api/public-api.md
public-docs/api/compatibility.md
```

### M0046 Authority

```text
docs/milestones/m0046-shared-order-fulfillment-samples-and-scalar-nullability-compatibility-hardening.md
docs/decisions/shared-order-fulfillment-sample-domain.md
docs/specs/type-model-ef-core-projection.md
samples/OrderFulfillment.Domain/
public-docs/samples.md
public-docs/samples/*.md
```

### M0047 Authority

```text
docs/milestones/m0047-audience-specific-descriptions-and-projection-description-policies.md
docs/decisions/replace-general-description-with-audience-specific-descriptions.md
docs/specs/audience-specific-description-semantics.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/configuration-domain-model-and-options-projection.md
```

### Release Validation

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
```

During Phase 1 read `.guide-sync/pending/` as synchronization metadata only.

## Files or Areas Likely Affected

```text
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/specs/*.md
docs/engineering/*.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/guides/*.md
public-docs/nuget/*.md
public-docs/api/*.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/versioning.md
public-docs/release-notes.md
samples/
eng/release-check.sh
eng/package-smoke.sh
eng/public-docs.sh
eng/samples.sh
.guide-sync/pending/
```

Implementation source is outside normal scope except for a narrow release blocker.

## Validation Tiers and Concrete Commands

### Phase 1

```sh
./eng/public-docs.sh
./eng/samples.sh
```

Stale-contract search:

```sh
grep -R "SemanticDescriptionAttribute\|SemanticDescription(\|IncludeXmlDocumentation\|SemanticTypeModelIncludeXmlDocumentation\|RequireXmlDocumentation\|generic canonical Description\|2\.3\.0 is the current" README.md docs public-docs samples src tests --exclude-dir=.git
```

Version search:

```sh
grep -R "2\.3\.0" README.md public-docs samples --exclude=release-notes.md --exclude-dir=.git
```

Review every retained match.

### Phase 2

```sh
./eng/check.sh
./eng/package.sh 2.4.0
./eng/package-smoke.sh 2.4.0
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.4.0
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

Inspect selected package archives when metadata or assets are not otherwise proven.

## Acceptance Criteria

### Documentation

- Applicable pending hints are resolved or precisely deferred.
- Current installation guidance uses 2.4.0.
- README describes audience-specific descriptions and shared samples.
- JSON Schema uses user descriptions by default.
- EF Core maps technical descriptions to table/column comments.
- Power BI uses user descriptions by default.
- Configuration and inspection docs distinguish audiences.
- No active docs recommend removed description APIs or XML switches.
- Release notes contain one coherent 2.4.0 section.
- Migration guidance requires manual audience classification.
- Specs, public docs, samples, and package READMEs agree.

### Release

- `./eng/check.sh` passes.
- `./eng/package.sh 2.4.0` produces the expected packages.
- `./eng/package-smoke.sh 2.4.0` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.4.0` passes.
- Package metadata and contents are reviewed.
- Analyzer/generator assets are present where required.
- No package is published.
- Publication decisions remain for human approval.

## Direct Documentation Impact

```text
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
affected specs and engineering docs
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/guides/*.md
public-docs/nuget/*.md
public-docs/api/*.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/versioning.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

The package adds:

```text
.guide-sync/pending/m0048-2-4-0-publication-follow-up.md
```

It tracks only the later human-approved publication phase.

Completed M0046 and M0047 hints should be resolved or removed according to repository convention.

## Human Review Requirements

Human review is required for:

- final package inventory;
- breaking compatibility and migration wording;
- XML-summary fallback wording;
- projection description mappings;
- diagnostics;
- shared sample readability;
- package contents;
- release-gate evidence;
- publication authorization;
- tag and GitHub release creation.

M0048 stops before publication.

## Out-of-Scope Guide Migration Work

M0048 is not a guide migration. Do not update, copy, or reference external guide documents as operational authority.

## Completion Evidence

Documentation synchronization completed before the final 2.4.0 release gate. The synchronization pass read all `.guide-sync/pending/` metadata, resolved the M0046 shared-sample/nullability and M0047 audience-specific-description follow-up topics into active specifications and public documentation, replaced current 2.3.0 installation guidance with 2.4.0 release-preparation guidance, and consolidated the fragmented M0046 preview and M0047 migration-note text into the coherent 2.4.0 release-notes section.

Retained stale-contract search matches are limited to historical milestone files or specification/decision text that explicitly documents removed APIs and migration boundaries. No current 2.3.0 guidance remains in `README.md`, `public-docs/`, or `samples/` outside historical release notes.

The 2.4.0 release-readiness gate produced the expected package inventory under `artifacts/nuget` and stopped before publication, tag creation, or GitHub release creation. Human review remains required for the final package inventory, breaking compatibility wording, migration guidance, XML-summary fallback wording, projection description mappings, diagnostics, package contents, release evidence, publication approval, tag creation, and GitHub release creation.
