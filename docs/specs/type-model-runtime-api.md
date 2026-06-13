# Type Model Runtime API Specification

## Purpose

Define the stable runtime surface for acquiring runtime canonical semantic model `TypeSchemaModel` instances and carrying diagnostics through runtime consumption.

## Authority

This specification is authoritative for:

- runtime model provider and service abstractions;
- runtime result and projection result contracts;
- model acquisition, transformation, and projection blocking behavior;
- caching and failure policy for the runtime service layer.

## Canonical Runtime Contracts

Runtime model consumption is centered on the canonical semantic model in `SemanticTypeModel.Abstractions.Canonical`.

### Model acquisition

```csharp
public interface ITypeSchemaModelProvider
{
    ValueTask<TypeSchemaModelResult> GetModelAsync(CancellationToken cancellationToken = default);
}

public sealed record TypeSchemaModelResult
{
    public TypeSchemaModel? Model { get; init; }
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }
    public bool HasModel { get; }
    public bool HasErrors { get; }
}
```

Rules:

- providers return the runtime canonical semantic model `TypeSchemaModel`, not projection-specific artifacts;
- providers may return diagnostics with or without a model;
- provider results are raw acquisition results and are not required to apply transformations or caching.

### Runtime service

```csharp
public interface ITypeSchemaModelService
{
    ValueTask<TypeSchemaModelResult> GetModelAsync(CancellationToken cancellationToken = default);
}
```

Rules:

- the runtime service is the cached application-facing access point;
- the runtime service composes exactly one registered provider with zero or more registered transformations;
- provider diagnostics become the initial diagnostics for transformation execution;
- when no transformations are registered, the runtime service returns the provider result unchanged;
- cancellation is propagated as `OperationCanceledException` and is not cached.

### Projection service

```csharp
public interface ITypeSchemaProjectionService<TProjection>
{
    ValueTask<SchemaProjectionResult<TProjection>> ProjectAsync(CancellationToken cancellationToken = default);
}
```

`SchemaProjectionResult<TProjection>` carries:

- the canonical model used for projection when available;
- the projected value when projection completed;
- `HasProjection` and `IsProjectionBlocked` flags;
- the accumulated diagnostics from model acquisition, transformation, and projection.

## Transformation Integration

Registered runtime transformations reuse the canonical semantic model contracts from `docs/specs/type-model-core.md`:

- `ISchemaTransformation.TransformAsync(TypeSchemaModelBuilder, SchemaTransformContext, CancellationToken)`;
- `SchemaTransformationPipeline`;
- `SchemaPipelineOptions`.

Rules:

- transformations execute in registration order;
- provider diagnostics are supplied as `InitialDiagnostics`;
- `TypeSchemaRuntimeOptions.ContinueOnError` and `PromoteWarningsToErrors` map directly to pipeline options;
- projection is blocked when the final model result contains any error diagnostics;
- callers can still inspect the model result and diagnostics even when projection is blocked.

## Caching and Failure Policy

`TypeSchemaRuntimeOptions` defines runtime-service policy:

- `CacheModelResult` default: `true`;
- `CacheFailureResults` default: `true`;
- `ContinueOnError` default: `false`;
- `PromoteWarningsToErrors` default: `false`.

Rules:

- caching happens at `ITypeSchemaModelService`, not at the raw provider contract;
- when `CacheModelResult=true`, the service caches the first successful or failed result according to `CacheFailureResults`;
- when `CacheModelResult=false`, the service reacquires and retransforms on every call;
- cached diagnostics are returned together with the cached model result.

## Diagnostics and Failure Semantics

Runtime diagnostics use structured `SchemaDiagnostic` values.

Required runtime codes introduced by M0011:

- `STM3001` — no runtime model provider registered;
- `STM3002` — multiple runtime model providers registered;
- `STM3003` — provider threw an exception;
- `STM3004` — transformation pipeline threw an exception;
- `STM3006` — no projection registered for the requested result type;
- `STM3007` — multiple projections registered for the requested result type;
- `STM3008` — projection blocked because the model result contains errors;
- `STM3009` — projection threw an exception.

Rules:

- runtime-provider and runtime-service diagnostics use `SchemaDiagnosticStage.Transformation` because model acquisition occurs before projection and no dedicated runtime stage exists yet;
- projection-service diagnostics use `SchemaDiagnosticStage.Projection`;
- missing registration behavior is result-based and deterministic rather than container-failure-based once runtime services are registered.

## Generated and Legacy Compatibility

Generated static factories remain the compatibility baseline:

```csharp
public static partial class AppSemanticTypeModel
{
    public static TypeSchemaModel Create();
}
```

Rules:

- the runtime surface accepts both canonical semantic models and legacy canonical models produced by current generator output;
- legacy model instances are adapted into the runtime canonical semantic model before transformations and projections run;
- generated and runtime-created models therefore meet at the same runtime canonical semantic model boundary.
