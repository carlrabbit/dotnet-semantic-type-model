# Type Model Annotation Specification

## Purpose

Define annotation namespacing, preservation, validation boundaries, and separation from canonical semantics.

## Authority

This spec is authoritative for:

- annotation key naming and reserved namespaces;
- annotation preservation behavior across transformations/projections;
- canonical-vs-projection annotation separation;
- annotation-related diagnostic expectations.

## Annotation Contract

- `Annotation.Key` is a namespaced key (`namespace.name`).
- `Annotation.Value` is typed as `object?`.
- `Annotation.Scope` identifies logical attachment scope.
- `Annotation.Source` captures provenance (`Declared`, `Imported`, `Generated`, `Transformed`, `Unknown`).
- `AnnotationBag` stores annotations without mutating core semantic contracts.

## Reserved Namespaces

- `schema.*`
- `jsonSchema.*`
- `jsonEditor.*`
- `dotnet.*`
- `efCore.*`
- `powerBi.*`
- `tom.*`
- `ui.*`

## Required Examples

- `ui.order`
- `ui.category`
- `jsonEditor.format`
- `jsonEditor.options`
- `efCore.tableName`
- `efCore.columnName`
- `efCore.valueGenerated`
- `powerBi.tableRole`
- `powerBi.dataCategory`
- `tom.measureExpression`

## Rules

- Core semantics must not depend on projection-specific annotation keys.
- Unknown annotations are preserved unless a transform explicitly removes them.
- Annotation values may be validated by target-specific transforms/projections.
- Invalid annotation values must produce diagnostics (warning/error by contract policy).
- Canonical model validation may validate key format and reserved namespace shape, but not target-specific business rules by default.

## Transformation Participation

- Transformations may read/write annotations.
- Projections may consume target namespaces and produce diagnostics for invalid values.
- Pass-through transforms must preserve unknown namespaces and values.
