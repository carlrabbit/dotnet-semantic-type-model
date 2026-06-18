# EF Core Projection

## Goal

Apply semantic metadata to EF Core `ModelBuilder` while leaving provider setup, migrations, and database operations under application control.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.2.0`.

## Packages

- `SemanticTypeModel.EFCore` for derivation and `ModelBuilder` projection.
- `Microsoft.EntityFrameworkCore` for EF Core metadata APIs.
- `SemanticTypeModel.Generators` and `SemanticTypeModel.DotNet` for code-first model generation.

## Minimal path

1. Generate the semantic model.
2. Derive the EF Core semantic model.
3. Check diagnostics.
4. Call `modelBuilder.ApplyEfCoreSemanticModel(result.Model)` inside `OnModelCreating`.
5. Add provider-specific EF Core configuration separately.

## Full example

```csharp
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.EFCore;

public sealed class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var result = AppSemanticTypeModel.Create().DeriveEfCoreModel();
        result.Diagnostics.ThrowIfErrors();
        modelBuilder.ApplyEfCoreSemanticModel(result.Model);
    }
}
```

## How it works

Annotated .NET code is extracted by the generator into a `TypeSchemaModel`. Core transformations normalize projection-neutral semantics. The target package derives a domain semantic model and then exports or applies target-specific output when that target supports it.

## Options and policies

Configure projection policy for unannotated objects, keys, table and column naming, owned values, inheritance strategy, relationships, converters, and envelope payload storage. Use EF Core provider APIs for provider-specific database behavior.

## Diagnostics

EF Core diagnostics report missing keys, unresolved relationship endpoints, duplicate projected names, invalid converter metadata, ambiguous inheritance, unsupported ownership shapes, and unsupported envelope payload storage policy.

## Common mistakes

- Treating JSON Schema files as the canonical authoring source for new models.
- Mixing target-specific metadata with projection-neutral semantics.
- Skipping diagnostic inspection before using projected output.
- Using stale pre-2.2 model namespace or shape names in current examples.

## Limitations

The package does not create `DbContext` types, choose a database provider, run migrations, create databases, enable temporal tables, configure query filters, or tune provider-specific JSON behavior.

## Related docs

- [SemanticTypeModel.EFCore package](../nuget/SemanticTypeModel.EFCore.md)
- [Code-first EF Core sample](../samples/code-first-ef-core.md)
- [Projection capabilities](projection-capabilities.md)
