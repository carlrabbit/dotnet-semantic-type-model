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

Supported `[SemanticStrongScalar]` values are classified using their underlying scalar kind where the
projection can represent it. Strong Scalar does not infer analytical identity, keys, relationships, or display
behavior.

`ulong` values use an exact-value-preserving String fallback with a diagnostic because the full CLR range does
not fit Power BI `Int64`. Decimal remains the fixed-decimal target, with a potential-loss diagnostic when the
canonical precision/scale does not prove the value domain fits.

## Diagnose

| Symptom | Likely cause | Fix |
|---|---|---|
| Duplicate table/column name | Naming policy collapses semantic names | Change labels/naming or select supported collision behavior. |
| Unresolved sort column | Referenced projected member does not exist after projection/naming | Correct sort metadata or the projected member selection. |
| Expected relationship missing | General STM relationship projection is not supported | Define target-specific analytical relationship behavior outside the canonical STM contract. |
| Lossy scalar mapping | Source semantic has no exact analytical representation | Accept the diagnostic intentionally or change source/target metadata. |
| `ulong` or unconstrained Decimal diagnostic | CLR range/precision exceeds the guaranteed Power BI numeric contract | Use the exact String fallback or add bounded canonical precision/scale metadata. |
| Unsupported nested shape | Owned/nested shape lacks a supported policy | Choose supported flatten/serialize/diagnose behavior where available. |

## Reference

The package does not authenticate, publish datasets, manage workspaces, schedule refresh, call REST/XMLA, create
PBIX files, or claim full TOM parity.

See [Projection capabilities](projection-capabilities.md), [Diagnostics](../diagnostics.md), and
`samples/code-first-powerbi/`.
