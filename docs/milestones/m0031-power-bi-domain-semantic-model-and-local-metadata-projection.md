# M0031: Power BI Domain Semantic Model and Local Metadata Projection

## Status

Planned.

## Maturity Mode

Domain package architecture implementation for a public package set.

The repository has public packages, package README sources, public samples, public API baselines, and package validation. This milestone updates `SemanticTypeModel.PowerBI` to the M0026-M0030 domain semantic model architecture and defines the durable Power BI boundary.

## Task Mode

Milestone implementation routing and architecture-boundary implementation.

This milestone implements Power BI as a domain semantic model projection. It adds one architectural decision that limits the package to semantic model derivation and deterministic local metadata output.

Do not introduce TBPs, issue templates, workflow YAML, non-root README files, generated code files, or broad public-documentation rewrites in this planning package.

## Goal

After M0031, a consumer can:

```text
annotate C# analytical/domain types;
generate or extract a canonical Semantic Type Model;
run core transformations;
derive a PowerBiSemanticModel through configurable Power BI transformations;
add explicit measures and calculated tables through options or custom transformations;
inspect diagnostics and transformation trace;
export deterministic local analytical metadata.
```

The architectural pipeline is:

```text
Code-generated canonical semantic model
  -> Power BI derivation transformations
  -> PowerBiSemanticModel
  -> local deterministic metadata output
```

## Required Authority

Read these documents before implementing any focus area:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/SPECS.md
docs/specs/code-first-semantic-model-architecture.md
docs/architecture/code-first-domain-projection-pipeline.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/decisions/power-bi-integration-stops-at-local-metadata-projection.md
```

Read these only when the selected focus area touches the relevant component:

```text
docs/specs/type-model-core.md
docs/specs/type-schema-model.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-annotations.md
docs/specs/diagnostics.md
docs/specs/type-model-projection-capabilities.md
docs/PUBLIC-DOCS.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/guides/power-bi-projection.md
public-docs/release-notes.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Define and implement `PowerBiSemanticModel` or equivalent package-owned analytical domain semantic model.
- Define Power BI derivation transformations using the M0028 domain derivation contract.
- Export deterministic local analytical metadata from the Power BI domain semantic model.
- Support:
  - tables;
  - columns;
  - relationships;
  - explicit measures;
  - explicit calculated tables;
  - display folders;
  - hidden/visible flags;
  - data categories;
  - summarization hints;
  - format strings;
  - sort-by-column metadata;
  - basic explicit hierarchies where current model support is sufficient;
  - carried annotations/extensions where explicitly configured.
- Support user extension through:
  - derivation options;
  - measure/calculated-table builders;
  - post-derive model configuration hooks;
  - custom Power BI transformations;
  - custom annotations consumed by custom transformations.
- Emit diagnostics for unsupported or ambiguous analytical projection.
- Integrate with M0027 query/inspection and M0028 trace behavior.
- Add short-running tests and at least one code-first Power BI sample.
- Keep behavior local and deterministic.

### Out of Scope

These are intentionally outside the Power BI package boundary and should not remain vague future backlog:

```text
Power BI Service publishing
workspace management
dataset deployment
authentication
gateway configuration
refresh scheduling
incremental refresh configuration
PBIX generation
Power BI REST API orchestration
Fabric integration
deployment pipelines
XMLA endpoint operations
query execution
credentials/secrets handling
```

These advanced tabular/TOM features are also out of M0031 unless a later accepted decision changes the boundary:

```text
full TOM parity
Tabular Editor replacement
calculation groups
perspectives
translations
row-level security
object-level security
calculated columns unless explicitly added later
complex partition management
refresh policies
detail rows expressions
full DAX authoring framework
DAX syntax validation
lineage/deployment metadata ownership
```

## Package Boundary

