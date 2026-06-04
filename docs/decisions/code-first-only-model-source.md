# Decision: Code Is the Only Canonical Model Source

## Status

Accepted for M0026.

## Context

SemanticTypeModel originally supported or implied multiple ways to create the canonical model, including JSON Schema import. The project direction is now narrower:

```text
The canonical semantic model is generated from .NET code.
External formats are projection targets or domain integrations, not canonical authoring sources.
```

The primary development loop is code-first:

```text
Annotate .NET types.
Run tests or a console sample.
Inspect diagnostics and model output.
Adjust annotations or transformation configuration.
Repeat.
```

The library should focus on semantic primitives, extraction/generation, querying, inspection, transformations, domain semantic models, and projections.

## Decision

Code is the only supported authoring source for canonical semantic models.

Supported canonical model creation paths are:

- runtime extraction from .NET code;
- compile-time generation from .NET code;
- loading a persisted snapshot that was originally generated from code.

Unsupported as canonical model authoring sources:

- JSON Schema import;
- OpenAPI import;
- TypeScript import;
- runtime model editing;
- arbitrary external schema import.

JSON Schema remains useful as a projection/export target.

## Rationale

- The project is a code-first semantic metadata framework, not a general schema interchange framework.
- Code-first extraction keeps source-of-truth ownership clear.
- Attribute extensibility and transformations are more valuable when the source is code.
- Query and inspection APIs can focus on .NET type-centered workflows.
- JSON Schema import introduces a second authoring model and broadens scope substantially.
- Runtime editing would turn the canonical model into an authoring tool, which is outside the intended development loop.

## Consequences

- Specs and public docs must stop presenting JSON Schema import as the primary or supported canonical model creation path.
- JSON Schema package behavior should be repositioned around export/projection from code-generated models.
- Existing JSON Schema import APIs must be removed, made internal, or documented as legacy/unsupported according to compatibility policy.
- Persisted model loading must be described as snapshot loading, not authoring.
- Domain integrations must derive domain semantic models from the canonical model.
- The query and inspection surface becomes a core product concern.
- Package READMEs and samples should lead with annotated code, generation, diagnostics, inspection, and projection.

## Alternatives Considered

### Keep JSON Schema Import as a Peer Source

Rejected because it makes the project a general schema interchange framework and competes with the code-first design.

### Keep Runtime Model Editing

Rejected because the intended development loop is annotation-driven and test/console inspection-driven.

### Support Multiple External Schema Sources Later

Deferred. A later accepted decision may add a source adapter, but the default architecture must remain code-first and snapshot-based.

### Treat Persisted Models as Authoring Files

Rejected. Persisted models are snapshots of code-generated models, not a separate authoring language.
