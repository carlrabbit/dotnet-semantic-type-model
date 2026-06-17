# M0039: Documentation Synchronization and 2.2.0 Release Preparation

## Status

Implemented pending human release review.

## Goal

Synchronize repository documentation after the implemented M0038 model-surface unification and prepare the 2.2.0 release candidate without publishing packages.

M0039 has two coordinated purposes:

1. Documentation synchronization: align repository-local authority docs, public docs, package README sources, samples, compatibility docs, and release notes with the implemented collapse of the `Model` / `Canonical` split.
2. Release readiness: run the 2.2.0 release gate and record human-review findings before any publish step.

Version **2.1.0 is already released**. M0039 targets the **2.2.0** line only.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Post-2.1.0 public package set preparing 2.2.0 |
| Capability-provider scope | The repository implements model contracts, source generation, transformations, domain projections, samples, public API compatibility documentation, package docs, and release validation. |
| Consumer/dogfood scope | Samples validate consumer package usage only where bounded by sample engineering policy. |

## Execution Mode

`documentation-sync + release-readiness`.

This milestone may read `.guide-sync/pending/` because the assigned work is documentation synchronization and release preparation. Ordinary implementation agents remain exempt from `.guide-profile.json` and `.guide-sync/` unless assigned guide migration, documentation synchronization, or release-readiness work.

## Scope

### In Scope

- Verify implementation state for M0038 before writing release-facing statements.
- Update index documents for M0038/M0039 and any M0038 spec/decision documents not yet indexed.
- Update milestone status and completion notes for M0038 if implementation evidence exists.
- Update public API compatibility documentation for the model-surface unification and any removed `Canonical`/old-shape surface.
- Update package README sources under `public-docs/nuget/` for affected packages.
- Update public guides for code-first generator usage, model-surface terminology, projection APIs, samples, System.Text.Json integration, EF Core, Power BI, and JSON Schema where consumer behavior changed.
- Update sample documentation so public samples no longer claim or imply hand-built canonical models as the code-first happy path.
- Update release notes for 2.2.0 with accurate, consumer-facing changes.
- Run release-readiness validation for version `2.2.0` without publishing.
- Record remaining release blockers or human review findings in the milestone or release notes when appropriate.

### Out of Scope

```text
implementation source changes unless required to fix documentation metadata only
test source changes
new generated code files
workflow YAML changes
build-script redesign
NuGet publication
release tag creation
GitHub release creation
external guide migration
copied guide documents
TBPs
issue templates
non-root README files
new feature work unrelated to M0038 documentation and 2.2.0 readiness
```

## Non-Goals

- Do not re-plan or re-implement M0038.
- Do not publish the 2.2.0 packages.
- Do not turn the documentation pass into broad unrelated cleanup.
- Do not make repository-local docs reference `carlrabbit/agentic-project-guides` as operational authority.
- Do not require ordinary implementation agents to read `.guide-profile.json`, `.guide-sync/`, or external guides outside assigned documentation-sync/release-readiness work.
- Do not claim compatibility, release status, or package behavior that is not verified by repository state and validation output.

## Focus Areas

### Focus Area 1 — Confirm M0038 Implementation State

#### Intent

Establish the factual basis for documentation and release notes before changing consumer-facing docs.

#### Requirements

- Inspect the implemented model surface and confirm the final public namespace and removed namespace behavior.
- Verify whether `SemanticTypeModel.Abstractions.Canonical` is absent from shipped source, public docs, samples, and public API compatibility documentation or explicitly documented as removed.
- Verify whether source-generated providers now return the unified model type and can be passed directly to projections.
- Verify whether samples compile and use generated providers rather than hand-built model instances.
- Record unresolved implementation/documentation mismatches as release blockers rather than masking them in docs.

#### Validation

- Tier 0/Tier 1 as needed:
  - targeted search for stale terms;
  - affected sample docs/source inspection;
  - focused sample command if available and fast.

### Focus Area 2 — Synchronize Internal Authority Indexes and Milestones

#### Intent

Keep repository-local navigation accurate after M0038 and before 2.2.0 release readiness.

#### Requirements

- Ensure `docs/MILESTONES.md` lists M0038 and M0039.
- Ensure `docs/SPECS.md` lists any M0038 specification documents.
- Ensure `docs/DECISIONS.md` lists any M0038 decision records.
- Ensure `docs/TERMINOLOGY.md` contains current canonical terms and does not preserve stale `Canonical` terminology as an active public model surface term.
- Update M0038 status only if implementation evidence supports the status change.
- Keep milestone documents as routers; do not duplicate full spec bodies inside milestone updates.

#### Validation

- Tier 0:

```sh
./eng/check-affected.sh docs/MILESTONES.md docs/SPECS.md docs/DECISIONS.md docs/TERMINOLOGY.md docs/milestones/m0038-collapse-model-canonical-split-and-align-generator-output.md docs/milestones/m0039-documentation-synchronization-and-2-2-0-release-preparation.md
```

