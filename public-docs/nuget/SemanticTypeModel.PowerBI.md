# SemanticTypeModel.PowerBI

## What this package does

`SemanticTypeModel.PowerBI` provides Power BI domain-model derivation and deterministic local analytical metadata export.

## Install

```sh
dotnet add package SemanticTypeModel.PowerBI --version 2.3.0
```

## Use when

- Install this package when you need Power BI domain-model derivation and deterministic local analytical metadata export.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.
- Produce local metadata that can be inspected before external tooling uses it.

## Minimal example

```csharp
using SemanticTypeModel.PowerBI;

var result = AppSemanticTypeModel.Create().DerivePowerBiModel();
result.Diagnostics.ThrowIfErrors();
PowerBiLocalMetadataExporter.ExportJson(result.Model, "artifacts/powerbi/model.json");
```

## Main APIs

| API | Purpose |
| --- | --- |
| `DerivePowerBiModel` | Derives analytical metadata. |
| `PowerBiLocalMetadataExporter.ExportJson` | Writes deterministic local metadata. |
| `PowerBiDerivationOptions` | Controls measures and model configuration. |
| `PowerBiSemanticModel` | Domain model for local analytical metadata. |

## Works with

- SemanticTypeModel.Core and generated model providers.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not publish to Power BI, create PBIX files, authenticate, call XMLA, or manage workspaces.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [Power BI projection guide](../guides/power-bi-projection.md)
