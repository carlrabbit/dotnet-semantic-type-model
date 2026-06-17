# M0021: Power BI Projection Hardening

## Status

Implemented in this repository. The milestone is tracked by issue #52 and hardens the Power BI projection package while preserving the package boundary that no Power BI service publishing is performed.

## Goal

Define, implement, validate, and document the Power BI projection as a supported projection package for the 1.0 path.

The goal is to make `SemanticTypeModel.PowerBI` capable of projecting the canonical semantic type model into a deterministic, testable Power BI conceptual model representation with clear capability boundaries, diagnostics, options, package smoke tests, and public documentation.

This milestone turns the Power BI projection from a prototype into a supported 1.0 candidate projection surface.

## Scope

This milestone covers:

- defining the supported Power BI projection model;
- implementing projection from canonical semantic types into Power BI-oriented metadata;
- supporting tables, columns, relationships, measures, roles, formatting, visibility, and summarization metadata;
- supporting fact/dimension classification where semantics are available;
- defining projection options and naming policies;
- defining unsupported-shape behavior and stable diagnostics;
- adding tests and package smoke tests;
- adding public documentation and NuGet package documentation;
- updating projection capability matrix content where applicable;
- preparing the package for inclusion in the 1.0 release readiness path.

The implementation must preserve this boundary:

```text
SemanticTypeModel.PowerBI projects semantic metadata into a Power BI model representation.
It does not publish to the Power BI service and does not require Power BI service credentials.
```

## Non-Goals

This milestone does not cover:

- publishing datasets or semantic models to the Power BI service;
- authenticating against Power BI REST APIs;
- managing workspaces, reports, dashboards, gateways, refresh schedules, or deployment pipelines;
- generating DAX beyond explicitly supported measure metadata or templates;
- implementing full Power BI semantic model parity;
- reverse-engineering existing Power BI datasets;
- creating PBIX files;
- implementing provider-specific database access;
- adding unrelated projections.

Power BI service publishing and deployment automation are post-1.0 candidates unless explicitly introduced by a later milestone.

## Implementation Router

Read only the authoritative documents needed for the focus area being implemented:

- relevant specs from `docs/specs/`;
- `docs/ENGINEERING.md` and `docs/engineering/command-contract.md` for validation-tier selection;
- `docs/PUBLIC-DOCS.md` and affected `public-docs/` pages only when the change is consumer-facing;
- architecture or decision records only when the change alters structure or rationale.

Historical research guide copies are non-authoritative references and are not required milestone reading.

## Focus Areas

Use the milestone scope to choose one or more focused implementation slices instead of treating the whole milestone as a single work item:

| Focus area | Validation tier | Documentation impact |
|---|---|---|
| Behavior or API implementation | Tier 1 during development, Tier 2 before completion | Direct when behavior is consumer-facing; otherwise update specs only when contracts change. |
| Tests and diagnostics | Tier 1 for the affected test project or diagnostic filter, Tier 2 before completion | Direct for public diagnostics; deferred only when examples require a later feature slice. |
| Public documentation, samples, or release readiness | Tier 0 for documentation checks, Tier 3 for package/release readiness | Direct for changed public docs and package README sources; record deferred docs explicitly. |

## Validation Tier

- Default implementation focus areas: Tier 1 during the inner loop, then Tier 2 before completion.
- Documentation-only focus areas: Tier 0 plus `./eng/public-docs.sh` when public documentation changes.
- Packaging or release focus areas: Tier 3 or Tier 4 as described by the release-readiness documents.

## Package

The implementation package is:

```text
SemanticTypeModel.PowerBI
```

The package owns:

- Power BI projection model types;
- Power BI projection options;
- Power BI projection annotations;
- Power BI projection diagnostics;
- Power BI package README;
- Power BI public guide;
- Power BI package smoke tests.

The package must not introduce Power BI service publishing as part of the core projection API.

## Conceptual Projection Boundary

The canonical semantic model must remain projection-neutral.

