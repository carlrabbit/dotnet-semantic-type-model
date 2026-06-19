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

## How it works

JSON Editor compatibility is an export mode of `SemanticTypeModel.JsonSchema`. It maps semantic display metadata and UI annotations to JSON Schema keywords and selected JSON Editor extension annotations. The repository does not ship a separate JSON Editor runtime package.

## Options and policies

| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
| `UiMode` | `JsonSchemaUiMode.None` | `None`, `JsonEditorCompatible` | Controls whether editor-specific output is emitted | Unknown modes are unsupported. |
| `IncludeJsonEditorCompatibilityAnnotations` | `false` | `true`, `false` | Emits selected JSON Editor-compatible annotations | Unsupported widgets are diagnostics. |
| `IncludeGenericUiAnnotations` | `false` | `true`, `false` | Emits generic `ui:*` extension annotations | Unknown hints may be diagnostics under strict mode. |
| Title mapping | Display name unless UI title is preferred | `SemanticDisplayName`, `SemanticName`, `ui.title` | Emits `title` | Duplicate UI names can collide. |
| Description mapping | Semantic description | `SemanticDescription` / documentation metadata | Emits `description` | Missing descriptions simply omit text. |
| Ordering metadata | No default | `SemanticOrder` or UI order annotations | Emits deterministic property ordering annotations | Invalid order values are diagnostics. |
| Enum labels | Enum names | `SemanticEnumValue(DisplayName=...)` | Emits display labels usable by editors | Labels without enum values are ignored/diagnosed. |
| Widget hints | None | Supported JSON Editor widget names through UI annotations | Emits widget annotation | Unsupported widgets are diagnostics; no custom plugin guarantee. |
| Version compatibility | Best-effort | JSON Editor-compatible annotations only | Helps common JSON Editor versions | Behavior can vary by JSON Editor version/plugin. |

## Schema fragment example

Without compatibility mode, semantic display metadata exports standard schema text only:

```json
{
  "type": "string",
  "title": "Target file path",
  "description": "Path used when the file provider is selected."
}
```

With `JsonEditorCompatible` and compatibility annotations enabled, the same member can include editor hints:

```json
{
  "type": "string",
  "title": "Target file path",
  "description": "Path used when the file provider is selected.",
  "options": { "inputAttributes": { "placeholder": "C:/archive/orders.json" } },
  "propertyOrder": 20
}
```

## Diagnostics

| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
| Unsupported widget diagnostic | UI metadata names a widget not supported by compatibility export | Use a supported widget hint or omit the widget. |
| Invalid ordering metadata | Order is duplicated, non-numeric, or cannot be represented | Use deterministic integer ordering. |
| Duplicate UI name | Title/name policy maps multiple properties to the same display key | Change display names or avoid title-as-name policies. |
| Version compatibility warning | Downstream JSON Editor version may not honor an emitted annotation | Test the schema in your selected JSON Editor version. |

## Common mistakes

- Looking for a `SemanticTypeModel.JsonEditor` package.
- Assuming JSON Editor compatibility annotations are standard JSON Schema validation keywords.
- Using custom widget names without checking diagnostics.
- Depending on one JSON Editor plugin version without testing exported fragments.

## Limitations

Compatibility mode does not guarantee behavior for every JSON Editor version or third-party plugin. It emits schema annotations only; it does not host, theme, or configure a UI runtime.

## Related docs

- [SemanticTypeModel.JsonSchema package](../nuget/SemanticTypeModel.JsonSchema.md)
- [JSON Schema guide](json-schema.md)
