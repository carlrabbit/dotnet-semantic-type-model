# M0052: Closed EF Core Semantic Model Application and 2.4.4 Release Preparation

## Status

Implemented.

## Goal

Correct the architectural direction after the 2.4.3 release.

M0051 introduced EF CLR convention augmentation as a response to EF Core convention leakage. That framing is too permissive for this library. SemanticTypeModel represents a closed domain model. EF Core is a projection target. EF Core conventions must not rediscover, reinterpret, or extend the semantic model behind STM's back.

M0052 must make `EfCoreSemanticModel` the complete EF application contract, enrich it with all source lineage and suppression metadata required for deterministic `ModelBuilder` application, make `ApplySemanticTypeModel(...)` and `ApplyEfCoreSemanticModel(...)` converge on the same closed application engine, demote or rename CLR convention augmentation, and prepare a non-publishing `2.4.4` patch release.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Published 2.4.3 package set |
| Release target | `2.4.4` |
| Execution mode | `ai-executed-human-reviewed` |
| Capability-provider scope | EF Core projection, enriched `EfCoreSemanticModel`, `ModelBuilder` application, source-lineage metadata, convention suppression, public API cleanup, diagnostics, samples, public docs, release readiness |
| Consumer/dogfood scope | Package-based EF sample validates that STM, not EF conventions, owns entity/value-object/property/relationship shape |

## Execution Mode

`ai-executed-human-reviewed`.

This is an architectural correction on a released patch line. Implementation must be narrow enough for 2.4.4, but strong enough to remove the incorrect authority model introduced by the term and behavior of CLR convention augmentation.

Human review is required for public API names, compatibility behavior, obsolete/rename strategy, diagnostics, default application mode, docs, package inventory, and publication.

## Architectural Premise

SemanticTypeModel owns a closed semantic domain.

```text
TypeSchemaModel
  -> EfCoreSemanticModel
  -> ModelBuilder
```

EF Core may provide runtime metadata APIs, provider mechanics, and CLR binding, but EF conventions must not decide what is an entity, value object, owned member, ignored member, navigation, relationship, table, or column. Those decisions belong to the derived `EfCoreSemanticModel`.

## Required Invariant

No EF Core convention-discovered member may affect the final EF model unless it is present in, or explicitly permitted by, `EfCoreSemanticModel`.

This invariant applies to root entities, value objects, owned members, inherited members, extension-data members, base-class members, navigation candidates, relationship candidates, dictionary members, and unsupported shapes.

## Scope

### 1. Enrich `EfCoreSemanticModel`

`EfCoreSemanticModel` must contain all information needed to apply the EF model without relying on EF conventions as authority.

At minimum, it must represent:

```text
Model
  SourceModelId
  ApplicationPolicy
  Diagnostics

Entity / Object Mapping
  SourceSemanticTypeId
  SourceClrTypeName
  SemanticRole
  IsRootEntity
  IsValueObject
  IsOwned
  TableName / schema
  Comment
  Keys
  Indexes
  Properties
  OwnedMappings
  IgnoredMembers
  SuppressedMembers

Property Mapping
  SourcePropertyId
  SourceMemberName
  SourceDeclaringClrTypeName
  StorageKind
  ClrType
  ProviderClrType
  Converter
  ColumnName
  Required/nullability
  Precision/max length
  Comment
  SemanticOnlyKind

Owned Mapping
  OwnerSourceTypeId
  OwnerClrTypeName
  NavigationName
  TargetSourceTypeId
  TargetClrTypeName
  TargetSemanticRole
  StoragePolicy
  SuppressedMembers
  FlattenedProperties / JsonColumn / OwnedNavigation / Diagnostic

Suppressed Member
  SourceMemberName
  SourceDeclaringClrTypeName
  Reason
  SemanticOnlyKind
```

Required semantic-only kind initially includes `ExtensionData`.

### 2. Closed ModelBuilder Application

Implement or refactor to a single closed application engine:

```text
EfCoreSemanticModel
  -> closed ModelBuilder application
```

Both public high-level paths must converge on it:

```text
ApplySemanticTypeModel(...)
  -> derive EfCoreSemanticModel
  -> ApplyEfCoreSemanticModel(...)

ApplyEfCoreSemanticModel(...)
  -> apply closed EF semantic model
```

