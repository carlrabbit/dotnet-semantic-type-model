# M0011 - Runtime API Surface and DI Integration

## Purpose

Define the first stable runtime surface for consuming generated and runtime-created semantic type models through a canonical provider/service pipeline.

## Delivered Scope

- Hardened runtime provider contract:
  - `ITypeSchemaModelProvider`
  - `TypeSchemaModelResult`
- Hardened runtime service contract:
  - `ITypeSchemaModelService`
  - `ITypeSchemaProjectionService<TProjection>`
  - `SchemaProjectionResult<TProjection>`
  - `TypeSchemaRuntimeOptions`
- New DI package boundary:
  - `SemanticTypeModel.DependencyInjection`
  - `AddSemanticTypeModelRuntime(...)`
  - `AddSemanticTypeModel(...)`
  - `AddSemanticTypeModelProvider<TProvider>()`
  - `AddSemanticTypeModelTransformation<TTransformation>()`
  - `AddSemanticTypeModelProjection<TProjection, TImplementation>(...)`
- Compatibility adapters for current legacy generated-model factories and other legacy `TypeSchemaModel` instances.
- JSON Schema runtime projection registration via `AddSemanticTypeModelJsonSchema()`.
- Deterministic runtime diagnostics for missing registrations, provider failures, projection blocking, and projection failures.
- Short-running DI fixtures covering provider registration, generated-factory compatibility, transformation order, projection blocking, JSON Schema proof, missing registration diagnostics, and caching behavior.

## Runtime API Summary

Runtime consumption now follows this path:

```text
model instance or factory -> ITypeSchemaModelProvider -> ITypeSchemaModelService -> transformations -> ITypeSchemaProjectionService<T>
```

The canonical hardened model remains the runtime boundary. Current legacy generated output is adapted into that boundary before transformations and projections execute.

## DI Boundary Summary

- `SemanticTypeModel.Abstractions` stays free of DI dependencies.
- `SemanticTypeModel.Core` stays free of DI dependencies.
- `SemanticTypeModel.DependencyInjection` owns `IServiceCollection` wiring and lifetime policy.
- Projection packages may add package-specific DI helpers, but they must register through the canonical runtime service path.

## Caching and Lifetime Behavior

Default runtime behavior:

- runtime model service is singleton;
- runtime projection service is singleton per projection result type;
- model results are cached by default;
- failure results are cached by default;
- callers can disable caching through `TypeSchemaRuntimeOptions`.

## Diagnostics and Logging

Diagnostics remain authoritative.

M0011 introduces deterministic runtime diagnostics (`STM3001`-`STM3009`) for registration and execution failures. Logging integration is still optional and is not required by the core runtime API.

## Fixture Coverage

Short-running tests cover:

1. existing hardened model registration;
2. generated/legacy static factory registration and caching;
3. custom provider registration and cancellation acceptance;
4. deterministic transformation order;
5. validation failure blocking projection;
6. JSON Schema runtime projection proof;
7. missing registration diagnostics;
8. caching behavior with runtime options.

## Non-goals Preserved

- no ASP.NET Core endpoint integration;
- no hosted background reload or model registry;
- no OpenAPI integration;
- no reflection scanning surface beyond existing capabilities;
- no long-running integration tests.
