# Packages

## 1.0 Release Candidate Package Set

The intended `1.0.0-rc.1` package set is:

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
| `SemanticTypeModel.Core` | Builders, validation, and transformation pipeline. |
| `SemanticTypeModel.JsonSchema` | JSON Schema import/export and JSON Editor-compatible UI-hint export mode. |
| `SemanticTypeModel.DotNet` | Attribute model and Roslyn-based .NET type extraction. |
| `SemanticTypeModel.Generators` | Incremental source generator for compile-time extraction. |
| `SemanticTypeModel.SystemTextJson` | System.Text.Json contract integration and annotations. |
| `SemanticTypeModel.DependencyInjection` | Runtime provider, transformation, and projection service registration. |
| `SemanticTypeModel.PowerBI` | Power BI projection metadata. |
| `SemanticTypeModel.EFCore` | EF Core model projection support. |

## Scenario Packages

- JSON Schema export/import: `SemanticTypeModel.JsonSchema`.
- JSON Editor-compatible schemas: `SemanticTypeModel.JsonSchema` with JSON Editor compatibility options.
- Code-first extraction: `SemanticTypeModel.DotNet` and optionally `SemanticTypeModel.Generators`.
- System.Text.Json integration: `SemanticTypeModel.SystemTextJson`.
- Runtime DI composition: `SemanticTypeModel.DependencyInjection` plus the projection package being registered.
- EF Core projection: `SemanticTypeModel.EFCore`.
- Power BI projection: `SemanticTypeModel.PowerBI`.

## Related Guides

- [JSON Schema guide](guides/json-schema.md)
- [JSON Editor compatibility guide](guides/json-editor-compatibility.md)
- [System.Text.Json guide](guides/system-text-json.md)
- [EF Core projection guide](guides/ef-core-projection.md)
- [Power BI projection guide](guides/power-bi-projection.md)
- [Projection capability matrix](guides/projection-capabilities.md)