`ApplySemanticTypeModel(...)` should become a convenience wrapper, not a separate behavior path.

### 3. Replace / Demote CLR Convention Augmentation

The M0051 concept `ClrConventionAugmentation` is too broad and too unopinionated.

Required change:

- remove it as the default authority model;
- rename it, obsolete it, or constrain it so it no longer communicates “EF conventions first, STM second”;
- default behavior must be closed semantic model application;
- if compatibility requires keeping the enum value temporarily, document it as legacy/compatibility and route it through closed application where possible.

Preferred naming:

```csharp
public enum EfCoreApplicationMode
{
    ClosedClrModel,
    SharedTypeModel
}
```

Avoid naming that suggests EF conventions own the model.

### 4. Strict EF Convention Boundary

When applying to CLR-backed EF metadata, the library must:

- suppress semantic-only members represented in `EfCoreSemanticModel`;
- configure only members represented or explicitly permitted by `EfCoreSemanticModel`;
- diagnose unexpected convention-created members when detectable;
- throw or emit diagnostics for `DbSet<ValueObject>` according to reviewed policy;
- prevent inherited `SemanticExtensionData` from becoming an EF property/navigation/relationship;
- prevent non-semantic base classes from becoming EF entities solely because they contributed semantic members.

The minimum hard failure still covered from M0051 remains:

```text
Entity A
  owns ValueObject B
B derives from non-semantic abstract ExtensibleObject
ExtensibleObject declares inherited SemanticExtensionData
EF DbContext exposes DbSet<A>
ModelBuilder application must not allow EF conventions to map ExtensionData
```

### 5. ApplyEfCoreSemanticModel Parity

`ApplyEfCoreSemanticModel(...)` must support the same closed application behavior as `ApplySemanticTypeModel(...)` when source lineage is present.

If a caller supplies an `EfCoreSemanticModel` without required lineage, behavior must be explicit:

```text
EFCORE_SOURCE_LINEAGE_REQUIRED
```

It must not silently fall back to lossy shared-type application unless the caller explicitly selected shared-type mode.

### 6. Shared-Type Projection Remains Explicit

Shared-type projection remains useful but must be explicitly named or selected.

Acceptable surface:

```csharp
modelBuilder.ApplyEfCoreSemanticModelAsSharedTypes(efModel);
```

or:

```csharp
options.ApplicationMode = EfCoreApplicationMode.SharedTypeModel;
```

Shared-type projection is secondary and must not be the default for CLR-backed applications.

### 7. Preserve Prior Patch Corrections

M0052 must preserve M0049 dictionary key/value extraction correctness, M0050 `Uri` and format-compatible scalar behavior, M0050 role-aware owned value-object storage behavior, M0051 inherited `SemanticExtensionData` regression coverage, and M0051 value-object root-boundary hardening.

### 8. Documentation and Release Notes

Update public docs to state:

```text
SemanticTypeModel owns a closed EF semantic model.
EF Core conventions are not semantic authority.
ApplySemanticTypeModel is a convenience path through EfCoreSemanticModel.
ApplyEfCoreSemanticModel is the lower-level closed application path.
Shared-type projection is explicit.
CLR convention augmentation is legacy, renamed, or constrained.
```

2.4.4 release notes must state:

```text
2.4.3 issue:
  EF CLR convention augmentation communicated and partially implemented the wrong authority model.
  EF conventions were still treated as a model source to augment, rather than as behavior to constrain.

2.4.4 correction:
  EfCoreSemanticModel is the complete EF application contract.
  ModelBuilder application is closed over the EF semantic model.
  ApplySemanticTypeModel and ApplyEfCoreSemanticModel converge on the same closed application engine.
  Source lineage and suppression metadata are represented explicitly.
  Shared-type projection is explicit.
```

## Non-Goals

- No broad EF provider-specific redesign.
- No migrations, database creation, DbContext generation, query filters, temporal tables, or provider-specific JSON storage.
- No support for arbitrary EF convention mapping outside the closed semantic model.
- No general dictionary persistence for EF.
- No extension-data storage in EF.
- No support for semantic `ValueObject` as an independent root entity.
- No unrelated documentation cleanup.
- No package publication, tag creation, or GitHub release creation inside this milestone.
- No copied external guide documents, TBPs, issue templates, workflow documents, or non-root README files.

