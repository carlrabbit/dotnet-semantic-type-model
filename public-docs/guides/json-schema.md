# JSON Schema

## Use

Derive the JSON Schema domain model from a canonical model and export Draft 2020-12 output:

```csharp
var derived = AppSemanticTypeModel.Create().DeriveJsonSchemaModel(options => options.UseDefaultTransformations());
derived.Diagnostics.ThrowIfErrors();
JsonSchemaExportResult export = JsonSchemaExporter.Export(derived.Model);
```

Semantic annotations are enabled by default. STM-only semantics appear in one `x-stm` object whose initial vocabulary is exactly `role`, `aggregateRoot`, `mutability`, `technicalDescription`, `keys`, `unit`, and `ui`. `UserDescription` remains the standard JSON Schema `description`; technical text is independently emitted as `x-stm.technicalDescription`.

Declared mutability is emitted only at the node where it was declared. Object keys retain composite member order and refer to emitted property names. Arbitrary JSON-compatible `ui.*` values pass through beneath `x-stm.ui` after stripping the `ui.` prefix.

## Configure

```csharp
JsonSchemaExportResult export = JsonSchemaExporter.Export(
    derived.Model,
    new JsonSchemaExportOptions
    {
        SchemaId = new Uri("https://example.invalid/customer.schema.json"),
        IncludeSemanticAnnotations = false, // plain JSON Schema without x-stm
    });
```

There is no JSON Editor compatibility mode, widget inference option, or closed UI vocabulary.

## Diagnose

| Symptom | Likely cause | Fix |
|---|---|---|
| `x-stm` is absent | `IncludeSemanticAnnotations` is false or the node has no supported declarations | Enable semantic annotations or add an explicit supported semantic declaration. |
| `JSONSCHEMA_UI_VALUE_NOT_JSON_COMPATIBLE` | A `ui.*` annotation value cannot be serialized as JSON | Use a string, number, Boolean, null, array, object, or `JsonElement`. |
| Technical text is absent from `description` | Technical and user descriptions are intentionally independent | Add `SemanticUserDescription` for standard `description`; inspect `x-stm.technicalDescription` for technical text. |
| Unsupported scalar/shape | No safe schema representation exists | Change the source shape or handle the projection diagnostic explicitly. |

Always review derivation/export diagnostics before consuming output.

## Reference

- [Core semantics](core-semantics.md)
- [Projection capabilities](projection-capabilities.md)
- [Diagnostics](../diagnostics.md)
- executable example: `samples/code-first-json-schema/`
