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

The EF Core projection supports provider-neutral metadata for:

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

## Envelope Payload Storage

Envelope metadata maps as normal scalar columns. Envelope payload storage is target-specific EF Core policy.

Supported policy concepts include:

- serialized JSON column;
- owned JSON mapping where the EF Core/provider shape supports it;
- owned same-table columns for owned reference payloads;
- owned separate table mapping where explicitly configured;
- ignored payload.

The default envelope payload storage policy is serialized JSON. Provider-specific JSON column types, JSON path indexes, computed columns, and database tuning remain user-owned EF Core configuration.

## Ownership and Evolution Semantics

Ownership semantics map to owned reference or owned collection policies. Version/revision, lifecycle state, and temporal validity members map as regular scalar members with optional configured indexes or alternate keys. `ExtensionData` is ignored by default unless configured as serialized JSON or summary metadata.

The package does not automatically replace primary keys, add global query filters, enable SQL Server temporal tables, or generate migrations from these semantics.

## Diagnostics

Unsupported or ambiguous mapping emits diagnostics. Typical cases include missing keys, unresolved relationship endpoints, invalid converter metadata, duplicate projected names, unsupported owned collections, unsupported many-to-many skip navigations, ambiguous inheritance strategy, unsupported envelope payload storage, invalid extension-data storage policy, and ambiguous ownership configuration.

## Non-Goals

This package does not replace EF Core application configuration. Users still own provider configuration, migrations, database creation, connection strings, transactions, seed data, runtime validation, and deployment.
