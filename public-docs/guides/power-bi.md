# Power BI

## Use

Derive deterministic local analytical metadata from the generated semantic model:

```csharp
var result = AppSemanticTypeModel.Create().DerivePowerBiModel(options =>
{
    options.Projection.UseNamingPolicy(PowerBiNamingPolicy.DisplayName);
    options.Projection.HideTechnicalKeys = true;
});

result.Diagnostics.ThrowIfErrors();
PowerBiLocalMetadataExporter.ExportJson(result.Model, "artifacts/powerbi/model.json");
```

The package produces local analytical metadata for supported tables, columns, measures, and related projection
decisions. It does not publish to Power BI services.

General canonical STM relationships were removed. The Power BI projection does not infer replacement
relationships from keys, CLR references, property names, or collection shape.

## Configure

Target policies can control naming, visibility, enum representation, owned-object behavior, envelope handling,
measures, calculated tables, categories/folders, sort metadata, summarization, and collision behavior where the
current projection exposes those options.

Use user-facing descriptions for report-author/analyst text. Technical descriptions are not an automatic
fallback for user-facing Power BI descriptions.

Lifecycle mutability is canonical semantic information but does not automatically change Power BI output.

## Diagnose

| Symptom | Likely cause | Fix |
|---|---|---|
| Duplicate table/column name | Naming policy collapses semantic names | Change labels/naming or select supported collision behavior. |
| Unresolved sort column | Referenced projected member does not exist after projection/naming | Correct sort metadata or the projected member selection. |
| Expected relationship missing | General STM relationship projection is not supported | Define target-specific analytical relationship behavior outside the canonical STM contract. |
| Lossy scalar mapping | Source semantic has no exact analytical representation | Accept the diagnostic intentionally or change source/target metadata. |
| Unsupported nested shape | Owned/nested shape lacks a supported policy | Choose supported flatten/serialize/diagnose behavior where available. |

## Reference

The package does not authenticate, publish datasets, manage workspaces, schedule refresh, call REST/XMLA, create
PBIX files, or claim full TOM parity.

See [Projection capabilities](projection-capabilities.md), [Diagnostics](../diagnostics.md), and
`samples/code-first-powerbi/`.