### Focus Area 3 — Synchronize Public Documentation and Package README Sources

#### Intent

Make consumer-facing docs accurately explain the 2.2.0 model surface and the supported code-first workflow.

#### Requirements

- Update `README.md` if the quick start, package list, sample list, or release notice still reflects pre-M0038 behavior.
- Update `public-docs/getting-started.md`, `public-docs/concepts.md`, and relevant guides if they mention `Canonical`, old shape graph types, hand-built models, or conversion/adapters as normal use.
- Update `public-docs/guides/core-semantics.md`, `json-schema.md`, `ef-core-projection.md`, `power-bi-projection.md`, `system-text-json.md`, and `projection-capabilities.md` only where consumer-facing behavior changed.
- Update `public-docs/nuget/*.md` for packages affected by model-surface, generator, projection, sample, or compatibility changes.
- Update `public-docs/api/compatibility.md` for the 2.2.0 model-surface break and removed types/namespaces.
- Update diagnostics docs only if diagnostics were added, removed, renumbered, or reworded.
- Do not document planned behavior as released behavior unless validation confirms it is implemented.

#### Validation

- Tier 0:

```sh
./eng/public-docs.sh
```

### Focus Area 4 — Synchronize Samples and Dogfood Documentation

#### Intent

Ensure public samples demonstrate supported consumer package usage after M0038.

#### Requirements

- Update `public-docs/samples.md` and relevant `public-docs/samples/*.md` pages.
- Ensure code-first samples describe generated providers as the normal path.
- Remove stale wording that says or implies manually constructed canonical models are normal sample behavior.
- Keep provider/consumer distinction explicit: the repository implements generator/projection capabilities; samples demonstrate consumer usage with prepared local packages.
- Validate package-based sample execution before release readiness.

#### Validation

- Tier 3 sample validation:

```sh
./eng/package.sh 2.2.0
./eng/samples.sh
```

### Focus Area 5 — Prepare 2.2.0 Release Notes, Versioning, and Compatibility Review

#### Intent

Prepare release-facing documentation for human review and release candidate validation.

#### Requirements

- Add a `2.2.0` section to `public-docs/release-notes.md`.
- State that 2.2.0 includes the model-surface unification and generator alignment when confirmed by implementation state.
- Document breaking changes and migration guidance for consumers using old `Canonical` or old shape-graph types.
- Update `public-docs/versioning.md` only if release/versioning policy changed.
- Update public API compatibility documentation documentation and compatibility docs based on actual `./eng/public-docs.sh` output.
- Record release blockers rather than presenting unresolved API or sample failures as complete.

#### Validation

- Tier 3:

```sh
./eng/public-docs.sh
./eng/public-docs.sh
```

### Focus Area 6 — Run 2.2.0 Release Readiness Gate

#### Intent

Verify 2.2.0 release readiness without publishing.

#### Requirements

- Run the release gate for version `2.2.0`.
- Capture validation failures as release blockers.
- Do not publish packages, create tags, or create GitHub releases.
- Require human review before any publish step.

#### Validation

- Tier 3 release candidate gate:

```sh
./eng/release-check.sh 2.2.0
```

## Implementation Constraints

- Keep this milestone documentation/release-preparation scoped.
- Use target-repository docs as implementation authority.
- Treat old copied setup/engineering guides in `docs/research/` as legacy, non-authoritative material unless a current local authority document says otherwise.
- Do not copy guide documents into the repository.
- Do not add non-root README files.
- Do not add TBPs, issue templates, broad guardrail docs, or workflow docs unless an explicit repository-local milestone requires them as project truth.
- Prefer evidence from repository files and validation output over inferred completion.
- If implementation and documentation disagree, document the mismatch as a blocker rather than adjusting docs to hide it.


## Execution Notes

M0039 consumed the milestone package, deleted `m0039-documentation-sync-and-2-2-0-release-preparation.zip` after unpacking, and synchronized 2.2.0 documentation based on repository evidence.

Implementation evidence recorded during the documentation pass:

- Unified model contracts are present under `src/SemanticTypeModel.Abstractions/Model/`.
- The source generator emits `global::SemanticTypeModel.Abstractions.Model.TypeSchemaModel Create()`.
- Active code-first samples for JSON Schema, EF Core, and Power BI use generated `AppSemanticTypeModel.Create()` providers.
- `SemanticTypeModel.Abstractions.Canonical` and removed old shape-graph terms are documented as 2.2.0 compatibility breaks, not supported current usage.

Release-readiness validation completed during M0039:

- `./eng/check-affected.sh README.md docs public-docs` passed.
- `./eng/public-docs.sh` passed.
- `./eng/package.sh 2.2.0` passed.
- `./eng/package-smoke.sh 2.2.0` passed.
- `./eng/samples.sh` passed.
- `./eng/public-docs.sh` passed after `./eng/restore.sh` generated missing assets.
- `./eng/release-check.sh 2.2.0` passed.

