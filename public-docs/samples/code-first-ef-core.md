# Code-First EF Core Sample

## Scenario Goal

Use an annotated C# domain model with the packaged source generator and project the generated semantic model into EF Core model metadata.

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

The sample prints the generated root identifier, adapter diagnostic count, and number of EF Core model-builder entity types.

## Consumer Pattern Demonstrated

A consumer lets the packaged generator produce `AppSemanticTypeModel.Create()`, adapts the semantic model to the hardened runtime model, and applies EF Core projection metadata to a local `ModelBuilder`.

## Non-Goals

This sample does not connect to a database, run migrations, require a provider package, or replace EF Core application configuration.
