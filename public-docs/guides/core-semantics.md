# Core Semantics

## Goal

Model domain meaning once so JSON Schema, EF Core, Power BI, System.Text.Json, and configuration projections can make target-specific decisions from the same generated semantic model.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.3.0`.

## Packages

- `SemanticTypeModel.DotNet` for semantic attributes.
- `SemanticTypeModel.Generators` for compile-time provider generation.
- `SemanticTypeModel.Core` for core vocabulary, transformations, diagnostics, and inspection.

## Minimal path

1. Add `SemanticTypeModel.DotNet`, `SemanticTypeModel.Generators`, and `SemanticTypeModel.Core`.
2. Mark entities, keys, value objects, ownership, envelopes, lifecycle, and extension data with semantic attributes.
3. Build the project so the provider is generated.
4. Call `AppSemanticTypeModel.Create()` and inspect diagnostics.
5. Pass the model to the projection package needed by your scenario.

## Full example

```csharp
using SemanticTypeModel;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.DotNet;

[SemanticType(SemanticTypeRole.Entity)]
public sealed partial class Customer
{
    [SemanticKey]
    public required string Id { get; init; }

    [SemanticDisplayName("Customer name")]
    public required string Name { get; init; }

    [SemanticOwned(Kind = SemanticOwnershipKind.Object)]
    public required Address BillingAddress { get; init; }
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed partial class Address
{
    public required string City { get; init; }
}

TypeSchemaModel model = AppSemanticTypeModel.Create();
```

## How it works

Annotated .NET code is extracted by the generator into a `TypeSchemaModel`. Core transformations normalize projection-neutral semantics. Target packages then decide how much of that meaning can be represented in their own output.

## Options and policies

Core semantics has no target-output option that changes JSON Schema, EF Core, Power BI, System.Text.Json, or Configuration by itself. The policy is to keep domain meaning projection-neutral and put representation choices in target packages.

| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
| Authoring source | Annotated .NET types | Semantic attributes and imported supported BCL annotations | Produces the generated semantic model | Unsupported attribute placement or invalid values are diagnostics. |
| Semantic role | `Unspecified` unless declared | `Entity`, `ValueObject`, `Dimension`, `Fact`, `Lookup`, `Event`, `Configuration`, `Form` | Gives target projections role-specific defaults | Unknown role strings are unsupported. |
| Requiredness/nullability | CLR required/nullability plus annotations | Required, nullable, optional | Feeds schema `required`, EF requiredness, validation, and serializer metadata | Contradictory required/nullability can cause projection diagnostics. |
| Target metadata | No target effect in Core | Namespaced annotations such as JSON Schema, EF, Power BI, System.Text.Json, Configuration | Preserved for target packages | Core does not validate every target-specific value. |

## Vocabulary inventory

| Semantic item | Use when | Authoring shape / attribute | Projection-neutral meaning | Major projection effects | Common mistake |
|---|---|---|---|---|---|
| Entity | Object has identity and lifecycle | `SemanticTypeRole.Entity` | Independently identifiable domain object | EF table/entity, Power BI table, JSON object schema | Omitting a key for EF scenarios. |
| ValueObject | Object is identified by containing value | `SemanticTypeRole.ValueObject` | No independent identity | EF owned/flattened/JSON policy; Power BI flatten/diagnose policy | Modeling an aggregate root as a value object. |
| Configuration | Type describes options binding | `SemanticTypeRole.Configuration` plus section metadata | Options contract root | Configuration options registration | Expecting provider setup. |
| Dimension | Descriptive analytical table | `SemanticTypeRole.Dimension` | Analytical lookup context | Power BI dimension role | Using fact measures on dimensions. |
| Fact | Measurable analytical event/table | `SemanticTypeRole.Fact` | Analytical measurement grain | Power BI fact role and summarization | Missing relationship keys. |
| Event | Time/event occurrence | `SemanticTypeRole.Event` | Occurrence in domain time | May become schema/table metadata; limited target behavior | Assuming automatic event sourcing. |
| Key | Identity member | `SemanticKey` | Primary identity | EF primary key; schema metadata; Power BI technical key | Marking nullable keys. |
| AlternateKey | Secondary identity | `SemanticKey(Kind = KeyKind.Alternate)` | Alternative uniqueness | EF alternate key/unique index policy | Treating it as primary identity. |
| Relationship | Association between types | `SemanticRelationship` | Domain link and cardinality | EF relationships; Power BI relationships | Leaving endpoint or FK ambiguous. |
| Required | Value must be present | C# `required`, BCL validation, semantic required metadata | Presence requirement | JSON Schema `required`; options validation; EF required | Confusing required with non-null CLR default values. |
| Nullable | Null is allowed when present | Nullable reference/value types | Nullability permission | JSON Schema null type, EF optional, STJ unchanged unless resolver supports it | Treating nullable as optional. |
| Constraint | Scalar/collection bounds | `SemanticStringConstraints`, `SemanticNumericConstraints`, `SemanticCollectionConstraints` | Validation rule | JSON Schema keywords; configuration validation when representable | Expecting EF check constraints automatically. |
| RequiredWhen | Conditional presence | `SemanticRequiredWhen` | Target required when source equals literal | JSON Schema conditional; Configuration validation; other targets diagnose/ignore | Referencing a misspelled source property. |
| Enum | Closed set of named values | CLR enum, `SemanticEnumValue` | Enumerated domain value | JSON enum, EF/Power BI storage policy, schema labels | Depending on numeric values without policy. |
| Format | Well-known scalar format | `SemanticFormat` | Formatting/validation hint | JSON Schema `format`; Power BI format metadata | Using format as custom parsing code. |
| DisplayName | User-facing name | `SemanticDisplayName` / `SemanticName` | Label, not identity | Titles, table/column labels, UI text | Replacing stable member names with labels. |
| Description | Human explanation | `SemanticDescription` | Documentation text | Schema descriptions, Power BI descriptions, diagnostics | Putting machine policy in prose. |
| Category | Grouping label | `SemanticCategory` | Logical grouping | UI groups, Power BI display folders/categories | Expecting storage changes. |
| Order | Display order | `SemanticOrder` | Deterministic ordering hint | JSON/UI property order and docs | Treating it as sort-by-column data. |
| Ownership | Containment/lifecycle dependency | `SemanticOwned` | Owned member follows owner lifecycle | EF owned mapping; Power BI flatten/diagnose | Marking shared entities as owned. |
| OwnedObject | Single owned object | `SemanticOwned(Kind = Object)` | One owned value | EF owned reference; JSON nested object | Forgetting requiredness on owned object. |
| OwnedCollection | Collection of owned values | `SemanticOwned(Kind = Collection)` | Owned element collection | EF collection policy; JSON array | Assuming every target supports nested collections. |
| Envelope | Wrapper around payload and metadata | `SemanticEnvelope` | Separates payload from transport/context metadata | Target-specific envelope policies | Projecting both wrapper and payload accidentally. |
| EnvelopePayload | Payload member inside envelope | `SemanticEnvelopePayload` | Business payload of envelope | JSON root policy; EF/Power BI payload policy | Omitting exactly one payload. |
| Version | Contract or data version | `SemanticVersion` / `SemanticVersioned` | Version marker | Preserved or displayed; no migration | Expecting automatic migrations. |
| Revision | Revision marker | `SemanticRevision` | Revision identity | Preserved/metadata; no concurrency behavior by default | Treating as EF rowversion. |
| CurrentVersion | Current flag/version pointer | `SemanticCurrentVersion` | Marks current revision/version | Metadata for projections | Expecting filtering. |
| TemporalValidity | Valid-time interval | `SemanticTemporalValidity`, `SemanticValidFrom`, `SemanticValidTo` | Domain validity range | Metadata or target-specific columns | Expecting EF temporal tables. |
| LifecycleState | State/status member | `SemanticLifecycleState` | Domain state value | Schema/Power BI metadata | Expecting workflow enforcement. |
| ExtensionData | Unknown/member extension bag | `SemanticExtensionData` | Extra data capture | JSON additional properties/STJ extension behavior; EF/Power BI limited | Using arbitrary object types instead of dictionary-like members. |

## Diagnostics

| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
| Invalid attribute usage | Semantic attribute placed on unsupported target | Move it to the supported type/member target. |
| Ownership cycle | Owned members recursively own each other | Break the cycle or use relationship semantics. |
| Envelope payload diagnostic | Envelope has zero or multiple payload members | Mark exactly one member with `SemanticEnvelopePayload`. |
| RequiredWhen diagnostic | Source member, operator, or literal cannot be resolved | Use `nameof`, equality-compatible literals, and supported scalar/enum values. |

## Common mistakes

- Using display names as stable model identifiers.
- Putting EF, Power BI, or JSON Schema decisions into core semantics instead of target options.
- Assuming preserved metadata means every projection enforces the behavior.

## Limitations

Core semantics does not create target output by itself. It does not choose database providers, publish analytical models, validate JSON documents at runtime, generate serializer contexts, or migrate configuration.

## Related docs

- [SemanticTypeModel.Core package](../nuget/SemanticTypeModel.Core.md)
- [SemanticTypeModel.DotNet package](../nuget/SemanticTypeModel.DotNet.md)
- [Projection capabilities](projection-capabilities.md)