Release-review items that remain human-owned:

- public API compatibility documentation diffs and compatibility wording;
- 2.2.0 release-note wording;
- sample behavior claims for focused samples that still use local model factories rather than generated providers;
- the completed 2.2.0 release-readiness validation output before any publish decision.

## Required Authority Documents

Always read:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/public-documentation.md
docs/engineering/release-readiness.md
docs/engineering/samples.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/milestones/m0038-collapse-model-canonical-split-and-align-generator-output.md
docs/specs/model-surface-unification.md
docs/decisions/unify-public-model-surface-under-model-namespace.md
```

Read when updating public docs, package docs, samples, API compatibility, or release notes:

```text
README.md
public-docs/getting-started.md
public-docs/concepts.md
public-docs/packages.md
public-docs/samples.md
public-docs/api/compatibility.md
public-docs/release-notes.md
public-docs/versioning.md
public-docs/guides/core-semantics.md
public-docs/guides/json-schema.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/system-text-json.md
public-docs/guides/projection-capabilities.md
public-docs/nuget/*.md
public-docs/samples/*.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
```

Read only if affected by discovered mismatch:

```text
docs/specs/current-canonical-model-surface.md
docs/specs/type-model-core.md
docs/specs/type-model-runtime-api.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
```

Do not read the external guide repository for implementation. Do not treat `docs/research/` guide copies as operational authority.

## Files or Areas Likely Affected

```text
README.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/TERMINOLOGY.md
docs/milestones/m0038-collapse-model-canonical-split-and-align-generator-output.md
public-docs/getting-started.md
public-docs/concepts.md
public-docs/packages.md
public-docs/guides/*.md
public-docs/nuget/*.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
public-docs/versioning.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
```

## Validation Tiers and Concrete Commands

### Tier 0 — documentation and affected-file validation

Run after documentation edits:

```sh
./eng/check-affected.sh README.md docs public-docs
./eng/public-docs.sh
```

### Tier 3 — release-candidate validation

Run before completing M0039:

```sh
./eng/package.sh 2.2.0
./eng/package-smoke.sh 2.2.0
./eng/samples.sh
./eng/public-docs.sh
./eng/public-docs.sh
./eng/release-check.sh 2.2.0
```

If `./eng/release-check.sh 2.2.0` already runs a subset or all of the preceding commands, prefer the command contract and avoid redundant execution unless debugging a failure.

### Tier 4 — explicitly out of scope

Do not run publish validation or publish commands in M0039 unless a later human explicitly changes the milestone scope.

## Acceptance Criteria

- M0038 implementation state is verified before consumer-facing docs or release notes claim completion.
- `docs/MILESTONES.md`, `docs/SPECS.md`, `docs/DECISIONS.md`, and `docs/TERMINOLOGY.md` are current for M0038/M0039 and model-surface terminology.
- Public docs no longer instruct consumers to use stale `Canonical` namespace, old shape-graph types, or hand-built canonical models as the normal code-first path.
- Package README sources explain the correct 2.2.0 usage for affected packages.
- Public sample docs match runnable sample behavior.
- `public-docs/api/compatibility.md` describes the 2.2.0 model-surface breaking change and migration path when applicable.
- `public-docs/release-notes.md` contains an accurate 2.2.0 section.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.2.0` passes or remaining failures are recorded as release blockers.
- No source, test, workflow, TBP, issue-template, copied-guide, or non-root README files are introduced.
- Human review is completed for public API compatibility documentation diffs, release notes, compatibility wording, and any remaining release blockers.

## Direct Documentation Impact

M0039 directly owns synchronization for:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/TERMINOLOGY.md
docs/milestones/m0038-collapse-model-canonical-split-and-align-generator-output.md
README.md
public-docs/**
```

Only update files that actually need synchronization after inspection.

## Deferred Documentation Synchronization Hints

Use `.guide-sync/pending/m0039-documentation-sync-and-2-2-0-release-preparation.md` as the handoff hint for this milestone. M0039 may consume `.guide-sync/pending/` because documentation synchronization is explicitly assigned.

After M0039 is complete, remove or archive resolved M0038/M0039 pending hints only if the repository has an established convention for resolved guide-sync hints. If no convention exists, leave a completion note in the relevant hint rather than deleting history opportunistically.

## Human Review Requirements

Human review is required for:

- any public API compatibility documentation change;
- breaking-change wording in `public-docs/api/compatibility.md`;
- `public-docs/release-notes.md` 2.2.0 release notes;
- sample behavior claims;
- unresolved release blockers;
- any decision to publish packages, create tags, or create a GitHub release.

## Out-of-Scope Guide Migration Work

M0039 is not a guide migration milestone.

Do not change `.guide-profile.json` unless it is directly inconsistent with repository truth. Do not copy guide documents into the repository. Do not make target-repository docs cite `carlrabbit/agentic-project-guides` as operational authority.