Power BI-specific metadata must be represented either as:

- namespaced annotations on the canonical model; or
- options supplied to the Power BI projection; or
- Power BI projection output metadata.

The canonical model must not become a Power BI/TOM object model.

The projection should follow this conceptual flow:

```text
TypeSchemaModel
  -> PowerBiProjectionModel
  -> optional serializer/export adapter
```

The first-class supported output is the repository-defined Power BI projection model. If a TOM adapter is implemented, it must be an adapter from that projection model rather than a replacement for it.

## Supported Projection Concepts

The projection must define supported behavior for at least the following concepts.

### Tables

Object/entity semantic types may project to Power BI tables.

The implementation must support:

- table name;
- display name;
- description;
- hidden flag;
- table role where known;
- source semantic type identity;
- annotations used to trace back to the canonical model.

Supported table roles should include at least:

```text
Fact
Dimension
Bridge
DegenerateDimension
Unknown
```

If the existing model has different role terms, map them consistently and document the mapping.

### Columns

Scalar members may project to Power BI columns.

The implementation must support:

- column name;
- display name;
- description;
- scalar data type;
- nullability;
- hidden flag;
- format string where known;
- summarization behavior;
- key/identifier metadata;
- source semantic member identity;
- annotations used to trace back to the canonical model.

Supported scalar mappings must include at least:

```text
string
boolean
integer
number / decimal / double
date
date-time
time, if represented by the model
guid / identifier, as string or documented identifier representation
enum, as configured text or numeric representation
```

Unsupported scalars must produce diagnostics rather than silently projecting incorrect metadata.

### Measures

The projection must define a measure representation even if initial measure generation is conservative.

The model should support:

- measure name;
- display name;
- description;
- expression, if explicitly supplied;
- format string;
- hidden flag;
- source semantic member or annotation identity where applicable.

The implementation must not invent business measures unless explicit semantic metadata or options request them.

### Relationships

The projection must support semantic relationships where sufficient metadata exists.

Relationship output must include:

- from table;
- from column;
- to table;
- to column;
- cardinality;
- direction, where supported;
- active/inactive flag, if represented;
- relationship name or stable identity;
- source semantic relationship identity.

The implementation must detect ambiguous relationships and report diagnostics.

### Formatting

The projection must support format metadata for common types:

- currency;
- percentage;
- integer;
- decimal;
- date;
- date-time;
- duration, if supported;
- custom format string annotations.

Formatting should be projection metadata. It must not alter the canonical scalar type.

### Summarization

The projection must support default summarization behavior for numeric columns.

Supported summarization values should include at least:

```text
None
Sum
Average
Min
Max
Count
DistinctCount
```

Default behavior must be deterministic and documented.

Identifier/key columns should default to no summarization unless explicitly configured otherwise.

### Visibility

The projection must support hidden/display metadata for:

- technical keys;
- foreign keys;
- relationship implementation columns;
- internal columns;
- explicitly hidden semantic members.

Default hiding behavior must be configurable.

## Required Public API Shape

The exact names may change during implementation, but equivalent capabilities must exist.

### Projection Entry Point

Provide an API similar to:

```csharp
PowerBiProjectionModel powerBiModel = semanticModel.ToPowerBiModel(options =>
{
    options.DefaultTableRole = PowerBiTableRole.Dimension;
    options.HideTechnicalKeys = true;
    options.HideForeignKeys = true;
    options.DefaultNumericSummarization = PowerBiSummarization.Sum;
});
```

or:

```csharp
PowerBiProjectionModel powerBiModel = PowerBiProjection.Project(
    semanticModel,
    options =>
    {
        options.DefaultTableRole = PowerBiTableRole.Dimension;
        options.UseNamingPolicy(PowerBiNamingPolicy.DisplayName);
    });
```

The API must be deterministic and usable without dependency injection.

If DI integration exists elsewhere in the repository, provide an optional DI registration only as a secondary API.

### Projection Options

Provide options equivalent to:

