# M0056: Preemptive EF Convention Suppression and 2.5.1 Release Preparation

## Status

Complete. Implementation, release-candidate validation, package-inventory inspection, and required human review are complete. Publication remains a separate, explicitly approved follow-up.

## Goal

Fix the released `2.5.0` EF application defect where EF Core convention discovery registers semantic `ValueKind` CLR types and other non-entity types as EF entity types, including keyless entities, before the semantic relational model can configure them as converted JSON properties.

Prepare a non-publishing `2.5.1` patch release.

This milestone preserves the opinionated `2.5.0` relational contract:

```text
Entity -> table
Semantic entity inheritance -> TPT
Owned ValueKind object -> JSON object column
Owned ValueKind collection -> JSON array column
ExtensionData -> JSON object column
Entity links -> identifiers only
```

The contract is correct. The application order and convention boundary are incomplete.

## Repository Profile

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Profile | `dotnet-library` |
| Role | Product repository and capability provider |
| Released baseline | `2.5.0` |
| Target release | `2.5.1` |
| Milestone type | Patch correction |
| Execution mode | `ai-executed-human-reviewed` |
| Publication | Separate human-approved follow-up |

## Released Defect

The current `ApplySemanticRelationalModel(...)` implementation performs this sequence:

```text
1. Read existing EF entity types.
2. Report all non-allowed types as EF_UNEXPECTED_CONVENTION_ENTITY.
3. Return immediately when diagnostics contain errors.
4. Only later attempt to remove contained ValueKind entity types and other unexpected entity types.
```

This is internally contradictory.

Convention-discovered `ValueKind` types cause an error before the cleanup phase can run. The cleanup phase is therefore unreachable for the exact defect it is intended to correct.

Real applications can consequently observe errors such as:

```text
semantic ValueKind discovered as keyless entity
ValueKind reported as unexpected convention entity
ValueKind reported as requiring a key
ValueKind treated as DbSet/root entity even though it is only an owned JSON property
```

## Required Behavioral Invariant

After semantic relational application:

```text
final EF entity CLR type set
==
projected semantic Entity CLR type set
```

No semantic `ValueKind`, non-semantic base, DTO, repository type, framework helper, record infrastructure type, or other convention-discovered CLR type may remain as an EF entity type.

Provider-owned internal metadata may be exempted only when explicitly identified and tested.

## Required Application Sequence

Replace the current application order with:

```text
1. Build the semantic entity allowlist.
2. Build the semantic non-entity suppression set.
3. Remove or ignore already convention-discovered non-allowed CLR entity types.
4. Register only allowed semantic Entity CLR types.
5. Ignore all CLR properties on allowed entities before explicit remapping.
6. Apply TPT.
7. Configure keys and scalar columns.
8. Configure JSON object, JSON array, and ExtensionData columns.
9. Remove any convention-discovered entity types introduced during configuration.
10. Finalize an exact allowlist audit.
11. Emit EF_UNEXPECTED_CONVENTION_ENTITY only when a non-allowed entity remains after correction.
```

Do not treat correctable convention discovery as an immediate fatal diagnostic before attempting correction.

## Convention Suppression Sets

### Allowed EF Entity CLR Types

Exactly:

```text
EfRelationalModel.Entities.Select(entity => entity.ClrType)
```

These correspond to semantic `Entity` types only.

### Explicitly Suppressed CLR Types

Include:

```text
semantic ValueKind CLR types reachable from EfJsonColumn
owned collection item ValueKind CLR types
nested ValueKind CLR types reachable inside JSON documents
non-semantic structural base CLR types when EF discovered them as entities
other CLR types referenced only by explicitly converted JSON properties
```

Do not suppress scalar/provider types.

### Unexpected Types

Any final EF entity CLR type not in the allowlist is unexpected.

Unexpected types are not tolerated merely because they are keyless.

## Correction Strategy

Use EF mutable-model APIs deliberately.

Acceptable implementation mechanisms include:

```text
modelBuilder.Ignore(type)
modelBuilder.Model.RemoveEntityType(entityType)
pre-registering ignored ValueKind types before semantic entities
repeating cleanup after explicit property configuration
```

The implementation may use more than one mechanism because EF conventions can reintroduce metadata while model configuration proceeds.

The behavior matters more than the exact API call.

## Required Ordering Correction

The implementation must not do this:

```text
audit -> report errors -> return -> cleanup
```

It must do this:

```text
cleanup/suppress -> apply -> cleanup -> audit -> return
```

Diagnostics from derivation remain blocking.

Convention-discovery diagnostics are blocking only after the package has attempted deterministic suppression and final-model correction.

## JSON Property Preservation

Suppressing a `ValueKind` as an EF entity must not suppress its use as the CLR type of a converted property.

Example:

```csharp
[SemanticOwned(Kind = SemanticOwnershipKind.Object)]
public CsvSourceSpecification? CsvSource { get; init; }
```

Required final model:

