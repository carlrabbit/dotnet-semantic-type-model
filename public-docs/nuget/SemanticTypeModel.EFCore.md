# SemanticTypeModel.EFCore

`SemanticTypeModel.EFCore` projects canonical models into EF Core-oriented metadata and `ModelBuilder` configuration.

```sh
dotnet add package SemanticTypeModel.EFCore --version 1.0.0
```

This package is part of the stable 1.0 package set. Public APIs follow the compatibility policy.

## Quick usage

```csharp
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.EFCore;

TypeSchemaModel model = AppSemanticTypeModel.Create();
var modelBuilder = new ModelBuilder(new Microsoft.EntityFrameworkCore.Metadata.Conventions.ConventionSet());
EfCoreModelBuilderProjectionResult result = modelBuilder.ApplySemanticTypeModel(
    model,
    options =>
    {
        options.DefaultSchema = "app";
        options.ProjectUnannotatedObjectsAsEntities = true;
    });
```

More details: `public-docs/guides/ef-core-projection.md`.
