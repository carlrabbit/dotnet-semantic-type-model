# Core Semantics

## Goal

Model domain meaning once so JSON Schema, EF Core, Power BI, System.Text.Json, and configuration projections can make target-specific decisions from the same generated semantic model.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.2.0`.

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

[SemanticType(SemanticTypeRole.Entity)]
public sealed partial class Customer
{
    [SemanticKey]
    public required string Id { get; init; }

    [SemanticName("Customer name")]
    public required string Name { get; init; }

    [SemanticOwnedObject]
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

Annotated .NET code is extracted by the generator into a `TypeSchemaModel`. Core transformations normalize projection-neutral semantics. The target package derives a domain semantic model and then exports or applies target-specific output when that target supports it.

## Options and policies

Use projection-neutral attributes for entity identity, value objects, requiredness, ownership, envelope payloads, lifecycle state, temporal validity, and extension data. Use target annotations only for representation choices such as JSON Schema keywords, EF Core table names, Power BI display folders, or System.Text.Json property names.

## Diagnostics

Core diagnostics report ambiguous semantics such as missing envelope payloads, duplicate semantic members, invalid temporal endpoints, unsupported extension-data shapes, and ownership cycles. Fix the source annotations first, then rerun target derivation.

## Common mistakes

- Treating JSON Schema files as the canonical authoring source for new models.
- Mixing target-specific metadata with projection-neutral semantics.
- Skipping diagnostic inspection before using projected output.
- Using stale pre-2.2 model namespace or shape names in current examples.

## Limitations

Core semantics do not create target output by themselves. They do not choose database providers, publish analytical models, validate JSON documents at runtime, or generate serializer contexts.

## Related docs

- [SemanticTypeModel.Core package](../nuget/SemanticTypeModel.Core.md)
- [SemanticTypeModel.DotNet package](../nuget/SemanticTypeModel.DotNet.md)
- [Projection capabilities](projection-capabilities.md)
