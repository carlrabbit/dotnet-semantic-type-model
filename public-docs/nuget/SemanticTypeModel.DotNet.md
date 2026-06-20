# SemanticTypeModel.DotNet

## What this package does

`SemanticTypeModel.DotNet` provides .NET attribute vocabulary and Roslyn extraction support for code-first semantic model authoring.

## Install

```sh
dotnet add package SemanticTypeModel.DotNet --version 2.3.0
```

## Use when

- Install this package when you need .NET attribute vocabulary and Roslyn extraction support for code-first semantic model authoring.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.

## Minimal example

```csharp
using SemanticTypeModel;

[SemanticType(SemanticTypeRole.Entity)]
public sealed partial class Customer
{
    [SemanticKey]
    public required string Id { get; init; }
}
```

## Main APIs

| API | Purpose |
| --- | --- |
| `SemanticTypeAttribute` | Marks a .NET type for semantic extraction. |
| `SemanticKeyAttribute` | Marks an identity property. |
| `SemanticEnvelopeAttribute` | Marks wrapper/payload semantics. |
| `SemanticOwnedObjectAttribute` | Marks lifecycle-contained object members. |

## Works with

- SemanticTypeModel.Generators, SemanticTypeModel.Core, and projection packages.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not publish schemas, configure EF Core, or replace target-specific package options.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [Core semantics guide](../guides/core-semantics.md)
