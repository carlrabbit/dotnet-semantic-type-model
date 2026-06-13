# JSON Schema Guide

`SemanticTypeModel.JsonSchema` derives a JSON Schema domain semantic model from the canonical semantic model and exports JSON Schema Draft 2020-12 documents.

Annotated .NET code is the supported authoring source for canonical semantic models. JSON Schema import is not the supported canonical model creation path for new consumers.

## Usage

```csharp
using SemanticTypeModel.JsonSchema;

TypeSchemaModel model = AppSemanticTypeModel.Create();

var result = model.DeriveJsonSchemaModel(options =>
{
    options.UseDefaultTransformations();
});

result.Diagnostics.ThrowIfErrors();

JsonSchemaExportResult exported = JsonSchemaExporter.Export(result.Model);
```

## Envelope Projection

Envelope semantics are projection-neutral. JSON Schema policy decides whether the exported root is the envelope wrapper, the payload, or an explicit envelope/payload combination.

Default behavior for an envelope root is to export the envelope object and represent the payload as a structured schema reference.

Explicit policies can inline the payload schema, expose a JSON-document payload, expose a serialized JSON string payload, or treat the payload as opaque.

## Evolution and Compatibility Semantics

Version, revision, lifecycle state, temporal validity, ownership, and extension-data semantics affect schema metadata and shape decisions without turning JSON Schema into a runtime validator.

Typical behavior:

- owned objects and collections are exported as structured schemas;
- revision and lifecycle-state members are exported as normal typed properties with semantic metadata;
- `ValidFrom` and `ValidTo` are exported as date-time properties;
- `ExtensionData` controls open-member compatibility through `additionalProperties` or `unevaluatedProperties` policy unless explicitly exposed as a normal property.

## JSON Editor Compatibility

JSON Editor compatibility is an export mode of this package; see [json-editor-compatibility.md](json-editor-compatibility.md).

## Non-Goals

`SemanticTypeModel.JsonSchema` does not make JSON Schema documents the canonical authoring source, does not perform runtime JSON validation, and does not infer application migration behavior from schema differences.
