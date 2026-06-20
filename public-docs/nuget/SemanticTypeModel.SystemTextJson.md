# SemanticTypeModel.SystemTextJson

## What this package does

`SemanticTypeModel.SystemTextJson` provides System.Text.Json metadata projection and resolver customization from semantic models.

## Install

```sh
dotnet add package SemanticTypeModel.SystemTextJson --version 2.3.0
```

## Use when

- Install this package when you need System.Text.Json metadata projection and resolver customization from semantic models.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.

## Minimal example

```csharp
using SemanticTypeModel.SystemTextJson;

var result = AppSemanticTypeModel.Create()
    .DeriveSystemTextJsonModel(options =>
        options.PropertyNameSource = SemanticJsonPropertyNameSource.ExistingJsonContract);
result.Diagnostics.ThrowIfErrors();
```

## Main APIs

| API | Purpose |
| --- | --- |
| `DeriveSystemTextJsonModel` | Derives resolver customization metadata. |
| `SemanticJsonPropertyNameSource` | Selects projected JSON name source. |
| `WithSemanticTypeModelJson` | Wraps an existing resolver or context. |
| `AddSemanticTypeModelJson` | Adds conservative resolver customization to options. |

## Works with

- SemanticTypeModel.DotNet, SemanticTypeModel.Generators, and user-authored JsonSerializerContext types.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not generate JsonSerializerContext declarations or replace custom converters.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [System.Text.Json guide](../guides/system-text-json.md)