| Package | Responsibility |
|---|---|
| `SemanticTypeModel.PowerBI` | Power BI domain semantic model, Power BI derivation transformations, diagnostics, and local deterministic metadata export. |
| `SemanticTypeModel.Core` | Query, inspection, transformation, and derivation result infrastructure. |
| `SemanticTypeModel.DotNet` | Code extraction metadata consumed by code-first samples and generated models. |
| `SemanticTypeModel.Generators` | Generated providers consumed by Power BI derivation samples. |

`SemanticTypeModel.PowerBI` must not become a Power BI deployment, service, PBIX, or full TOM automation framework.

## Focus Areas

### Focus Area 1 — Architectural Boundary and Public Scope

#### Intent

Make the Power BI boundary explicit and enforceable.

#### Required Authority

```text
docs/decisions/power-bi-integration-stops-at-local-metadata-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/code-first-semantic-model-architecture.md
```

#### Implementation Requirements

- Preserve the boundary: semantic model derivation and deterministic local metadata output only.
- Do not add Power BI Service publishing, PBIX generation, workspace management, authentication, refresh scheduling, gateway configuration, XMLA operations, or full TOM parity.
- Ensure public API names and docs do not imply service deployment or PBIX ownership.
- Ensure package behavior can be tested without network access, credentials, Power BI Desktop, a workspace, XMLA, or service APIs.

#### Validation

- Tier 1: Power BI package tests without network/service/PBIX dependencies; public API tests if API names change; sample tests if public sample changes.
- Tier 2 before completion if code changes.
- Tier 3 only if package layout or package consumption behavior changes.

#### Direct Documentation Impact

```text
docs/decisions/power-bi-integration-stops-at-local-metadata-projection.md
docs/specs/type-model-powerbi-tom-projection.md
```

#### Deferred Documentation Impact

```text
README.md
public-docs/guides/power-bi-projection.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/release-notes.md
```

### Focus Area 2 — Power BI Domain Semantic Model

#### Intent

Create the Power BI package-owned analytical domain semantic model.

#### Required Authority

```text
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/architecture/code-first-domain-projection-pipeline.md
```

#### Implementation Requirements

- Define `PowerBiSemanticModel` or equivalent.
- Represent:
  - tables;
  - columns;
  - relationships;
  - measures;
  - calculated tables;
  - hierarchies where explicitly modeled;
  - display folders;
  - hidden/visible flags;
  - data categories;
  - summarization;
  - format strings;
  - sort-by-column;
  - annotations/extensions;
  - diagnostics.
- Keep the domain model explicit enough that exporters do not rely on scattered ad hoc annotation lookups.
- Provide inspection integration or package-specific inspection methods.

#### Validation

- Tier 1: domain model construction tests; domain model inspection tests; deterministic ordering tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-powerbi-tom-projection.md
```

#### Deferred Documentation Impact

```text
public-docs/nuget/SemanticTypeModel.PowerBI.md
```

### Focus Area 3 — Power BI Derivation Pipeline

#### Intent

Derive `PowerBiSemanticModel` from code-generated canonical models using M0028.

#### Required Authority

```text
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
```

#### Implementation Requirements

- Expose a domain derivation API equivalent to `DerivePowerBiModel`.
- Return a `SemanticDerivationResult<PowerBiSemanticModel>` or equivalent.
- Reuse core default transformations where appropriate.
- Add Power BI-specific derivation transformations.
- Allow users to configure, add, remove, replace, and order Power BI transformations in code.
- Accumulate diagnostics and trace entries.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
var result = model.DerivePowerBiModel(options =>
{
    options.UseDefaultTransformations();
    options.Transformations.Replace(new MyPowerBiNamingTransformation());
});

result.Diagnostics.ThrowIfErrors();
PowerBiLocalMetadataExporter.Export(result.Model, outputPath);
```

#### Validation

