# M0057: EF Model Shape Test Matrix and Inherited Member Placement for 2.5.3

## Status

Implemented; release candidate awaiting human review and publication approval.

## Goal

Fix the EF Core package defect where relational application attempts to ignore or configure a property on the wrong CLR declaring type, especially when the property is inherited from a semantic base entity or a non-semantic structural base.

Prepare a non-publishing `2.5.3` patch release.

This milestone also establishes a permanent EF model-shape test matrix. One real-life model is not sufficient coverage for EF projection. The EF package must be validated against deliberately small, surgical fixture models that isolate inheritance, ownership, JSON conversion, convention suppression, and TPT placement failures.

## Repository Profile

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Profile | `dotnet-library` |
| Role | Product repository and capability provider |
| Released baseline | `2.5.x` |
| Target release | `2.5.3` |
| Milestone type | Patch plus permanent test-surface expansion |
| Execution mode | `ai-executed-human-reviewed` |
| Publication | Separate human-approved follow-up |

## Problem Statement

The `2.5.0` reset made the EF projection contract intentionally smaller. `2.5.1` corrected convention-discovered `ValueKind` entities. A remaining failure occurs when EF application tries to ignore or configure a property on a CLR type where that property is not declared.

Typical failure shape:

```text
semantic entity derives from a semantic base entity
semantic entity also inherits members from a non-semantic base
ModelBuilder application loops over CLR properties
application ignores/configures by member name on the wrong EntityTypeBuilder
EF reports that the property is located on a base type
```

This is not only a single bug. It shows that the EF package needs an explicit member-placement model and a systematic test matrix.

## Architectural Rule

Every relational column must know where it is declared and where it is stored.

For every projected property, derive:

```text
Semantic declaring type
CLR declaring type
Semantic storage entity
CLR storage entity
Column/property member name
Storage table
```

The application layer must configure each property through the `EntityTypeBuilder` that owns the storage table, not merely through the CLR type currently being iterated.

## Required Placement Rules

### Property Declared on Concrete Entity

Example:

```text
ImportSpecification.ImportName
```

Configure on:

```text
ImportSpecification
```

### Property Declared on Semantic Base Entity

Example:

```text
Specification.DisplayName
ImportSpecification : Specification
```

Configure on:

```text
Specification
```

Do not configure or ignore the inherited `DisplayName` property again on `ImportSpecification`.

### Property Declared on Non-Semantic Base of a Semantic Entity

Example:

```text
VersionedExtensibleObject.SchemaVersion
Specification : VersionedExtensibleObject
```

Configure on the first semantic storage entity:

```text
Specification
```

`VersionedExtensibleObject` is not an EF entity.

### Property Declared on Non-Semantic Base of a Non-Inheritance Entity

Example:

```text
VersionedObject.SchemaVersion
VersionedOrder : VersionedObject
```

Configure on:

```text
VersionedOrder
```

### Inherited ExtensionData

Example:

```text
ExtensibleObject.ExtensionData
Specification : ExtensibleObject
```

Configure as JSON column on the first semantic storage entity.

### Inherited Owned ValueKind Object

Example:

```text
SourceConfiguredObject.Source
SourceOrder : SourceConfiguredObject
```

Configure as JSON object column on the semantic storage entity.

### Inherited Owned ValueKind Collection

Example:

```text
FieldConfiguredObject.DerivedFields
FieldConfiguredOrder : FieldConfiguredObject
```

Configure as JSON array column on the semantic storage entity.

### Hidden or Duplicate Property Name

Do not select a reflection member nondeterministically. Emit `EF_MEMBER_DECLARATION_AMBIGUOUS` unless the semantic model can unambiguously resolve placement.

## Relational Model Metadata Requirement

Extend the EF relational model so column descriptors carry enough placement metadata.

At minimum:

