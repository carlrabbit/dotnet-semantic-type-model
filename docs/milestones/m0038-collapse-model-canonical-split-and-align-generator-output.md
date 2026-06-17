# M0038: Collapse Model/Canonical Split and Align Generator Output

## Status

Implemented pending 2.2.0 release review.

## Goal

Collapse the active split between `SemanticTypeModel.Abstractions.Model` and `SemanticTypeModel.Abstractions.Canonical` so the repository has one public canonical semantic model surface and source-generated models can be consumed directly by all projections.

After M0038:

```text
SemanticTypeModel.Abstractions.Model
  is the sole public namespace for canonical semantic model contracts.

SemanticTypeModel.Abstractions.Canonical
  is removed from shipped source, public docs, and public API compatibility documentation or documented only as a removed 2.2.0 compatibility break.

SemanticTypeModel.Abstractions.Model.TypeShape / ObjectShape / PropertyShape / ShapeRef
  old shape-graph contracts are removed rather than kept as compatibility shims.

Source generator output
  returns the unified Model.TypeSchemaModel and can be passed directly to JSON Schema, EF Core, Power BI, System.Text.Json, DI, query, inspection, and transformation APIs.
```

This milestone targets the **2.2.0** line. Version **2.1.0 is already released** and must not be treated as the release-preparation target for this work.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Post-2.1.0 public package set preparing the 2.2.0 line |
| Capability-provider scope | The repository implements semantic model contracts, source generation, transformations, domain projections, samples, public API compatibility documentation, and package documentation. |
| Consumer/dogfood scope | Samples must prove consumer package usage only where bounded by the sample engineering policy. |

## Execution Mode

`ai-executed-human-reviewed`.

The design authority is clear: the current canonical model contracts must become the single public model surface under `SemanticTypeModel.Abstractions.Model`, and the old shape graph must be removed. Human review is required for public API breakage, namespace migration scope, and release-note wording.

## Scope

### In Scope

- Define `SemanticTypeModel.Abstractions.Model` as the sole public namespace for canonical semantic model contracts.
- Move or recreate the current canonical contracts from `SemanticTypeModel.Abstractions.Canonical` under `SemanticTypeModel.Abstractions.Model`.
- Remove the old `SemanticTypeModel.Abstractions.Model` shape graph.
- Remove adapters, shims, overloads, samples, tests, and docs that depend on the old shape graph.
- Update the source generator to emit unified `SemanticTypeModel.Abstractions.Model` contracts.
- Update builder/factory support so generated providers can construct the unified model without hand-written sample models.
- Update `SemanticTypeModel.Core` transformation, validation, query, and inspection APIs to use the unified model surface.
- Update JSON Schema, EF Core, Power BI, System.Text.Json, and dependency-injection packages to consume the unified model surface.
- Replace public samples that manually construct canonical models with code-first generator-backed samples.
- Update public API compatibility documentation and compatibility documentation for the 2.2.0 breaking surface cleanup.
- Update package README sources, public guides, sample docs, and release notes where behavior or usage changes.

### Out of Scope

```text
external guide migration
copying guide documents into this repository
release publication
NuGet publishing
workflow YAML changes
build-script redesign
generated source files checked into the repository
TBPs
issue templates
non-root README files
new projection capabilities unrelated to model-surface unification
provider-specific EF Core behavior
Power BI service/PBIX behavior
JSON Schema import revival
```

## Non-Goals

- Do not keep the old shape graph as a supported compatibility layer.
- Do not add new adapters from old `Model` types to new `Model` types.
- Do not require consumers to call a conversion method before using generated models with projections.
- Do not make ordinary implementation agents read the external guide repository, `.guide-profile.json`, or `.guide-sync/`.
- Do not broaden this work into unrelated semantic vocabulary, projection-policy, or release-readiness features.

## Required Authority Documents

Implementation agents must read only the documents relevant to their focus area.

