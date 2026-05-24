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
- diagnostics model: `SchemaDiagnostic` with severity, code, message, model path, source, projection target.

## Invariants

- Finalized models are immutable to consumers.
- Type lookup is id-based and stable across runtime usage.
- Requiredness, nullability, and collection cardinality are distinct.
- Annotation storage is separate from core semantic meaning.
- Transformations and projections are reusable and independent from concrete projection targets.
- Unsupported/lossy projection cases are diagnosable.

## Transformation and Projection Contracts

Canonical contracts support runtime and compile-time usage through:

- `ISchemaTransformation.TransformAsync(TypeSchemaModelBuilder, SchemaTransformContext)`
- `ISchemaProjection<T>.Project(TypeSchemaModel, SchemaProjectionContext)`

These contracts must preserve projection independence and diagnostic emission capability.

## Examples

The required examples are represented at contract level in short-running tests:

- form/editor object;
- EF-style entity model;
- Power BI semantic model candidate;
- JSON Schema composition (`$defs`, `$ref`, `oneOf`, `allOf`, annotation-preserved keywords).
