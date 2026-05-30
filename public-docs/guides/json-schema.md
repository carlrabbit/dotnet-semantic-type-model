# JSON Schema Guide

`SemanticTypeModel.JsonSchema` imports and exports canonical semantic type models as JSON Schema documents.

```csharp
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.JsonSchema.Import;

JsonSchemaImportResult imported = JsonSchemaImporter.Import(schemaText);
JsonSchemaExportResult exported = JsonSchemaExporter.Export(imported.Model!);
```

Use `JsonSchemaExportOptions` for dialect and UI-hint settings. JSON Editor compatibility is an export mode of this package; see [json-editor-compatibility.md](json-editor-compatibility.md).
