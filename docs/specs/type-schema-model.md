# Type Schema Model Specification

## Purpose

Define the canonical runtime representation of semantic type information used by this repository.

## Authority

This specification is authoritative for:
- the shape model surface;
- model invariants;
- lookup and traversal behavior;
- failure semantics for model access and model construction.

## Canonical Types

The canonical semantic type model consists of:
- `TypeSchemaModel`;
- `TypeShape`;
- `ScalarShape`;
- `EnumShape`;
- `ObjectShape`;
- `ArrayShape`;
- `DictionaryShape`;
- `UnionShape`;
- `PropertyShape`;
- `ShapeRef`;
- `SchemaAnnotation`;
- `ConstraintSet` and `ConstraintEntry`.

## Invariants

- `TypeSchemaModel` is immutable after construction.
- Named shapes are stored in `TypeSchemaModel.Shapes` and are addressable by identifier.
- `TypeSchemaModel.RootIdentifier` may be null.
- `TypeSchemaModel.Root` resolves from `RootIdentifier` when one is present.
- `ShapeRef` represents either a named reference or an inline shape.
- A named reference must resolve through the containing `TypeSchemaModel`.
- `TypeSchemaModelBuilder.Build()` must reject unresolved named references.
- `TypeSchemaModelBuilder.Build()` must reject a missing root shape when `SetRoot` was used.
- `TypeSchemaModel.TraverseAll()` must visit each named shape at most once.
- Inline shapes discovered through traversal are included in traversal results.

## Lookup Behavior

- `TryGetShape(identifier)` returns the named shape or null.
- `GetShape(identifier)` returns the named shape or throws `InvalidOperationException` when absent.
- `ShapeRef.Resolve(model)` returns the referenced named shape or the inline shape.
- `ShapeRef.Resolve(model)` throws `InvalidOperationException` when the reference has neither identifier nor inline shape.

## Traversal Behavior

`TypeSchemaModel.TraverseAll()` performs a graph walk over:
- all named shapes in the model;
- object property type references;
- array item references;
- dictionary value references;
- union option references.

Traversal order is implementation-defined, but named shapes must not be yielded more than once.

## Constraints and Annotations

- Annotations carry descriptive metadata.
- Constraints carry named validation values.
- Shapes and properties may carry annotations.
- Shapes may carry constraints.

## Related Documents

- [json-schema-adapter.md](json-schema-adapter.md)
- ../architecture/canonical-schema-model.md
- ../architecture/transformation-pipeline.md
