# Power BI Projection

`SemanticTypeModel.PowerBI` projects canonical semantic type models to deterministic Power BI/tabular metadata.

## Boundary

The projection package does not publish to the Power BI service, authenticate with Power BI REST APIs, create PBIX files, or manage workspaces. The first-class output is an inspectable projection model.

## Entry points

```csharp
PowerBiProjectionModel model = semanticModel.ToPowerBiModel(options =>
{
    options.DefaultTableRole = PowerBiTableRole.Dimension;
    options.HideTechnicalKeys = true;
    options.HideForeignKeys = true;
    options.DefaultNumericSummarization = PowerBiSummarization.Sum;
});
```

For compatibility with earlier preview code, `PowerBiTabularProjection` still returns `TabularModelDefinition`.

## Supported metadata

The projection includes:

- tables with display name, description, hidden state, role, source type id, and annotations;
- columns with display name, data type, nullability, hidden state, key metadata, format string, summarization, source property id, and annotations;
- relationships with endpoint table/column names, cardinality, direction, active state, and source relationship id;
- DAX computed members as measures when explicitly represented in the source model.

## Diagnostics

Unsupported or ambiguous metadata emits `POWERBI_*` projection diagnostics instead of being silently dropped. Examples include unsupported nested shapes, invalid annotation values, lossy scalar mappings, duplicate projected names, unsupported measure expression languages, and ambiguous relationship endpoints.
