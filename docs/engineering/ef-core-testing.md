# EF Core Testing

## Purpose

Define the durable repository test architecture for SemanticTypeModel EF Core behavior.

The EF Core product contract lives in `docs/specs/ef-core.md`. This document defines how repository tests and fixtures prove that contract across projection, source generation, EF model finalization, provider behavior, and packed-package consumption.

The goal is systematic compatibility coverage rather than accumulating milestone-specific regression tests.

## Core Principle

Boundary-crossing EF behavior must be tested through the boundary where it can fail.

A successful result at one layer does not imply success at another:

```text
canonical semantics
    -> provider-neutral relational projection
    -> compile-time manifest and EF generation
    -> generated C# compilation
    -> finalized EF IModel
    -> provider persistence/change tracking
    -> packed NuGet consumer
```

Do not treat manually constructed canonical models as proof that Roslyn nullability, generated source, EF generic API typing, provider metadata, or package analyzer execution are correct.

## Four Validation Layers

### Layer 1 — Relational projection

Proves:

```text
TypeSchemaModel -> EfRelationalModel
```

Use this layer for provider-neutral storage classification, semantic requiredness/nullability, inheritance placement, and projection diagnostics.

Directly constructed canonical models are acceptable when the test is intentionally isolated to this layer.

### Layer 2 — Generation

Proves:

```text
annotated CLR
    -> SemanticTypeModel.Generators
    -> semantic manifest
    -> SemanticTypeModel.EFCore.Generators
    -> generated EF source
    -> successful C# compilation
```

Successful mapping scenarios must normally start from real annotated CLR source and use the real generator chain.

Generated-source compilation is the primary success assertion. Text assertions should be limited to contractually significant generated shape or deterministic output.

Synthetic manifests remain appropriate for isolated generator diagnostics such as malformed payloads, unsupported manifest versions, suite-version mismatch, missing CLR members, duplicate ownership, and generated-name collisions.

Do not use synthetic manifests as the only proof of a successful mapping shape.

### Layer 3 — Final EF model

Proves:

```text
generated configuration
    -> real DbContext/ModelBuilder
    -> EF model finalization
    -> final IModel metadata
```

Use this layer for converter/comparer metadata, property nullability, provider CLR type, keys, inheritance, composition, hooks, and interactions with EF conventions.

### Layer 4 — Provider behavior

Proves:

```text
finalized model
    -> SQLite schema
    -> save/load/update
    -> change tracking
```

SQLite is the repository's default provider-backed compatibility boundary unless a provider-specific contract explicitly requires another provider.

Where nullability or conversion/comparison semantics matter, persistence tests must exercise state transitions rather than only one non-null round trip.

## Storage and Nullability Matrix

For every supported storage strategy where property nullability is meaningful, tests must deliberately cover required and nullable property use rather than relying on incidental overlap between unrelated tests.

Minimum matrix:

| Storage shape | Required property | Nullable property |
|---|---:|---:|
| direct scalar | required | required |
| enum/string conversion | required | required |
| supported strong scalar/identifier | required | required |
| `System.Uri` | required | required |
| binary | required | required where the CLR/storage contract supports null |
| owned JSON object/value kind | required | required |
| owned JSON collection/value-kind collection | required | required |
| extension data JSON | as defined by the semantic contract | required where supported |

Collection **property/container** nullability is in scope.

M0064 does not introduce a new semantic contract for nullable collection **items**. If implementation requires a new rule for element nullability, return that issue to planning.

## Required Layer Coverage

Not every case requires a provider round trip, but every important storage family must have intentional layer coverage.

| Behavior | L1 projection | L2 generation/compile | L3 final model | L4 provider |
|---|---:|---:|---:|---:|
| direct scalar required/nullable | yes | yes | representative | representative |
| enum required/nullable | yes | yes | yes | representative |
| strong scalar required/nullable | yes | yes | yes | representative |
| URI required/nullable | yes | yes | yes | representative |
| binary required/nullable where supported | yes | yes | yes | representative |
| owned JSON object required/nullable | yes | yes | yes | yes |
| owned JSON collection required/nullable | yes | yes | yes | yes |
| extension data JSON | yes | yes | yes | yes where persistence semantics matter |
| TPT/member placement | yes | yes | yes | yes |
| multiple selected semantic models + manual entities | not required | yes | yes | yes |
| manifest/version/name/ownership diagnostics | not required | yes | not required | not required |

The matrix is a coverage contract, not a requirement to create one test method per cell. Parameterized cases and shared fixtures are preferred where they keep failures attributable.

