# Canonical Schema Model Architecture

## Status

Legacy architecture note superseded for model-source authority by [code-first-domain-projection-pipeline.md](code-first-domain-projection-pipeline.md).

## Purpose

Describe the structural role of the canonical semantic type model in the repository.

## Scope

This document defines:
- the canonical model boundary;
- project responsibilities;
- dependency direction for the milestone 2 implementation.

For current architecture, code is the only supported canonical model authoring source and external formats are projection targets or domain integrations.

## Structure

### `SemanticTypeModel.Abstractions`

`SemanticTypeModel.Abstractions` defines the canonical model types and the contracts for runtime providers, transformations, and projections.

### `SemanticTypeModel.Core`

`SemanticTypeModel.Core` owns runtime model construction and validation concerns.

### `SemanticTypeModel.DependencyInjection`

`SemanticTypeModel.DependencyInjection` owns `Microsoft.Extensions.DependencyInjection` registration for runtime providers, runtime services, transformations, and projection wiring.

### `SemanticTypeModel.JsonSchema`

`SemanticTypeModel.JsonSchema` projects code-generated canonical models to JSON Schema and may retain legacy/internal import components only for compatibility or tests; it must not present JSON Schema import as canonical model authoring.

## Dependency Rules

- `SemanticTypeModel.Abstractions` must not depend on adapter projects.
- `SemanticTypeModel.Core` may depend on `SemanticTypeModel.Abstractions`.
- `SemanticTypeModel.DependencyInjection` may depend on `SemanticTypeModel.Abstractions` and `SemanticTypeModel.Core`, but not the reverse.
- Adapter projects may depend on both `SemanticTypeModel.Abstractions` and `SemanticTypeModel.Core`.
- The canonical model remains the source of truth for runtime access.

## Integration Contract

External schema dialects are integrated as projection targets or domain-specific semantic model integrations rather than as canonical model sources or runtime storage representations.

## Related Documents

- ../specs/type-schema-model.md
- code-first-domain-projection-pipeline.md
- transformation-pipeline.md
- ../decisions/code-first-only-model-source.md
- ../decisions/json-schema-as-primary-dialect.md (superseded for model source authority)
