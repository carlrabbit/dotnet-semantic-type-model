# EF Core Relational Projection

## Contract

Version 2.6.1 has one CLR-backed application path. Explicit semantic entities become tables; semantic entity inheritance uses TPT. Scalar members become columns, enums become string provider columns, strong identifiers become their underlying scalar, binary values remain binary, and explicitly owned ValueKind members become serialized JSON columns. `SemanticExtensionData` is persisted as a JSON object.

No value kind becomes an entity. The projection does not use `OwnsOne` or `OwnsMany`, does not create navigations or relationships, and does not inspect interfaces, generic constraints, record infrastructure, DTOs, repositories, framework helpers, static members, or method signatures.

Property declaration and relational storage are tracked separately. Semantic-base members are configured only on the semantic-base TPT table; members inherited from non-semantic CLR bases are stored by the first semantic entity; derived tables contain only derived state (plus the TPT key).

The package owns the EF convention boundary. Before semantic configuration it suppresses already discovered ValueKinds and other non-entities, and after configuration it removes new convention discoveries. The final EF CLR entity set is exactly the semantic Entity CLR set. JSON-owned objects and collection items remain converted properties, never owned or keyless EF entities; consumers do not call `ModelBuilder.Ignore(...)` for them.

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

Unsupported combinations are omitted and reported deterministically, including owned entities, undeclared ValueKind storage, entity object references, entity object collections, arbitrary dictionaries, unsupported scalars and strong-ID shapes, invalid inheritance, residual unexpected convention entities, non-serializable JSON values, and duplicate table or column names. `EF_UNEXPECTED_CONVENTION_ENTITY` is emitted only after deterministic correction and the final exact-set audit. Do not continue database startup when derivation contains errors.

## Provider strategy

JSON documents use deterministic `System.Text.Json` serialization through an EF value converter and structural comparison. The provider stores the converted value as a string column; provider-specific JSON querying is outside this contract.

The package does not choose a provider, create a `DbContext`, run migrations, create production databases, or configure relationships.
