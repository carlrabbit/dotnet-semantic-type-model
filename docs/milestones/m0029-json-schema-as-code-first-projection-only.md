# M0029: JSON Schema as Code-First Projection Only

## Status

Planned.

## Maturity Mode

Domain package architecture implementation for a public package set.

The repository has public packages, package README sources, public samples, public API compatibility documentation, and package validation. This milestone changes the public role of `SemanticTypeModel.JsonSchema` from a general import/export adapter to the reference implementation of the M0026/M0028 domain semantic model architecture.

## Task Mode

Milestone implementation routing.

This milestone implements JSON Schema as the first domain-specific semantic model projection after M0026, M0027, and M0028. It does not introduce TBPs, issue templates, workflow YAML, non-root README files, generated code files, or broad public-documentation rewrites in this planning package.

## Goal

After M0029, a consumer can:

```text
annotate C# code;
generate or extract a canonical Semantic Type Model;
query and inspect diagnostics;
derive a JsonSchemaSemanticModel through configurable transformations;
inspect derivation diagnostics and trace;
export deterministic JSON Schema Draft 2020-12;
do all of this without using JSON Schema import as a canonical model source.
```

The architectural pipeline is:

```text
Code-generated canonical semantic model
  -> JSON Schema derivation transformations
  -> JsonSchemaSemanticModel
  -> JSON Schema Draft 2020-12 export
```

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
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
```

Read these only when the selected focus area touches the relevant component:

```text
docs/specs/type-model-core.md
docs/specs/type-schema-model.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-annotations.md
docs/specs/diagnostics.md
docs/PUBLIC-DOCS.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
public-docs/guides/json-schema.md
public-docs/release-notes.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Define and implement `JsonSchemaSemanticModel` or equivalent package-owned JSON Schema domain semantic model.
- Define JSON Schema derivation transformations using the M0028 domain derivation contract.
- Treat JSON Schema as an export/projection target, not a canonical model source.
- Remove, obsolete, or clearly mark JSON Schema import APIs as legacy/internal according to compatibility constraints.
- Export deterministic JSON Schema Draft 2020-12 from the JSON Schema domain semantic model.
- Support object, scalar, required, nullable, array, dictionary, enum, format, title, description, annotations, `$defs`, and `$ref` export.
- Support simple `oneOf` and `anyOf` export for code-derived alternatives.
- Emit diagnostics for unsupported advanced composition or unsupported export shapes.
- Integrate with M0027 query/inspection and M0028 transformation trace behavior.
- Update or add tests for code-first generated models and deterministic export.
- Rewrite or replace JSON Schema samples so the primary sample is code-first projection, not roundtrip/import.

### Out of Scope

- JSON Schema import as canonical model creation.
- Public JSON Schema roundtrip workflow.
- Full JSON Schema Draft 2020-12 parity.
- OpenAPI import/export.
- JSON Editor runtime.
- Runtime canonical model editing.
- JSON validation engine.
- Schema registry.
- Remote references or network loading.
- `dynamicRef` / `dynamicAnchor`.
- `if` / `then` / `else`.
- `not`.
- `dependentSchemas`.
- `unevaluatedProperties` semantics.
- Full discriminator semantics.
- Arbitrary nested boolean-schema composition.
- Full `allOf` reduction.
- Release publication.

## Package Boundary

| Package | Responsibility |
|---|---|
| `SemanticTypeModel.JsonSchema` | JSON Schema domain semantic model, JSON Schema derivation transformations, diagnostics, and JSON Schema export. |
| `SemanticTypeModel.Core` | Query, inspection, transformation, and derivation result infrastructure. |
| `SemanticTypeModel.DotNet` | Code extraction metadata consumed by code-first samples and generated models. |
| `SemanticTypeModel.Generators` | Generated providers consumed by code-first JSON Schema samples. |

`SemanticTypeModel.JsonSchema` must not become a general schema import framework.

## Focus Areas

### Focus Area 1 — Public Source Boundary and Import Removal

#### Intent

Stop presenting JSON Schema import as a supported source for canonical models.

