# M0032: Core Semantic Vocabulary and Envelope Semantics

## Status

Planned.

## Maturity Mode

Core semantic-authoring usability implementation for a public package set.

The repository has code-first authoring, public packages, public samples, domain semantic model projections, and public documentation surfaces. This milestone improves the authoring contract by documenting the core semantic vocabulary and by adding the `Envelope` semantic as a projection-neutral primitive.

## Task Mode

Milestone implementation routing and core semantic contract implementation.

This milestone defines user-facing core semantics, their usage guidance, and the new `Envelope` semantic. It does not introduce TBPs, issue templates, workflow YAML, non-root README files, generated code files, or broad public-documentation rewrites in this planning package.

## Goal

After M0032, a consumer can answer:

```text
Which core semantics exist?
What does each semantic mean?
When should I use a core semantic instead of target-specific metadata?
What will JSON Schema, EF Core, and Power BI derive from each semantic?
How do I mark an envelope/wrapper type and its payload?
When does an envelope become the projection root instead of the wrapped payload?
```

The target authoring flow is:

```text
Annotated .NET code
  -> core semantic vocabulary
  -> canonical semantic model
  -> domain semantic models
  -> JSON Schema / EF Core / Power BI outputs
```

## Required Authority

Read these documents before implementing any focus area:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/SPECS.md
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-core.md
docs/specs/type-model-annotations.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-transformation-and-domain-derivation.md
```

Read these only when the selected focus area touches the relevant component:

```text
docs/specs/code-first-semantic-model-architecture.md
docs/specs/type-schema-model.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-query-and-inspection.md
docs/specs/diagnostics.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/type-model-projection-capabilities.md
docs/PUBLIC-DOCS.md
public-docs/guides/core-semantics.md
public-docs/nuget/*.md
public-docs/release-notes.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Add a core semantic vocabulary specification.
- Define for each core semantic: name, kind, description, best-use guidance, avoid guidance, projection implications, example usage, and diagnostic/ambiguity notes.
- Add terminology for core semantic vocabulary and envelope semantics.
- Add `Envelope`, `EnvelopePayload`, and `EnvelopeMetadata` as core semantic vocabulary entries.
- Add .NET attribute vocabulary for envelope semantics.
- Define canonical annotation keys for envelope semantics.
- Define envelope invariants and diagnostics.
- Define projection implications for JSON Schema, EF Core, and Power BI.
- Add short-running tests for extraction/generation, transformation normalization, diagnostics, and inspection.
- Provide implementation guidance for keeping projection-specific metadata separate from core semantics.

### Out of Scope

- Runtime editing of canonical models.
- New target-specific projection implementation work except where envelope semantics require a small projection policy hook.
- Broad public documentation rewrite.
- New issue templates.
- TBPs.
- Release publication.
- Full tutorial authoring pass.
- New package creation.

## Package Boundary

| Package | Responsibility |
|---|---|
| `SemanticTypeModel.Abstractions` | Shared envelope/core semantic primitives only if required for public contracts. |
| `SemanticTypeModel.Core` | Core semantic vocabulary constants, normalization, invariants, diagnostics, and inspection behavior. |
| `SemanticTypeModel.DotNet` | Envelope/core semantic attribute extraction metadata. |
| `SemanticTypeModel.Generators` | Envelope/core semantic generated metadata support. |
| Domain packages | Consume core envelope semantics through domain derivation policies, without redefining the core meaning. |

## Focus Areas

### Focus Area 1 — Core Semantic Vocabulary Reference

#### Intent

Create an authoritative vocabulary reference so users know what to annotate and why.

#### Required Authority

```text
docs/specs/core-semantic-vocabulary.md
docs/TERMINOLOGY.md
docs/specs/type-model-core.md
docs/specs/type-model-annotations.md
```

#### Implementation Requirements

- Add `docs/specs/core-semantic-vocabulary.md`.
- Include entries for existing core semantics and the new envelope semantics.
- Each entry must include description, best-used guidance, avoid guidance, projection implications, example, and diagnostics/ambiguity notes.
- Separate core semantics from projection-specific metadata.
- Avoid introducing undocumented terms outside `docs/TERMINOLOGY.md`.

#### Validation

- Tier 0 for documentation-only content.
- Tier 1 if spec tests or vocabulary validation tooling exists or is added.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/core-semantic-vocabulary.md
docs/TERMINOLOGY.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/core-semantics.md
README.md
public-docs/getting-started.md
public-docs/nuget/*.md
```

### Focus Area 2 — Envelope Core Semantic

#### Intent

Add a projection-neutral semantic for wrapper types that carry, manage, version, transport, persist, audit, or contextualize another semantic payload.

#### Required Authority

```text
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-core.md
docs/specs/type-model-annotations.md
```

#### Implementation Requirements

- Add core semantic entries: `Envelope`, `EnvelopePayload`, and `EnvelopeMetadata`.
- Define envelope invariants:
  - an envelope is a wrapper boundary;
  - an envelope normally has exactly one payload;
  - envelope metadata describes the envelope lifecycle/context, not the payload domain state;
  - envelope semantics do not erase payload semantics;
  - projection policy decides whether the envelope or payload is the projection root for a target.
- Add canonical annotations or primitives required to represent envelope semantics.
- Add diagnostics for missing, duplicate, ambiguous, or unsupported envelope payloads.

#### Validation

- Tier 1: core vocabulary tests; envelope normalization tests; diagnostics tests; inspection tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-core.md
docs/specs/type-model-annotations.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/core-semantics.md
public-docs/diagnostics/*.md if new public diagnostics are added
```

### Focus Area 3 — .NET Attribute Vocabulary for Envelope

#### Intent

Make envelope semantics usable in code-first authoring.

#### Required Authority

```text
docs/specs/type-model-dotnet-attributes.md
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-compile-time-generator.md
```

#### Implementation Requirements

- Add attributes equivalent to `SemanticEnvelopeAttribute`, `SemanticEnvelopePayloadAttribute`, and `SemanticEnvelopeMetadataAttribute`.
- Support type-level envelope marking.
- Support property-level payload marking.
- Support property-level metadata marking.
- Support optional envelope purpose where it can be projection-neutral.
- Avoid target-specific storage decisions in core attributes.
- Extraction/generation must preserve attribute intent and transformations must derive canonical envelope meaning.
- Invalid usage emits diagnostics.

#### Validation

- Tier 1: attribute extraction tests; generator tests if generated metadata changes; invalid target diagnostics tests; duplicate payload diagnostics tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-dotnet-attributes.md
docs/specs/core-semantic-vocabulary.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/core-semantics.md
public-docs/nuget/SemanticTypeModel.DotNet.md
public-docs/nuget/SemanticTypeModel.Generators.md
```

### Focus Area 4 — Projection Policy Implications

#### Intent

Make it clear how envelope semantics affect JSON Schema, EF Core, and Power BI without redefining those domain specs in this milestone.

#### Required Authority

```text
docs/specs/core-semantic-vocabulary.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
```

#### Implementation Requirements

- Define projection-neutral envelope policy concepts: envelope-as-root, payload-as-root, payload embedded, payload reference, payload serialized, and payload opaque.
- Domain packages may expose target-specific options, but the core semantic meaning remains unchanged.
- JSON Schema implication: envelope root exports wrapper object; payload root exports payload schema; payload may be `$ref`, inline, or serialized based on domain option.
- EF Core implication: envelope may become persistence/cache entity; metadata maps as columns; payload may be owned, JSON/serialized, converted, ignored, or separately mapped based on explicit target policy.
- Power BI implication: envelope metadata may become reporting columns; payload may be ignored, flattened, or serialized based on explicit analytical policy.
- Unsupported or ambiguous projection root choices emit diagnostics.

#### Validation

- Tier 1: projection policy tests if domain packages are touched; diagnostics tests for ambiguous root selection; no broad projection rewrites unless selected.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/core-semantic-vocabulary.md
```

Update domain specs only when implementation changes their behavior directly:

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/core-semantics.md
target-specific public guides when examples are added
```

### Focus Area 5 — Diagnostics and Inspection

#### Intent

Make semantic vocabulary and envelope behavior inspectable and diagnosable.

#### Required Authority

```text
docs/specs/core-semantic-vocabulary.md
docs/specs/diagnostics.md
docs/specs/type-model-query-and-inspection.md
```

#### Implementation Requirements

- Add diagnostics for envelope with no payload, envelope with multiple payloads without explicit policy, payload marker outside envelope, envelope metadata outside envelope, envelope payload not represented in canonical model, envelope and payload both selected as projection root without explicit policy, and unsupported payload representation for a target.
- Add deterministic inspection text for envelope semantics.
- Ensure diagnostics are queryable by code, stage, path, and projection target where applicable.

#### Validation

- Tier 1: diagnostic ID uniqueness tests; envelope diagnostics tests; inspection snapshot tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/core-semantic-vocabulary.md
docs/specs/diagnostics.md if diagnostic ranges or IDs change
public-docs/diagnostics/*.md if diagnostics are public
```

#### Deferred Documentation Impact

```text
public-docs/guides/core-semantics.md
public-docs/diagnostics.md
```

## Required Acceptance Criteria

M0032 is complete when:

- `docs/specs/core-semantic-vocabulary.md` exists and is authoritative.
- Core semantic entries include description, best-use guidance, avoid guidance, projection implications, examples, and diagnostic notes.
- Existing core semantics are documented at least to the baseline listed by the vocabulary spec.
- `Envelope`, `EnvelopePayload`, and `EnvelopeMetadata` are defined as core semantics.
- Envelope semantics are projection-neutral.
- Envelope does not erase payload semantics.
- Projection policy can choose envelope-as-root or payload-as-root explicitly.
- .NET attribute vocabulary supports envelope, payload, and metadata declaration.
- Extraction/generation preserves envelope attribute intent.
- Transformations derive canonical envelope semantics and diagnostics.
- Missing, duplicate, or misplaced envelope payload/metadata markers emit diagnostics.
- Inspection output includes envelope semantics deterministically.
- JSON Schema, EF Core, and Power BI implications are documented in the vocabulary spec.
- Tier 2 validation passes if code changes, or inability to run it is explicitly reported with exact lower-tier validation performed.
- No TBPs, issue templates, non-root README files, workflow YAML, broad public-doc rewrites, or generated code files are introduced by the planning package itself.

## Validation Plan

Use the smallest validation tier that can catch the expected regression.

### Tier 0

Use for documentation-only portions:

```sh
dotnet format --verify-no-changes --include docs/milestones/m0032-core-semantic-vocabulary-and-envelope-semantics.md docs/specs/core-semantic-vocabulary.md docs/TERMINOLOGY.md docs/specs/type-model-dotnet-attributes.md
```

Use actual repository-supported documentation validation commands if available.

### Tier 1

Use focused validation for:

```text
core semantic normalization tests
envelope attribute extraction tests
generator metadata tests
envelope diagnostics tests
inspection snapshot tests
domain projection policy tests if domain packages are touched
```

Expected command shape:

```sh
./eng/test-filter.sh <semantic-vocabulary-or-envelope-filter>
./eng/test-project.sh <affected-core-or-dotnet-test-project>
./eng/check-affected.sh src/SemanticTypeModel.Core src/SemanticTypeModel.DotNet src/SemanticTypeModel.Generators tests
```

Use actual repository project names after inspecting the solution.

### Tier 2

Run before completing implementation work when code changes:

```sh
./eng/check.sh
```

### Tier 3

Run only if package layout, package README generation, public API baseline, or package consumption behavior changes:

```sh
./eng/package.sh <version>
./eng/package-smoke.sh <version>
./eng/public-api.sh
./eng/samples.sh
```

This is not a release publication milestone; do not run publish validation.

## Direct Documentation Impact

The implementation should directly update:

```text
docs/specs/core-semantic-vocabulary.md
docs/TERMINOLOGY.md
docs/specs/type-model-dotnet-attributes.md
```

Update these only when implementation changes their behavior directly:

```text
docs/specs/type-model-core.md
docs/specs/type-model-annotations.md
docs/specs/diagnostics.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
```

## Deferred Documentation Impact

Leave explicit notes for a later documentation synchronization pass covering:

```text
docs/SPECS.md
docs/MILESTONES.md
README.md
public-docs/getting-started.md
public-docs/guides/core-semantics.md
public-docs/nuget/*.md
public-docs/samples.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md if new diagnostics are public
public-docs/release-notes.md
```

Do not perform broad public documentation synchronization as part of this implementation milestone unless a consumer-facing behavior change directly requires it.