```csharp
public sealed class PowerBiProjectionOptions
{
    public PowerBiNamingPolicy NamingPolicy { get; set; }
    public PowerBiTableRole DefaultTableRole { get; set; }
    public PowerBiEnumProjectionMode EnumProjectionMode { get; set; }
    public PowerBiSummarization DefaultNumericSummarization { get; set; }
    public bool HideTechnicalKeys { get; set; }
    public bool HideForeignKeys { get; set; }
    public bool IncludeUnsupportedAnnotations { get; set; }
    public bool TreatRelationshipsAsRequired { get; set; }
}
```

The final option names may differ, but the implementation must support equivalent policy control.

### Projection Model

Define an output model equivalent to:

```csharp
public sealed class PowerBiProjectionModel
{
    public IReadOnlyList<PowerBiTable> Tables { get; }
    public IReadOnlyList<PowerBiRelationship> Relationships { get; }
    public IReadOnlyList<PowerBiDiagnostic> Diagnostics { get; }
}
```

The output model must be inspectable in tests without needing external Power BI tooling.

### Annotation Names

Provide typed constants for Power BI-specific annotation keys, for example:

```csharp
public static class PowerBiAnnotationNames
{
    public const string TableRole = "powerBi.tableRole";
    public const string TableName = "powerBi.tableName";
    public const string ColumnName = "powerBi.columnName";
    public const string MeasureExpression = "powerBi.measureExpression";
    public const string FormatString = "powerBi.formatString";
    public const string Summarization = "powerBi.summarization";
    public const string Hidden = "powerBi.hidden";
}
```

If the repository has an existing annotation-name pattern, follow it.

## Attribute and Annotation Integration

Power BI-specific annotations must be projection-specific and should live in `SemanticTypeModel.PowerBI`, not in `SemanticTypeModel.Abstractions`.

Projection-neutral semantic attributes from M0014 must be consumed where applicable.

Recommended separation:

```text
Semantic attributes
  express canonical meaning, such as entity, key, relationship, display name, description, constraints

Power BI annotations
  express Power BI presentation/projection behavior, such as table role, format string, hidden flag, summarization
```

Potential Power BI-specific attributes may include:

```csharp
[SemanticPowerBiTableRole(PowerBiTableRole.Dimension)]
[SemanticPowerBiFormatString("#,0.00")]
[SemanticPowerBiSummarization(PowerBiSummarization.Sum)]
[SemanticPowerBiHidden]
[SemanticPowerBiMeasure("SUM(Sales[Amount])")]
```

The exact naming must follow repository conventions.

Power BI attributes must be optional. Users should be able to get a useful projection from canonical semantic metadata alone.

## Mapping Requirements

### Entity/Object to Table

A semantic entity/object may project to a table when it is not explicitly excluded.

Rules:

- entity role should prefer table projection;
- value-object role should project only when referenced or explicitly configured;
- simple object members may project as nested structures only if the Power BI projection explicitly supports flattening;
- unsupported nested object shapes must produce diagnostics.

### Member to Column

A scalar member projects to a column when:

- it is included by semantic extraction;
- it is not explicitly ignored for Power BI projection;
- it maps to a supported Power BI scalar type.

### Key to Column Metadata

Semantic key metadata must project to key/identifier metadata.

Default behavior:

- key columns are included;
- technical key visibility follows `HideTechnicalKeys`;
- key columns default to no summarization.

### Relationship to Relationship Metadata

Semantic relationship metadata must project to Power BI relationship metadata when:

- both endpoint tables can be projected;
- relationship keys can be resolved;
- cardinality can be inferred or explicitly supplied.

Ambiguity must produce diagnostics.

### Enum to Column

Enum projection must be configurable.

Supported modes:

```text
Name
Value
NameAndValue, if represented as separate metadata or table
```

Default mode must be documented.

### Complex/Nested Types

Nested complex types must follow a documented policy:

- flatten when configured and deterministic;
- project as related table when configured and relationship metadata exists;
- reject with diagnostic when unsupported.

