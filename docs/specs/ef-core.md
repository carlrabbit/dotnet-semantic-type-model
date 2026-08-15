# EF Core Contract

## Status

Current EF Core specification for the generated-configuration architecture.

This specification replaces the prior runtime closed-`ModelBuilder`, convention-suppression, source-lineage application, exact-entity-allowlist, role-aware storage-mode, and generated-configuration fragment specifications.

## Purpose

Define the supported EF Core boundary for:

- provider-neutral relational derivation and inspection;
- compile-time semantic manifest consumption;
- explicit semantic-model selection in persistence projects;
- source-generated `IEntityTypeConfiguration<TEntity>` application;
- provider-neutral storage policy;
- composition with multiple semantic models and application-owned entities;
- consumer extension points;
- diagnostics and validation expectations.

## Architectural Invariant

> A generated semantic EF model configures only the CLR entities represented and owned by the selected semantic model. The application owns final `DbContext` composition.

Generated STM configuration must not enumerate, remove, ignore, reject, or audit unrelated entities in the surrounding EF model.

## Package Boundary

### `SemanticTypeModel.EFCore`

Owns:

- provider-neutral relational derivation/inspection contracts;
- semantic-model selection API used by persistence projects;
- shared converter/comparer/helper primitives required by generated configuration;
- EF-specific semantic diagnostics/contracts that are not analyzer packaging concerns.

`DeriveEfRelationalModel` remains a provider-neutral inspection/derivation API. It is not the supported mechanism for globally applying/repairing a mutable `ModelBuilder`.

### `SemanticTypeModel.EFCore.Generators`

Owns compile-time CLR-to-EF application for selected semantic models.

It is installed in the persistence project as an analyzer/source-generator package.

## Intended Project Layering

```text
Domain / semantic-model project
  references SemanticTypeModel.DotNet
  references SemanticTypeModel.Generators as analyzer
  does not require EF Core
        |
        v
Persistence project
  references the domain/model project
  references SemanticTypeModel.EFCore
  references SemanticTypeModel.EFCore.Generators as analyzer
  references EF Core/provider packages
```

One persistence project may select multiple semantic-model assemblies.

## Compile-Time Semantic Manifest

`SemanticTypeModel.Generators` emits deterministic semantic manifest metadata for generator-to-generator consumption.

The current manifest schema version is 1.

The manifest contains enough canonical semantics and CLR lineage for EF generation, including the identities/shapes required to locate semantic types and members, determine entity/value roles, apply keys/requiredness, classify scalar/value storage, and preserve inheritance/member placement.

The manifest is:

- generated from code-first semantic authority;
- deterministic;
- assembly metadata consumed through compiler/Roslyn metadata;
- not loaded by executing the referenced model assembly;
- not a second user-authored semantic model format.

Compatibility/evolution policy between future manifest schema versions is not defined by this specification.

## Explicit Model Selection

A persistence assembly explicitly selects the semantic-model assemblies it wants to project to EF, conceptually:

```csharp
[assembly: GenerateSemanticEfModel(typeof(FinanceModelMarker))]
[assembly: GenerateSemanticEfModel(typeof(AccountingModelMarker))]
```

Requirements:

- selection is explicit and strongly typed;
- the generator does not automatically scan every transitive reference for semantic manifests;
- multiple selections are supported;
- unrelated referenced semantic models are not silently imported;
- selected manifests must be valid and readable by the generator;
- duplicate ownership of the same CLR Entity across selected semantic models is a generation error, not an ordering convention.

## Generated Configuration Shape

For each semantic `Entity`, the generator emits one internal partial configuration implementing `IEntityTypeConfiguration<TEntity>`.

Conceptually:

```csharp
internal partial class ImportSpecificationConfiguration
    : IEntityTypeConfiguration<ImportSpecification>
{
    public void Configure(EntityTypeBuilder<ImportSpecification> builder)
    {
        ConfigureBeforeGenerated(builder);
        ConfigureGenerated(builder);
        ConfigureAfterGenerated(builder);
    }

    private static void ConfigureGenerated(
        EntityTypeBuilder<ImportSpecification> builder)
    {
        // deterministic ordinary EF Core configuration calls
    }

    static partial void ConfigureBeforeGenerated(
        EntityTypeBuilder<ImportSpecification> builder);

    static partial void ConfigureAfterGenerated(
        EntityTypeBuilder<ImportSpecification> builder);
}
```

No `IEntityTypeConfiguration<T>` is generated for structural value shapes, enums, DTOs, interfaces, repositories, non-semantic base classes, or other non-entities.

Those shapes may still contribute semantic/storage information to an owning Entity configuration.

## Generated Registration

Each selected semantic model receives one deterministic public registration extension, conceptually:

```csharp
modelBuilder.ApplyFinanceSemanticModel();
```

Registration rules:

- only configurations generated for that selected semantic model are applied;
- semantic base entities are registered before derived entities;
- remaining ordering is stable/deterministic by semantic/CLR identity;
- `ApplyConfigurationsFromAssembly` is not the canonical STM registration mechanism because STM already knows the exact generated configuration set.

## Application Composition

Normal application usage is conceptually:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyFinanceSemanticModel();
    modelBuilder.ApplyAccountingSemanticModel();
    modelBuilder.ApplyConfiguration(new AuditRecordConfiguration());
}
```

The application may combine:

- multiple independently generated semantic models;
- ordinary manually configured EF entities;
- framework/application-specific EF metadata.

An STM generated model must not enforce an exact global entity set.

## Consumer Extension and Hotfix Boundary

Generated configuration is inspectable generated C# and exposes two partial hooks:

```text
ConfigureBeforeGenerated
  -> STM generated configuration
  -> ConfigureAfterGenerated
