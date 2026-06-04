# Code-First Domain Projection Pipeline

## Status

Authoritative architecture document.

## Purpose

Define the structural pipeline for code-first SemanticTypeModel usage and domain-specific semantic model derivation.

## Architecture

The repository architecture is:

```text
Annotated .NET code
  -> runtime extraction or compile-time generation
  -> canonical Semantic Type Model
  -> query, inspect, validate, transform
  -> domain-specific semantic model
  -> domain-specific functionality
```

## Layers

### Code Source Layer

The code source layer is the only supported authoring source for canonical models.

Inputs:

- C# types;
- SemanticTypeModel core attributes;
- custom alias attributes;
- domain-specific attributes;
- generator/extraction configuration.

### Extraction and Generation Layer

This layer creates canonical `TypeSchemaModel` instances.

Implementations:

- runtime extraction in `SemanticTypeModel.DotNet`;
- compile-time provider generation in `SemanticTypeModel.Generators`.

Outputs:

- canonical semantic model;
- diagnostics;
- optional persisted snapshot.

### Canonical Model Layer

The canonical model represents projection-neutral semantic meaning.

Responsibilities:

- stable identifiers;
- semantic primitives;
- annotations;
- constraints;
- diagnostics;
- query support;
- inspection support;
- transformation support.

Non-responsibilities:

- JSON Schema document authoring;
- EF Core metadata model finalization;
- Power BI service model finalization;
- runtime editing.

### Transformation Layer

Transformations derive meaning.

Transformation categories:

| Category | Purpose |
|---|---|
| Core normalization | Normalize extracted .NET metadata into canonical semantic primitives. |
| Core derivation | Derive primitives from aliases and conventions. |
| Domain derivation | Derive domain-specific semantics from core primitives and domain attributes. |
| Validation | Emit diagnostics for invalid, missing, or ambiguous semantics. |

Users may configure or replace transformation sequences in code when a package exposes such customization.

### Domain Semantic Model Layer

Each domain package creates a domain-specific semantic model.

Examples:

| Domain package | Domain semantic model |
|---|---|
| `SemanticTypeModel.JsonSchema` | JSON Schema semantic model |
| `SemanticTypeModel.EFCore` | EF Core semantic model |
| `SemanticTypeModel.PowerBI` | Power BI semantic model |
| `SemanticTypeModel.SystemTextJson` | System.Text.Json resolver customization model |

The domain semantic model is the boundary between generic semantic metadata and domain functionality.

### Domain Functionality Layer

Domain functionality operates on the domain semantic model.

Examples:

- JSON Schema document export;
- EF Core `ModelBuilder` configuration;
- Power BI local metadata output;
- System.Text.Json resolver customization.

## Domain Pipelines

### JSON Schema

```text
Canonical Semantic Type Model
  -> JSON Schema derivation transformations
  -> JSON Schema semantic model
  -> JSON Schema Draft 2020-12 export
```

JSON Schema import is not a canonical model source.

### EF Core

```text
Canonical Semantic Type Model
  -> EF Core derivation transformations
  -> EF Core semantic model
  -> ModelBuilder.ApplySemanticTypeModel(...)
```

EF Core database creation and migrations are outside this library’s domain.

### Power BI

```text
Canonical Semantic Type Model
  -> Power BI derivation transformations
  -> Power BI semantic model
  -> local Power BI metadata output
```

Power BI service publishing, PBIX generation, and full TOM parity are outside this library’s domain.

### System.Text.Json

```text
Canonical Semantic Type Model
  -> System.Text.Json metadata derivation
  -> resolver customization model
  -> IJsonTypeInfoResolver / JsonSerializerOptions behavior
```

SemanticTypeModel does not generate `JsonSerializerContext` declarations or custom serializers.

## Query and Inspection Path

The development loop requires inspection before and after transformations:

```text
Canonical model
  -> text summary
  -> diagnostics summary
  -> transformation trace
  -> domain model summary
```

Query and inspection APIs must work with:

- CLR types when available;
- string identifiers as fallback;
- persisted snapshots when code is unavailable.

## Persistence Path

```text
Code-generated canonical model
  -> persisted snapshot
  -> loaded canonical model
  -> query/inspect/transform/project
```

A persisted snapshot is not an authoring format. It preserves access to a code-generated semantic model when the codebase is unavailable.

## Dependency Direction

Expected dependency direction:

```text
Abstractions
  <- Core
  <- DotNet
  <- Generators
  <- JsonSchema / EFCore / PowerBI / SystemTextJson
  <- DependencyInjection
```

Domain packages may depend on core abstractions and shared runtime utilities, but core packages must not depend on domain packages.

## Architectural Constraints

- Canonical model remains projection-neutral.
- Domain models are explicit and package-owned.
- Domain functionality must not rely on scattered ad hoc annotation lookups as its primary model.
- Diagnostics are emitted when derivation cannot be done safely.
- Consumer-facing behavior must be deterministic and short-running.
