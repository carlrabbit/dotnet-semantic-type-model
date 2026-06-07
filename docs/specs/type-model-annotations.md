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

## Merge and Normalization Policy

The M0005 baseline annotation policy is:

- keys remain namespaced `AnnotationKey` values;
- reserved namespaces use canonical casing exactly as listed in this spec;
- malformed keys are diagnosable and removed by default during normalization;
- unknown namespaces remain preserved;
- duplicate keys are allowed at ingestion time but normalized deterministically;
- default merge behavior is last-wins;
- duplicate-key merges emit diagnostics, with reserved-namespace conflicts surfaced as warnings;
- transforms preserve existing annotation provenance through `Annotation.Source` unless a future transform explicitly rewrites provenance.

## Transformation Participation

- Transformations may read/write annotations.
- Projections may consume target namespaces and produce diagnostics for invalid values.
- Pass-through transforms must preserve unknown namespaces and values.
- `NormalizeAnnotationsTransformation` is the baseline reusable transform for key validation, namespace normalization, and duplicate-key merge behavior.

## Core Semantic Vocabulary Annotations

The canonical core semantic vocabulary is specified in `docs/specs/core-semantic-vocabulary.md`.

Envelope annotations are projection-neutral and use the `schema.*` namespace:

- `schema.envelope` marks the wrapper boundary on an object type.
- `schema.envelope.purpose` preserves optional projection-neutral purpose text.
- `schema.envelope.payload` marks the wrapped payload member.
- `schema.envelope.metadata` marks lifecycle or context metadata members.

Target-specific choices such as JSON Schema `$ref` placement, EF Core ownership, serialized storage, Power BI flattening, or payload omission must use target policy rather than changing these core annotations.
