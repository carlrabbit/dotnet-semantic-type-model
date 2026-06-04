# Transformation Pipeline Architecture

## Status

Legacy pipeline note superseded for model-source authority by [code-first-domain-projection-pipeline.md](code-first-domain-projection-pipeline.md).

## Purpose

Describe how model acquisition, transformation, and projection compose around the canonical semantic type model.

## Pipeline Stages

The current architecture separates:
1. code-first model acquisition through runtime extraction, compile-time generation, or snapshot loading;
2. canonical model construction and validation;
3. reusable transformation through `ISchemaTransformation`;
4. domain semantic model derivation and outbound projection through `ISchemaProjection<T>`.

## Design Rules

- Code-first providers produce the canonical `TypeSchemaModel`.
- External schema formats do not produce canonical models unless a later accepted decision adds a source adapter.
- Transformations accept a canonical model and return a canonical model or domain semantic model.
- Projections derive a domain semantic model before domain-specific functionality returns a target representation.
- Each stage is independently reusable in runtime and future compile-time flows.
- Adapter-specific behavior stays outside the canonical model project.

## Legacy Milestone 2 Baseline

Milestone 2 used a JSON Schema source and projection to establish the original pipeline contract. M0026 supersedes that source authority: JSON Schema is now a projection/export target, and any retained import behavior is legacy/internal compatibility behavior rather than a public authoring path.

## Related Documents

- canonical-schema-model.md
- ../specs/type-schema-model.md
- ../specs/code-first-semantic-model-architecture.md
- ../specs/json-schema-adapter.md
- ../decisions/code-first-only-model-source.md