```

`ConfigureAfterGenerated` is the normal application override/hotfix boundary because it runs after STM's generated mapping.

Consumers may use ordinary `EntityTypeBuilder<TEntity>` APIs inside matching partial classes. Generated source itself is not manually edited or committed.

The generator does not require fine-grained custom hooks for every EF feature; the full builder hook is the escape hatch for application-owned configuration and provider-specific behavior.

## Provider-Neutral Storage Policy

The default mapping is intentionally narrow and opinionated.

| Semantic shape | EF representation |
|---|---|
| semantic `Entity` | EF entity/table |
| semantic Entity inheritance | TPT inheritance |
| direct scalar | scalar column |
| enum | string provider value |
| `System.Uri` | string provider value |
| strong scalar/identifier with supported underlying shape | underlying provider scalar with conversion/comparison as required |
| `byte[]` | direct binary property |
| `ReadOnlyMemory<byte>` | supported binary conversion |
| owned structural value object | JSON-converted property column |
| owned structural collection | JSON-converted array/property column |
| nested structural value shape | nested JSON representation within the owning value |
| semantic extension data | JSON object column |

Requiredness/nullability is applied from semantic meaning rather than left solely to EF convention inference.

## Ownership and Structural Values

Semantic ownership is lifecycle containment, not EF owned-entity configuration.

Current EF policy therefore does **not** infer:

```text
OwnsOne
OwnsMany
entity navigation relationships
many-to-many relationships
```

from canonical ownership alone.

Owned structural value shapes are represented according to the current JSON storage policy. A semantic Entity remains an Entity rather than becoming a structural value because another member references it.

Unsupported or ambiguous unowned object/array/dictionary shapes require diagnostics or explicit application configuration; STM must not guess a relationship/storage model.

## Scalars and Strong Shapes

Provider-neutral scalar classification supports the CLR shapes defined by the current shared EF storage policy, including common primitives, GUID/date/time values, enums, URI, binary shapes, and supported strong scalar/identifier wrappers.

Unsupported scalar/strong shapes must produce deterministic diagnostics rather than silently converting through an arbitrary string representation.

## Relationship Policy

STM EF integration is not a general relationship inference engine.

The provider-neutral contract does not infer arbitrary domain navigations or relationship cardinality from CLR references/collections.

Applications remain free to add ordinary EF relationships through `ConfigureBeforeGenerated`, `ConfigureAfterGenerated`, or separate application-owned configurations where those relationships are outside STM's semantic storage contract.

## Inheritance

Semantic Entity inheritance uses TPT in the current provider-neutral policy.

TPH/TPC selection is not part of the current STM contract.

The generator must preserve correct member placement and base-before-derived configuration deterministically.

## Diagnostics

The EF generator owns deterministic diagnostics for invalid/missing manifests, unsupported selections/mappings, duplicate CLR Entity ownership, and other generation failures.

The current generator diagnostic surface occupies STM5037-STM5046. Diagnostic IDs are stable contracts and must follow the repository diagnostics specification; do not reuse or renumber IDs during documentation consolidation.

## Testing Contract

EF behavior must be validated at the layer where the behavior exists.

### Provider-neutral relational tests

Use semantic-model/domain-model tests to validate pure derivation and storage-policy decisions.

### CLR/Roslyn and generator tests

Use real compilation/source-generator tests to validate:

- semantic manifest emission/consumption;
- model selection;
- generated configuration shape;
- deterministic registration;
- diagnostics;
- compilation of generated source;
- `ConfigureBeforeGenerated`/`ConfigureAfterGenerated` hooks.

### Real EF/provider integration

Use compiled CLR models, a real `DbContext`, EF model finalization, and provider-backed tests for behavior that crosses EF conventions/provider metadata.

Where persistence semantics matter, validate create/save/reload behavior rather than metadata alone.

Composition tests must cover multiple independently compiled semantic-model assemblies plus application-owned EF entities in one persistence project/`DbContext`.

### Packed-package smoke

`SemanticTypeModel.EFCore.Generators` must be tested from the packed NuGet artifact when validating analyzer discovery, private dependencies, package asset layout, cross-project manifest discovery, or consumer compilation.

A project-reference-only test does not prove the packaged analyzer works.

## Deliberate Non-Goals

The current provider-neutral EF contract does not own or infer:

- arbitrary domain navigation mapping;
- `OwnsOne` / `OwnsMany` semantic storage;
- automatic many-to-many relationships;
- TPH/TPC options;
- provider-specific JSON query semantics;
- `DbContext` generation;
- migration generation/ownership;
- production database creation/lifecycle;
- automatic selection of all referenced semantic models;
- per-entity manual-mode opt-out from generated configuration.

Provider-specific or application-specific EF behavior should normally be implemented through ordinary application configuration unless a future explicit contract adds a target/provider extension.

## Retired Runtime Application Architecture

The following are not part of the current supported architecture and must not be reintroduced as compatibility behavior without a new explicit architectural decision:

- runtime `ApplySemanticTypeModel` / `ApplySemanticRelationalModel` as global model application APIs;
- global convention suppression/cleanup owned by one semantic model;
- removing unrelated EF entities from `ModelBuilder`;
- exact global semantic entity-set enforcement;
- final-model audits that reject unrelated application entities.

The reason is composability: one selected semantic model does not own the global mutable EF model.
