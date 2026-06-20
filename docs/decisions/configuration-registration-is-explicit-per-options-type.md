# Decision: Configuration Registration Is Explicit per Options Type

## Status

Accepted for M0044 planning.

## Context

A semantic model may describe configuration contracts for an entire solution, while a specific service uses only a subset.

Registering the complete Configuration domain model creates unwanted noise and forces consumers to maintain exclusion or skip lists.

The current Configuration package already derives all configuration types from the complete model, but application registration should remain a deployment-boundary decision.

## Decision

The primary Configuration registration API explicitly selects one options type:

```csharp
services.AddSemanticOptions<ColdStorageOptions>(configuration, model);
```

The runtime adapter is the canonical implementation of binding and validation behavior.

Generated convenience methods delegate to the runtime adapter.

Full-model derivation remains available for inspection and tooling but does not imply application registration.

Required section presence is represented as Configuration-specific metadata with `Optional` and `Required` values.

## Consequences

- A service consciously registers only the options types it uses.
- No skip or exclusion list is needed.
- A complete solution model can be reused by multiple services safely.
- Runtime and generated registration behavior cannot drift because generated helpers delegate to runtime code.
- `OptionsBuilder<TOptions>` remains available for standard .NET Options composition.
- Call-site overrides are limited to deployment-specific concerns.
- Model/programming failures occur during derivation or registration.
- Deployed configuration failures occur through Options validation.

## Alternatives Considered

### Register the Complete Configuration Model

Rejected. It makes the semantic model, rather than the application, decide the deployment boundary.

### Add Include/Exclude Filters to Complete-Model Registration

Rejected. Explicit inclusion is clearer and avoids maintaining exclusion lists as the solution model grows.

### Generate All Registration Logic Directly

Rejected. This would duplicate runtime binding and validation behavior and create drift risk.

### Make Section Presence a Core Semantic

Rejected. Section existence describes a Configuration binding-source requirement, not projection-neutral model meaning.