## Nullable Owned JSON Regression Contract

The published 4.0.0 regression exposed a generated C# type mismatch for nullable owned JSON properties.

For a nullable reference property such as:

```csharp
public WebServiceSourceConfiguration? Configuration { get; set; }
```

the generated EF configuration must compile against EF APIs whose model-side generic type is the declared nullable property type.

The test system must prove all of these independently:

1. relational projection records the property as nullable;
2. the complete generator chain preserves the nullable reference type in generated generic API usage where the generic argument represents the property CLR type;
3. generated source compiles with nullable reference types enabled and repository warning policy active;
4. the finalized EF model reports an optional property with the expected converter/comparer metadata;
5. SQLite persistence supports:
   - insert/reload `null`;
   - `null -> value`;
   - `value -> changed value`;
   - `value -> null`.

Storage classification may normalize/unpack a CLR type internally. That classification step must not erase the declared property nullability needed by generated C# APIs.

The same principle applies to nullable owned JSON collection properties.

## Fixture Architecture

### Valid compatibility fixtures

Successful mapping scenarios should normally originate from annotated CLR fixtures.

The same semantic scenario should be reusable across as many applicable layers as practical so that CLR source, manifest content, generated C#, EF metadata, and provider behavior cannot drift into unrelated parallel representations.

Avoid maintaining separate hand-authored CLR and canonical versions of the same valid scenario unless a layer-isolation test specifically needs it.

### Hand-built canonical fixtures

Use directly constructed `TypeSchemaModel` fixtures for:

- pure provider-neutral derivation tests;
- malformed or otherwise impossible canonical states;
- focused tests where code-first acquisition would obscure the behavior under test.

They are not substitutes for generator/provider compatibility coverage.

### Synthetic manifest fixtures

Use synthetic manifests for EF generator error isolation and manifest-contract diagnostics.

They are not the primary success path for supported CLR mapping scenarios.

### Compatibility fixtures versus real-world fixtures

Keep two purposes distinct:

**Compatibility fixtures** are small, regular, and matrix-oriented. They exist to cover dimensions such as required/nullable, scalar/JSON, singular/collection, base/derived, and shared value-kind reuse.

**Real-world fixtures** are intentionally richer and exist to expose interactions among realistic model features.

Do not rely on a real-world fixture to provide exhaustive matrix coverage.

### Composition fixtures

Keep the multi-model + manual-entity scenario as a dedicated composition smoke test.

It does not replace property-shape compatibility coverage.

### Naming

Durable current test classes, fixtures, and fixture projects should be named for the product behavior they validate, not for the milestone that introduced them.

When M0064 restructures the existing EF test system, normalize touched `M0060...` EF test/fixture names into behavior-oriented names. Do not perform unrelated repository-wide historical renaming.

## Reuse and Inheritance Cases

The compatibility matrix must include enough reuse/inheritance coverage to prove that nullability belongs to a property use, not to the structural value type globally.

At minimum retain or establish cases equivalent to:

```text
optional owned value on a base type
required owned value on a derived type

same structural value type:
    required in one property/entity
    nullable in another
```

Exact fixture types and file organization are implementation-owned.

## Package Smoke

Packed-generator validation must consume the generated `.nupkg` artifacts rather than project references.

The package-smoke consumer must include at least one nullable owned JSON property and prove that:

```text
model package
    -> SemanticTypeModel.Generators from package
    -> manifest
    -> SemanticTypeModel.EFCore.Generators from package
    -> generated consumer compilation
```

succeeds.

Package smoke need not duplicate the full SQLite state-transition suite; its responsibility is packed analyzer/dependency/manifest/generated-compilation integrity.

## Test Organization

The repository may use existing test projects or consolidate fixtures/projects as implementation mechanics require.

The durable architectural requirements are:

- layer responsibilities remain distinguishable;
- compatibility cases are systematic and reusable;
- successful generator cases use the real generator chain;
- provider tests use real finalized EF models;
- packed analyzer behavior is tested from packages;
- broad composition smoke remains separate from the property compatibility matrix;
- failures remain attributable to a specific layer/case.

Do not create a new project solely because a matrix exists if the current projects can represent the layers clearly.

## Completion Discipline

For an EF bug that crosses generation/provider/package boundaries:

1. reproduce at the earliest failing real boundary;
2. add the smallest focused regression;
3. add or repair the relevant matrix case so the family is covered systematically;
4. run the required layer tests;
5. run Tier 2;
6. run packed-package validation when analyzer/package behavior is affected.

A one-off regression test does not replace the matrix obligation.