Silent lossy flattening is not acceptable.

### Collections

Collections must follow a documented policy:

- collection of scalars is unsupported unless explicitly represented;
- collection of entities may project as relationship/table when endpoint identity is known;
- collection of value objects requires explicit support or diagnostic.

### Dictionaries

Dictionaries should be unsupported for the initial hardened projection unless a deliberate representation is implemented.

Unsupported dictionaries must produce a stable diagnostic.

### Unions / Polymorphism

Union and polymorphic shapes should be unsupported or limited for the initial hardened projection.

Unsupported union/polymorphism must produce stable diagnostics.

## Diagnostics

Add stable Power BI projection diagnostics. Diagnostic IDs may be renamed to match repository policy, but equivalent coverage must exist.

Suggested IDs:

### PBI001: Unsupported scalar type

A member uses a scalar type that cannot be projected to a Power BI column.

### PBI002: Unsupported nested object shape

A nested object cannot be flattened or projected as a related table under current options.

### PBI003: Ambiguous relationship endpoints

A relationship cannot be projected because endpoints or keys are ambiguous.

### PBI004: Missing relationship key

A relationship references a table or member that lacks resolvable key metadata.

### PBI005: Unsupported collection shape

A collection cannot be projected under current options.

### PBI006: Unsupported dictionary shape

A dictionary cannot be projected to the Power BI model.

### PBI007: Unsupported union or polymorphic shape

A union or polymorphic model cannot be represented in the Power BI projection.

### PBI008: Conflicting table role metadata

A semantic type has conflicting table role annotations or role inference conflicts with explicit metadata.

### PBI009: Invalid measure expression metadata

A measure expression annotation is malformed or attached to an unsupported target.

### PBI010: Invalid format string metadata

A format string annotation is incompatible with the projected column or measure type.

Each diagnostic must include:

- stable diagnostic ID;
- severity;
- clear message;
- model path where available;
- source location where available;
- actionable guidance.

## Tests

Add or update tests for Power BI projection behavior.

### Unit Tests

Cover:

- entity to table projection;
- scalar member to column projection;
- display name and description mapping;
- key column metadata;
- hidden technical key behavior;
- hidden foreign key behavior;
- enum projection modes;
- numeric summarization defaults;
- date/date-time format metadata;
- custom format string metadata;
- explicit table role annotations;
- fact/dimension classification;
- relationship projection;
- unsupported dictionary diagnostic;
- unsupported union/polymorphism diagnostic;
- unsupported nested object diagnostic;
- deterministic ordering and naming.

### Snapshot Tests

Add stable snapshot-style tests for representative projection outputs.

Snapshots should cover:

- simple dimension table;
- fact table with measures;
- fact-to-dimension relationship;
- entity with hidden technical keys;
- model with unsupported shapes and diagnostics.

Snapshot format should be text or JSON and suitable for review in source control.

### Integration Tests

Cover:

- projection from code-first annotated model;
- projection from runtime-created semantic model;
- projection using Power BI-specific annotations;
- projection using only projection-neutral semantic metadata.

### Package Smoke Tests

Update package smoke tests so a clean consumer project can:

- reference `SemanticTypeModel.PowerBI` from packed artifacts;
- create or load a small semantic model;
- project it to a Power BI projection model;
- inspect tables, columns, relationships, and diagnostics;
- build without project references.

## Samples

Add or update a sample, for example:

```text
samples/PowerBi.Basic
```

The sample should show:

- annotated C# entities;
- semantic type model creation or generation;
- Power BI projection options;
- table/column/relationship output;
- diagnostic output for unsupported or ambiguous shapes;
- no Power BI service dependency.

If repository sample naming differs, follow the existing convention.

## Documentation Requirements

Create or update:

```text
docs/specs/power-bi-projection.md
docs/specs/projection-capability-matrix.md
docs/MILESTONES.md
docs/SPECS.md
docs/PUBLIC-DOCS.md
public-docs/packages.md
public-docs/concepts.md
public-docs/guides/power-bi.md
public-docs/diagnostics.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/release-notes.md
```

