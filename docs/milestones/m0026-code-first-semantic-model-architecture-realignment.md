# M0026: Code-First Semantic Model Architecture Realignment

## Status

Implemented.

## Completed Outcomes

- Code-first-only canonical model source authority is established in the code-first architecture specification and decision record.
- JSON Schema import is marked unsupported as canonical model creation, with retained import behavior documented only as legacy/internal compatibility where needed.
- Persisted model load/save is positioned as model snapshot behavior, not model authoring.
- Consumer-facing runtime editing of canonical models is explicitly unsupported.
- Core semantic primitives, custom attribute extensibility, alias attributes, transformations, query, and inspection are defined at authority level.
- JSON Schema, EF Core, Power BI, and System.Text.Json behavior is routed through domain-specific semantic model derivation before domain-specific functionality.
- Unsupported feature scope is listed in authority documents.
- Public documentation and package README surfaces that require a later synchronization pass remain deferred; this milestone implementation did not perform that broad public documentation synchronization pass.

## Maturity Mode

Architectural scope correction for a public package set.

The repository has public packages, public samples, public API baselines, package README sources, and package validation. This milestone changes the project’s core product direction and must update authoritative specifications and architecture before implementation agents continue feature work.

## Task Mode

Milestone implementation routing and authority realignment.

This milestone is not a broad documentation synchronization pass. It changes behavioral authority, architecture authority, and durable rationale for the project. Direct documentation impact is limited to the documents required to make the new architecture implementable.

Do not introduce TBPs, issue templates, workflow YAML, non-root README files, implementation source patches, generated code, or broad public-documentation rewrites in this planning package.

## Goal

Realign SemanticTypeModel around the code-first vision:

```text
Annotated .NET code
  -> runtime extraction or compile-time generation
  -> canonical Semantic Type Model
  -> query, inspect, validate, transform, persist, load
  -> domain-specific semantic model
  -> domain-specific functionality
```

The project must stop treating external schema formats as sources of the canonical model.

## Product Direction

SemanticTypeModel is a code-first semantic metadata framework for .NET.

The canonical model is generated from annotated .NET code through runtime extraction or compile-time source generation.

The canonical model may be persisted and loaded as a snapshot after generation. A persisted model is not an authoring source; it is a reusable representation of a model that originally came from code.

JSON Schema, EF Core, Power BI, and System.Text.Json integrations must become domain-specific semantic model projections or integrations over code-generated semantic models.

## Required Authority

