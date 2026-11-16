# JSON Schema

## Use

Derive the JSON Schema domain model from a canonical model and export Draft 2020-12 output:

```csharp
var derived = AppSemanticTypeModel.Create().DeriveJsonSchemaModel(options => options.UseDefaultTransformations());
derived.Diagnostics.ThrowIfErrors();
JsonSchemaExportResult export = JsonSchemaExporter.Export(derived.Model);
```

Semantic annotations are enabled by default. STM-only semantics appear in one `x-stm` object; property-level
Logical Type metadata is preserved as `x-stm.logicalType` without changing the standard scalar schema.
`UserDescription` remains the standard JSON Schema `description`; technical text is independently emitted as
`x-stm.technicalDescription`.

Declared mutability is emitted only at the node where it was declared. Object keys retain composite member order and refer to emitted property names. Arbitrary JSON-compatible `ui.*` values pass through beneath `x-stm.ui` after stripping the `ui.` prefix.

The JSON representation fidelity contract is one-way: supported STM-configured System.Text.Json output
validates against the derived schema. SemanticTypeModel does not promise bidirectional serializer/schema
equivalence. `x-stm` preserves the implemented semantic vocabulary, including roles, keys, Display Identity,
Access Paths, lifecycle/evolution, ownership, envelope, extension-data, and enum-value metadata.

### Root and definitions

The exported document has one `Root` plus `Definitions`; together they contain the projected types. The chosen
root is intentionally omitted from `$defs`. STM selects the type matching the canonical model ID, falling back
to the ordinal-first TypeId; an envelope policy may select its payload. To use another consumer root, compose a
new schema document that preserves the exported original root under `$defs` and keeps its existing references
resolvable. If that root contains references to `#`, rebase those references to the preserved root's `$defs`
entry inside the new document. There is no separate root-selection API.

### Conditional presence

`[SemanticRequiredWhen]` emits an equality condition that first requires the source property to be present,
then requires the target property when the typed value matches. It does not require a non-null target, forbid
other variant properties, or enforce exactly one variant. A nullable target may be present as `null` when its
schema allows it. JSON Schema validation enforces the condition; System.Text.Json serializes but does not perform
general semantic validation, and EF Core/Power BI do not enforce it implicitly.

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

For the tandem fidelity path, use canonical semantic property names, compatible string-enum serialization,
and otherwise bounded serializer settings. Existing `ExistingJsonContract` remains the System.Text.Json
default and has no blanket schema-fidelity guarantee. Representation-changing converters or number handling,
reference-preservation wire shapes, unmatched noncanonical members, and explicit polymorphism are outside this
initial guarantee. System.Text.Json is not a general JSON Schema validation framework.

## Diagnose

| Symptom | Likely cause | Fix |
|---|---|---|
| `x-stm` is absent | `IncludeSemanticAnnotations` is false or the node has no supported declarations | Enable semantic annotations or add an explicit supported semantic declaration. |
| `JSONSCHEMA_UI_VALUE_NOT_JSON_COMPATIBLE` | A `ui.*` annotation value cannot be serialized as JSON | Use a string, number, Boolean, null, array, object, or `JsonElement`. |
| Technical text is absent from `description` | Technical and user descriptions are intentionally independent | Add `SemanticUserDescription` for standard `description`; inspect `x-stm.technicalDescription` for technical text. |
| Unsupported scalar/shape | No safe schema representation exists | Change the source shape or handle the projection diagnostic explicitly. |
| Selected type is missing from `$defs` | That type is the document root | Preserve the root schema when composing a downstream document; do not expect the root duplicated under `$defs`. |
| Missing discriminator appears to satisfy an expected conditional | The source property was absent or the condition was interpreted as non-null/exclusive behavior | Use the exported source-presence guard and remember that `RequiredWhen` requires target presence only. Validate with a Draft 2020-12 validator. |

Always review derivation/export diagnostics before consuming output.

## Reference

- [Core semantics](core-semantics.md)
- [Projection capabilities](projection-capabilities.md)
- [Diagnostics](../diagnostics.md)
- executable example: `samples/code-first-json-schema/`
