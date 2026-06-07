# Code-First EF Core Sample

## Scenario Goal

Use an annotated C# domain model with the packaged source generator, derive an EF Core domain semantic model, and apply provider-neutral EF Core configuration to `ModelBuilder`.

## Packages Used

- `Microsoft.EntityFrameworkCore`
- `SemanticTypeModel.Abstractions`
- `SemanticTypeModel.Core`
- `SemanticTypeModel.DotNet`
- `SemanticTypeModel.EFCore`
- `SemanticTypeModel.Generators`

## Run Command

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

The sample project path is `samples/code-first-ef-core/code-first-ef-core.csproj`.

## Expected Output

The sample prints generated model information, EF Core derivation diagnostics, and provider-neutral `ModelBuilder` metadata counts.

## Consumer Pattern Demonstrated

A consumer lets the packaged generator produce `AppSemanticTypeModel.Create()`, derives `EfCoreSemanticModel`, checks diagnostics, and applies it to a local `ModelBuilder`.

## Non-Goals

This sample does not connect to a database, run migrations, require a provider package, discover or generate a `DbContext`, or replace EF Core application configuration.