## Focus Areas

### 1. Audit Current EF Application Paths

Inspect all public ModelBuilder extension methods and internal application paths:

```text
ApplySemanticTypeModel
ApplyEfCoreSemanticModel
ApplyProjectedModel
ApplyClrConventionAugmentation
shared-type application code
closed CLR application code, if any
```

### 2. Make EfCoreSemanticModel Lineage-Preserving

Add tests proving the derived EF semantic model contains enough information to apply CLR type identity, CLR member identity, semantic role, source ids, inherited member declaring type, semantic-only extension-data suppression, owned/value-object storage policy, and root/value-object classification.

### 3. Converge Public APIs

Refactor so:

```text
ApplySemanticTypeModel(model)
  == derive EfCoreSemanticModel + ApplyEfCoreSemanticModel(efModel)
```

for closed model application.

### 4. Rename or Constrain Application Modes

Remove, rename, or constrain `ClrConventionAugmentation`.

Required post-fix semantics:

```text
default mode:
  closed semantic model application

shared-type mode:
  explicit

legacy convention augmentation:
  not default, documented as compatibility only if retained
```

### 5. Enforce Closed Convention Boundary

Regression tests must assert EF conventions cannot map inherited `ExtensionData`, value objects as root entities, relationships absent from `EfCoreSemanticModel`, or non-semantic base classes as EF entities.

### 6. ApplyEfCoreSemanticModel Source-Lineage Behavior

Tests must cover:

```text
ApplyEfCoreSemanticModel with complete lineage
  -> closed CLR application succeeds

ApplyEfCoreSemanticModel without required lineage
  -> explicit diagnostic/exception

ApplyEfCoreSemanticModel in shared-type mode
  -> works without CLR lineage when appropriate
```

### 7. Preserve ExtensionData Regression

Use the concrete constellation:

```csharp
[SemanticType(SemanticTypeRole.Entity)]
public sealed class A
{
    [SemanticOwned]
    public required B Value { get; init; }
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class B : ExtensibleObject
{
    public required string Name { get; init; }
}

public abstract class ExtensibleObject
{
    [SemanticExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
```

Expected EF result:

```text
A is root entity.
B is not root entity.
ExtensibleObject is not an EF entity.
ExtensionData is not an EF property.
ExtensionData is not an EF navigation.
ExtensionData is not an EF relationship.
ModelBuilder build succeeds.
```

### 8. Update Samples

Update package-based EF sample to demonstrate preferred closed application. Samples should not teach broad CLR convention augmentation.

### 9. Documentation Synchronization

Update the new spec plus existing EF projection, CLR convention suppression, role-aware owned storage, EF guide, compatibility docs, and release notes.

### 10. 2.4.4 Release Preparation

Run full package and release checks for `2.4.4`. Do not publish packages.

## Implementation Constraints

- Closed semantic model application is the default.
- EF conventions are constrained; they are not model authority.
- Enrich `EfCoreSemanticModel` before adding more convention repair logic.
- `ApplyEfCoreSemanticModel(...)` must not remain a weaker path.
- `ApplySemanticTypeModel(...)` must not contain unique behavior that cannot be represented in `EfCoreSemanticModel`.
- Shared-type projection must be explicit.
- Value objects must not become root EF entities.
- Semantic-only members must be suppressed according to `EfCoreSemanticModel`.
- Do not require `[NotMapped]` as the supported default fix.
- Keep public API additions minimal and stable.
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

### Source and Tests

