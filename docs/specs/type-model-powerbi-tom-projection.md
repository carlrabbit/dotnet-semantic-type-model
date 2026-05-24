# Type Model Power BI / TOM Projection Specification

## Purpose

Define deterministic projection of hardened canonical `TypeSchemaModel` contracts into a TOM-like intermediate tabular metadata model.

## Authority

This specification is authoritative for:

- Power BI / TOM projection scope in M0007;
- projection naming and annotation precedence;
- table/column/relationship/measure mapping behavior;
- projection options and unsupported-shape behavior;
- required diagnostic expectations for this projection target.

## Dependency Boundary

- Projection package: `SemanticTypeModel.PowerBI`.
- Core abstraction contracts remain independent from Power BI/TOM-specific dependencies.
- M0007 uses an internal TOM-like model and does not require Power BI service, XMLA, credentials, or network access.

## Projection Model Contract

The projection result is represented by:

- `TabularModelDefinition`
- `TabularTableDefinition`
- `TabularColumnDefinition`
- `TabularRelationshipDefinition`
- `TabularMeasureDefinition`

Minimum semantics:

- tables with columns and measures;
- relationship endpoints and cardinality;
- column key/nullability/hidden/data-category/format metadata;
- carried annotations;
- accumulated structured projection diagnostics.

## Annotation Namespaces

Projection consumes:

- `powerBi.*`
- `tom.*`
- `ui.*` (shared display metadata where relevant)
- `schema.*` (shared metadata where relevant)

Required handled (or diagnosed if invalid) keys:

- `powerBi.tableRole`
- `powerBi.dataCategory`
- `powerBi.displayFolder`
- `powerBi.formatString`
- `powerBi.isHidden`
- `tom.tableName`
- `tom.columnName`
- `tom.measureExpression`
- `tom.measureFormatString`
- `tom.relationshipName`

## Naming Precedence

### Table names

1. `tom.tableName`
2. `powerBi.tableName` (when present)
3. canonical `DisplayName`
4. canonical `Name`

### Column names

1. `tom.columnName`
2. `powerBi.columnName` (when present)
3. property `DisplayName`
4. property `Name`

### Relationship names

1. `tom.relationshipName`
2. canonical relationship id (`RelationshipId.Value`)

Invalid annotation value types are diagnosable.

## Object-to-Table Mapping

Object types project to tables when one of these is true:

- role is `Fact`, `Dimension`, `Lookup`, or `Entity`;
- explicit `powerBi.tableRole` annotation is present;
- options enable projection of unannotated objects.

Value objects do not become tables by default.

## Property-to-Column Mapping

### Scalar mapping baseline

- Boolean -> Boolean
- String -> String
- Integer -> Int64
- Number -> Double or Decimal (options-driven)
- Decimal -> Decimal
- Date -> Date
- Time -> Time
- DateTime -> DateTime
- DateTimeOffset -> DateTime (lossy diagnostic)
- Duration -> String (diagnostic)
- Guid -> String
- Binary -> Binary (with mapping diagnostic in baseline prototype)
- Json -> String (diagnostic)

Lossy mappings are diagnosable.

### Enum mapping

- default: string-based storage;
- option for numeric storage when enum metadata indicates numeric backing.

### Key influence

- key-participating properties set `IsKey = true`.
- relationship projection emits diagnostics when required key assumptions are not met.

## Relationships

Projection supports:

- one-to-one;
- one-to-many;
- many-to-one.

Many-to-many is diagnosed as unsupported in the baseline prototype unless later explicitly enabled.

Relationship projection requires:

- both endpoint types projected as tables;
- endpoint properties projected as columns.

Missing endpoint/table/column resolution is diagnosable.

## Measures (Computed Members)

- computed members with expression language `DAX` project to measures by default;
- expression text and language are preserved;
- display folder and format string annotations are preserved when present;
- no DAX syntax validation is performed in M0007;
- unsupported expression languages are diagnosed unless explicitly preserved by option.

## Value Objects, Arrays, Dictionaries, and Unions

### Value objects

Options determine behavior:

- diagnose;
- flatten deterministic nested names (`parent_child`);
- serialize as JSON/string column.

### Arrays/dictionaries/unions/nested unsupported shapes

Options determine behavior:

- diagnose;
- ignore with warning;
- serialize as JSON/string.

No silent shape loss is allowed.

## Projection Options

`PowerBiProjectionOptions` includes behavior controls for:

- unannotated object table projection;
- value object mode;
- unsupported shape behavior;
- enum mode;
- numeric mode;
- hidden column inclusion;
- name collision behavior;
- unsupported expression preservation.

Behavior must be deterministic.

## Name Collision Handling

Duplicate projected names for tables, columns, measures, and relationships must be handled deterministically:

- diagnose-and-skip (default), or
- suffix-based renaming when configured.

## Diagnostics

Projection emits `SchemaDiagnostic` entries with:

- `Stage = Projection`
- `ProjectionTarget = PowerBi`

Required diagnostic classes include:

- object not projected (missing role/annotation);
- invalid projection annotation type/value;
- unsupported shapes;
- value object mode conflicts;
- relationship endpoint/key projection failures;
- lossy scalar mapping;
- unsupported measure expression language;
- duplicate projected names.

## Test Coverage Requirements

Short-running tests must cover at least:

1. dimension table fixture;
2. fact + relationship fixture;
3. computed DAX measure fixture (+ unsupported language diagnostic);
4. value object mode fixture;
5. unsupported shape fixture;
6. name collision fixture.
