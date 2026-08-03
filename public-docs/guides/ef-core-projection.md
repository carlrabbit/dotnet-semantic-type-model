# EF Core Relational Projection

## Contract

Version 2.5.0 has one CLR-backed application path. Explicit semantic entities become tables; semantic entity inheritance uses TPT. Scalar members become columns, enums become strings, strong identifiers become their underlying scalar, binary values remain binary, and explicitly owned ValueKind members become serialized JSON columns. `SemanticExtensionData` is persisted as a JSON object.

No value kind becomes an entity. The projection does not use `OwnsOne` or `OwnsMany`, does not create navigations or relationships, and does not inspect interfaces, generic constraints, record infrastructure, DTOs, repositories, framework helpers, static members, or method signatures.

## Usage

```csharp
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.EFCore;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var result = AppSemanticTypeModel.Create().DeriveEfRelationalModel();
        result.Diagnostics.ThrowIfErrors();
        modelBuilder.ApplySemanticRelationalModel(result.Model);
    }
}
```

Or use the equivalent convenience call:

```csharp
var result = modelBuilder.ApplySemanticTypeModel(AppSemanticTypeModel.Create());
```

## Diagnostics

Unsupported combinations are omitted and reported deterministically, including owned entities, undeclared ValueKind storage, entity object references, entity object collections, arbitrary dictionaries, unsupported scalars and strong-ID shapes, invalid inheritance, unexpected convention entities, non-serializable JSON values, and duplicate table or column names. Do not continue database startup when derivation contains errors.

## Provider strategy

JSON documents use deterministic `System.Text.Json` serialization through an EF value converter and structural comparison. The provider stores the converted value as a string column; provider-specific JSON querying is outside this contract.

The package does not choose a provider, create a `DbContext`, run migrations, create production databases, or configure relationships.
