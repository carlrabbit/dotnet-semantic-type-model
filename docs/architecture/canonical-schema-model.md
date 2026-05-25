# Canonical Schema Model Architecture

## Purpose

Describe the structural role of the canonical semantic type model in the repository.

## Scope

This document defines:
- the canonical model boundary;
- project responsibilities;
- dependency direction for the milestone 2 implementation.

## Structure

### `SemanticTypeModel.Abstractions`

`SemanticTypeModel.Abstractions` defines the canonical model types and the contracts for schema sources, transformations, and projections.

### `SemanticTypeModel.Core`

`SemanticTypeModel.Core` owns runtime model construction and validation concerns.

### `SemanticTypeModel.DependencyInjection`

`SemanticTypeModel.DependencyInjection` owns `Microsoft.Extensions.DependencyInjection` registration for runtime providers, runtime services, transformations, and projection wiring.

### `SemanticTypeModel.JsonSchema`

`SemanticTypeModel.JsonSchema` adapts between JSON Schema and the canonical model through import and export components, and may expose package-specific DI registration helpers that still flow through the canonical runtime model service.

## Dependency Rules

- `SemanticTypeModel.Abstractions` must not depend on adapter projects.
- `SemanticTypeModel.Core` may depend on `SemanticTypeModel.Abstractions`.
- `SemanticTypeModel.DependencyInjection` may depend on `SemanticTypeModel.Abstractions` and `SemanticTypeModel.Core`, but not the reverse.
- Adapter projects may depend on both `SemanticTypeModel.Abstractions` and `SemanticTypeModel.Core`.
- The canonical model remains the source of truth for runtime access.

## Integration Contract

External schema dialects are integrated through source and projection contracts rather than by storing dialect-specific documents as the runtime representation.

## Related Documents

- ../specs/type-schema-model.md
- transformation-pipeline.md
- ../decisions/json-schema-as-primary-dialect.md
