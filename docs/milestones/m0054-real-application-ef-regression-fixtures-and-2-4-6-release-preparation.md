# M0054: Real Application EF Regression Fixtures and 2.4.6 Release Preparation

## Status

Completed.

## Goal

Stabilize EF Core projection and closed `ModelBuilder` application against real application-shaped semantic models.

The 2.4.5 line improved source-lineage diagnostics and derivation application policy, but real models still show that EF source lineage is too broad and that existing tests do not represent production shapes. M0054 adds anonymized real-life regression fixtures based on two attached application models, introduces real EF Core `ModelBuilder` tests, adds SQLite in-memory integration tests, and fixes EF source-lineage scope so framework/interface/compiler-adjacent types are not treated as EF lineage candidates.

Target patch release: `2.4.6`.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Published 2.4.5 package set |
| Release target | `2.4.6` |
| Execution mode | `ai-executed-human-reviewed` |
| Capability-provider scope | EF Core projection, source-lineage filtering, real CLR `DbContext` model building, SQLite integration tests, anonymized regression fixtures, samples, public docs, release readiness |
| Fixture basis | Real-life specification-modeling and import-state-persistence reference models provided by the user; must be anonymized and rewritten into repository sample terminology |

## Execution Mode

`ai-executed-human-reviewed`.

This milestone is a stabilization and test-surface correction milestone. Human review is required for fixture anonymization, naming, test boundaries, SQLite dependency usage, diagnostic expectations, release notes, and publication.

## Problem Statement

The library has repeatedly passed isolated unit tests while failing realistic application models.

The current failure symptom is lineage diagnostics for types that should not participate in EF application at all, such as:

```text
IEquatable<T>
static generic marker interfaces
System.Xml / framework helper types
record infrastructure
non-semantic framework or compiler-adjacent types
```

This indicates that EF source lineage is still being built from the canonical object-model surface too broadly instead of from the EF projection/application scope.

## Core Architectural Rule

EF source lineage must be projection-scope driven.

```text
include:
  root EF entity source types
  owned/value-object source types reachable from projected EF mappings
  declaring CLR types for included semantic members

exclude:
  interfaces
  generic marker/static interfaces
  framework helper types
  compiler-generated / record infrastructure
  non-semantic base types as roots
  DTO/request/query types not selected for EF projection
  repository abstractions
```

A non-semantic abstract base class may contribute inherited member lineage, but must not become a root EF source type by itself.

## Fixture Sources and Anonymization

The attached real-life ZIP files must be used only as source material for regression fixture design. Do not copy private/business naming into public repository tests.

### Fixture A: Order Intake Specification Model

Anonymized from the specification-modeling sample.

Use the repository's existing example language. Suggested names:

| Source concept | Anonymized fixture concept |
|---|---|
| specification entity base | `ConfigurableSpecification` |
| import specification | `OrderIntakeSpecification` |
| import/source type enum | `OrderIntakeSourceType` |
| delivery contract | `PartnerDeliveryAgreement` |
| import schedule | `OrderIntakeSchedule` |
| polling config | `SourcePollingPolicy` |
| CSV source | `DelimitedFileSource` |
| XML source | `StructuredFileSource` |
| web service 1 source | `PrimaryApiSource` |
| web service 2 source | `SecondaryApiSource` |
| post-processing | `NormalizationPipeline` |
| derived property | `DerivedOrderField` |
| specification marker interface | `IConfigurationKind<TSelf>` |
| non-semantic versioned base | `VersionedExtensibleObject` |

Required shape:

```text
abstract non-semantic base:
  VersionedExtensibleObject
    SchemaVersion
    ExtensionData with JsonExtensionData + SemanticExtensionData

abstract semantic entity base:
  ConfigurableSpecification : VersionedExtensibleObject

concrete semantic entity:
  OrderIntakeSpecification : ConfigurableSpecification, IConfigurationKind<OrderIntakeSpecification>

owned value objects:
  PartnerDeliveryAgreement
  OrderIntakeSchedule
  SourcePollingPolicy
  DelimitedFileSource
  StructuredFileSource
  PrimaryApiSource
  SecondaryApiSource
  NormalizationPipeline

optional owned value objects:
  DelimitedFileSource?
  StructuredFileSource?
  PrimaryApiSource?
  SecondaryApiSource?

owned collection:
  IReadOnlyList<DerivedOrderField>
```

