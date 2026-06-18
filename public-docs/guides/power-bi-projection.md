# Power BI Projection

## Goal

Derive local analytical metadata from a semantic model so reporting shape can be inspected before external Power BI tooling uses it.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.2.0`.

## Packages

- `SemanticTypeModel.PowerBI` for derivation and local metadata export.
- `SemanticTypeModel.Generators` and `SemanticTypeModel.DotNet` for code-first model generation.

## Minimal path

1. Generate the semantic model.
2. Derive the Power BI semantic model.
3. Add explicit measures or calculated tables through options when needed.
4. Check diagnostics.
5. Export deterministic local metadata.

## Full example

```csharp
using SemanticTypeModel.PowerBI;

var result = AppSemanticTypeModel.Create().DerivePowerBiModel(options =>
{
    options.Measures.Add<Order>("Total Sales", "SUM(Orders[Amount])");
});

result.Diagnostics.ThrowIfErrors();
PowerBiLocalMetadataExporter.ExportJson(result.Model, "artifacts/powerbi/model.json");
```

## How it works

Annotated .NET code is extracted by the generator into a `TypeSchemaModel`. Core transformations normalize projection-neutral semantics. The target package derives a domain semantic model and then exports or applies target-specific output when that target supports it.

## Options and policies

Configure table visibility, display folders, data categories, summarization hints, format strings, sort-by-column metadata, explicit measures, calculated tables, ownership flattening, and envelope payload policy.

## Diagnostics

Power BI diagnostics report duplicate table or column names, unsupported nested shapes, lossy scalar mappings, unresolved sort-by-column references, ambiguous relationships, unsupported expression languages, and unstable extension-data flattening.

## Common mistakes

- Treating JSON Schema files as the canonical authoring source for new models.
- Mixing target-specific metadata with projection-neutral semantics.
- Skipping diagnostic inspection before using projected output.
- Using stale pre-2.2 model namespace or shape names in current examples.

## Limitations

The package does not publish datasets, authenticate with Power BI, create PBIX files, manage workspaces, schedule refresh, call REST or XMLA endpoints, or provide full TOM parity.

## Related docs

- [SemanticTypeModel.PowerBI package](../nuget/SemanticTypeModel.PowerBI.md)
- [Code-first Power BI sample](../samples/code-first-powerbi.md)
- [Projection capabilities](projection-capabilities.md)
