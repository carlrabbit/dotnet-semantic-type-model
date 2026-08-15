# SemanticTypeModel.EFCore

## What this package does

Provides EF relational inspection contracts, explicit semantic-model selection, and converter/comparer primitives consumed by generated EF configuration.

## Install

```sh
dotnet add package SemanticTypeModel.EFCore --version 3.0.0
```

## Use when

Use it with `SemanticTypeModel.EFCore.Generators` in an EF persistence project, or use `DeriveEfRelationalModel` for inspection.

## Minimal example

```csharp
[assembly: GenerateSemanticEfModel(typeof(FinanceModelMarker))]
```

## Main APIs

- `GenerateSemanticEfModelAttribute`
- `DeriveEfRelationalModel`
- `SemanticEfValueConverters`

## Works with

EF Core 10 and `SemanticTypeModel.EFCore.Generators` 3.0.0.

## Does not do

It does not own migrations, database creation, provider setup, or global cleanup of a `ModelBuilder`. Runtime `ApplySemanticTypeModel` and `ApplySemanticRelationalModel` are not the 3.0 application contract.

## More documentation

See the [EF projection guide](../guides/ef-core-projection.md).