Required language features:

```text
records
record inheritance
abstract records
sealed records
generic static marker interface
inherited semantic members
JsonExtensionData
SemanticExtensionData
SemanticRequiredWhen
SemanticOwned(Kind = Object)
SemanticOwned collection case
Uri
DateOnly
TimeOnly
TimeSpan
DateTimeOffset
Guid
```

### Fixture B: Order Fulfillment Run State Model

Anonymized from the import-state-persistence reference model.

Suggested names:

| Source concept | Anonymized fixture concept |
|---|---|
| run id | `FulfillmentRunId` |
| import id | `OrderSourceId` |
| execution id | `SourceExecutionId` |
| technical failure id | `ProcessingFailureId` |
| management operation id | `ControlOperationId` |
| component state id | `ComponentSnapshotId` |
| persisted import state | `PersistedOrderSourceState` |
| import statistics | `OrderSourceStatistics` |
| component state envelope | `ComponentStateEnvelope` |
| import run snapshot | `OrderFulfillmentRunSnapshot` |
| import execution record | `SourceExecutionRecord` |
| technical failure record | `ProcessingFailureRecord` |
| management operation record | `ControlOperationRecord` |
| save request | `SaveFulfillmentRunRequest` |
| overview DTO | `FulfillmentRunOverview` |
| repository | `FulfillmentRunStateRepository` |

Required shape:

```text
record struct identifiers
aggregate-like persisted run snapshot
nested value objects
execution/failure/control-operation history
IReadOnlyList<T>
IReadOnlyDictionary<string,string>
ReadOnlyMemory<byte>
request DTOs
overview DTOs
repository abstraction
```

Add SemanticTypeModel annotations in the test fixture so the EF projection expectations are explicit. DTOs and repository abstractions must not become EF entities.

## Scope

### 1. Add Anonymized Regression Fixture Source

Add dedicated test fixture code, not public production samples unless intentionally promoted later.

Preferred structure:

```text
tests/fixtures/SemanticTypeModel.RealWorldFixtures/
  OrderIntakeSpecificationModel/
    Model.cs
    OrderIntakeSpecificationDbContext.cs
  OrderFulfillmentRunStateModel/
    Model.cs
    OrderFulfillmentRunStateDbContext.cs
```

Alternative location is acceptable if repository conventions prefer colocated test fixtures.

The fixture must be rewritten/anonymized. It must not expose original private/business identifiers.

### 2. Add Derivation Regression Tests

For `OrderIntakeSpecificationModel`:

```text
DeriveEfCoreModel_ClosedClrModel_ReturnsNoLineageErrors_ForOrderIntakeSpecificationModel
DeriveEfCoreModel_ClosedClrModel_DoesNotReportLineage_ForIEquatable
DeriveEfCoreModel_ClosedClrModel_DoesNotReportLineage_ForGenericConfigurationInterface
DeriveEfCoreModel_ClosedClrModel_DoesNotReportLineage_ForSystemXmlOrJsonInfrastructure
DeriveEfCoreModel_ClosedClrModel_PreservesInheritedExtensionDataAsSuppressedMember
DeriveEfCoreModel_ClosedClrModel_DoesNotTreatVersionedExtensibleObjectAsRootEntity
DeriveEfCoreModel_ClosedClrModel_DoesNotTreatValueObjectsAsRootEntities
DeriveEfCoreModel_ClosedClrModel_ReportsOwnedCollectionPolicyOnlyForActualOwnedCollection
```

For `OrderFulfillmentRunStateModel`:

