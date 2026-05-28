# EF Core Projection Guide

`SemanticTypeModel.EFCore` projects a canonical `TypeSchemaModel` into EF Core model configuration.

## Usage

```csharp
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.EFCore;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    TypeSchemaModel model = AppSemanticTypeModel.Create();
    EfCoreModelBuilderProjectionResult result = modelBuilder.ApplySemanticTypeModel(
        model,
        options =>
        {
            options.DefaultSchema = "app";
            options.ProjectUnannotatedObjectsAsEntities = true;
        });

    if (result.Diagnostics.Count > 0)
    {
        // inspect projection diagnostics
    }
}
```

## Baseline behavior

- Consumes the canonical semantic model (not raw CLR reflection).
- Applies provider-neutral EF Core model configuration for projected entities, properties, keys, and unique indexes.
- Returns deterministic diagnostics for unsupported or ambiguous shapes.
