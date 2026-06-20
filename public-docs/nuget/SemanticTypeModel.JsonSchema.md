# SemanticTypeModel.JsonSchema

## What this package does

`SemanticTypeModel.JsonSchema` provides JSON Schema domain-model derivation and JSON Schema Draft 2020-12 export from generated semantic models.

## Install

```sh
dotnet add package SemanticTypeModel.JsonSchema --version 2.3.0
```

## Use when

- Install this package when you need JSON Schema domain-model derivation and JSON Schema Draft 2020-12 export from generated semantic models.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.
- Export deterministic JSON Schema Draft 2020-12 documents.
- Emit JSON Editor-compatible UI hints from JSON Schema export options.

## Minimal example

```csharp
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;

var result = AppSemanticTypeModel.Create()
    .DeriveJsonSchemaModel(options => options.UseDefaultTransformations());
result.Diagnostics.ThrowIfErrors();
var document = JsonSchemaExporter.Export(result.Model);
```

## Main APIs

| API | Purpose |
| --- | --- |
| `DeriveJsonSchemaModel` | Derives the JSON Schema domain semantic model. |
| `JsonSchemaExporter.Export` | Exports a JSON Schema document. |
| `JsonSchemaExportOptions` | Controls export shape and UI compatibility. |
| `JsonSchemaUiMode.JsonEditorCompatible` | Enables JSON Editor-compatible UI hints. |

## Works with

- SemanticTypeModel.Core, SemanticTypeModel.Generators, and JSON Editor-compatible UI export options.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not make JSON Schema import the canonical authoring path and does not perform runtime JSON validation.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [JSON Schema guide](../guides/json-schema.md)
- [JSON Editor compatibility](../guides/json-editor-compatibility.md)