#### Required Authority

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/code-first-semantic-model-architecture.md
docs/decisions/code-first-only-model-source.md
```

#### Implementation Requirements

- Identify public import APIs and public docs that imply JSON Schema import creates canonical models.
- Remove, obsolete, or mark import APIs unsupported according to compatibility policy.
- Keep retained import code internal, test-only, or legacy/compatibility only.
- Ensure package samples do not use import as their primary flow.
- Ensure diagnostics and API messages point users to code-first generation and projection.

#### Validation

- Tier 1:
  - affected JsonSchema tests;
  - package public API tests if APIs are removed or obsoleted;
  - sample validation if samples change.
- Tier 2 before completion if code or public contracts change.
- Tier 3 only if package layout or package consumption behavior changes.

#### Direct Documentation Impact

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
```

#### Deferred Documentation Impact

```text
README.md
public-docs/getting-started.md
public-docs/guides/json-schema.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
public-docs/samples.md
public-docs/release-notes.md
```

### Focus Area 2 — JSON Schema Domain Semantic Model

#### Intent

Create the JSON Schema package-owned domain semantic model.

#### Required Authority

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/architecture/code-first-domain-projection-pipeline.md
```

#### Implementation Requirements

- Add `JsonSchemaSemanticModel` or equivalent.
- Represent document metadata, root schema, `$defs`, references, object schemas, scalar schemas, arrays, dictionaries, enums, constraints, annotations, and simple composition.
- Keep the domain model independent from `System.Text.Json` runtime contract customization.
- Keep the domain model explicit enough that exporter functionality does not rely on scattered ad hoc annotation lookups.
- Provide inspection support or use M0027 inspection hooks.

#### Validation

- Tier 1:
  - domain model construction tests;
  - domain model inspection tests;
  - deterministic ordering tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/json-schema-domain-model-and-export.md
```

#### Deferred Documentation Impact

```text
public-docs/nuget/SemanticTypeModel.JsonSchema.md
```

### Focus Area 3 — JSON Schema Derivation Pipeline

#### Intent

Derive the JSON Schema domain semantic model from a code-generated canonical model using M0028.

#### Required Authority

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
```

#### Implementation Requirements

- Expose a domain derivation API equivalent to `DeriveJsonSchemaModel`.
- Return a `SemanticDerivationResult<JsonSchemaSemanticModel>` or equivalent.
- Reuse or compose core default transformations where appropriate.
- Add JSON Schema-specific derivation transformations.
- Allow users to configure, add, remove, replace, and order JSON Schema transformations in code.
- Accumulate diagnostics and trace entries.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
var result = model.DeriveJsonSchemaModel(options =>
{
    options.UseDefaultTransformations();
    options.Transformations.Replace(new MyJsonSchemaNamingTransformation());
});

result.Diagnostics.ThrowIfErrors();
var document = JsonSchemaExporter.Export(result.Model);
```

#### Validation

- Tier 1:
  - derivation pipeline tests;
  - transformation replacement tests;
  - diagnostic accumulation tests;
  - generated model derivation tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/json-schema-domain-model-and-export.md
```

#### Deferred Documentation Impact

```text
consumer sample docs if derivation customization is demonstrated
```

### Focus Area 4 — Baseline Export Features

#### Intent

Export useful deterministic Draft 2020-12 JSON Schema from the domain semantic model.

#### Required Authority

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
```

#### Required Export Support

```text
object types
scalar types
required properties
nullable properties
arrays
dictionaries
enums
format
title
description
annotations
$defs
$ref
deterministic property order
deterministic $defs order
```

#### Validation

- Tier 1:
  - exporter tests for each baseline feature;
  - deterministic JSON output tests;
  - code-first generated model export test.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/json-schema.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
```

### Focus Area 5 — Simple `oneOf` and `anyOf` Export

#### Intent

Support idiomatic JavaScript-facing alternatives without implementing full JSON Schema composition semantics.

#### Required Authority

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
```

#### Implementation Requirements

- Support simple `oneOf` for exclusive semantic alternatives.
- Support simple `anyOf` for non-exclusive alternatives.
- Use deterministic `$ref` branches into `$defs` where possible.
- Preserve deterministic branch ordering.
- Emit diagnostics for:
  - empty alternatives;
  - unresolved alternatives;
  - unsupported inline complex alternatives;
  - unsupported nested composition;
  - unsupported discriminator behavior.
- Do not reintroduce JSON Schema import.

#### Validation