If the repository has equivalent paths, update those instead.

Documentation must explain:

- what the Power BI projection does;
- what it does not do;
- how semantic entities map to Power BI tables;
- how scalar members map to columns;
- how relationships map;
- how table roles work;
- how measures are represented;
- how formatting and summarization are represented;
- how hidden metadata is handled;
- unsupported shapes and diagnostics;
- how to use projection options;
- package dependency and installation guidance.

## Projection Capability Matrix Impact

Update the projection capability matrix so Power BI support is explicit for at least:

```text
object/entity
scalar
required/nullable
enum
dictionary
collection
relationship
key
computed member
measure
format string
summarization
hidden/display metadata
nested object
union/polymorphism
```

Each entry must indicate one of:

```text
supported
partial
unsupported
not applicable
```

Partial entries must link or refer to limitations.

## Public Documentation Impact

Update public docs so users can understand the package without reading tests or source code.

At minimum:

- add a Power BI guide;
- add package documentation for `SemanticTypeModel.PowerBI`;
- update the package map;
- update release notes;
- update diagnostics documentation;
- link to capability matrix limitations.

## Package and Release Impact

Ensure `SemanticTypeModel.PowerBI` is included in prerelease and 1.0 release readiness when implemented.

Package metadata must include:

```text
PackageId
Title
Description
Authors
RepositoryUrl
RepositoryType
PackageTags
PackageReadmeFile
PackageLicenseExpression
PublishRepositoryUrl
EmbedUntrackedSources
IncludeSymbols
SymbolPackageFormat
```

The package README source must be:

```text
public-docs/nuget/SemanticTypeModel.PowerBI.md
```

## Implementation Notes

The implementation should prefer a repository-owned projection model that can be tested without external Power BI dependencies.

If a TOM adapter is added, keep it isolated and optional.

Projection output must be deterministic:

- stable ordering;
- stable names;
- stable diagnostics;
- stable annotation keys;
- stable formatting of snapshot outputs.

Do not silently drop unsupported members. Unsupported or degraded projection behavior must be visible through diagnostics.

Do not introduce service credentials, workspace IDs, tenant IDs, or deployment configuration into the core projection package.

## Validation

At minimum, implementation must pass:

```sh
./eng/check.sh
```

If release scripts exist, implementation must also pass:

```sh
./eng/package-smoke.sh 0.1.0-alpha
./eng/public-docs.sh
./eng/public-docs.sh
```

If `./eng/release-check.sh` exists and is expected for milestone validation, run:

```sh
./eng/release-check.sh 0.1.0-alpha
```

Use the repository’s current version argument if it differs from `0.1.0-alpha` when the milestone is implemented.

## Acceptance Criteria

The milestone is complete when:

- `SemanticTypeModel.PowerBI` exposes a supported Power BI projection API;
- projection output is inspectable without external Power BI service dependencies;
- entity/object semantic types can project to tables;
- scalar members can project to columns;
- key metadata is projected;
- relationship metadata is projected where resolvable;
- table roles are supported or inferred where possible;
- format string metadata is supported;
- summarization metadata is supported;
- hidden/display metadata is supported;
- unsupported shapes produce stable diagnostics;
- projection options are documented and tested;
- deterministic projection output is tested;
- package smoke tests consume packed `SemanticTypeModel.PowerBI` artifacts;
- public docs explain Power BI package usage and limitations;
- projection capability matrix includes Power BI behavior;
- package README source exists;
- release notes are updated;
- `docs/MILESTONES.md` is updated;
- no non-root README files are introduced;
- repository validation commands pass.

## Completion Report

When closing this milestone, report:

- Power BI public APIs added;
- projection concepts supported;
- unsupported concepts and diagnostics added;
- package metadata updates;
- tests added;
- sample added;
- public docs updated;
- capability matrix updates;
- validation commands run;
- whether package smoke tests consumed packed artifacts.
