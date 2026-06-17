# Type Schema Model

## Status

Superseded for M0038 model contracts by `docs/specs/model-surface-unification.md`.

## Contract

The canonical semantic type model public contracts are represented by `SemanticTypeModel.Abstractions.Model` after M0038.

The supported model family includes `TypeSchemaModel`, typed identifiers, annotation bags, `TypeDefinition` subtypes, property definitions, key definitions, relationship definitions, constraints, scalar metadata, enum metadata, composition metadata, and typed references.

The legacy shape graph that used `TypeShape`, `ObjectShape`, `PropertyShape`, `ShapeRef`, and `SchemaAnnotation` is removed from shipped source and public API compatibility documentation after M0038.

## Traversal

Consumers should traverse `TypeSchemaModel.Types` and use `TypeSchemaModel.TypesById`, `TryGetType`, or `GetType` for deterministic lookup by `TypeId`.

Projection packages should derive target-owned domain semantic models from the unified model surface rather than adapting through the removed legacy shape graph.