```text
DeriveEfCoreModel_ReturnsNoUnexpectedLineageErrors_ForRunStatePersistenceModel
DeriveEfCoreModel_HandlesRecordStructIdentifiers
DeriveEfCoreModel_HandlesReadOnlyDictionaryReferences_WithConfiguredUnsupportedShapePolicy
DeriveEfCoreModel_HandlesReadOnlyMemoryPayload_WithExpectedBinaryPolicy
DeriveEfCoreModel_DoesNotProjectRequestDtosAsEntities
DeriveEfCoreModel_DoesNotProjectRepositoryAbstractionsAsEntities
```

### 3. Add Real EF Core ModelBuilder Tests

Add tests that build an actual EF Core model:

```csharp
using var context = new TestDbContext(options);
IModel model = context.Model;
```

Required assertions:

```text
ClosedClrModel_ModelBuilder_BuildsModel_ForOrderIntakeSpecification
ClosedClrModel_ModelBuilder_DoesNotMapExtensionData_AsPropertyNavigationOrRelationship
ClosedClrModel_ModelBuilder_DoesNotMapIEquatableOrConfigurationInterface
ClosedClrModel_ModelBuilder_DoesNotCreateEntity_ForVersionedExtensibleObject
ClosedClrModel_ModelBuilder_DoesNotCreateRootEntity_ForOwnedValueObjects
ClosedClrModel_ModelBuilder_ConfiguresOwnedOptionalValueObjects
ClosedClrModel_ModelBuilder_HandlesOwnedCollectionPolicyDeterministically
```

### 4. Add SQLite Integration Tests

Add SQLite in-memory integration tests.

Use a shared open in-memory connection:

```csharp
var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<TestDbContext>()
    .UseSqlite(connection)
    .Options;

await using var context = new TestDbContext(options);
await context.Database.EnsureCreatedAsync();
```

Required tests:

```text
Sqlite_EnsureCreated_Succeeds_ForOrderIntakeSpecificationModel
Sqlite_InsertAndLoad_Succeeds_ForMinimalOrderIntakeSpecification
Sqlite_EnsureCreated_Succeeds_ForOrderFulfillmentRunStateModel
Sqlite_InsertAndLoad_Succeeds_ForMinimalFulfillmentRunSnapshot
Sqlite_DoesNotCreateColumns_ForExtensionData
Sqlite_DoesNotCreateTables_ForValueObjectRoots
```

If insert/load is not yet realistic for some storage policies, require `EnsureCreated` and metadata assertions now, and document insert/load limitations.

### 5. Fix EF Source Lineage Scope

Change source-lineage construction so it does not iterate over all canonical object definitions as EF lineage candidates.

Required behavior:

```text
lineage scope is derived from EF projection/application scope
non-semantic framework/interface/helper types are excluded
record infrastructure is excluded
generic static marker interfaces are excluded
non-semantic base classes may provide member declaring type metadata but are not root EF source types
```

Lineage diagnostics must not mention:

```text
IEquatable
IConfigurationKind
System.Xml
System.Text.Json internals
Dictionary internals
StringComparer
record infrastructure
repository abstractions
DTOs not selected for EF projection
```

unless such a type is explicitly marked as a semantic EF-applicable type by the fixture.

### 6. Improve Diagnostic Quality

Current diagnostics should be filtered by EF-applicable scope.

Do not emit `EFCORE_SOURCE_LINEAGE_CLR_TYPE_NOT_RESOLVED` for excluded types.

If such a type reaches EF scope unexpectedly, emit a better diagnostic:

```text
EFCORE_SOURCE_LINEAGE_TYPE_OUT_OF_SCOPE
```

Message intent:

```text
A non-semantic framework/interface/helper type reached EF source-lineage scope. EF lineage must be derived from projected EF scope, not the full canonical object graph.
```

### 7. Improve Test Surface

Add a short engineering note or docs update stating that EF tests require three layers:

```text
unit tests for projection and lineage mechanics
real ModelBuilder tests using CLR DbContext
SQLite integration tests for provider-backed model creation and basic persistence
```

### 8. 2.4.6 Release Preparation

Run full package and release checks for `2.4.6`.

Do not publish packages.

## Non-Goals