Always read:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/SPECS.md
docs/specs/model-surface-unification.md
docs/decisions/unify-public-model-surface-under-model-namespace.md
```

Read when modifying model contracts, builders, transformations, diagnostics, query, or inspection:

```text
docs/specs/current-canonical-model-surface.md
docs/specs/type-model-core.md
docs/specs/type-model-runtime-api.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
docs/specs/diagnostics.md
```

Read when modifying extraction, generator output, or generated-provider tests:

```text
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
```

Read when modifying projection packages:

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/system-text-json-contract-integration.md
```

Read when modifying public docs, package README sources, sample docs, public API compatibility documentation, or release notes:

```text
docs/PUBLIC-DOCS.md
docs/engineering/public-documentation.md
docs/engineering/samples.md
docs/engineering/release-readiness.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Focus Areas

### Focus Area 1 — Establish Unified Model Contracts

#### Intent

Make the current canonical contracts the only public model contracts under `SemanticTypeModel.Abstractions.Model`.

#### Implementation Requirements

- Move or recreate the current canonical contract set under `SemanticTypeModel.Abstractions.Model`.
- Remove old shape-graph contracts from `SemanticTypeModel.Abstractions.Model`.
- Remove `SemanticTypeModel.Abstractions.Canonical` from public source after migration.
- Preserve semantic expressiveness from the current canonical model, including typed identifiers, annotation bags, type definitions, property definitions, key definitions, relationship definitions, constraints, composition, scalar metadata, and enum metadata.
- Keep contracts immutable or immutable-by-convention as currently specified.
- Do not introduce old-to-new adapters as a default path.

#### Validation

- Tier 1:
  - focused `SemanticTypeModel.Abstractions` tests;
  - compile of packages referencing model contracts;
  - public API compatibility documentation diff review.
- Tier 2 before completion.

### Focus Area 2 — Align Source Generator Output

#### Intent

Make generated code produce the unified model surface so code-first samples and consumers can pass generated models directly into projections.

#### Implementation Requirements

- Update generator output from `global::SemanticTypeModel.Abstractions.Model` old shape graph to unified `global::SemanticTypeModel.Abstractions.Model` contracts.
- Generated `Create()` must return the unified `Model.TypeSchemaModel`.
- Generated output must not reference `SemanticTypeModel.Abstractions.Canonical`.
- Generated output must not require a conversion adapter before projection.
- Update generator baseline tests and source-generation public docs.
- Ensure generated provider naming and deterministic output remain stable except for intentional model-contract changes.

#### Validation

- Tier 1:
  - generator baseline tests;
  - generated source snapshot tests;
  - sample compile using generated provider output.
- Tier 2 before completion.

### Focus Area 3 — Migrate Core Runtime, Transformation, Query, and Inspection

#### Intent

Remove the split from internal services and make all core APIs use the unified model surface.

#### Implementation Requirements

- Update `SemanticTypeModel.Core` transformations, validation, query, inspection, and building helpers to use unified `Model` contracts.
- Remove legacy or old-shape compatibility overloads.
- Remove stale `Canonical` aliases in production source.
- Keep deterministic transformation traces, diagnostics, and inspection output.
- Ensure diagnostics identify unsupported or ambiguous model states after migration.

#### Validation

- Tier 1:
  - core transformation tests;
  - validation tests;
  - query and inspection tests;
  - diagnostics stability tests when diagnostic IDs change.
- Tier 2 before completion.

### Focus Area 4 — Migrate Projection and Integration Packages

#### Intent

Make every domain projection and integration package consume the same generated model type.

#### Implementation Requirements

- Update JSON Schema derivation/export entry points to use unified `Model.TypeSchemaModel`.
- Update EF Core derivation and `ModelBuilder` projection to use unified `Model.TypeSchemaModel`.
- Update Power BI derivation and local metadata projection to use unified `Model.TypeSchemaModel`.
- Update System.Text.Json domain model derivation and resolver customization to use unified `Model.TypeSchemaModel`.
- Update dependency-injection registration and projection services to use unified model contracts.
- Remove old-shape overloads and adapters unless a human reviewer explicitly approves a temporary obsolete shim.

#### Validation

- Tier 1:
  - affected package test projects;
  - domain derivation tests for JSON Schema, EF Core, Power BI, and System.Text.Json;
  - DI projection registration tests.
- Tier 2 before completion.

### Focus Area 5 — Replace Hand-Built Public Samples with Generated Code-First Models

#### Intent

Make public samples demonstrate the supported consumer path instead of hand-constructing model contracts.

#### Implementation Requirements

- Update `samples/code-first-json-schema` to use annotated C# model classes and the generated provider.
- Update `samples/code-first-ef-core` to use annotated C# model classes and the generated provider.
- Update `samples/code-first-powerbi` to use annotated C# model classes and the generated provider.
- Update `samples/system-text-json-resolver` where needed so it demonstrates generated-provider input instead of old-shape or hand-built model input.
- Keep any test-only hand-built model factories internal to tests and clearly scoped; public samples must not use them.
- Update `public-docs/samples/*.md` and package README examples to match the sample code.

#### Validation

- Tier 1:
  - affected sample builds;
  - focused sample validation when available.
- Tier 3 sample/package validation before completion because samples and public package behavior are affected.

### Focus Area 6 — Public API, Compatibility, and Documentation Update

#### Intent

Make the intentional breaking model-surface cleanup visible to consumers and stable enough for the 2.2.0 line.

#### Implementation Requirements

- Update public API compatibility documentation for all affected packages.
- Update compatibility docs to state that the old shape-graph model and `Canonical` namespace are removed in the 2.2.0 line.
- Update public docs and package README sources so examples show generated code-first models passed directly to projections.
- Update release notes under a 2.2.0 heading without implying release publication.
- Update `docs/SPECS.md`, `docs/DECISIONS.md`, and `docs/MILESTONES.md` indexes if implementation introduces or changes authority docs.

#### Validation

- Tier 0/Tier 1 for documentation and public API compatibility documentation checks during editing.
- Tier 3 package/public-doc/sample validation before completion.

## Implementation Constraints

- Keep the change scoped to model-surface unification and generator alignment.
- Prefer deletion of old-shape contracts over compatibility adapters.
- Do not silently change projection semantics beyond what is required to accept the unified model type.
- Keep code-first annotated .NET code as the supported authoring source.
- Preserve deterministic ordering in generated output, transformations, inspections, exports, and sample artifacts.
- Do not add non-root README files.
- Do not copy or reference external guide documents as repository authority.
- Do not treat `.guide-profile.json` or `.guide-sync/` as required reading for ordinary implementation work.

## Files or Areas Likely Affected

```text
src/SemanticTypeModel.Abstractions/Canonical/**
src/SemanticTypeModel.Abstractions/Model/**
src/SemanticTypeModel.Abstractions/PublicAPI.*.txt
src/SemanticTypeModel.Core/**
src/SemanticTypeModel.DotNet/**
src/SemanticTypeModel.Generators/**
src/SemanticTypeModel.JsonSchema/**
src/SemanticTypeModel.EFCore/**
src/SemanticTypeModel.PowerBI/**
src/SemanticTypeModel.SystemTextJson/**
src/SemanticTypeModel.DependencyInjection/**
tests/unit/**
samples/code-first-json-schema/**
samples/code-first-ef-core/**
samples/code-first-powerbi/**
samples/system-text-json-resolver/**
public-docs/**
docs/specs/**
docs/decisions/**
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
```

## Validation Tiers and Concrete Commands

Use focused validation during implementation, then run the required completion gates.

### Tier 1 — Focused Inner Loop

Use the smallest affected-area command that catches the current change:

```sh
./eng/check-affected.sh src/SemanticTypeModel.Abstractions src/SemanticTypeModel.Core src/SemanticTypeModel.Generators
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Core.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.PowerBI.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.SystemTextJson.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.DependencyInjection.Tests.Unit
```

### Tier 2 — Implementation Completion Gate

Run before completing M0038 implementation work:

```sh
./eng/check.sh
```

### Tier 3 — Package, Public API, Public Docs, and Sample Gate

Required because this milestone changes public package APIs, samples, and package documentation:

```sh
./eng/package.sh 0.0.0-m0038
./eng/package-smoke.sh 0.0.0-m0038
./eng/samples.sh
./eng/public-docs.sh
./eng/public-docs.sh
```

Do not run publish commands. Do not require `./eng/release-check.sh 2.2.0` unless this milestone is explicitly expanded into release readiness.

## Acceptance Criteria

- `SemanticTypeModel.Abstractions.Model` contains the unified canonical semantic model contracts.
- The old `SemanticTypeModel.Abstractions.Model` shape graph is removed from shipped source.
- `SemanticTypeModel.Abstractions.Canonical` is removed from shipped source and public API compatibility documentation.
- The source generator emits a provider whose `Create()` method returns the unified `Model.TypeSchemaModel`.
- Generated models are accepted directly by JSON Schema, EF Core, Power BI, System.Text.Json, DI, transformation, query, and inspection APIs.
- Public samples use generated code-first providers rather than hand-built canonical models.
- Tests no longer rely on old-shape compatibility except for explicitly removed/negative migration tests if needed.
- Public API compatibility documentations reflect the intentional 2.2.0 breaking cleanup.
- Public docs and release notes describe the cleanup without claiming release publication.
- Tier 2 and required Tier 3 commands pass, or any environment limitation is explicitly documented by the implementation agent.


## M0039 Verification Notes

M0039 verified the implemented model-surface unification before preparing 2.2.0 release-facing documentation:

- Production model contracts live under `src/SemanticTypeModel.Abstractions/Model/`.
- The source generator emits `global::SemanticTypeModel.Abstractions.Model.TypeSchemaModel Create()`.
- Targeted searches found no shipped source namespace declaration for `SemanticTypeModel.Abstractions.Canonical` and no old shape-graph type declarations in `src/`.
- Code-first JSON Schema, EF Core, and Power BI samples call generated `AppSemanticTypeModel.Create()` providers directly.
- Remaining non-code-first samples that use local model factories are documented as focused compatibility/DI/resolver examples rather than the preferred authoring path and require human release review.

## Direct Documentation Impact

Update as part of M0038 implementation when the related code changes are made:

```text
docs/specs/model-surface-unification.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
public-docs/api/compatibility.md
public-docs/guides/json-schema.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/system-text-json.md
public-docs/nuget/SemanticTypeModel.Abstractions.md
public-docs/nuget/SemanticTypeModel.Core.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/nuget/SemanticTypeModel.SystemTextJson.md
public-docs/samples.md
public-docs/samples/code-first-json-schema.md
public-docs/samples/code-first-ef-core.md
public-docs/samples/code-first-powerbi.md
public-docs/samples/system-text-json-resolver.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

The planning package creates:

```text
.guide-sync/pending/m0038-model-surface-unification.md
```

This hint tracks post-implementation documentation checks for public docs, sample docs, package README sources, compatibility notes, release notes, and stale references to `Abstractions.Canonical` or the removed old shape graph.

Implementation agents are not required to read `.guide-sync/` unless assigned documentation synchronization work.

## Human Review Requirements

Human review is required for:

- the public API break caused by removing the old shape graph and `Canonical` namespace;
- whether any temporary obsolete compatibility shim is acceptable; default answer is no;
- release-note wording for the 2.2.0 line;
- sample behavior after replacing hand-built models with generated providers;
- public API compatibility documentation diffs;
- any decision to retain test-only model factories.

## Out-of-Scope Guide Migration Work

M0038 is not a guide-system migration milestone. Do not update external guide references, `.guide-profile.json`, guide prompt templates, or copied guide files as part of this milestone unless a separate guide-migration task explicitly requests it.
