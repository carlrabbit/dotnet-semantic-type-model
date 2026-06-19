# JSON Schema

## Goal

Export deterministic JSON Schema Draft 2020-12 documents from a generated code-first semantic model.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.2.0`.

## Packages

- `SemanticTypeModel.JsonSchema` for derivation and export.
- `SemanticTypeModel.Generators` and `SemanticTypeModel.DotNet` when the model comes from annotated code.
- `SemanticTypeModel.Core` for transformations and diagnostics.

## Minimal path

1. Generate a `TypeSchemaModel` from annotated .NET code.
2. Call `DeriveJsonSchemaModel(options => options.UseDefaultTransformations())`.
3. Check diagnostics.
4. Call `JsonSchemaExporter.Export(result.Model)`.
5. Write or inspect the exported document.

## Full example

```csharp
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;

var result = AppSemanticTypeModel.Create()
    .DeriveJsonSchemaModel(options => options.UseDefaultTransformations());

result.Diagnostics.ThrowIfErrors();

JsonSchemaExportResult export = JsonSchemaExporter.Export(result.Model, new JsonSchemaExportOptions
{
    SchemaId = new Uri("https://example.invalid/schemas/customer.schema.json"),
    IncludeProjectionAnnotations = false,
    UiExport = new JsonSchemaUiExportOptions
    {
        UiMode = JsonSchemaUiMode.JsonEditorCompatible,
        IncludeJsonEditorCompatibilityAnnotations = true,
    },
});

Console.WriteLine(export.Document.RootElement.GetRawText());
```

## How it works

JSON Schema derivation consumes the generated semantic model, applies default transformations when requested, then exports a schema document. The exporter is code-first only for new consumers; JSON Schema import is not the recommended authoring path.

## Options and policies

| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
| Dialect | `Draft202012` | `JsonSchemaDialect.Draft202012` | Emits Draft 2020-12 schema keywords | Other dialects are unsupported. |
| `SchemaId` | No default | Any absolute or relative `Uri` accepted by caller | Emits `$id` | Invalid URI construction fails before export. |
| Root selection policy | Domain root | Object roots from the derived model; envelope policy when envelope semantics exist | Chooses exported root schema | Ambiguous envelope payload policy is diagnostic. |
| Envelope wrapper vs payload | Wrapper unless a target policy selects payload | Envelope wrapper, payload-only, or target-specific policy when available | Controls whether envelope metadata appears in schema | Missing/multiple payload members are diagnostics. |
| Reference policy | Deterministic definitions/references | Derived type references | Reuses schemas for known types | Unresolved type references are diagnostics. |
| `additionalProperties` | Closed for known object properties unless extension data allows extras | Closed object or extension-data shape | Controls unknown JSON members | Unsupported extension-data member shapes are diagnostics. |
| Required/nullability mapping | CLR/semantic requiredness and nullable metadata | Required, optional, nullable | Emits `required` and null-capable schemas | Contradictions may produce diagnostics. |
| Enum mapping | Names/display metadata | CLR enum names and `SemanticEnumValue` labels/descriptions | Emits enum values and optional labels | Unsupported enum backing assumptions are diagnostics. |
| Format mapping | Format when present | `email`, `uri`, `hostname`, `ipv4`, `ipv6`, `date`, `time`, `date-time`, `duration`, `uuid`, custom strings | Emits `format` | Unknown custom formats are emitted as annotations/hints, not validators. |
| Extension-data policy | Preserve when dictionary-like extension data is marked | `SemanticExtensionData` dictionary-like member | Allows unknown keys through extension-data schema | Non-dictionary extension data is diagnostic. |
| `RequiredWhen` | Export when representable | Equality against supported scalar/enum literal | Emits conditional schema shape | Unsupported operator/literal or target policy uses STM1020-STM1024 style diagnostics. |
| UI export mode | `JsonSchemaUiMode.None` | `None`, `JsonEditorCompatible` | Adds no UI annotations or JSON Editor-compatible annotations | Unsupported widgets or invalid order are diagnostics. |
| `IncludeProjectionAnnotations` | `true` | `true`, `false` | Keeps or suppresses projection/unsupported-keyword annotations | Suppressing annotations can hide explanatory metadata but not diagnostics. |
| `UiHintOptions.StrictKnownHintsOnly` | `false` | `true`, `false` | Turns unknown UI hints into stricter diagnostics | Unknown UI hint keys fail under strict mode. |

## Diagnostics

| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
| Duplicate projected property names | Semantic name, JSON name, or UI title maps two members to the same key | Change the source names or selected name policy. |
| Unsupported scalar mapping | CLR type has no JSON Schema scalar equivalent | Add a semantic format, converter boundary, or ignore/replace the member. |
| Invalid UI hint | Widget/order/title metadata cannot be represented in selected UI mode | Remove the hint or use JSON Editor-compatible values. |
| Lossy conditional export | `RequiredWhen` cannot become a safe JSON Schema condition | Use equality against a supported scalar/enum source. |

## Common mistakes

- Assuming `format` validates values in every downstream validator.
- Enabling JSON Editor annotations in general-purpose API schemas without checking consumers.
- Treating extension data as permission for arbitrary typed CLR objects.
- Suppressing projection annotations and then ignoring diagnostics.

## Limitations

The package does not perform runtime JSON validation, does not make schema import the recommended authoring path, and does not infer application migration behavior from schema differences.

## Related docs

- [SemanticTypeModel.JsonSchema package](../nuget/SemanticTypeModel.JsonSchema.md)
- [JSON Editor compatibility](json-editor-compatibility.md)
- [Code-first JSON Schema sample](../samples/code-first-json-schema.md)
