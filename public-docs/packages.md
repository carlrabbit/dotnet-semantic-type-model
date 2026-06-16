# Packages

## Current Stable Package Set

The current stable package set is:

- `SemanticTypeModel.Abstractions`
- `SemanticTypeModel.Core`
- `SemanticTypeModel.JsonSchema`
- `SemanticTypeModel.DotNet`
- `SemanticTypeModel.Generators`
- `SemanticTypeModel.SystemTextJson`
- `SemanticTypeModel.DependencyInjection`
- `SemanticTypeModel.PowerBI`
- `SemanticTypeModel.EFCore`

`SemanticTypeModel.JsonEditor` is not a package. JSON Editor compatibility is exposed by `SemanticTypeModel.JsonSchema` as JSON Schema UI-hint export using `JsonSchemaUiMode.JsonEditorCompatible`.

## Package Roles

| Package | Role |
| --- | --- |
| `SemanticTypeModel.Abstractions` | Shared model, runtime, diagnostics, and compatibility contracts. |
| `SemanticTypeModel.Core` | Canonical model, core semantic vocabulary, transformation pipeline, diagnostics, and inspection helpers. |
| `SemanticTypeModel.JsonSchema` | JSON Schema domain semantic model derivation and Draft 2020-12 export. |
| `SemanticTypeModel.DotNet` | Attribute model and Roslyn-based .NET type extraction. |
| `SemanticTypeModel.Generators` | Incremental source generator for compile-time extraction. |
| `SemanticTypeModel.SystemTextJson` | System.Text.Json contract metadata import, annotation constants, domain semantic model derivation, and resolver customization helpers. |
| `SemanticTypeModel.DependencyInjection` | Runtime provider, transformation, and projection service registration. |
| `SemanticTypeModel.PowerBI` | Power BI domain semantic model derivation and local metadata projection. |
| `SemanticTypeModel.EFCore` | EF Core domain semantic model derivation and provider-neutral `ModelBuilder` projection. |

## Scenario Packages

- Core semantic authoring: `SemanticTypeModel.Core`, `SemanticTypeModel.DotNet`, and optionally `SemanticTypeModel.Generators`.
- JSON Schema export: `SemanticTypeModel.JsonSchema`.
- JSON Editor-compatible schemas: `SemanticTypeModel.JsonSchema` with JSON Editor compatibility options.
- System.Text.Json integration: `SemanticTypeModel.SystemTextJson`.
- Runtime DI composition: `SemanticTypeModel.DependencyInjection` plus the projection package being registered.
- EF Core projection: `SemanticTypeModel.EFCore`.
- Power BI projection: `SemanticTypeModel.PowerBI`.

## Related Guides

- [Core semantics guide](guides/core-semantics.md)
- [JSON Schema guide](guides/json-schema.md)
- [JSON Editor compatibility guide](guides/json-editor-compatibility.md)
- [System.Text.Json guide](guides/system-text-json.md)
- [EF Core projection guide](guides/ef-core-projection.md)
- [Power BI projection guide](guides/power-bi-projection.md)
- [Projection capability matrix](guides/projection-capabilities.md)
