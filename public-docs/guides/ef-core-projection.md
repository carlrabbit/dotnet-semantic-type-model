# EF Core Projection

## Goal

Apply semantic metadata to EF Core `ModelBuilder` while leaving provider setup, migrations, and database operations under application control.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.4.0`.

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

### Nullable value-type projection

EF Core projection preserves nullable scalar value types as `Nullable<T>` in the projected EF model and in the applied EF Core `IProperty` metadata. Use tests, not public samples, for exhaustive nullable scalar matrices.

### Audience-specific descriptions

EF Core maps `TechnicalDescription` to provider-neutral table and column comments. XML `<summary>` is a technical-description fallback, so it can become an EF Core comment. `UserDescription` is retained for user-facing projections and is not used as a silent EF Core comment fallback.

## 2.4.1 Extension-Data Dictionary Note

2.4.1 corrects a 2.4.0 extraction defect for dictionary-backed extension data. Valid extension-data dictionaries such as `Dictionary<string, JsonElement>` are preserved in the canonical model with resolvable key and value types; projection-specific behavior remains unchanged.

## 2.4.2 role-aware owned storage

`SemanticOwned` no longer means “flatten.” EF Core first considers ownership kind, target role, and target shape. Owned value objects follow `ValueObjectProjectionMode`: `Flatten` creates scalar/enum columns and `SerializeJson` creates one provider-neutral string JSON column. `Owned` is explicitly diagnosed because true `OwnsOne` application is not yet implemented. Object-role owned members, entity-role owned members, and owned collections require explicit policies and are not silently flattened or serialized. `Uri` scalars project as strings in the provider-neutral model.

## 2.4.4 closed EF application

SemanticTypeModel owns a closed EF semantic model; EF Core conventions are not semantic authority. `ApplySemanticTypeModel` is the convenience path that derives an `EfCoreSemanticModel` and passes it to the same closed engine used by the lower-level `ApplyEfCoreSemanticModel`. The derived model preserves source type, CLR type, property/member, declaring type, semantic role, ownership, storage, and semantic-only suppression lineage.

Closed CLR application is the default (`EfCoreApplicationMode.ClosedClrModel`). It suppresses convention-discovered members absent from the semantic contract, including inherited extension data, keeps value objects reachable only through semantic ownership, and rejects value objects exposed as root `DbSet<T>` types. A lineage-free model fails explicitly with `EFCORE_SOURCE_LINEAGE_REQUIRED`; derive it again from the canonical model or use `ApplySemanticTypeModel`.

Shared-type projection is secondary and explicit:

```csharp
modelBuilder.ApplyEfCoreSemanticModelAsSharedTypes(efModel);
```

The former `ClrConventionAugmentation` and `SharedTypeProjection` enum names remain obsolete compatibility aliases. They no longer describe the preferred authority model. `[NotMapped]` is not required for the supported closed path.

## 2.4.5 source lineage and derivation policy

Select application policy during derivation, including when the model will be applied later:

```csharp
var result = model.DeriveEfCoreModel(options =>
{
    options.ApplicationMode = EfCoreApplicationMode.SharedTypeModel;
});
```

`ClosedClrModel` remains the default and requires resolvable CLR type and public-member lineage. These failures are errors. `SharedTypeModel` makes CLR lineage optional and reports unavailable CLR lineage as warnings; invalid semantic ownership shapes remain errors. Check `result.Diagnostics` for `EFCORE_OWNED_TARGET_TYPE_NOT_FOUND`, `EFCORE_OWNED_TARGET_TYPE_AMBIGUOUS`, `EFCORE_OWNED_TARGET_SHAPE_UNSUPPORTED`, `EFCORE_OWNED_COLLECTION_LINEAGE_POLICY_REQUIRED`, `EFCORE_SOURCE_LINEAGE_CLR_TYPE_NOT_RESOLVED`, and `EFCORE_SOURCE_LINEAGE_MEMBER_NOT_FOUND`.

`ApplySemanticTypeModel(...)` uses this same derivation path and returns both derivation and application-relevant diagnostics; owned target mistakes no longer surface as raw LINQ exceptions.


## 2.4.6 real-application compatibility validation

EF compatibility is validated at three complementary layers: focused unit tests prove projection and source-lineage mechanics, real CLR `DbContext` tests exercise actual `ModelBuilder` construction, and SQLite in-memory integration tests prove provider-backed model creation and basic persistence. Source lineage follows the EF projection/application scope rather than every canonical object definition, so unrelated interfaces, framework helpers, DTOs, and repository abstractions do not become EF candidates.

Provider-backed SQLite checks live in the dedicated `SemanticTypeModel.EFCore.Tests.Integration` project so native-provider validation remains separate from the short-running unit-test surface.

Closed CLR application suppresses projected CLR members that require a converter or unsupported-shape storage policy which has not been configured, and reports `EFCORE_SOURCE_LINEAGE_STORAGE_UNSUPPORTED`. In 2.4.6 this includes record-struct identifiers and `ReadOnlyMemory<byte>` payloads in the run-state regression fixture; SQLite round-trip coverage verifies supported keys and owned values while metadata assertions verify that unsupported members are absent rather than silently convention-mapped.
