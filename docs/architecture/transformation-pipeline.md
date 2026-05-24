# Transformation Pipeline Architecture

## Purpose

Describe how schema acquisition, transformation, and projection compose around the canonical semantic type model.

## Pipeline Stages

The baseline architecture separates:
1. schema acquisition through `ISchemaModelSource`;
2. canonical model construction and validation;
3. reusable transformation through `ISchemaTransformation`;
4. outbound projection through `ISchemaProjection<T>`.

## Design Rules

- Sources produce the canonical `TypeSchemaModel`.
- Transformations accept a canonical model and return a canonical model.
- Projections consume a canonical model and return a target representation.
- Each stage is independently reusable in runtime and future compile-time flows.
- Adapter-specific behavior stays outside the canonical model project.

## Milestone 2 Baseline

Milestone 2 provides the baseline JSON Schema source and JSON Schema projection, establishing the pipeline contract without coupling future adapters to JSON Schema internals.

## Related Documents

- canonical-schema-model.md
- ../specs/type-schema-model.md
- ../specs/json-schema-adapter.md