```text
ImportSpecification is an EF entity.
CsvSource is an EF scalar property using a JSON converter.
CsvSourceSpecification is not an EF entity.
CsvSourceSpecification is not keyless.
CsvSourceSpecification does not require DbSet registration.
```

For collections:

```csharp
[SemanticOwned(Kind = SemanticOwnershipKind.Collection)]
public IReadOnlyList<DerivedPropertySpecification> DerivedProperties { get; init; }
```

Required final model:

```text
DerivedProperties is one JSON array column.
DerivedPropertySpecification is not an EF entity.
No child table exists.
No key is required.
```

## TPT Preservation

The convention suppression phase must not remove semantic entity base types required for TPT.

For:

```text
Specification
ImportSpecification : Specification
WorkflowSpecification : Specification
```

the final EF entity set must contain exactly:

```text
Specification
ImportSpecification
WorkflowSpecification
```

and no owned `ValueKind` type.

## Final Entity Allowlist Audit

After all configuration and cleanup:

```csharp
HashSet<Type> expected = model.Entities
    .Select(entity => entity.ClrType)
    .ToHashSet();

HashSet<Type> actual = modelBuilder.Model
    .GetEntityTypes()
    .Select(entity => entity.ClrType)
    .Where(type => type is not null)
    .ToHashSet();
```

The audit must verify set equality.

For every remaining unexpected CLR type, emit:

```text
EF_UNEXPECTED_CONVENTION_ENTITY
```

The diagnostic must identify:

```text
unexpected CLR type
whether EF marked it keyless
which navigation/property path caused discovery when EF metadata exposes that information
expected semantic entity allowlist
```

Do not emit this diagnostic for a type that was successfully removed.

## Tests

### Unit Application Tests

Add tests that start from a `ModelBuilder` already polluted by conventions.

Required cases:

```text
Apply_Removes_PreDiscovered_ValueKind_Entity
Apply_Removes_PreDiscovered_ValueKind_CollectionItem
Apply_Removes_PreDiscovered_NonSemantic_Base
Apply_Preserves_Allowed_Semantic_Entities
Apply_Preserves_Tpt_Base_And_Derived_Entities
Apply_Maps_ValueKind_Object_As_Json_Property
Apply_Maps_ValueKind_Collection_As_Json_Array_Property
Apply_DoesNot_Return_EF_UNEXPECTED_CONVENTION_ENTITY_When_Correction_Succeeds
Apply_Reports_EF_UNEXPECTED_CONVENTION_ENTITY_When_Type_Remains_After_Correction
```

Construct polluted models explicitly where necessary:

```csharp
modelBuilder.Entity<CsvSourceSpecification>().HasNoKey();
modelBuilder.Entity<DerivedPropertySpecification>().HasNoKey();
```

Then apply the semantic relational model and assert that both types are absent from the final model.

### Exact Entity Inventory Tests

For every real fixture:

```text
actual EF CLR entity set equals expected semantic Entity CLR set
```

Do not assert only the presence of expected entities. Assert absence of every additional entity.

Required direct assertions:

```csharp
context.Model.FindEntityType(typeof(CsvSourceSpecification)) is null
context.Model.FindEntityType(typeof(XmlSourceSpecification)) is null
context.Model.FindEntityType(typeof(DerivedPropertySpecification)) is null
```

Also assert that the owner properties remain mapped as converted properties.

### Real ModelBuilder Tests

Use the anonymized real-life specification fixture.

The test DbContext should contain ordinary entity `DbSet` properties and call the public semantic application path from `OnModelCreating`.

Required outcomes:

```text
model builds without keyless ValueKind errors
Specification/ImportSpecification/WorkflowSpecification are the only relevant EF entities
owned object ValueKinds are JSON columns
owned collections are JSON array columns
no ValueKind appears as an EF entity
```

### SQLite Integration Tests

Required:

```text
Sqlite_EnsureCreated_Succeeds_When_ValueKinds_Were_ConventionDiscoverable
Sqlite_RoundTrip_Succeeds_With_Owned_Json_Object
Sqlite_RoundTrip_Succeeds_With_Owned_Json_Array
Sqlite_Final_Model_Contains_Only_Semantic_Entities
```

The tests must use the public package application path, not internal test-only setup.

### DbSet Misuse Regression

Add a regression fixture where:

```text
DbContext contains DbSet<ImportSpecification>
ImportSpecification has CsvSourceSpecification? property
CsvSourceSpecification is Semantic ValueKind
```

The final model must not require or infer:

```text
DbSet<CsvSourceSpecification>
a key for CsvSourceSpecification
a table for CsvSourceSpecification
keyless entity metadata for CsvSourceSpecification
```

## Test Quality Requirement

A test is insufficient if it only validates `EfRelationalModel`.

At least one test must inspect the final EF `IModel`.

At least one test must run SQLite `EnsureCreated`.

At least one test must insert and reload an entity containing both:

```text
owned JSON object
owned JSON array
```

