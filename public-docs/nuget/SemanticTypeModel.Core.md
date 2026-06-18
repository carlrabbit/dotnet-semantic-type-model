# SemanticTypeModel.Core

## What this package does

`SemanticTypeModel.Core` provides core semantic vocabulary, transformation pipeline, diagnostics helpers, and inspection support for canonical semantic models.

## Install

```sh
dotnet add package SemanticTypeModel.Core --version 2.2.0
```

## Use when

- Install this package when you need core semantic vocabulary, transformation pipeline, diagnostics helpers, and inspection support for canonical semantic models.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.

## Minimal example

```csharp
using SemanticTypeModel.Core.Transformation;

var result = model.Transform(options =>
{
    options.UseCoreDefaults();
});
result.Diagnostics.ThrowIfErrors();
```

## Main APIs

| API | Purpose |
| --- | --- |
| `CoreSemanticAnnotationKeys` | Names for projection-neutral semantic annotations. |
| `ISemanticModelTransformation` | Transformation step contract. |
| `SemanticModelTransformationPipeline` | Deterministic transformation pipeline. |
| `DiagnosticCollectionExtensions` | Helpers for checking diagnostic results. |

## Works with

- SemanticTypeModel.Abstractions, SemanticTypeModel.DotNet, SemanticTypeModel.Generators, and projection packages.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not extract .NET code, generate providers, or export target-specific artifacts by itself.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [Core semantics guide](../guides/core-semantics.md)
