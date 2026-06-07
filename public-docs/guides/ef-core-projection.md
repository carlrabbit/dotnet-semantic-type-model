# EF Core Projection Guide

`SemanticTypeModel.EFCore` derives an EF Core domain semantic model from the canonical `TypeSchemaModel` and applies provider-neutral configuration to EF Core `ModelBuilder`.

## Boundary

The package owns semantic mapping into `ModelBuilder`. It does not create databases, generate migrations, discover or generate `DbContext` types, perform provider-specific SQL Server/PostgreSQL configuration, validate a runtime database, or configure global query filters.

## Usage

```csharp
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.EFCore;

TypeSchemaModel model = AppSemanticTypeModel.Create();

var result = model.DeriveEfCoreModel(options =>
{
    options.UseDefaultTransformations();
});

result.Diagnostics.ThrowIfErrors();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyEfCoreSemanticModel(result.Model);
}
```

## Supported Mapping

The 2.0.0 EF Core projection supports provider-neutral metadata for:

- entities and properties;
- table and column names;
- primary keys and alternate keys;
- single-column, composite, and unique indexes;
- requiredness and nullability;
- max length and precision/scale;
- explicit value converters or provider CLR type metadata;
- explicit simple relationships;
- explicit owned/value-object mapping;
- explicit user-selected inheritance strategies: TPH, TPT, and TPC.

## Diagnostics

Unsupported or ambiguous mapping emits diagnostics. Typical cases include missing keys, unresolved relationship endpoints, invalid converter metadata, duplicate projected names, unsupported owned collections, unsupported many-to-many skip navigations, and ambiguous inheritance strategy.

## Non-Goals

This package does not replace EF Core application configuration. Users still own provider configuration, migrations, database creation, connection strings, transactions, seed data, runtime validation, and deployment.
