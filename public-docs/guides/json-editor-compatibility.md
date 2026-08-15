# JSON Editor Compatibility

## Use

JSON Editor compatibility is an export mode of `SemanticTypeModel.JsonSchema`; there is no separate
`SemanticTypeModel.JsonEditor` package.

```csharp
JsonSchemaExportResult export = JsonSchemaExporter.Export(
    result.Model,
    new JsonSchemaExportOptions
    {
        UiHintOptions = new UiHintOptions
        {
            StrictKnownHintsOnly = true,
            PreferUiTitleOverDisplayName = true,
        },
        UiExport = new JsonSchemaUiExportOptions
        {
            UiMode = JsonSchemaUiMode.JsonEditorCompatible,
            IncludeGenericUiAnnotations = true,
            IncludeJsonEditorCompatibilityAnnotations = true,
        },
    });
```

## Configure

Important controls are `UiMode`, JSON Editor compatibility-annotation inclusion, generic UI annotations,
strict-known-hint behavior, title mapping, ordering metadata, enum labels, and widget hints.

Compatibility annotations are output metadata, not standard JSON Schema validation semantics. Downstream JSON
Editor versions/plugins may interpret them differently.

## Diagnose

| Symptom | Likely cause | Fix |
|---|---|---|
| Unsupported widget | UI metadata names a widget not supported by compatibility export | Use a supported widget or omit it. |
| Invalid/duplicate order | UI ordering metadata conflicts | Use deterministic unique integer ordering. |
| Duplicate UI name | Selected title/name policy collapses members | Change display/semantic names or policy. |
| Downstream editor ignores an annotation | Editor version/plugin does not support the emitted extension | Test the exact downstream editor version and adjust compatibility options. |

## Reference

Compatibility mode emits schema annotations only; it does not host, configure, or theme a JSON Editor runtime.
See [JSON Schema](json-schema.md) for the surrounding export flow.
