# JSON Editor Compatibility

## Goal

Emit JSON Schema with JSON Editor-compatible UI hints without installing a separate JSON Editor package.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.2.0`.

## Packages

- `SemanticTypeModel.JsonSchema` for JSON Schema export and UI compatibility mode.
- `SemanticTypeModel.DotNet` and `SemanticTypeModel.Generators` for code-first model generation.

## Minimal path

1. Derive a JSON Schema semantic model.
2. Export with `JsonSchemaUiMode.JsonEditorCompatible`.
3. Include JSON Editor compatibility annotations when needed.
4. Review diagnostics for unsupported UI hints.

## Full example

```csharp
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Export;

JsonSchemaExportResult export = JsonSchemaExporter.Export(result.Model, new JsonSchemaExportOptions
{
    UiExport = new JsonSchemaUiExportOptions
    {
        UiMode = JsonSchemaUiMode.JsonEditorCompatible,
        IncludeJsonEditorCompatibilityAnnotations = true,
    },
});
```

## How it works

Annotated .NET code is extracted by the generator into a `TypeSchemaModel`. Core transformations normalize projection-neutral semantics. The target package derives a domain semantic model and then exports or applies target-specific output when that target supports it.

## Options and policies

Use UI export options to select compatibility mode and decide whether JSON Editor-specific annotations are included. Keep UI hints target-specific; do not model them as core domain meaning.

## Diagnostics

The exporter reports unsupported widgets, invalid ordering metadata, duplicate UI names, and hints that cannot be represented safely in JSON Editor-compatible output.

## Common mistakes

- Treating JSON Schema files as the canonical authoring source for new models.
- Mixing target-specific metadata with projection-neutral semantics.
- Skipping diagnostic inspection before using projected output.
- Using stale pre-2.2 model namespace or shape names in current examples.

## Limitations

There is no standalone `SemanticTypeModel.JsonEditor` package. Compatibility mode does not guarantee behavior for every JSON Editor version or third-party plugin.

## Related docs

- [SemanticTypeModel.JsonSchema package](../nuget/SemanticTypeModel.JsonSchema.md)
- [JSON Schema guide](json-schema.md)
