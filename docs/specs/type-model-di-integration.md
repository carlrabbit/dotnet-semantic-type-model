# Type Model DI Integration Specification

## Purpose

Define the `Microsoft.Extensions.DependencyInjection` integration pattern for the semantic type model runtime API.

## Authority

This specification is authoritative for:

- the DI package boundary;
- service registration methods for providers, factories, transformations, and projections;
- lifetime and caching defaults exposed through DI;
- projection registration and package-specific extension patterns.

## Package Boundary

- Core abstractions remain in `SemanticTypeModel.Abstractions`.
- Runtime adapters and validation logic remain in `SemanticTypeModel.Core`.
- `Microsoft.Extensions.DependencyInjection` dependencies are isolated to `SemanticTypeModel.DependencyInjection` and projection packages that expose DI extension methods.
- Projection packages must not introduce DI dependencies into `SemanticTypeModel.Abstractions` or `SemanticTypeModel.Core`.

## Required Registrations

`SemanticTypeModel.DependencyInjection` exposes these registration methods:

```csharp
services.AddSemanticTypeModelRuntime();
services.AddSemanticTypeModel(model);
services.AddSemanticTypeModel(factory);
services.AddSemanticTypeModelProvider<TProvider>();
services.AddSemanticTypeModelTransformation<TTransformation>();
services.AddSemanticTypeModelProjection<TProjection, TImplementation>(ProjectionTarget target);
```

Rules:

- `AddSemanticTypeModelRuntime()` registers `ITypeSchemaModelService` and `ITypeSchemaProjectionService<T>` even when no provider is present;
- `AddSemanticTypeModel(model)` and `AddSemanticTypeModel(factory)` implicitly register runtime services;
- provider registration supports both canonical semantic models and legacy generated-model outputs;
- `AddSemanticTypeModelProvider<TProvider>()` registers the provider as a singleton;
- transformations are singletons and execute in registration order;
- projections are singletons and are keyed by projection result type plus `ProjectionTarget` metadata.

## Lifetime Rules

- `ITypeSchemaModelService` is a singleton.
- `ITypeSchemaProjectionService<T>` is a singleton.
- provider registrations created by `AddSemanticTypeModel(...)` are singleton registrations.
- custom provider types registered through `AddSemanticTypeModelProvider<TProvider>()` are singleton registrations.
- projection implementations registered through `AddSemanticTypeModelProjection<TProjection, TImplementation>()` are singleton registrations.

Caching behavior is controlled by `TypeSchemaRuntimeOptions` on the singleton model service.

## Missing and Invalid Registration Policy

Once runtime services are registered:

- no provider -> `STM3001` result diagnostic;
- multiple providers -> `STM3002` result diagnostic;
- no projection for the requested result type -> `STM3006` result diagnostic;
- multiple projections for the requested result type -> `STM3007` result diagnostic.

Container resolution must remain deterministic:

- `ITypeSchemaModelService` resolves even before a provider is added when `AddSemanticTypeModelRuntime()` has been called;
- `ITypeSchemaProjectionService<T>` resolves even before a projection is added when runtime services have been registered.

## Projection Package Pattern

Projection packages may add package-specific helpers that delegate to the generic registration pattern.

Example:

```csharp
services.AddSemanticTypeModelJsonSchema();
```

Rules:

- package-specific helpers must still register through the canonical runtime model path;
- projection packages may adapt the canonical semantic model to projection-specific exporters internally;
- no direct generator-to-projection shortcut may bypass `ITypeSchemaModelProvider` / `ITypeSchemaModelService`.

## JSON Schema Proof Path

`SemanticTypeModel.JsonSchema` may expose:

```csharp
services.AddSemanticTypeModelJsonSchema();
```

Rules:

- JSON Schema registration is optional and package-specific;
- the JSON Schema runtime path composes with the canonical runtime model service;
- current generator output may flow through DI as legacy model factories, then be adapted to the runtime canonical semantic model before the JSON Schema projection executes.
