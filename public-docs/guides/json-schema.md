# JSON Schema

## Use

Generate a canonical model from annotated .NET code, derive the JSON Schema domain model, review diagnostics,
then export Draft 2020-12 output.

```csharp
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;

var result = AppSemanticTypeModel.Create()
    .DeriveJsonSchemaModel(options => options.UseDefaultTransformations());

result.Diagnostics.ThrowIfErrors();

JsonSchemaExportResult export = JsonSchemaExporter.Export(result.Model);
Console.WriteLine(export.Document.RootElement.GetRawText());
```

## Configure

Common export controls include schema ID, projection annotations, root/envelope selection policy, and UI export
options.

```csharp
JsonSchemaExportResult export = JsonSchemaExporter.Export(
    result.Model,
    new JsonSchemaExportOptions
    {
        SchemaId = new Uri("https://example.invalid/customer.schema.json"),
        IncludeProjectionAnnotations = false,
        UiExport = new JsonSchemaUiExportOptions
        {
            UiMode = JsonSchemaUiMode.JsonEditorCompatible,
            IncludeJsonEditorCompatibilityAnnotations = true,
        },
    });
```

Current semantic behavior includes deterministic definitions/references, required/nullability mapping, enum
mapping, supported scalar formats, extension-data handling, and representable `RequiredWhen` conditions.

`UserDescription` maps to user-facing schema description. Technical descriptions are separate and are emitted
only through explicit target behavior/options.

JSON Schema import may remain for compatibility/tooling, but annotated .NET code plus generated providers is the
supported public authoring path.

## Diagnose

| Symptom | Likely cause | Fix |
|---|---|---|
| Duplicate projected property names | Naming policies collapse distinct members | Change semantic/JSON naming policy or source names. |
| Unsupported scalar/shape | No safe schema representation exists | Change source shape or handle the target-specific boundary explicitly. |
| Invalid UI hint | Selected UI compatibility mode cannot represent the hint | Remove/change the hint or UI mode. |
| Lossy conditional export | `RequiredWhen` source/operator/literal is not safely representable | Use a supported typed equality condition. |
| Extension data diagnostic | Member is not a supported dictionary-like extension-data shape | Use a supported extension-data member type. |

Always review derivation/export diagnostics before consuming output.

## Reference

- [JSON Editor compatibility](json-editor-compatibility.md)
- [Core semantics](core-semantics.md)
- [Projection capabilities](projection-capabilities.md)
- [Diagnostics](../diagnostics.md)
- executable example: `samples/code-first-json-schema/`