- Tier 1: derivation pipeline tests; transformation replacement tests; diagnostic accumulation tests; generated model derivation tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-powerbi-tom-projection.md
```

#### Deferred Documentation Impact

```text
consumer sample docs if derivation customization is demonstrated
```

### Focus Area 4 — Tables, Columns, Relationships, and Analytical Metadata

#### Intent

Support useful analytical metadata projection without attempting full TOM parity.

#### Required Authority

```text
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/type-model-core.md
docs/specs/type-model-dotnet-attributes.md
```

#### Required Mapping Support

```text
table derivation from Entity, Fact, Dimension, Lookup, or explicit powerBi metadata
column derivation from scalar properties
relationship derivation from explicit relationship metadata
hidden/visible flags
data categories
summarization hints
format strings
display folders
sort-by-column metadata
table and column descriptions
carried annotations/extensions when configured
```

#### Rules

- Core semantics provide analytical defaults.
- Power BI-specific metadata overrides Power BI-specific representation details.
- Invalid combinations emit diagnostics.
- Lossy scalar mappings emit diagnostics.
- Unsupported nested/collection shapes must not be silently lost.

#### Validation

- Tier 1: table mapping tests; column mapping tests; relationship mapping tests; format/category/summarization tests; sort-by-column tests; deterministic export tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-powerbi-tom-projection.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/power-bi-projection.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
```

### Focus Area 5 — Measures and Calculated Tables as User Extensions

#### Intent

Support user-owned DAX artifacts without turning the package into a DAX authoring framework.

#### Required Authority

```text
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/type-model-transformation-and-domain-derivation.md
```

#### Required Support

- Explicit measures through derivation options or domain model configuration.
- Explicit calculated tables through derivation options or domain model configuration.
- Custom transformations that add measures/calculated tables.
- Display folder, description, format string, hidden flag, and annotations for measures where applicable.
- Diagnostics for unsupported expression languages if language is modeled.
- No DAX syntax validation.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
var result = model.DerivePowerBiModel(options =>
{
    options.UseDefaultTransformations();

    options.Measures.Add<Order>(
        name: "Total Sales",
        dax: "SUM(Orders[Amount])",
        configure: measure =>
        {
            measure.FormatString = "#,0.00 €";
            measure.DisplayFolder = "Sales";
        });

    options.CalculatedTables.Add(
        name: "Active Customers",
        dax: "FILTER(Customers, Customers[IsActive] = TRUE())");
});
```

Custom transformation behavior must also be possible:

```csharp
options.Transformations.AddAfter<DerivePowerBiTablesTransformation>(
    new MyCalculatedTablesTransformation());
```

#### Validation

- Tier 1: explicit measure tests; explicit calculated table tests; custom transformation extension tests; unsupported expression diagnostics tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-powerbi-tom-projection.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/power-bi-projection.md
sample docs if measures/calculated tables are demonstrated
```

### Focus Area 6 — Local Metadata Export

#### Intent

Produce deterministic local output from the Power BI domain semantic model.

#### Required Authority

```text
docs/specs/type-model-powerbi-tom-projection.md
docs/decisions/power-bi-integration-stops-at-local-metadata-projection.md
```

#### Required Support

At least one deterministic local output must exist:

```text
PowerBiSemanticModel inspection text
neutral JSON metadata document
TMDL-like local file/folder output
TOM script text
```

The implementation may support more than one format, but it must not require Power BI Desktop, Power BI Service, XMLA, credentials, or network access.

#### Rules

- `PowerBiSemanticModel` is the stable package domain contract.
- Export formats are replaceable.
- Output ordering is deterministic.
- Output must be suitable for snapshot tests.
- Unsupported exporter target features emit diagnostics.

#### Validation

