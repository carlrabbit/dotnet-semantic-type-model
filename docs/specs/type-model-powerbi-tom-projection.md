# Type Model Power BI / Local Metadata Projection Specification

## Status

Authoritative behavioral specification for the Power BI domain semantic model and local metadata projection.

## Purpose

Define deterministic derivation of code-generated canonical semantic models into a Power BI analytical domain semantic model and deterministic local metadata output.

This specification is authoritative for:

- Power BI domain semantic model behavior;
- Power BI derivation pipeline behavior;
- Power BI metadata precedence;
- tables, columns, relationships, measures, calculated tables, and analytical metadata;
- user extension points for DAX artifacts;
- local deterministic export behavior;
- unsupported Power BI/TOM scope boundaries;
- Power BI projection diagnostics.

## Product Role

`SemanticTypeModel.PowerBI` projects code-generated canonical semantic models into local analytical metadata.

The package flow is:

```text
Code-generated canonical Semantic Type Model
  -> Power BI derivation transformations
  -> PowerBiSemanticModel
  -> deterministic local metadata output
```

The package does not publish to the Power BI Service, generate PBIX files, authenticate, manage workspaces, schedule refresh, execute XMLA operations, or provide full TOM parity.

## Dependency Boundary

- Projection package: `SemanticTypeModel.PowerBI`.
- Core abstraction contracts remain independent from Power BI/TOM-specific dependencies.
- The package exposes a domain derivation API and local metadata export API.
- Baseline usage must not require Power BI Desktop, Power BI Service, XMLA, credentials, network access, a workspace, or PBIX generation.

## Domain Semantic Model Contract

Power BI behavior is derived through a Power BI domain semantic model before local metadata output is emitted.

The domain semantic model must represent:

```text
model metadata
tables
columns
relationships
measures
calculated tables
hierarchies where explicitly modeled
display folders
hidden/visible flags
data categories
summarization hints
format strings
sort-by-column metadata
annotations/extensions
diagnostics
```

The exporter must operate from the Power BI domain semantic model, not from scattered ad hoc annotation lookups.

## Derivation API

The package must expose a domain derivation API equivalent to:

```csharp
var result = model.DerivePowerBiModel(options =>
{
    options.UseDefaultTransformations();
});
```

The result must follow the M0028 pattern:

```csharp
SemanticDerivationResult<PowerBiSemanticModel>
```

or provide equivalent behavior:

```text
domain model
diagnostics
transformation trace
```

Users must be able to configure Power BI derivation transformations in code.

## Local Export API Contract

The package exposes an API equivalent to:

```csharp
PowerBiLocalMetadataExporter.ExportJson(result.Model, outputPath);
```

A convenience API may derive and export in one call only if diagnostics and trace remain available.

M0031 implements deterministic inspection text and neutral JSON metadata. Additional replaceable local formats may be added later. At least one deterministic local output format must exist:

```text
inspection text
neutral JSON metadata
TMDL-like local file/folder output
TOM script text
```

Output must not require service access, credentials, XMLA, Power BI Desktop, or PBIX generation.

## Annotation Namespaces

Projection may consume:

```text
powerBi.*
tom.*
schema.*
ui.*
dotnet.*
```

Required handled or explicitly diagnosed metadata includes:

```text
powerBi.tableRole
powerBi.tableName
powerBi.columnName
powerBi.dataCategory
powerBi.displayFolder
powerBi.formatString
powerBi.isHidden
powerBi.summarizeBy
powerBi.sortByColumn
powerBi.measure
powerBi.calculatedTable
powerBi.hierarchy
tom.tableName
tom.columnName
tom.measureExpression
tom.measureFormatString
tom.relationshipName
schema.*
ui.*
```

## Precedence

### Table names

1. `tom.tableName`
2. `powerBi.tableName`
3. configured naming transformation
4. canonical display name
5. canonical name

### Column names

1. `tom.columnName`
2. `powerBi.columnName`
3. configured naming transformation
4. property display name
5. property name

### Relationship names

1. `tom.relationshipName`
2. configured naming transformation
3. canonical relationship id

### Measure names

1. explicit measure builder/name
2. `powerBi.measure` metadata name
3. configured naming transformation
4. canonical computed member name

Invalid annotation value types are diagnosable.

## Table Mapping

Object types project to tables when one of these is true:

```text
role is Fact, Dimension, Lookup, Aggregate, or Entity
explicit powerBi.tableRole annotation is present
options enable projection of unannotated objects
```

Value objects do not become tables by default.

## Column Mapping

Scalar properties project to columns when they are compatible with Power BI metadata projection or explicitly configured.

Required support:

```text
CLR/canonical type metadata
nullability
key participation
hidden flag
data category
format string
summarization hint
sort-by-column metadata
description/display text
annotations/extensions when configured
```

Lossy scalar mappings are diagnosable.

Unsupported nested/collection shapes must not be silently dropped.

## Scalar and Enum Mapping

Provider-neutral analytical scalar mapping must be deterministic.

Baseline mapping should handle:

```text
Boolean
String
Integer
Number
Decimal
Date
Time
DateTime
DateTimeOffset with lossy diagnostic unless configured
Duration with diagnostic unless configured
Guid as String unless configured
Binary with diagnostic unless configured
Enum as string by default unless configured
```

## Relationships

Projection supports explicit simple analytical relationships:

