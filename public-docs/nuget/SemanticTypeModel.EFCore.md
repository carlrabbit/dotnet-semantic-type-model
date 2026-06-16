# SemanticTypeModel.EFCore

`SemanticTypeModel.EFCore` derives an EF Core domain semantic model from canonical SemanticTypeModel metadata and applies provider-neutral configuration to EF Core `ModelBuilder`.

```sh
dotnet add package SemanticTypeModel.EFCore --version 2.2.0
```

This package is part of the stable package set. Public APIs follow the compatibility policy.

## Quick usage

```csharp
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.EFCore;

TypeSchemaModel model = AppSemanticTypeModel.Create();

var result = model.DeriveEfCoreModel(options =>
{
    options.UseDefaultTransformations();
});

result.Diagnostics.ThrowIfErrors();

var modelBuilder = new ModelBuilder(new Microsoft.EntityFrameworkCore.Metadata.Conventions.ConventionSet());
modelBuilder.ApplyEfCoreSemanticModel(result.Model);
```

## Supported scope

The package supports semantic mapping for entities, properties, keys, alternate keys, indexes, requiredness/nullability, conversions, explicit simple relationships, explicit owned/value-object mapping, explicit inheritance strategy mapping, envelope payload storage policies, and projection of evolution/lifecycle semantics as provider-neutral metadata.

It does not create databases, generate migrations, discover or generate `DbContext` types, configure providers, validate a runtime database, configure global query filters, enable temporal tables, or tune provider-specific JSON behavior.

More details: `public-docs/guides/ef-core-projection.md`.
