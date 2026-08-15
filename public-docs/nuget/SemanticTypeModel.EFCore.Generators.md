# SemanticTypeModel.EFCore.Generators

## What this package does

Generates ordinary, composable EF Core `IEntityTypeConfiguration<TEntity>` source for semantic Entities selected by a persistence project.

## Install

```sh
dotnet add package SemanticTypeModel.EFCore.Generators --version 3.0.0
```

Install it as a private analyzer dependency and also reference `SemanticTypeModel.EFCore`.

## Use when

Use it when a `DbContext` stores one or more compile-time semantic models alongside application-owned EF entities.

## Minimal example

```csharp
[assembly: GenerateSemanticEfModel(typeof(FinanceModelMarker))]

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyFinanceSemanticModel();
}
```

The selected model assembly must use `SemanticTypeModel.Generators`, which emits manifest schema version 1 as assembly metadata. Generation never loads or executes that assembly.

## Main APIs

- `GenerateSemanticEfModelAttribute` explicitly selects a model assembly.
- One internal partial `IEntityTypeConfiguration<TEntity>` is generated for each semantic Entity.
- A public deterministic `Apply<Model>()` extension registers base configurations before derived configurations.
- `ConfigureBeforeGenerated` and `ConfigureAfterGenerated` partial hooks bracket generated calls; use the after hook for application overrides.

Generated direct EF Core calls apply manifest nullability explicitly and preserve scalar, enum-string, URI-string, strong-identifier, binary, and JSON ValueKind storage rules. Runtime code supplies only converter, comparer, and helper primitives.

To write generated files for inspection:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

## Works with

`SemanticTypeModel.Generators`, `SemanticTypeModel.EFCore`, and EF Core 10. Multiple selected models and unrelated manually configured entities can share one `DbContext`.

## Does not do

It does not generate configurations for ValueKinds, enums, DTOs, or other nonentities. It does not inspect, ignore, remove, reject, or validate unrelated EF entity types, and provides no CLI or per-entity opt-out. Selecting models that own the same CLR Entity produces `STM5041`.

## More documentation

See [EF Core projection](../guides/ef-core-projection.md) and [packages](../packages.md).
