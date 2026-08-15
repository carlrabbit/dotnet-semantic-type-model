# EF Core Relational Projection

## Goal

Generate ordinary EF Core entity configurations from an explicitly selected semantic model without changing unrelated entities in the surrounding `DbContext`.

## Prerequisites

The model assembly targets .NET 10, references `SemanticTypeModel.DotNet`, and runs `SemanticTypeModel.Generators`. The persistence project references the model assembly and EF Core 10.

## Packages

- Model project: `SemanticTypeModel.DotNet` and private analyzer `SemanticTypeModel.Generators`.
- Persistence project: `SemanticTypeModel.EFCore` and private analyzer `SemanticTypeModel.EFCore.Generators`.

## Minimal path

```csharp
[assembly: GenerateSemanticEfModel(typeof(FinanceModelMarker))]

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyFinanceSemanticModel();
}
```

The marker selects exactly one referenced assembly manifest. There is no transitive-reference scan.

## Full example

```csharp
[assembly: GenerateSemanticEfModel(typeof(FinanceModelMarker))]
[assembly: GenerateSemanticEfModel(typeof(AccountingModelMarker))]

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyFinanceSemanticModel();
    modelBuilder.ApplyAccountingSemanticModel();
    modelBuilder.ApplyConfiguration(new AuditRecordConfiguration());
}
```

Application customization uses the generated configuration's partial class:

```csharp
internal partial class AccountConfiguration
{
    static partial void ConfigureAfterGenerated(EntityTypeBuilder<Account> builder)
    {
        builder.HasIndex(account => account.DisplayName).IsUnique();
    }
}
```

## How it works

`SemanticTypeModel.Generators` emits deterministic manifest schema version 1 as assembly metadata. It contains semantic type and property identities, CLR type/member/declaring lineage, type shape, role, ownership, nullability, keys, and inheritance inputs. The persistence generator reads that metadata through Roslyn without loading or executing the model assembly.

It emits one internal partial `IEntityTypeConfiguration<TEntity>` for each semantic Entity and no configuration for ValueKinds, enums, configuration types, or other nonentities. Generated `Configure` calls `ConfigureBeforeGenerated`, direct EF calls, and then `ConfigureAfterGenerated`. The public registration extension applies semantic base configurations before derived configurations.

Generated code configures only selected-model Entities. It never enumerates, removes, rejects, or validates unrelated EF entity types. Multiple semantic models and manual entities therefore compose normally.

## Options and policies

| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
| Model selection | No model selected | Repeatable `GenerateSemanticEfModel(typeof(Marker))` | Generates one registration extension per selected manifest | STM5037-STM5040 |
| Entity mapping | Generated | Semantic Entity only | Table and `IEntityTypeConfiguration<TEntity>` | STM5044-STM5046 |
| Inheritance | TPT | Semantic CLR base/derived Entity chain | Base registration precedes derived | STM5046 when lineage is invalid |
| ValueKind storage | Explicit ownership | Owned object, owned collection, extension data | One JSON-converted property column | STM5046 for undeclared/invalid storage |
| URI storage | `System.Uri` or nullable `System.Uri` | URI value | String column through generated converter | Manifest nullability controls `IsRequired` |
| Customization | Generated mapping | Before and after partial hooks | `AfterGenerated` is the normal override/hotfix point | Compile error for an incorrect partial signature |
| Relationships | None | Identifier-shaped scalar members | No navigation inference | STM5046 for entity/object/collection shapes |

## Supported items

| Semantic item | Target behavior | Default | Override / policy | Diagnostics |
|---|---|---|---|---|
| Entity | Table and generated configuration | Included | After hook can refine normal EF metadata | STM5044-STM5046 |
| Entity inheritance | TPT | Base before derived | No TPH/TPC option | STM5046 |
| Scalar | Column | Direct property mapping | After hook | STM5046 |
| Enum | String provider column | `HasConversion<string>()` | After hook | STM5046 |
| Strong identifier | Underlying scalar provider value | `Value` plus matching constructor | After hook | STM5046 |
| Binary | Binary property/conversion | `byte[]` or `ReadOnlyMemory<byte>` | After hook | STM5046 |
| Owned ValueKind/object collection | JSON string conversion and structural comparer | Requires semantic ownership | After hook | STM5046 |
| Nonentity | No EF configuration | Excluded | None | None |

## Diagnostics

| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
| STM5037 | Selected model project did not emit a manifest | Install/run `SemanticTypeModel.Generators` in that project |
| STM5039 | Generator versions disagree on manifest schema | Align model and EF generator package versions |
| STM5041 | Two selected manifests own one CLR Entity | Select a single owning model |
| STM5045 | CLR member changed after manifest generation | Rebuild the model project and correct the member |
| STM5046 | Member shape violates the retained EF storage policy | Use a supported scalar/identifier/binary shape or explicit ValueKind ownership |

## Common mistakes

- Installing the EF generator in the model project instead of the persistence project.
- Forgetting the assembly-level selection attribute.
- Calling `ApplyConfigurationsFromAssembly` instead of the generated explicit extension.
- Editing generated files instead of implementing `ConfigureAfterGenerated`.
- Expecting navigation inference, `OwnsOne`, or `OwnsMany`.

## Limitations

The integration does not choose a provider, create migrations, create production databases, infer relationships, offer per-entity opt-out, or provide a materialization CLI. Provider-specific JSON querying remains provider-owned.

Generated documents are visible in IDE generated-source nodes. To write physical files for inspection:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files are not committed.

## Related docs

- [EF generator package](../nuget/SemanticTypeModel.EFCore.Generators.md)
- [Projection capabilities](projection-capabilities.md)
- [Diagnostics](../diagnostics.md)
- [Code-first EF sample](../samples/code-first-ef-core.md)
