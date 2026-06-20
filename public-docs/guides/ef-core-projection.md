# EF Core Projection

## Goal

Apply semantic metadata to EF Core `ModelBuilder` while leaving provider setup, migrations, and database operations under application control.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.3.0`.

## Packages

- `SemanticTypeModel.EFCore` for derivation and `ModelBuilder` projection.
- `Microsoft.EntityFrameworkCore` for EF Core metadata APIs.
- `SemanticTypeModel.Generators` and `SemanticTypeModel.DotNet` for code-first model generation.

## Minimal path

1. Generate the semantic model.
2. Derive the EF Core semantic model.
3. Check diagnostics.
4. Call `modelBuilder.ApplyEfCoreSemanticModel(result.Model)` inside `OnModelCreating`.
5. Add provider-specific EF Core configuration separately.

## Full example

```csharp
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.EFCore;

public sealed class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var result = AppSemanticTypeModel.Create().DeriveEfCoreModel(options =>
        {
            options.Projection = options.Projection with
            {
                ProjectUnannotatedObjectsAsEntities = false,
                ValueObjectProjectionMode = ValueObjectEfProjectionMode.Owned,
                AlternateKeyProjectionMode = AlternateKeyProjectionMode.UniqueIndex,
            };

            options.Envelopes.For<OrderEnvelope>()
                .UseEnvelopeAsEntity()
                .Payload(e => e.Payload)
                .StoreAsOwnedColumns("Payload_");
        });

        result.Diagnostics.ThrowIfErrors();
        modelBuilder.ApplyEfCoreSemanticModel(result.Model);
    }
}
```

## How it works

EF Core projection creates provider-neutral EF metadata from semantic roles, keys, relationships, ownership, converters, and envelope policies. The application still owns `DbContext` construction, provider selection, migrations, database creation, query filters, and provider-specific configuration.

## Options and policies

| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
| Unannotated objects | `false` | `ProjectUnannotatedObjectsAsEntities` true/false | Controls whether ordinary object types become EF entities | Unannotated types are ignored unless enabled. |
| Key discovery | Semantic keys required for normal entities | `SemanticKey`, key inference only if generated model contains it | Configures primary keys | Missing keys are diagnostics unless keyless entities are allowed. |
| Keyless entities | `false` | `AllowKeylessEntities` true/false | Allows query-like entity metadata without keys | Use intentionally; updates may not be possible. |
| Alternate keys | `AlternateKey` | `AlternateKey`, `UniqueIndex`, `AnnotationOnly` | Chooses EF alternate-key or index representation | Unsupported duplicate key names are diagnostics. |
| Required/nullability | Semantic/CLR metadata | Required and nullable members | Applies EF required/optional metadata | Contradictions are diagnostics. |
| Value object handling | `Flatten` | `Diagnose`, `Owned`, `Flatten`, `SerializeJson` | Projects nested values as owned, flattened, JSON, or diagnostic | Unsupported nested graphs follow `UnsupportedShapeBehavior`. |
| Owned object | Semantic ownership | `SemanticOwned(Kind=Object)` plus value-object policy | Configures owned/same-table or selected policy | Ownership cycles are diagnostics. |
| Owned collection | Semantic ownership | `SemanticOwned(Kind=Collection)` | Configures supported owned collection metadata | Provider-specific storage remains application-owned. |
| Relationship endpoints | Semantic relationship metadata | Principal type/key, FK, cardinality | Applies EF relationship metadata | Unresolved endpoints or ambiguous FK are diagnostics. |
| Table naming | CLR/semantic name | `PreferDisplayNamesForTableAndColumnNames` true/false; explicit annotations | Chooses table names | Name collisions follow collision behavior. |
| Column naming | CLR/semantic member name | Same naming policy as table/column names | Chooses column names | Duplicate projected names are diagnostics or suffixed. |
| Enum conversion | `String` | `String`, `Numeric` | Configures enum storage preference | Numeric storage can be unsupported for some enum metadata. |
| Unsupported scalar/shape | `Diagnose` | `Diagnose`, `IgnoreWithWarning`, `SerializeJson` | Controls arrays, dictionaries, unions, nested unsupported objects | Diagnose emits errors/warnings and skips unsafe shapes. |
| Envelope storage | Serialized JSON payload by policy object | `SerializedJson`, `OwnedJson`, `OwnedSameTable`, `OwnedSeparateTable`, `Ignored` | Maps envelope payload storage | Missing payload selection or unsupported provider shape is diagnostic. |
| Name collisions | `Diagnose` | `Diagnose`, `Suffix` | Errors/skips duplicates or appends deterministic suffixes | Suffixing can change expected database names. |

## Diagnostics

| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
| Missing key | Entity role without primary key | Add `SemanticKey` or set `AllowKeylessEntities` for read-only query types. |
| Relationship endpoint unresolved | Principal type, FK, or principal key cannot be matched | Use `SemanticRelationship` with explicit names. |
| Duplicate table/column name | Naming policy maps multiple members to same name | Change names or use `NameCollisionBehavior.Suffix`. |
| Unsupported owned collection | Selected policy cannot represent the collection provider-neutrally | Select JSON/separate-table policy or configure provider-specific EF manually. |
| Envelope payload storage diagnostic | Envelope policy lacks selected payload or uses unsupported storage | Mark one payload and choose an explicit storage policy. |

## Common mistakes

- Expecting this package to choose SQL Server, PostgreSQL, SQLite, or migrations.
- Marking every object as an entity when value-object ownership is intended.
- Using display names for database identifiers without reviewing collision risk.
- Assuming temporal validity creates EF temporal tables.

## Limitations

The package does not create `DbContext` types, choose a database provider, run migrations, create databases, enable temporal tables, configure query filters, or tune provider-specific JSON behavior.

## Related docs

- [SemanticTypeModel.EFCore package](../nuget/SemanticTypeModel.EFCore.md)
- [Code-first EF Core sample](../samples/code-first-ef-core.md)
- [Projection capabilities](projection-capabilities.md)