```text
one-to-one
one-to-many
many-to-one
relationship endpoints
relationship cardinality
active/inactive flag when explicitly modeled
cross-filter direction when explicitly modeled and provider-neutral
relationship name
```

Rules:

- both endpoint types must resolve to projected tables;
- endpoint properties must resolve to projected columns when required;
- unsupported many-to-many or ambiguous relationships emit diagnostics;
- relationship inference beyond explicit metadata is out of scope.

## Measures

Explicit measures are supported.

Required support:

```text
measure name
owning table
DAX expression text
expression language metadata when modeled
format string
display folder
description
hidden flag
annotations/extensions
```

Rules:

- no DAX syntax validation is performed;
- unsupported expression languages emit diagnostics unless preservation is explicitly configured;
- code configuration is preferred for DAX-heavy artifacts;
- attribute-based measure metadata is allowed for simple cases if the repository already supports it.

Candidate behavior:

```csharp
options.Measures.Add<Order>(
    name: "Total Sales",
    dax: "SUM(Orders[Amount])",
    configure: measure =>
    {
        measure.FormatString = "#,0.00 €";
        measure.DisplayFolder = "Sales";
    });
```

## Calculated Tables

Explicit calculated tables are supported as user-owned DAX artifacts.

Required support:

```text
table name
DAX expression text
expression language metadata when modeled
description
display folder
hidden flag
annotations/extensions
optional columns when explicitly described
```

Rules:

- no DAX syntax validation is performed;
- calculated tables are added through options, model configuration, or custom transformations;
- calculated table export must be deterministic;
- unsupported expression languages emit diagnostics unless preservation is explicitly configured.

Candidate behavior:

```csharp
options.CalculatedTables.Add(
    name: "Active Customers",
    dax: "FILTER(Customers, Customers[IsActive] = TRUE())",
    configure: table =>
    {
        table.Description = "Customers currently marked active.";
        table.DisplayFolder = "Derived";
    });
```

## User Extension Points

The package must support extension through code.

Supported extension mechanisms:

```text
derivation options
measure builder
calculated table builder
post-derive model configuration hook
custom Power BI transformations
custom annotations consumed by custom transformations
```

Candidate custom transformation behavior:

```csharp
options.Transformations.AddAfter<DerivePowerBiTablesTransformation>(
    new CurrentSnapshotCalculatedTableTransformation());
```

Extension rules:

- custom transformations operate on the domain derivation pipeline;
- custom transformations emit diagnostics through the shared diagnostic model;
- users should not need to fork exporters for common DAX artifacts;
- unsupported extension metadata must be diagnosable.

## Hierarchies

Basic explicit hierarchies are supported only when metadata is sufficient.

Required support where implemented:

```text
hierarchy name
owning table
ordered levels
level column references
display folder
hidden flag
description
```

Unsupported or unresolved hierarchy levels emit diagnostics.

## Value Objects, Arrays, Dictionaries, and Unions

Default behavior is diagnostic unless configured otherwise.

Supported modes may include:

```text
diagnose
ignore with warning
flatten deterministic nested names
serialize as string/JSON metadata
```

No silent shape loss is allowed.

Inheritance and union-like alternatives are not primary Power BI features. When supported, strategies must be explicit, for example flattening, single table, or table-per-concrete-type. Arbitrary automatic polymorphism inference is out of scope.

## Name Collision Handling

Duplicate projected names for tables, columns, measures, calculated tables, relationships, and hierarchies must be handled deterministically:

```text
diagnose-and-skip by default
suffix-based renaming only when configured
```

## Diagnostics

Projection emits structured diagnostics with:

```text
Stage = Projection
ProjectionTarget = PowerBi
model path when available
transformation id when emitted during derivation
related model paths when applicable
```

Required diagnostic classes include:

```text
object not projected
invalid projection annotation type/value
unsupported shape
value object mode conflict
relationship endpoint/table/column projection failure
lossy scalar mapping
unsupported measure expression language
unsupported calculated table expression language
unresolved sort-by-column reference
unresolved hierarchy level
duplicate projected name
unsupported local export target feature
service/PBIX/XMLA operation requested
```

## Determinism

Power BI derivation and local metadata export must be deterministic.

Required deterministic ordering:

```text
tables by canonical type identifier or configured table name
columns by declaration/order metadata when available, then name
relationships by model path/name
measures by table then name unless explicitly ordered
calculated tables by name unless explicitly ordered
hierarchies by table then name
annotations/extensions by key
diagnostics by model path/code/order of discovery
```

## Inspection Integration

The Power BI domain model and derivation result must integrate with M0027/M0028 inspection.

Required behavior:

```csharp
derived.Diagnostics.ToDiagnosticText();
derived.Trace.ToTransformationText();
PowerBiLocalMetadataExporter.Inspect(derived.Model);
```

or equivalent package-specific inspection methods.

Inspection output must be deterministic and suitable for tests.

## Unsupported Scope

The Power BI package does not define or own:

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
full TOM parity
```

## Test Coverage Requirements

Short-running tests must cover:

```text
domain model derivation from generated model
table mapping
column mapping
relationship mapping
measure addition
calculated table addition
custom transformation extension
display folder mapping
hidden flag mapping
data category mapping
summarization mapping
format string mapping
sort-by-column mapping
basic hierarchy mapping when implemented
unsupported shape diagnostics
unsupported expression diagnostics
duplicate/collision diagnostics
deterministic local export
deterministic inspection output
no service/network/PBIX dependency
```

## Non-Goals

M0031 does not define:

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