```text
EfScalarColumn
  PropertyId
  MemberName
  ColumnName
  ClrType
  ProviderType
  IsNullable
  DeclaringClrType
  StorageClrType
  SemanticDeclaringTypeId
  StorageSemanticTypeId

EfJsonColumn
  PropertyId
  MemberName
  ColumnName
  JsonShape
  ValueType
  IsNullable
  DeclaringClrType
  StorageClrType
  SemanticDeclaringTypeId
  StorageSemanticTypeId
```

Equivalent names are acceptable, but the implementation must distinguish declaration from storage.

## Application Rule

Do not implement EF application as:

```text
for each entity CLR type:
  for each public property on that CLR type:
    ignore/configure by name
```

Instead:

```text
for each projected entity:
  ignore only properties declared on that CLR type when safe

for each projected column:
  configure the column on its StorageClrType
  use the member from DeclaringClrType
  never configure inherited semantic-base properties on derived TPT entities
```

The implementation must not call `Ignore(property.Name)` against an entity builder when EF considers that property declared on a base entity type.

## Permanent Test Model Matrix

Add a dedicated test fixture project or namespace:

```text
tests/fixtures/SemanticTypeModel.EFCoreModelShapes/
```

Alternative location is acceptable if repository conventions prefer colocated fixtures, but the fixture suite must remain distinct from the large real-life fixtures.

Required surgical model shapes:

1. `FlatOrder` — flat semantic entity baseline.
2. `VersionedOrder : VersionedObject` — non-semantic base scalar.
3. `ExtensibleOrder : ExtensibleObject` — non-semantic base `ExtensionData`.
4. `SourceOrder : SourceConfiguredObject` — non-semantic base owned `ValueKind` object.
5. `FieldConfiguredOrder : FieldConfiguredObject` — non-semantic base owned `ValueKind` collection.
6. `Specification`, `ImportSpecification`, `WorkflowSpecification` — semantic TPT inheritance.
7. `VersionedExtensibleObject`, `Specification`, `ImportSpecification`, `WorkflowSpecification` — TPT with non-semantic grandbase.
8. `SourceOptions` reused by multiple semantic entities as JSON object.
9. `SourceOptions : VersionedValue` — `ValueKind` with inherited scalar.
10. `SourceOptions` owns nested `RetryPolicy` — nested `ValueKind`.
11. Polluted `ModelBuilder` pre-registering `ValueKind` as keyless entities.
12. Hidden property name with `new`.
13. Semantic base property plus derived property with same name.
14. Non-semantic base plus semantic base chain.
15. Optional owned `ValueKind` on base plus required owned `ValueKind` on derived.

## Mandatory Assertions

Every ModelBuilder test must assert the exact final EF entity inventory:

```text
actual EF CLR entity types == expected semantic Entity CLR types
```

Do not merely assert presence of expected entities.

Every inheritance test must assert property placement:

```text
semantic-base properties exist on semantic base entity type
derived properties exist on derived entity type
non-semantic-base properties exist on first semantic storage entity
inherited properties are not duplicated on derived TPT entities
```

Every ValueKind test must assert:

```text
ValueKind type is not in IModel.GetEntityTypes()
ValueKind is not keyless
ValueKind has no table
owner property remains mapped as converted JSON property
```

## SQLite Integration Requirement

At least one SQLite test per fixture group:

```text
flat baseline
non-semantic base scalar
non-semantic base ExtensionData
semantic TPT inheritance
TPT plus non-semantic grandbase
owned JSON object
owned JSON array
nested ValueKind JSON
```

Each test must run:

```text
EnsureCreated
insert supported minimal instance
load supported minimal instance
assert final EF model inventory
```

If insert/load is not supported for a shape, the limitation must be explicit and human-reviewed.

## Real-Life Fixture Role

The anonymized real-life fixtures remain end-to-end acceptance tests.

They are not a substitute for the surgical matrix.

## Diagnostics

Add:

```text
EF_MEMBER_DECLARATION_AMBIGUOUS
EF_MEMBER_STORAGE_ENTITY_UNRESOLVED
EF_MEMBER_DECLARING_TYPE_MISMATCH
```

