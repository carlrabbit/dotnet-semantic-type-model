# M0002 - Baseline Semantic Type Model

## Goal

Implement the first usable canonical semantic type model for the SemanticTypeModel repository.

The milestone establishes:

- canonical semantic schema model abstractions;
- runtime query model;
- JSON Schema import/export baseline;
- transformation pipeline infrastructure;
- schema projection infrastructure;
- runtime model construction;
- baseline compile-time integration points.

The milestone must prove that:

```text
JSON Schema -> canonical semantic type model -> JSON Schema
```

is possible without semantic loss for the supported baseline feature set.

## Scope

This milestone includes:

- canonical model abstractions;
- runtime schema model creation;
- JSON Schema Draft 2020-12 baseline support;
- schema traversal/query APIs;
- immutable model representation;
- transformation pipeline contracts;
- baseline transformation execution;
- schema projection contracts;
- baseline JSON Schema exporter;
- runtime model validation;
- baseline documentation and architecture.

## Required Naming

The following names are mandatory:

- Solution: `SemanticTypeModel.slnx`
- Root namespace: `SemanticTypeModel`
- Package prefix: `SemanticTypeModel.*`

## Required Projects

The milestone must implement and wire the following projects:

```text
SemanticTypeModel.Abstractions
SemanticTypeModel.Core
SemanticTypeModel.JsonSchema
```

Optional additional internal projects are allowed if justified.

## Required Concepts

The implementation must introduce the following concepts.

### Canonical model

At minimum:

```text
TypeSchemaModel
TypeShape
ObjectShape
ScalarShape
EnumShape
ArrayShape
DictionaryShape
PropertyShape
SchemaAnnotation
ConstraintSet
```

The model must be immutable after finalization.

### Runtime query model

The model must support:

- lookup by identifier;
- traversal of referenced shapes;
- property inspection;
- annotation inspection;
- constraint inspection;
- graph traversal.

### Transformations

Introduce reusable transformation contracts.

At minimum:

```text
ISchemaTransformation
ISchemaModelSource
ISchemaProjection<T>
```

Transformations must be reusable across runtime and compile-time scenarios.

### JSON Schema support

Implement baseline support for JSON Schema Draft 2020-12.

At minimum:

- object schemas;
- scalar schemas;
- arrays;
- enums;
- required properties;
- nullable semantics;
- `$defs`;
- `$ref`;
- `oneOf` baseline support;
- annotations.

## Required Architecture Decisions

The implementation must follow these architectural rules.

### Canonical model first

The canonical semantic model is the source of truth.

JSON Schema is an adapter layer, not the internal storage format.

### Separation of responsibilities

Separate:

```text
schema acquisition
schema normalization
semantic enrichment
transformation
projection/export
```

### No runtime CLR type generation requirement

The implementation must not require runtime CLR type emission.

The canonical model itself is the runtime representation.

### Runtime-first model access

The runtime query surface is mandatory.

Compile-time generation is optional in this milestone.

### Projection-oriented architecture

The architecture must support future projections to:

- EF Core;
- Power BI metadata models;
- OpenAPI;
- TypeScript;
- documentation systems.

without redesigning the canonical model.

## Required Documentation

Create:

```text
docs/specs/type-schema-model.md
docs/specs/json-schema-adapter.md
docs/architecture/canonical-schema-model.md
docs/architecture/transformation-pipeline.md
docs/decisions/json-schema-as-primary-dialect.md
```

Update:

```text
docs/SPECS.md
docs/ARCHITECTURE.md
docs/DECISIONS.md
```

## Required Tests

Add short-running tests for:

- model construction;
- schema traversal;
- JSON Schema import;
- JSON Schema export;
- roundtrip behavior;
- `$ref` resolution;
- validation failures;
- immutable model behavior.

The tests must avoid:

- network access;
- large schemas;
- timing-sensitive behavior;
- benchmark-like workloads.

## Required Validation

The following commands must succeed:

```text
./eng/build.sh
./eng/test.sh
./eng/check.sh
```

## Non-Goals

This milestone does not implement:

- EF Core integration;
- Power BI integration;
- OpenAPI export;
- source generators;
- Roslyn analyzers;
- advanced polymorphism;
- distributed schema registries;
- runtime IL emission;
- schema persistence.

## Risks

### Overfitting to JSON Schema

The canonical model must remain projection-oriented and not merely mirror JSON Schema.

### Premature compile-time coupling

Compile-time generation must not dominate the architecture.

The runtime canonical model is the primary abstraction.

### Semantic ambiguity

Distinguish clearly between:

- CLR semantics;
- JSON Schema semantics;
- projection-specific semantics.

## Exit Criteria

The milestone is complete when:

- a canonical semantic type model exists;
- runtime querying works;
- JSON Schema Draft 2020-12 baseline import/export works;
- roundtrip tests pass for supported features;
- transformation contracts exist;
- projection contracts exist;
- architecture and specs are documented;
- all validation commands succeed.