- No provider-specific production JSON storage redesign.
- No full relational persistence redesign for every collection/dictionary shape.
- No support for arbitrary EF convention mapping outside the closed EF semantic model.
- No public exposure of original user/private model names.
- No repository abstraction generation.
- No broad DTO mapping feature.
- No migrations support beyond EF metadata and SQLite `EnsureCreated` tests.
- No package publication, tag creation, or GitHub release creation inside this milestone.
- No copied external guide documents, TBPs, issue templates, workflow documents, or non-root README files.

## Implementation Constraints

- Fixtures must be anonymized.
- The real-life source ZIPs are source material only and must not be copied verbatim into public docs/tests.
- EF lineage must be projection-scope driven.
- Real `ModelBuilder` tests are mandatory.
- SQLite in-memory integration tests are mandatory unless blocked by explicit human-reviewed repository constraints.
- `ExtensionData` must not become an EF property/navigation/relationship.
- Semantic value objects must not become root EF entities.
- Non-semantic bases must not become EF entities.
- Static generic marker interfaces must not participate in EF lineage.
- Record infrastructure must not participate in EF lineage.
- Use canonical `eng/` scripts.
- Do not publish packages.

## Required Authority Documents

### Always Read

```text
AGENTS.md
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/engineering/command-contract.md
docs/engineering/packaging.md
docs/engineering/release-readiness.md
docs/engineering/samples.md
public-docs/release-notes.md
public-docs/api/compatibility.md
```

### EF Architecture Authority

```text
docs/specs/ef-core-real-application-regression-fixtures.md
docs/specs/ef-core-source-lineage-scope-filtering.md
docs/specs/ef-core-source-lineage-diagnostics.md
docs/specs/ef-core-closed-modelbuilder-application.md
docs/specs/ef-core-clr-convention-suppression.md
docs/specs/ef-core-role-aware-owned-storage.md
docs/specs/type-model-ef-core-projection.md
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-compile-time-generator.md
public-docs/guides/ef-core-projection.md
public-docs/guides/core-semantics.md
```

### Source, Fixtures, and Tests

```text
src/SemanticTypeModel.EFCore/
src/SemanticTypeModel.DotNet/
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.Abstractions/
src/SemanticTypeModel.Core/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
tests/unit/SemanticTypeModel.DotNet.Tests.Unit/
tests/unit/SemanticTypeModel.Generators.Tests.Unit/
tests/integration/
tests/fixtures/
tests/package-smoke/
samples/OrderFulfillment.Domain/
samples/code-first-ef-core/
```

### Release Validation

```text
eng/check.sh
eng/package.sh
eng/package-smoke.sh
eng/samples.sh
eng/public-docs.sh
eng/release-check.sh
src/*/*.csproj
samples/*/*.csproj
```

Ordinary implementation agents must not read `.guide-profile.json` or `.guide-sync/`.

## Files or Areas Likely Affected

```text
src/SemanticTypeModel.EFCore/
src/SemanticTypeModel.DotNet/
src/SemanticTypeModel.Generators/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
tests/unit/SemanticTypeModel.DotNet.Tests.Unit/
tests/unit/SemanticTypeModel.Generators.Tests.Unit/
tests/integration/
tests/fixtures/
tests/package-smoke/
docs/specs/ef-core-real-application-regression-fixtures.md
docs/specs/ef-core-source-lineage-scope-filtering.md
docs/specs/ef-core-source-lineage-diagnostics.md
docs/specs/ef-core-closed-modelbuilder-application.md
public-docs/guides/ef-core-projection.md
public-docs/api/compatibility.md
public-docs/release-notes.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
.guide-sync/pending/
```

## Validation Tiers and Concrete Commands

### Tier 1 — Focused Loop

```sh
./eng/test-filter.sh RealWorldFixtures
./eng/test-filter.sh OrderIntakeSpecification
./eng/test-filter.sh OrderFulfillmentRunState
./eng/test-filter.sh SourceLineage
./eng/test-filter.sh IEquatable
./eng/test-filter.sh ExtensionData
./eng/test-filter.sh ModelBuilder
./eng/test-filter.sh Sqlite
./eng/test-filter.sh EFCore

./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
```

