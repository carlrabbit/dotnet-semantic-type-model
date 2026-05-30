# JSON Editor Compatibility

JSON Editor compatibility is a feature of `SemanticTypeModel.JsonSchema`, not a standalone package.

Enable JSON Editor-compatible UI hints during JSON Schema export:

```csharp
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Export;

JsonSchemaExportResult result = JsonSchemaExporter.Export(model, new JsonSchemaExportOptions
{
    UiExport = new JsonSchemaUiExportOptions
    {
        UiMode = JsonSchemaUiMode.JsonEditorCompatible,
        IncludeJsonEditorCompatibilityAnnotations = true,
    },
});
```

The exporter emits supported JSON Editor-oriented keywords, such as property ordering and widget hints, alongside the JSON Schema document. Unsupported hints produce diagnostics rather than requiring a separate `SemanticTypeModel.JsonEditor` package.
