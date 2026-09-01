# EF Core

## Use

SemanticTypeModel generates ordinary EF Core entity configuration from an explicitly selected semantic model
without owning the application's `DbContext` or unrelated entities.

Model project:

- reference `SemanticTypeModel.DotNet`;
- run `SemanticTypeModel.Generators` so the assembly contains a semantic manifest.

Persistence project:

- reference `SemanticTypeModel.EFCore`;
- run `SemanticTypeModel.EFCore.Generators`;
- reference the model project/assembly;
- select the model explicitly.

```csharp
[assembly: GenerateSemanticEfModel(typeof(FinanceModelMarker))]

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyFinanceSemanticModel();
}
```

Multiple semantic models and manual application entities compose normally:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyFinanceSemanticModel();
    modelBuilder.ApplyAccountingSemanticModel();
    modelBuilder.ApplyConfiguration(new AuditRecordConfiguration());
}
```

## Configure

Generated code emits one internal partial `IEntityTypeConfiguration<TEntity>` per semantic Entity. Customize an
entity through the generated partial hooks instead of editing generated source:

When independently generated models contain the same simple CLR type name, generated configuration type names
include the CLR metadata identity so both models can be selected in one application. Applications continue to
use the generated model registration extensions; configuration type names are implementation details.

```csharp
internal partial class AccountConfiguration
{
    static partial void ConfigureAfterGenerated(EntityTypeBuilder<Account> builder)
    {
        builder.HasIndex(x => x.DisplayName).IsUnique();
    }
}
```

`ConfigureAfterGenerated` is the normal application override point.

### Current mapping contract

| Semantic shape | Generated EF behavior |
|---|---|
| Entity | EF entity/table configuration |
| Entity inheritance | TPT; semantic base configuration before derived |
| Scalar | Property/column |
| Enum | String provider representation |
| `Uri` | String provider representation |
| `char` | String provider representation |
| Strong Scalar | Underlying provider scalar when supported; in owned JSON it is serialized as the scalar, not `{ "Value": ... }` |
| `byte[]` / `ReadOnlyMemory<byte>` | Binary mapping/conversion |
| Owned ValueKind/object/collection | JSON-converted property according to retained ownership/storage policy |
| Extension data | JSON storage |
| Nonentity | No standalone EF entity configuration |

The integration deliberately does **not** infer arbitrary navigations, call `OwnsOne`/`OwnsMany`, create
many-to-many mappings, offer TPH/TPC alternatives, generate a `DbContext`, choose a provider, create migrations,
or own database lifecycle.

`[SemanticStrongScalar]` is explicit nominal scalar meaning. It does not infer keys, identifiers, ownership,
or relational storage policy. The `SpecificationVersionId(Guid Value)` owned-JSON shape is supported through
the underlying GUID representation.

## Diagnose

| Diagnostic/symptom | Cause | Fix |
|---|---|---|
| `STM5037` | Selected model assembly has no semantic manifest | Run `SemanticTypeModel.Generators` in the model project and rebuild. |
| `STM5039` | Manifest version unsupported | Align all `SemanticTypeModel.*` package versions exactly. |
| `STM5041` | Two selected models own one CLR Entity | Select one owning semantic model. |
| `STM5044`/`STM5045` | CLR type/member no longer matches manifest | Clean/rebuild model and persistence projects; fix renamed/removed members. |
| `STM5046` | Member/entity shape violates supported EF contract | Use a supported scalar/identifier/binary/owned-JSON shape or handle mapping manually after generated configuration. |
| No generated config for a ValueKind/DTO | Only semantic Entities receive standalone configurations | Keep the type as a value shape or make its semantic role intentionally Entity. |
| `sbyte`, `ushort`, `uint`, or `ulong` member is diagnosed | Provider-independent EF mappings do not silently widen these integer forms | Choose a provider-specific/manual mapping or use a supported CLR representation; Strong Scalar wrapping does not bypass this boundary. |
| Manual entity disappears/changes | Application composition is wrong, not expected generated behavior | Generated config must touch only selected semantic Entities; inspect generated source and manual `OnModelCreating`. |

To inspect emitted source, enable `EmitCompilerGeneratedFiles`; see [Configuration](../configuration.md).

## Reference

- [Diagnostics](../diagnostics.md)
- [Troubleshooting](../troubleshooting.md)
- [Projection capabilities](projection-capabilities.md)
- executable example: `samples/code-first-ef-core/`
