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
    options.Projection.UseNamingPolicy(PowerBiNamingPolicy.DisplayName);
    options.Projection.HideTechnicalKeys = true;
    options.Projection.ValueObjectProjectionMode = ValueObjectProjectionMode.Flatten;
    options.Projection.DefaultNumericSummarization = PowerBiSummarization.Sum;

    options.Measures.Add<Order>("Total Sales", "SUM(Orders[Amount])");
    options.CalculatedTables.Add("Recent Orders", "FILTER(Orders, Orders[IsRecent] = TRUE())");
    options.Envelopes.For<OrderEnvelope>().UseEnvelopeMetadataTable().SummarizePayload(e => e.Payload, "PayloadSummary");
});

result.Diagnostics.ThrowIfErrors();
PowerBiLocalMetadataExporter.ExportJson(result.Model, "artifacts/powerbi/model.json");
```

## How it works

Power BI projection produces deterministic local metadata: tables, columns, relationships, measures, calculated tables, formatting, visibility, display folders, and diagnostics. It does not publish datasets, authenticate, create PBIX files, call REST/XMLA APIs, or claim full TOM parity.

## Options and policies

| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
| Dimension | Role metadata | `SemanticTypeRole.Dimension` or Power BI table role annotation | Projects a dimension table role | Invalid table-role annotation is diagnostic. |
| Fact | Role metadata | `SemanticTypeRole.Fact` | Projects a fact table role and numeric summarization candidates | Missing relationships reduce analytical usefulness. |
| DisplayName/Description | Preserve semantic metadata | `SemanticDisplayName`, `SemanticDescription` | Sets table/column labels and descriptions | Duplicate display names can collide under display-name naming. |
| Format | No default format string | Format annotations / Power BI format metadata | Emits local format metadata | Unsupported format strings are preserved/diagnosed depending metadata. |
| Enums | `Name` | `Name`, `DisplayName`, `NumericWhenAvailable` | Chooses categorical column values | Numeric mode requires compatible metadata. |
| Relationships | Semantic relationships | Cardinalities and endpoint metadata | Emits Power BI relationship metadata | Ambiguous/incomplete relationships are warnings or errors when required. |
| Measures | No explicit measures | `options.Measures.Add<TTable>` or table-name overload | Adds DAX measures to a table | Unsupported expression languages or missing tables are diagnostics. |
| Calculated tables | None | `options.CalculatedTables.Add(name, dax)` | Adds local calculated-table metadata | Unsupported expression language is diagnostic. |
| Owned objects | `Diagnose` | `Diagnose`, `Flatten`, `SerializeJson` | Flattens or serializes nested values | Unsupported nested shapes follow unsupported-shape behavior. |
| Extension data | Not flattened by default | Supported dictionary-like extension data | Preserved/diagnosed as target metadata | Unstable arbitrary keys are diagnostics. |
| Envelope payloads | Metadata table, payload ignored | `MetadataTable`, `PayloadTable`, `EnvelopeAndPayload`; payload `Ignored`, `Summary`, `Flattened` | Controls analytical view of envelopes | Missing payload selection or unsupported flattening is diagnostic. |
| Table visibility | Visible unless hidden by policy | `HideTechnicalKeys`, `HideForeignKeys`, column visibility metadata | Hides technical columns in local metadata | Hidden columns can be omitted only if `IncludeHiddenColumns` is false. |
| Display folders | No default | Category/display folder annotations | Groups fields/measures | Invalid paths are diagnostics. |
| Data categories | No default | Data-category annotations | Marks geography/URL/image-like columns | Unsupported category is diagnostic. |
| Sort-by-column | No default | Sort-by-column metadata | Emits sort relationship | Unresolved sort column is diagnostic. |
| Summarization hints | Numeric columns `Sum` | `PowerBiSummarization` values including `None`, `Sum` | Sets default summarization | Invalid annotation resolves to `None` and diagnostic. |
| Name collisions | `Diagnose` | `Diagnose`, `Suffix` | Diagnoses duplicates or suffixes names | Suffixes alter visible report names. |

## Diagnostics

| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
| Duplicate table or column name | Naming policy maps multiple items to one name | Change labels/names or use suffix collision behavior. |
| Unresolved sort-by-column | Sort metadata names a missing projected column | Correct the reference after naming policy is applied. |
| Lossy scalar mapping | DateTimeOffset, duration, JSON, or binary lacks exact analytical type | Accept warning, change source type, or add explicit metadata. |
| Unsupported nested shape | Array/dictionary/owned object selected without flatten/serialize policy | Set `ValueObjectProjectionMode` or `UnsupportedShapeBehavior`. |
| Ambiguous relationship | Endpoints/cardinality are incomplete | Add explicit `SemanticRelationship` metadata. |

## Common mistakes

- Treating local metadata export as Power BI service publication.
- Adding measures to a table name before checking the projected table name.
- Hiding keys before relationships and sort columns are reviewed.
- Expecting arbitrary extension data keys to become stable report columns.

## Limitations

The package does not publish datasets, authenticate with Power BI, create PBIX files, manage workspaces, schedule refresh, call REST or XMLA endpoints, or provide full TOM parity.

## Related docs

- [SemanticTypeModel.PowerBI package](../nuget/SemanticTypeModel.PowerBI.md)
- [Code-first Power BI sample](../samples/code-first-powerbi.md)
- [Projection capabilities](projection-capabilities.md)
