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
});

result.Diagnostics.ThrowIfErrors();
PowerBiLocalMetadataExporter.Export(result.Model, "artifacts/powerbi");
```

## Supported Metadata

The Power BI projection includes:

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

## Envelope and Ownership Projection

Envelope semantics default to an envelope metadata table. Payload body projection is opt-in because arbitrary payload graphs can create unstable analytical shapes.

Supported policy concepts include:

- metadata only;
- metadata with payload summary;
- flattened payload;
- payload as separate tables;
- payload as root;
- ignored payload.

Ownership semantics can flatten owned objects into an owner table or project owned collections as child tables when explicitly useful for analysis.

## Evolution and Lifecycle Semantics

Version, revision, current-version, lifecycle-state, and temporal-validity members project as analytical columns. They can be used as slicers, grouping columns, timeline columns, or effective-date metadata.

`ExtensionData` is ignored by default. It can be projected as summary metadata such as `HasExtensionData`, `ExtensionDataCount`, or known extension-key metadata when explicitly configured.

## Extension Points

Users can extend derivation through options, measure builders, calculated-table builders, post-derive model configuration hooks, custom transformations, and custom annotations consumed by custom transformations.

## Diagnostics

Unsupported or ambiguous metadata emits diagnostics instead of being silently dropped. Typical cases include unsupported nested shapes, invalid annotation values, lossy scalar mappings, duplicate projected names, unsupported expression languages, unresolved sort-by-column references, ambiguous relationship endpoints, unsupported envelope payload policies, and unstable extension-data flattening.