```text
src/SemanticTypeModel.EFCore/
src/SemanticTypeModel.DotNet/
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.Abstractions/
src/SemanticTypeModel.Core/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
tests/unit/SemanticTypeModel.DotNet.Tests.Unit/
tests/unit/SemanticTypeModel.Generators.Tests.Unit/
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
src/SemanticTypeModel.Abstractions/
src/SemanticTypeModel.Core/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
tests/unit/SemanticTypeModel.DotNet.Tests.Unit/
tests/unit/SemanticTypeModel.Generators.Tests.Unit/
tests/package-smoke/
samples/OrderFulfillment.Domain/
samples/code-first-ef-core/
docs/specs/ef-core-closed-modelbuilder-application.md
docs/specs/ef-core-clr-convention-suppression.md
docs/specs/ef-core-role-aware-owned-storage.md
docs/specs/type-model-ef-core-projection.md
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
./eng/test-filter.sh EfCoreSemanticModel
./eng/test-filter.sh ClosedModelBuilder
./eng/test-filter.sh Convention
./eng/test-filter.sh ExtensionData
./eng/test-filter.sh ValueObject
./eng/test-filter.sh ApplyEfCoreSemanticModel
./eng/test-filter.sh EFCore

./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
```

### Tier 2 — Repository Completion

```sh
./eng/check.sh
```

### Tier 3 — 2.4.4 Package and Release Validation

```sh
./eng/package.sh 2.4.4
./eng/package-smoke.sh 2.4.4
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.4.4
```

Inspect package inventory:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

## Acceptance Criteria

### Architecture

- `EfCoreSemanticModel` contains source lineage and suppression metadata required for closed ModelBuilder application.
- `ApplySemanticTypeModel(...)` derives `EfCoreSemanticModel` and applies it through the same engine as `ApplyEfCoreSemanticModel(...)`.
- `ApplyEfCoreSemanticModel(...)` supports closed application when source lineage is present.
- `ApplyEfCoreSemanticModel(...)` explicitly rejects closed CLR application when required lineage is missing.
- Shared-type projection remains explicit.
- CLR convention augmentation is removed, renamed, constrained, or made non-default.

### Closed EF Boundary

- EF conventions cannot map inherited `SemanticExtensionData`.
- EF conventions cannot promote semantic `ValueObject` types to root EF entities.
- EF conventions cannot introduce relationships absent from `EfCoreSemanticModel`.
- EF conventions cannot map non-semantic base classes as EF entities merely because they contribute semantic members.
- Unexpected convention-created metadata is suppressed, overridden, diagnosed, or rejected.

### Regression Preservation

- M0049 dictionary extraction tests remain passing.
- M0050 format-compatible scalar / `Uri` tests remain passing.
- M0050 role-aware owned storage tests remain passing.
- M0051 inherited extension-data tests remain passing.
- Real DbContext model build succeeds for inherited extension-data value-object scenario.

### Documentation

- Docs state that STM owns a closed EF semantic model.
- Docs do not present CLR convention augmentation as the primary model.
- Docs explain `ApplySemanticTypeModel` as a convenience wrapper.
- Docs explain `ApplyEfCoreSemanticModel` as the lower-level closed application API.
- Docs explain explicit shared-type projection.
- Release notes document the 2.4.3 issue and 2.4.4 correction.

### Release Readiness

- `./eng/check.sh` passes.
- `./eng/package.sh 2.4.4` produces expected packages.
- `./eng/package-smoke.sh 2.4.4` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.4.4` passes.
- No package is published during milestone implementation.
- Publication remains explicit human-approved follow-up.

## Direct Documentation Impact

Implementation must update:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/specs/ef-core-closed-modelbuilder-application.md
docs/specs/ef-core-clr-convention-suppression.md
docs/specs/ef-core-role-aware-owned-storage.md
docs/specs/type-model-ef-core-projection.md
public-docs/guides/ef-core-projection.md
public-docs/api/compatibility.md
public-docs/samples.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

The package adds:

```text
.guide-sync/pending/m0052-2-4-4-publication-follow-up.md
```

It tracks only later human-approved publication, tag, release, and post-publication verification.

Ordinary implementation agents do not need to read `.guide-sync/`.

## Human Review Requirements

Human review is required for application mode names, default application behavior, public API obsoletion/rename strategy, `EfCoreSemanticModel` lineage contract, diagnostics for missing lineage and unexpected EF convention metadata, compatibility wording, docs migration from augmentation language to closed-model language, sample clarity, 2.4.4 release wording, package contents, and publication approval.

## Out-of-Scope Guide Migration Work

M0052 is not a guide migration.

Do not update, copy, or reference external guide documents as target-repository operational authority.