Use diagnostics only for semantic/model defects. Do not surface raw EF exceptions for known placement cases.

## Documentation

Update EF docs to explain:

```text
property declaration and storage placement are distinct
non-semantic base members are stored on the first semantic storage entity
semantic-base members are stored on the semantic base table
derived TPT entities store only derived state plus TPT key
ValueKind members are JSON properties, never EF entities
```

Add `2.5.3` release notes.

## Non-Goals

```text
new EF relationship concepts
TPH or TPC
OwnsOne or OwnsMany
foreign-key inference
navigation mapping
general-purpose EF modeling
manual consumer ignores
backward compatibility with removed 2.4.x APIs
```

## Required Authority Documents

```text
AGENTS.md
README.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/specs/opinionated-ef-relational-projection-contract.md
docs/specs/ef-convention-suppression-and-exact-entity-allowlist.md
docs/milestones/m0055-opinionated-relational-projection-contract-and-ef-core-package-reset-for-2-5-0.md
docs/milestones/m0056-preemptive-ef-convention-suppression-and-2-5-1-release-preparation.md
public-docs/guides/ef-core-projection.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/release-notes.md
```

## Source and Test Areas

```text
src/SemanticTypeModel.EFCore/EfRelationalProjection.cs
src/SemanticTypeModel.EFCore/EfRelationalModel.cs
tests/fixtures/SemanticTypeModel.EFCoreModelShapes/
tests/fixtures/SemanticTypeModel.RealWorldFixtures/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
tests/integration/SemanticTypeModel.EFCore.Tests.Integration/
samples/code-first-ef-core/
tests/package-smoke/
```

## Validation Commands

### Focused Validation

```sh
./eng/test-filter.sh MemberPlacement
./eng/test-filter.sh DeclaringType
./eng/test-filter.sh NonSemanticBase
./eng/test-filter.sh Tpt
./eng/test-filter.sh ValueKind
./eng/test-filter.sh ExtensionData
./eng/test-filter.sh ModelShape
./eng/test-filter.sh M0057

./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
./eng/test-project.sh tests/integration/SemanticTypeModel.EFCore.Tests.Integration
```

### Repository Completion

```sh
./eng/check.sh
```

### 2.5.3 Release Preparation

```sh
./eng/package.sh 2.5.3
./eng/package-smoke.sh 2.5.3
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.5.3
```

Inspect package inventory:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

Do not publish packages.

## Acceptance Criteria

### Member Placement

- Inherited semantic-base properties are configured on semantic base entity builders only.
- Non-semantic-base properties are configured on the first semantic storage entity.
- Derived TPT entities do not configure inherited semantic-base properties as local properties.
- Ignoring a property never targets a builder where EF considers the property declared on a base type.
- Hidden member names produce deterministic diagnostics.

### Relational Model

- Column descriptors include declaring and storage metadata.
- Application uses storage metadata instead of ad hoc reflection loops.
- Reflection member lookup is deterministic.
- No raw EF wrong-declaring-type exception is surfaced.

### Test Matrix

- The EF model-shape fixture suite exists.
- All mandatory model shapes are covered.
- Each ModelBuilder test checks exact entity inventory.
- Each inheritance test checks property placement.
- Each ValueKind test checks absence from EF entity set.

### Integration

- SQLite tests cover flat, base, TPT, JSON object, JSON array, nested JSON, and extension-data shapes.
- Real-life fixture tests still pass.
- Insert/load is tested for all supported shapes.

### Release

- `./eng/check.sh` passes.
- `./eng/package.sh 2.5.3` passes.
- `./eng/package-smoke.sh 2.5.3` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.5.3` passes.
- No package is published inside the milestone.

## Human Review Requirements

Human review is required for:

```text
member placement metadata names
hidden-member diagnostic behavior
non-semantic-base storage rules
TPT property placement
SQLite round-trip coverage
real fixture inventory
test matrix completeness
2.5.3 package inventory
release notes
publication approval
```
