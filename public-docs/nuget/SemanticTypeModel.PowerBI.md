# SemanticTypeModel.PowerBI

`SemanticTypeModel.PowerBI` derives a Power BI domain semantic model from canonical SemanticTypeModel metadata and emits deterministic local analytical metadata.

```sh
dotnet add package SemanticTypeModel.PowerBI --version 2.2.0
```

This package is part of the stable package set. Public APIs follow the compatibility policy.

## Projection boundary

The package derives local Power BI metadata. It does not publish datasets, authenticate with Power BI, create PBIX files, manage workspaces, schedule refresh, call XMLA endpoints, or provide full TOM parity.

## Basic usage

```csharp
var result = semanticModel.DerivePowerBiModel(options =>
{
    options.UseDefaultTransformations();
    options.Measures.Add<Order>("Total Sales", "SUM(Orders[Amount])");
});

result.Diagnostics.ThrowIfErrors();
PowerBiLocalMetadataExporter.Export(result.Model, "artifacts/powerbi");
```

The output is inspectable without Power BI tooling and includes tables, columns, relationships, explicit measures, explicit calculated tables, analytical metadata, projection diagnostics, envelope metadata policies, ownership projection behavior, and evolution/lifecycle columns.

More details: `public-docs/guides/power-bi-projection.md`.
