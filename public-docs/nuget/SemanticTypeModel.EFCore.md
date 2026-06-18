# SemanticTypeModel.EFCore

## What this package does

`SemanticTypeModel.EFCore` provides EF Core domain-model derivation and provider-neutral ModelBuilder projection from semantic models.

## Install

```sh
dotnet add package SemanticTypeModel.EFCore --version 2.2.0
```

## Use when

- Install this package when you need EF Core domain-model derivation and provider-neutral ModelBuilder projection from semantic models.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.
- Apply semantic metadata inside DbContext.OnModelCreating.

## Minimal example

```csharp
using SemanticTypeModel.EFCore;

var result = AppSemanticTypeModel.Create().DeriveEfCoreModel();
result.Diagnostics.ThrowIfErrors();
modelBuilder.ApplyEfCoreSemanticModel(result.Model);
```

## Main APIs

| API | Purpose |
| --- | --- |
| `DeriveEfCoreModel` | Derives EF Core semantic metadata. |
| `ApplyEfCoreSemanticModel` | Applies provider-neutral metadata to ModelBuilder. |
| `ApplySemanticTypeModel` | Projects directly from a TypeSchemaModel to ModelBuilder. |
| `EfCoreDerivationOptions` | Controls derivation and projection policies. |

## Works with

- Microsoft.EntityFrameworkCore, SemanticTypeModel.Core, and generated model providers.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not create DbContext types, run migrations, select database providers, or tune provider-specific SQL.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [EF Core projection guide](../guides/ef-core-projection.md)
- [Core semantics guide](../guides/core-semantics.md)