- Tier 1:
  - simple `oneOf` export tests;
  - simple `anyOf` export tests;
  - deterministic branch order tests;
  - unsupported composition diagnostics tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/json-schema.md
sample docs if composition is demonstrated
```

### Focus Area 6 — Code-First Sample and Package README Readiness

#### Intent

Make the consumer path clear enough to use without JSON Schema import.

#### Required Authority

```text
docs/specs/json-schema-domain-model-and-export.md
docs/engineering/samples.md
docs/PUBLIC-DOCS.md
```

#### Implementation Requirements

- Ensure the primary JSON Schema sample is code-first.
- The sample should:
  - annotate C# types;
  - generate or extract a canonical model;
  - inspect diagnostics or semantic text;
  - derive the JSON Schema domain model;
  - export deterministic JSON Schema.
- Remove or de-emphasize roundtrip/import samples from public status.
- Update package README source only if implementation changes consumer-facing quickstart content in the same PR.

#### Validation

- Tier 1:
  - affected sample run;
  - package-based sample validation if sample packages changed.
- Tier 2 before completion if code changes.
- Tier 3 only if package artifacts/package README generation changes.

#### Direct Documentation Impact

```text
sample code comments
affected sample documentation only if sample paths/behavior change
```

#### Deferred Documentation Impact

```text
public-docs/samples.md
public-docs/samples/*.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
README.md
```

## Required Acceptance Criteria

M0029 is complete when:

- JSON Schema import is no longer a supported canonical model creation path.
- Public samples do not present JSON Schema import/roundtrip as the primary JSON Schema workflow.
- A JSON Schema domain semantic model exists.
- JSON Schema derivation uses the M0028 domain derivation contract.
- Derivation returns model, diagnostics, and transformation trace.
- Users can configure JSON Schema derivation transformations in code.
- JSON Schema export reads from the JSON Schema domain semantic model.
- Baseline deterministic export works for object, scalar, required, nullable, array, dictionary, enum, format, title, description, annotations, `$defs`, and `$ref`.
- Simple `oneOf` export works for exclusive code-derived alternatives.
- Simple `anyOf` export works for non-exclusive code-derived alternatives.
- Unsupported advanced composition emits diagnostics.
- Export output is deterministic.
- Code-first generated model tests cover JSON Schema derivation and export.
- Tier 2 validation passes, or any inability to run it is explicitly reported with the exact lower-tier validation performed.
- No TBPs, issue templates, non-root README files, workflow YAML, broad public-doc rewrites, or generated code files are introduced by the planning package itself.

## Validation Plan

Use the smallest validation tier that can catch the expected regression.

### Tier 1

Use focused validation for:

```text
JsonSchema domain model tests
JsonSchema derivation tests
JsonSchema exporter tests
simple oneOf/anyOf tests
unsupported composition diagnostics tests
code-first generated model export tests
affected sample tests
```

Expected command shape:

```sh
./eng/test-project.sh <json-schema-test-project>
./eng/test-filter.sh <json-schema-domain-or-export-filter>
./eng/check-affected.sh src/SemanticTypeModel.JsonSchema tests samples/code-first-json-schema
```

Use actual repository project names after inspecting the solution.

### Tier 2

Run before completing implementation work:

```sh
./eng/check.sh
```

### Tier 3

Run only if package layout, package README generation, or package consumption behavior changes:

```sh
./eng/package.sh <version>
./eng/package-smoke.sh <version>
./eng/samples.sh
```

This is not a release publication milestone; do not run publish validation.

## Direct Documentation Impact

The implementation should directly update:

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
```

Update related existing specs only when implementation changes contradict current authority:

```text
docs/specs/json-schema-adapter.md
docs/specs/code-first-semantic-model-architecture.md
docs/specs/type-model-transformation-and-domain-derivation.md
```

## Deferred Documentation Impact

Leave explicit notes for a later documentation synchronization pass covering:

```text
docs/SPECS.md index entry for the new spec
docs/MILESTONES.md index entry for M0029
README.md
public-docs/getting-started.md
public-docs/guides/json-schema.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/release-notes.md
docs/DECISIONS.md only if a new decision is later created
```

Do not perform broad public documentation synchronization as part of this implementation milestone unless a consumer-facing behavior change directly requires it.