Read these documents before implementing any focus area:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/SPECS.md
docs/specs/code-first-semantic-model-architecture.md
docs/architecture/code-first-domain-projection-pipeline.md
docs/decisions/code-first-only-model-source.md
```

Read these only when the selected focus area touches the relevant component:

```text
docs/specs/type-schema-model.md
docs/specs/type-model-core.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-contract-integration.md
docs/specs/type-model-di-integration.md
docs/PUBLIC-DOCS.md
public-docs/packages.md
public-docs/getting-started.md
public-docs/samples.md
public-docs/nuget/*.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Establish code-first-only canonical model source authority.
- Remove JSON Schema import as a supported source of the canonical model.
- Define persisted model load/save as model snapshot behavior, not authoring.
- State that runtime editing of the canonical model is unsupported.
- Define canonical semantic primitives as the core product surface.
- Define model querying and inspection as first-class development-loop features.
- Define custom attribute extensibility and alias attributes.
- Define domain-specific semantic model derivation for JSON Schema, EF Core, Power BI, and System.Text.Json.
- Define transformations as the mechanism for deriving core semantics and domain semantics.
- Define projections as domain-model creation followed by domain-specific functionality.
- Identify public docs and package README surfaces that will need later synchronization.
- Keep implementation focused on authority and architecture alignment before feature expansion.

### Out of Scope

- Implementing the full query API.
- Implementing the full inspection/text-formatting API.
- Rebuilding all domain projections in this milestone.
- Rewriting all public documentation in this milestone.
- Release publication.
- Creating a JSON editor runtime.
- OpenAPI import/export.
- TypeScript generation.
- Power BI service integration.
- PBIX generation.
- EF Core database creation or migration execution.
- Runtime editing of canonical models.
- JSON Schema import as canonical model creation.
- Custom serializer implementation.
- TBPs and issue templates.

## Architecture Summary

The authoritative architecture is defined in:

```text
docs/architecture/code-first-domain-projection-pipeline.md
```

The core pipeline is:

```text
Code source
  -> extraction/generation
  -> canonical semantic model
  -> query/inspect/validate
  -> transformation pipeline
  -> domain semantic model
  -> domain functionality
```

## Focus Areas

### Focus Area 1 — Authority Realignment

#### Intent

Update behavioral authority so code is the only supported canonical model source.

#### Required Authority

```text
docs/specs/code-first-semantic-model-architecture.md
docs/decisions/code-first-only-model-source.md
docs/SPECS.md
```

#### Implementation Requirements

- Identify specs that currently imply JSON Schema import creates canonical model authority.
- Mark JSON Schema import as removed, unsupported, or legacy-internal according to implementation constraints.
- Ensure specs distinguish code-generated canonical models, persisted model snapshots, domain-specific semantic models, and projection outputs.
- Ensure public contracts do not present external schemas as the primary model authoring source.

#### Validation

- Tier 0 for documentation-only authority changes.
- Tier 1 for focused tests if implementation changes behavior or removes APIs.
- Tier 2 before completion if code or public contracts change.

#### Direct Documentation Impact

```text
docs/specs/code-first-semantic-model-architecture.md
docs/architecture/code-first-domain-projection-pipeline.md
docs/decisions/code-first-only-model-source.md
```

#### Deferred Documentation Impact

```text
README.md
public-docs/getting-started.md
public-docs/packages.md
public-docs/release-notes.md
docs/SPECS.md index update
docs/DECISIONS.md index update
docs/ARCHITECTURE.md index update if present
```

### Focus Area 2 — Model Source and Persistence Boundaries

#### Intent

Separate model generation from model persistence.

#### Required Authority

```text
docs/specs/code-first-semantic-model-architecture.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-compile-time-generator.md
```

#### Implementation Requirements

- The canonical model is generated from code by runtime extraction or compile-time generation.
- Loading a persisted model snapshot is allowed without access to the codebase.
- Persisted snapshots are not an external schema authoring path.
- Runtime model editing is unsupported.
- Builder APIs remain implementation/support infrastructure, not a consumer editing product.

#### Validation

- Tier 1 if persistence/load behavior or tests change.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/code-first-semantic-model-architecture.md
```

#### Deferred Documentation Impact

```text
public-docs/nuget/*.md if package quickstarts mention persistence or JSON Schema import
```

### Focus Area 3 — Semantic Primitives and Attribute Extensibility

#### Intent

Make the core semantic primitive system explicit and extensible.

#### Required Authority

```text
docs/specs/code-first-semantic-model-architecture.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-annotations.md
```

#### Implementation Requirements

- Define canonical semantic primitives such as entity, value object, key, relationship, display name, description, format, constraints, and category.
- Allow custom attributes to alias core primitives or carry domain-specific metadata.
- Define transformation rules for deriving domain metadata from core semantics.
- Emit diagnostics when domain metadata cannot be derived safely.

#### Validation

- Tier 1 for extraction/generator tests when attributes are changed.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/code-first-semantic-model-architecture.md
docs/TERMINOLOGY.md
```

#### Deferred Documentation Impact

```text
package README configuration references
public-docs/samples/*.md if samples are updated
```

### Focus Area 4 — Query and Inspection Development Loop

#### Intent

Support the intended development loop:

```text
Annotate types
Run test or console sample
Inspect diagnostics and model output
Adjust annotations or transformation configuration
Repeat
```

#### Required Authority

```text
docs/specs/code-first-semantic-model-architecture.md
docs/specs/type-model-core.md
```

#### Implementation Requirements

- Define query API direction around strongly typed access first, with string fallback.
- Support model lookup by CLR type, property expression, and canonical string identifier.
- Define text inspection output for semantic model summaries, diagnostics, transformation results, and domain model summaries.
- Keep inspection deterministic and suitable for tests/snapshots.

#### Validation

- Tier 1 for focused query/inspection tests if implemented.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/code-first-semantic-model-architecture.md
```

#### Deferred Documentation Impact

```text
public-docs/samples.md
package README quickstart
diagnostics documentation if formatter output becomes public contract
```

### Focus Area 5 — Domain-Specific Semantic Models

#### Intent

Require domain integrations to derive domain-specific semantic models before domain-specific functionality.

#### Required Authority

```text
docs/specs/code-first-semantic-model-architecture.md
docs/architecture/code-first-domain-projection-pipeline.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-contract-integration.md
```

#### Implementation Requirements

- JSON Schema must be treated as a projection/export target from code-generated models, not an input source.
- EF Core must derive an EF Core semantic model before configuring `ModelBuilder`.
- Power BI must derive a Power BI semantic model before emitting local metadata.
- System.Text.Json must use imported metadata and resolver customization, not serializer generation.
- Domain models must emit diagnostics for missing, ambiguous, or unsupported derivations.
- End users must be able to configure or replace transformation steps in code.

#### Validation

- Tier 1 for each affected projection package.
- Tier 2 before completion if code changes.
- Tier 3 only when package layout or package consumption behavior changes.

#### Direct Documentation Impact

```text
docs/specs/code-first-semantic-model-architecture.md
docs/architecture/code-first-domain-projection-pipeline.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/json-schema.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/system-text-json.md
public-docs/nuget/*.md
```

### Focus Area 6 — Scope Removal and Public Positioning

#### Intent

Remove or clearly mark unsupported the features that no longer match the product direction.

#### Required Authority

```text
docs/specs/code-first-semantic-model-architecture.md
docs/decisions/code-first-only-model-source.md
docs/PUBLIC-DOCS.md
```

#### Features to Remove or Mark Unsupported

```text
JSON Schema import as canonical model source
runtime canonical model editing
broad website-style docs as a project deliverable
OpenAPI import/export
TypeScript generation
Power BI service integration
PBIX generation
full TOM parity
EF Core database creation/migration execution
custom JSON serializer
standalone JsonEditor package/runtime
internal generator harnesses as public samples
```

#### Validation

- Tier 0 for documentation-only unsupported-scope changes.
- Tier 1/Tier 2 when code or tests change.

#### Direct Documentation Impact

```text
docs/specs/code-first-semantic-model-architecture.md
docs/decisions/code-first-only-model-source.md
```

#### Deferred Documentation Impact

```text
README.md
public-docs/getting-started.md
public-docs/packages.md
public-docs/release-notes.md
```

## Required Acceptance Criteria

M0026 is complete when:

- Code-first-only canonical model source authority is documented.
- JSON Schema import is no longer presented as a supported canonical model source.
- Persisted model snapshots are distinguished from model authoring sources.
- Runtime canonical model editing is explicitly unsupported.
- The architecture requires domain-specific semantic models for JSON Schema, EF Core, Power BI, and System.Text.Json behavior.
- Core semantic primitives and custom attribute extensibility are defined at authority level.
- Query and inspection are defined as core product surfaces for the development loop.
- Transformation/projection terminology is aligned with the new architecture.
- Unsupported features are clearly listed in authority docs.
- Direct authority documents in this milestone are updated.
- Tier 0 documentation validation is performed for documentation-only changes.
- Tier 1/Tier 2 validation is performed for any implementation changes made under this milestone.
- No TBPs, issue templates, non-root README files, implementation patches, generated code, or workflow YAML are introduced by this planning package itself.

## Validation Plan

Use the smallest validation tier that can catch the expected regression.

### Tier 0

Use for documentation-only authority changes:

```sh
./eng/public-docs.sh
```

when public documentation is touched.

### Tier 1

Use focused validation when code changes affect:

```text
DotNet extraction
source generation
query/inspection APIs
domain model derivation
JSON Schema export
EF Core projection
Power BI projection
System.Text.Json resolver behavior
```

### Tier 2

Run before completing implementation work that changes code or public contracts:

```sh
./eng/check.sh
```

### Tier 3

Run only if package layout or package consumption behavior changes:

```sh
./eng/package.sh <version>
./eng/package-smoke.sh <version>
./eng/samples.sh
```

Do not run publish validation. This is not a release publication milestone.

## Direct Documentation Impact

The implementation should directly update:

```text
docs/specs/code-first-semantic-model-architecture.md
docs/architecture/code-first-domain-projection-pipeline.md
docs/decisions/code-first-only-model-source.md
docs/TERMINOLOGY.md
```

and any affected existing spec whose behavior directly contradicts the new authority.

## Deferred Documentation Impact

Leave explicit notes for a later documentation synchronization pass covering:

```text
README.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/ARCHITECTURE.md if present
public-docs/getting-started.md
public-docs/concepts.md
public-docs/packages.md
public-docs/guides/*.md
public-docs/nuget/*.md
public-docs/release-notes.md
public-docs/samples.md
```

Do not perform broad public documentation synchronization in this milestone unless a changed behavior directly requires a consumer-facing correction.
