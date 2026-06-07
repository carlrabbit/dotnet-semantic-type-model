# Power BI Projection Guide

`SemanticTypeModel.PowerBI` derives a Power BI domain semantic model from the canonical `TypeSchemaModel` and exports deterministic local analytical metadata.

## Boundary

The package owns semantic analytical metadata derivation and local deterministic output. It does not publish to the Power BI Service, authenticate, manage workspaces, generate PBIX files, schedule refresh, call XMLA endpoints, orchestrate REST APIs, or provide full Tabular Object Model parity.

## Usage

```csharp
using SemanticTypeModel.PowerBI;

TypeSchemaModel model = AppSemanticTypeModel.Create();

var result = model.DerivePowerBiModel(options =>
{
    options.UseDefaultTransformations();

    options.Measures.Add<Order>(
        name: "Total Sales",
        dax: "SUM(Orders[Amount])");

    options.CalculatedTables.Add(
        name: "Active Customers",
        dax: "FILTER(Customers, Customers[IsActive] = TRUE())");
});

result.Diagnostics.ThrowIfErrors();

PowerBiLocalMetadataExporter.Export(result.Model, "artifacts/powerbi");
```

## Supported Metadata

The 2.0.0 Power BI projection includes:

- tables and columns;
- relationships;
- explicit measures;
- explicit calculated tables;
- display folders;
- hidden/visible flags;
- data categories;
- summarization hints;
- format strings;
- sort-by-column metadata;
- basic explicit hierarchies when modeled;
- deterministic inspection and local metadata output.

## Extension Points

Users can extend derivation through options, measure builders, calculated-table builders, post-derive model configuration hooks, custom transformations, and custom annotations consumed by custom transformations.

## Diagnostics

Unsupported or ambiguous metadata emits diagnostics instead of being silently dropped. Typical cases include unsupported nested shapes, invalid annotation values, lossy scalar mappings, duplicate projected names, unsupported expression languages, unresolved sort-by-column references, and ambiguous relationship endpoints.