If an integration test project exists or is added:

```sh
./eng/test-project.sh tests/integration/SemanticTypeModel.EFCore.Tests.Integration
```

### Tier 2 — Repository Completion

```sh
./eng/check.sh
```

### Tier 3 — 2.4.6 Package and Release Validation

```sh
./eng/package.sh 2.4.6
./eng/package-smoke.sh 2.4.6
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.4.6
```

Inspect package inventory:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

## Acceptance Criteria

### Fixture Coverage

- Anonymized `OrderIntakeSpecificationModel` fixture exists.
- Anonymized `OrderFulfillmentRunStateModel` fixture exists.
- Fixture code preserves the structural complexity of the attached real-life models.
- Fixture names match repository sample terminology.
- Original private/business names are not copied into public tests/docs.

### Derivation and Lineage

- `DeriveEfCoreModel(ClosedClrModel)` succeeds for both fixtures without unexpected lineage errors.
- EF lineage diagnostics are not emitted for `IEquatable<T>`.
- EF lineage diagnostics are not emitted for static generic marker interfaces.
- EF lineage diagnostics are not emitted for framework/helper types that are not EF-applicable.
- Non-semantic abstract bases contribute inherited member metadata without becoming EF roots.
- `ExtensionData` is represented as suppressed EF member on derived semantic types.
- Semantic value objects are not root EF entities.
- Owned collection behavior is deterministic and diagnostic only for actual owned collection policy gaps.

### ModelBuilder

- Real CLR `DbContext` model builds for the order-intake specification fixture.
- Real CLR `DbContext` model builds for the fulfillment-run-state fixture.
- `ExtensionData` is not an EF property.
- `ExtensionData` is not an EF navigation.
- `ExtensionData` is not an EF relationship.
- `IEquatable<T>` / marker interfaces are not mapped.
- Non-semantic bases are not root EF entity types.
- Owned optional value objects are configured deterministically.

### SQLite Integration

- SQLite in-memory `EnsureCreated` succeeds for both fixtures.
- Minimal insert/load succeeds where the current storage policy supports it.
- Where insert/load is not yet supported, the test asserts metadata and documents the explicit limitation.
- SQLite schema does not include extension-data columns.
- SQLite schema does not create root tables for semantic value objects.

### Documentation

- Specs document real-application fixture policy.
- Specs document EF source-lineage scope filtering.
- Public EF guide states that real `ModelBuilder` and SQLite tests are part of EF compatibility validation.
- Release notes document 2.4.6 as a real-application EF regression hardening release.

### Release Readiness

- `./eng/check.sh` passes.
- `./eng/package.sh 2.4.6` produces expected packages.
- `./eng/package-smoke.sh 2.4.6` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.4.6` passes.
- No package is published during milestone implementation.
- Publication remains explicit human-approved follow-up.

## Direct Documentation Impact

Implementation must update:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/specs/ef-core-real-application-regression-fixtures.md
docs/specs/ef-core-source-lineage-scope-filtering.md
docs/specs/ef-core-source-lineage-diagnostics.md
docs/specs/ef-core-closed-modelbuilder-application.md
public-docs/guides/ef-core-projection.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

The package adds:

```text
.guide-sync/pending/m0054-2-4-6-publication-follow-up.md
```

It tracks only later human-approved publication, tag, release, and post-publication verification.

Ordinary implementation agents do not need to read `.guide-sync/`.

## Human Review Requirements

Human review is required for:

- anonymized fixture names;
- whether fixture code sufficiently preserves real-life structure;
- integration test project location;
- SQLite dependency and test configuration;
- insert/load expectations for unsupported storage shapes;
- diagnostic code and severity updates;
- source-lineage filtering rules;
- release notes wording;
- package contents;
- publication approval.

## Out-of-Scope Guide Migration Work

M0054 is not a guide migration.

Do not update, copy, or reference external guide documents as target-repository operational authority.
