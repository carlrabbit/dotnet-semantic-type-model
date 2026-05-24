# M0007 - Power BI TOM Projection Prototype

## Purpose

Deliver the first Power BI / Tabular Object Model projection prototype over the hardened canonical `TypeSchemaModel`, without introducing service or deployment dependencies.

## Delivered Runtime Surface

- New projection package: `SemanticTypeModel.PowerBI`.
- TOM-like intermediate metadata model:
  - `TabularModelDefinition`
  - `TabularTableDefinition`
  - `TabularColumnDefinition`
  - `TabularRelationshipDefinition`
  - `TabularMeasureDefinition`
- Projection entrypoint:
  - `PowerBiTabularProjection : ISchemaProjection<TabularModelDefinition>`
- Deterministic projection options:
  - table candidacy for unannotated objects
  - value object modes (diagnose, flatten, serialize)
  - unsupported-shape behavior (diagnose, ignore-with-warning, serialize)
  - enum and numeric mapping modes
  - hidden column inclusion
  - name collision behavior (diagnose or suffix)

## Supported Baseline Mapping

- Object roles (`Fact`, `Dimension`, `Lookup`, `Entity`) and `powerBi.tableRole` map objects to tables.
- Scalar and enum properties map to tabular columns.
- Keys influence `IsKey`.
- Canonical relationships map to tabular relationships for one-to-one, one-to-many, and many-to-one.
- DAX computed members map to measures with expression/language preserved.
- Display metadata and projection annotations are consumed with precedence rules from the Power BI/TOM spec.

## Diagnostics Coverage

M0007 emits projection diagnostics for:

- object not projected due to missing role/annotation;
- value object handling mode requirements;
- unsupported nested/object/array/dictionary/union shapes;
- relationship endpoint table/column/key resolution failures;
- lossy scalar mappings (for example DateTimeOffset, Duration, Json);
- unsupported measure expression languages;
- invalid projection annotation value types;
- duplicate projected names across tables, columns, measures, and relationships.

## Fixture/Test Coverage

Short-running unit tests cover:

1. Dimension table projection (key, scalar columns, display metadata, data category).
2. Fact + dimension relationship projection and DAX measure projection.
3. DAX measure preservation and unsupported expression-language diagnostics.
4. Value-object behavior across diagnose/flatten/serialize modes.
5. Unsupported shape diagnostics and configured serialization behavior.
6. Name collision diagnostics and deterministic suffix behavior.

## Non-goals Preserved

- no Power BI service connectivity;
- no XMLA endpoint deployment;
- no credentials/auth integration;
- no refresh policy implementation;
- no calculation-group support;
- no DAX parser/validator;
- no EF Core, OpenAPI, browser, JavaScript, TypeScript, or Playwright dependency additions.
