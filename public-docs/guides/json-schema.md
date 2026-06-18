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
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;

var result = AppSemanticTypeModel.Create()
    .DeriveJsonSchemaModel(options => options.UseDefaultTransformations());

result.Diagnostics.ThrowIfErrors();
JsonSchemaExportResult export = JsonSchemaExporter.Export(result.Model);
Console.WriteLine(export.Document.RootElement.GetRawText());
```

## How it works

Annotated .NET code is extracted by the generator into a `TypeSchemaModel`. Core transformations normalize projection-neutral semantics. The target package derives a domain semantic model and then exports or applies target-specific output when that target supports it.

## Options and policies

Choose envelope root or payload root policy, payload representation, reference behavior, strictness for additional properties, UI export mode, and handling for extension data. Keep semantic names and JSON serialization names separate unless a target policy intentionally maps them.

## Diagnostics

JSON Schema diagnostics report unresolved type references, unsupported scalar mappings, duplicate projected names, ambiguous envelope payload policy, invalid UI hints, and lossy projection choices. Treat diagnostics as part of the export result, not as console-only messages.

## Common mistakes

- Treating JSON Schema files as the canonical authoring source for new models.
- Mixing target-specific metadata with projection-neutral semantics.
- Skipping diagnostic inspection before using projected output.
- Using stale pre-2.2 model namespace or shape names in current examples.

## Limitations

The package does not make JSON Schema import the recommended authoring path for new consumers, does not perform runtime JSON validation, and does not infer application migration behavior from schema differences.

## Related docs

- [SemanticTypeModel.JsonSchema package](../nuget/SemanticTypeModel.JsonSchema.md)
- [JSON Editor compatibility](json-editor-compatibility.md)
- [Code-first JSON Schema sample](../samples/code-first-json-schema.md)
