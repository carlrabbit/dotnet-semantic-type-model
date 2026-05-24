# Type Model Core Specification

## Purpose

Define the hardened canonical semantic type model contracts and invariants.

## Authority

This specification is authoritative for:

- canonical type-model layers;
- model contracts and required concepts;
- runtime lookup/reference behavior;
- transformation/projection contract intent;
- diagnostics model requirements for model and projection stages.

## Canonical Layers

1. Shape layer (`TypeDefinition`, `TypeKind`, concrete type definitions)
2. Member layer (`PropertyDefinition`, `Cardinality`, `Mutability`)
3. Constraint layer (`ConstraintSet` and typed constraint objects)
4. Semantic layer (`EntitySemantics`, `EntityRole`, keys, relationships, computed members)
5. Projection annotation layer (`Annotation`, `AnnotationBag`, namespaced keys)

## Core Contracts

The hardened model surface includes:

- root model: `TypeSchemaModel` + stable id lookup via `TypeId`;
- core type hierarchy: `TypeDefinition` with `TypeKind` including `Any` and `Never`;
- object model: `ObjectTypeDefinition`, `PropertyDefinition`, `ObjectComposition`;
- scalar model: `ScalarTypeDefinition`, `ScalarKind`, `NumericPrecision`;
- enum model: `EnumTypeDefinition`, `EnumValueDefinition`, `EnumStorageKind`;
- collection/map model: `ArrayTypeDefinition`, `DictionaryTypeDefinition`;
- composition model: `UnionTypeDefinition` (`OneOf`/`AnyOf`) and `IntersectionTypeDefinition`;
- reference model: stable `TypeRef(TypeId)` for recursive/reference graphs;
- semantics model: `EntitySemantics`, `KeyDefinition`, `RelationshipDefinition`, computed members;
- diagnostics model: `SchemaDiagnostic` with severity, code, message, stage, model path, source, projection target, and related model paths.

## Invariants

- Finalized models are immutable to consumers.
- Type lookup is id-based and stable across runtime usage.
- Requiredness, nullability, and collection cardinality are distinct.
- Annotation storage is separate from core semantic meaning.
- Transformations and projections are reusable and independent from concrete projection targets.
- Unsupported/lossy projection cases are diagnosable.

## Transformation and Projection Contracts

Canonical contracts support runtime and compile-time usage through:

- `ISchemaTransformation.TransformAsync(TypeSchemaModelBuilder, SchemaTransformContext, CancellationToken)`
- `ISchemaProjection<T>.Project(TypeSchemaModel, SchemaProjectionContext)`

These contracts must preserve projection independence and diagnostic emission capability.

## Transformation Pipeline Contract

The hardened runtime pipeline is represented by `SchemaTransformationPipeline` plus:

- `SchemaPipelineOptions` for execution policy and initial diagnostics;
- `SchemaPipelineResult` for the transformed model and accumulated diagnostics;
- `SchemaTransformContext` for per-transform execution state;
- `SchemaDiagnosticSink` for structured diagnostic emission;
- `AnnotationPolicy` for duplicate-key and malformed-key behavior.

Pipeline rules:

- transformations run sequentially in configured order;
- the pipeline clones the input model before execution and returns a fresh output snapshot;
- diagnostics accumulate across executed transforms;
- by default, execution stops before the next transform after an error diagnostic is recorded;
- `ContinueOnError` allows later transforms to run after errors;
- warning promotion happens through the diagnostic sink, not by mutating previously emitted diagnostics;
- M0005 does not introduce parallel transform execution.

## Diagnostic Model

Structured diagnostics must be machine-queryable by code, severity, stage, and path.

`SchemaDiagnostic` carries:

- `Code`;
- `Severity`;
- `Message`;
- `Stage`;
- `ModelPath`;
- `Source` when a source-format path/span is known;
- `PipelineStage` when emitted from a named transform stage;
- `ProjectionTarget` when target-specific;
- `RelatedModelPaths` when a diagnostic refers to multiple model locations.

`SchemaDiagnosticStage` values are:

- `Import`
- `Transformation`
- `Validation`
- `Export`
- `Projection`

## Model Path Format

Model paths use stable slash-separated segments rooted at `/types`.

- Paths are human-readable and machine-comparable.
- Path segments escape `~` as `~0` and `/` as `~1`.
- Canonical examples:
  - `/types/Customer`
  - `/types/Customer/properties/email`
  - `/types/Customer/keys/Primary`
  - `/types/Order/relationships/Customer`
  - `/types/SalesFact/computedMembers/TotalAmount`

## Examples

The required examples are represented at contract level in short-running tests:

- form/editor object;
- EF-style entity model;
- Power BI semantic model candidate;
- JSON Schema composition (`$defs`, `$ref`, `oneOf`, `allOf`, annotation-preserved keywords).