## Diagnostics

Retain:

```text
EF_UNEXPECTED_CONVENTION_ENTITY
```

but redefine its application timing:

```text
Only emitted after deterministic convention suppression and final exact-set audit.
```

Do not add a configuration option to permit ValueKinds as keyless entities.

There is no valid mode where a semantic `ValueKind` becomes an EF entity.

## Public API

No new configuration switch is required.

The existing public paths must behave correctly:

```csharp
model.DeriveEfRelationalModel(...)
modelBuilder.ApplySemanticRelationalModel(...)
modelBuilder.ApplySemanticTypeModel(...)
```

Do not expose EF convention suppression as a consumer responsibility.

## Documentation

Update the EF guide and package documentation to state:

```text
The package actively suppresses EF convention discovery for semantic ValueKinds.
Only semantic Entity types remain in the final EF model.
JSON-owned ValueKinds are converted properties, not owned/keyless EF entities.
```

Document the final exact entity-set invariant.

Add `2.5.1` release notes describing:

```text
fixed semantic ValueKinds being convention-discovered as keyless entities
fixed premature EF_UNEXPECTED_CONVENTION_ENTITY diagnostics
fixed cleanup ordering in ApplySemanticRelationalModel
added exact final entity allowlist validation
```

## Non-Goals

```text
new relational concepts
new ownership modes
EF OwnsOne or OwnsMany
support for ValueKinds as keyless entities
consumer-configurable convention policy
relationship inference
foreign-key inference
navigation mapping
TPH or TPC
backward compatibility work unrelated to the patch
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
docs/milestones/m0055-opinionated-relational-projection-contract-and-ef-core-package-reset-for-2-5-0.md
public-docs/guides/ef-core-projection.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/release-notes.md
```

## Source and Test Areas

```text
src/SemanticTypeModel.EFCore/EfRelationalProjection.cs
src/SemanticTypeModel.EFCore/EfRelationalModel.cs
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/M0055RelationalContractTests.cs
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/M0055DiagnosticContractTests.cs
tests/integration/SemanticTypeModel.EFCore.Tests.Integration/M0055SqliteTests.cs
tests/fixtures/SemanticTypeModel.RealWorldFixtures/
samples/code-first-ef-core/
tests/package-smoke/
```

## Validation Commands

### Focused Validation

```sh
./eng/test-filter.sh Convention
./eng/test-filter.sh ValueKind
./eng/test-filter.sh Keyless
./eng/test-filter.sh UnexpectedConventionEntity
./eng/test-filter.sh ModelBuilder
./eng/test-filter.sh Sqlite
./eng/test-filter.sh M0056

./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
./eng/test-project.sh tests/integration/SemanticTypeModel.EFCore.Tests.Integration
```

### Repository Completion

```sh
./eng/check.sh
```

### 2.5.1 Release Preparation

```sh
./eng/package.sh 2.5.1
./eng/package-smoke.sh 2.5.1
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.5.1
```

Inspect package inventory:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

Do not publish packages.

## Acceptance Criteria

### Convention Boundary

- Semantic `ValueKind` types are removed or ignored before fatal convention diagnostics are evaluated.
- Already discovered keyless ValueKinds are removed.
- JSON object property CLR types remain usable as converted properties.
- JSON collection item CLR types do not remain as EF entities.
- Non-semantic bases do not remain as EF entities.
- Semantic entity TPT roots and derived entities remain intact.

### Ordering

- Cleanup occurs before convention diagnostics become blocking.
- Cleanup is repeated after explicit mapping.
- Final audit occurs after all correction attempts.
- `EF_UNEXPECTED_CONVENTION_ENTITY` is emitted only for residual unexpected types.

### Exact Final Model

- Final EF CLR entity set exactly equals projected semantic Entity CLR set.
- No semantic ValueKind is present in `IModel.GetEntityTypes()`.
- No ValueKind is marked keyless.
- No ValueKind table is created.
- No ValueKind `DbSet` is required.

### Real Fixture

- Specification, ImportSpecification, and WorkflowSpecification remain three TPT entities/tables.
- CsvSource-like ValueKinds are JSON object columns.
- Derived-property ValueKind collections are JSON array columns.
- The real fixture builds through the public `OnModelCreating` path.

### Integration

- SQLite `EnsureCreated` succeeds.
- Owned JSON object round trip succeeds.
- Owned JSON array round trip succeeds.
- Final SQLite-backed EF model contains only semantic entities.

### Release

- `./eng/check.sh` passes.
- `./eng/package.sh 2.5.1` passes.
- `./eng/package-smoke.sh 2.5.1` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.5.1` passes.
- No package is published inside the milestone.

## Human Review Requirements

Human review is required for:

```text
mutable-model cleanup strategy
Ignore versus RemoveEntityType ordering
provider metadata exemptions
final entity-set audit
diagnostic timing and wording
real fixture model inventory
SQLite round-trip results
2.5.1 package inventory
release notes
publication approval
```