- Tier 1: export tests; deterministic output snapshot tests; no network/service dependency tests.
- Tier 2 before completion if code changes.
- Tier 3 only if package consumption behavior changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-powerbi-tom-projection.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/power-bi-projection.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
```

### Focus Area 7 — Diagnostics, Inspection, and Sample

#### Intent

Make Power BI derivation usable in the same code-first test/console loop as JSON Schema and EF Core.

#### Required Authority

```text
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/engineering/samples.md
```

#### Implementation Requirements

- Power BI derivation diagnostics include code, severity, model path, transformation id, projection target, and related model paths where available.
- Power BI domain model can be inspected deterministically.
- Transformation trace is available.
- Add or update a code-first Power BI sample.
- Sample must not require Power BI Desktop, Power BI Service, credentials, XMLA, workspace access, PBIX generation, or network access.
- Sample should demonstrate at least one explicit measure and one explicit calculated table or a custom transformation adding one of them.

#### Validation

- Tier 1: diagnostic tests; inspection snapshot tests; sample validation if sample changes.
- Tier 2 before completion if code changes.
- Tier 3 only if package/sample consumption behavior changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-powerbi-tom-projection.md
public-docs/diagnostics/*.md only if new public diagnostics are added
```

#### Deferred Documentation Impact

```text
public-docs/samples.md
public-docs/samples/*.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/guides/power-bi-projection.md
```

## Required Acceptance Criteria

M0031 is complete when:

- The Power BI architectural boundary decision is documented.
- `SemanticTypeModel.PowerBI` does not own service publishing, PBIX generation, workspace management, authentication, refresh scheduling, XMLA operations, or full TOM parity.
- A Power BI domain semantic model exists.
- Power BI derivation uses the M0028 domain derivation contract.
- Derivation returns model, diagnostics, and transformation trace.
- Users can configure Power BI derivation transformations in code.
- Tables, columns, relationships, hidden/visible flags, data categories, summarization hints, format strings, display folders, and sort-by-column metadata are represented where sufficient metadata exists.
- Explicit measures are supported.
- Explicit calculated tables are supported.
- User extension through custom transformations is demonstrated by tests.
- At least one deterministic local metadata export exists.
- Unsupported or ambiguous Power BI mapping emits diagnostics.
- Power BI domain model and derivation trace inspection are deterministic.
- Code-first generated model tests cover Power BI derivation and local metadata export.
- Tier 2 validation passes, or any inability to run it is explicitly reported with the exact lower-tier validation performed.
- No TBPs, issue templates, non-root README files, workflow YAML, broad public-doc rewrites, or generated code files are introduced by the planning package itself.

## Validation Plan

Use the smallest validation tier that can catch the expected regression.

### Tier 1

Use focused validation for:

```text
PowerBi domain model tests
PowerBi derivation tests
local metadata export tests
measure tests
calculated table tests
custom transformation extension tests
relationship tests
format/category/summarization tests
diagnostic tests
code-first generated model tests
affected sample tests
```

Expected command shape:

```sh
./eng/test-project.sh <powerbi-test-project>
./eng/test-filter.sh <powerbi-domain-or-export-filter>
./eng/check-affected.sh src/SemanticTypeModel.PowerBI tests samples/code-first-powerbi
```

Use actual repository project names after inspecting the solution.

### Tier 2

Run before completing implementation work:

```sh
./eng/check.sh
```

### Tier 3

Run only if package layout, package README generation, or package consumption behavior changes:

```sh
./eng/package.sh <version>
./eng/package-smoke.sh <version>
./eng/samples.sh
```

This is not a release publication milestone; do not run publish validation.

## Direct Documentation Impact

The implementation should directly update:

```text
docs/specs/type-model-powerbi-tom-projection.md
docs/decisions/power-bi-integration-stops-at-local-metadata-projection.md
```

Update related existing specs only when implementation changes contradict current authority:

```text
docs/specs/code-first-semantic-model-architecture.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-projection-capabilities.md
```

## Deferred Documentation Impact

Leave explicit notes for a later documentation synchronization pass covering:

```text
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
README.md
public-docs/getting-started.md
public-docs/guides/power-bi-projection.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md if new diagnostics are public
public-docs/release-notes.md
```

Do not perform broad public documentation synchronization as part of this implementation milestone unless a consumer-facing behavior change directly requires it.
