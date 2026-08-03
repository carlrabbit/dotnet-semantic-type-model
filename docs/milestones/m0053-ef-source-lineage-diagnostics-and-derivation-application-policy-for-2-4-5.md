# M0053: EF Source Lineage Diagnostics and Derivation Application Policy for 2.4.5

## Status

Complete; 2.4.5 publication requires human approval.

## Goal

Fix the 2.4.4 EF Core source-lineage defects and complete the application-policy model for `DeriveEfCoreModel(...)`.

M0052 established that `EfCoreSemanticModel` is the complete EF application contract. The released 2.4.4 implementation still has two gaps:

1. `EfCoreSourceLineage.Create(...)` can throw runtime LINQ exceptions while creating owned mappings, especially because it assumes every owned target resolves to exactly one `ObjectTypeDefinition`.
2. `ApplicationMode` / application policy is configurable on the `ModelBuilder` convenience path, but not on `DeriveEfCoreModel(...)`, so manually derived `EfCoreSemanticModel` instances cannot reliably carry their intended closed/shared application policy.

M0053 prepares `2.4.5` as a patch release that makes source-lineage construction diagnostic-first, hardens owned target resolution, and makes application policy part of EF model derivation.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Published 2.4.4 package set |
| Release target | `2.4.5` |
| Execution mode | `ai-executed-human-reviewed` |
| Capability-provider scope | EF Core derivation, EF source-lineage construction, application policy propagation, diagnostics, source generator diagnostics where statically possible, ModelBuilder application, tests, samples, docs, release readiness |
| Consumer/dogfood scope | Package-based EF sample validates owned target diagnostics, closed application policy propagation, and no raw lineage exceptions |

## Execution Mode

`ai-executed-human-reviewed`.

This is a released runtime failure in the EF source-lineage layer and an architectural completeness gap in EF derivation. The implementation must be narrow, regression-driven, and preserve M0049 through M0052 behavior.

## Problems to Fix

### Problem 1 — Unsafe Owned Mapping Resolution

Current source-lineage behavior assumes:

```text
owned property -> property.Type.Id -> exactly one ObjectTypeDefinition
```

That is not safe. Owned properties may reference:

```text
missing type id
non-object type
array / collection type
dictionary type
union type
scalar / enum type
ambiguous duplicate type definitions
unsupported ownership shape
```

A released package must not surface these as raw `Single(...)` / LINQ runtime exceptions.

### Problem 2 — Lineage Diagnostics Are Outside the Diagnostic Surface

Source-lineage creation happens after projection diagnostics are produced. If lineage construction throws, consumers see runtime exceptions instead of actionable STM diagnostics.

Required behavior:

```text
lineage errors become SchemaDiagnostic entries
lineage diagnostics are included in derivation result diagnostics
lineage diagnostics are included in ModelBuilder projection result diagnostics
closed CLR application refuses invalid lineage through documented diagnostics/errors
```

### Problem 3 — DeriveEfCoreModel Cannot Configure ApplicationMode

`ApplySemanticTypeModel(...)` can assign application policy while deriving and applying, but `DeriveEfCoreModel(...)` does not expose application mode/policy.

Required behavior:

```text
EfCoreDerivationOptions exposes ApplicationMode/ApplicationPolicy
DeriveEfCoreModel stores the selected policy in EfCoreSemanticModel.ApplicationPolicy
ApplySemanticTypeModel delegates through DeriveEfCoreModel so the paths converge
lineage diagnostic severity can depend on application policy
```

## Scope

### 1. Source-Lineage Result Object

Replace lineage construction that can throw with a diagnostic-aware result.

Acceptable design:

```csharp
internal sealed record EfCoreSourceLineageResult
{
    public required IReadOnlyList<EfCoreSourceTypeMapping> SourceTypes { get; init; }
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }
}
```

or equivalent.

`EfCoreSourceLineage.Create(...)` must either return this result or accept a diagnostic sink. Raw LINQ failures are not acceptable.

### 2. Guarded Owned Target Resolution

Add explicit owned target resolution.

Required resolution cases:

| Case | Required behavior |
|---|---|
| exactly one object target | create `EfCoreOwnedMapping` |
| no target found | diagnostic |
| multiple targets found | diagnostic |
| target exists but is not object | diagnostic |
| target is collection/array/dictionary/union | diagnostic or explicit owned-collection policy handling if already supported |
| target is scalar/enum | diagnostic |
| target shape is unsupported for current application policy | diagnostic |

Do not use raw `.Single(...)`, `.First(...)`, or equivalent assumptions for model-authoring errors.

### 3. Ownership Kind Classification

Lineage creation must distinguish:

```text
owned single object
owned collection
owned dictionary
owned unsupported shape
schema.ownership marker without precise shape
```

Do not treat all `schema.ownership` annotations as single object ownership.

### 4. Diagnostic Codes

Add stable diagnostics for at least:

```text
EFCORE_OWNED_TARGET_TYPE_NOT_FOUND
EFCORE_OWNED_TARGET_TYPE_AMBIGUOUS
EFCORE_OWNED_TARGET_SHAPE_UNSUPPORTED
EFCORE_OWNED_COLLECTION_LINEAGE_POLICY_REQUIRED
EFCORE_SOURCE_LINEAGE_CLR_TYPE_NOT_RESOLVED
EFCORE_SOURCE_LINEAGE_MEMBER_NOT_FOUND
EFCORE_SOURCE_LINEAGE_REQUIRED
```

Severity guidance:

```text
ClosedClrModel:
  missing/invalid required CLR lineage is error
  unsupported owned target lineage is error or blocking diagnostic

SharedTypeModel:
  CLR lineage may be warning or informational where not required
  invalid semantic ownership shape remains diagnostic
```

### 5. Derivation Application Policy

Add application policy to EF derivation options.

Expected API shape:

```csharp
public sealed class EfCoreDerivationOptions
{
    public EfCoreApplicationMode ApplicationMode { get; set; }
        = EfCoreApplicationMode.ClosedClrModel;

    public EfCoreProjectionOptions Projection { get; set; }
    public EfCoreEnvelopeProjectionOptions Envelopes { get; }
    public SchemaTransformationPipeline Transformations { get; }
}
```

or equivalent naming that matches final public API.

`DeriveEfCoreModel(...)` must set:

```csharp
EfCoreSemanticModel.ApplicationPolicy = options.ApplicationMode
```

### 6. Path Convergence

Refactor `ApplySemanticTypeModel(...)` so it uses `DeriveEfCoreModel(...)` rather than duplicating EF projection + lineage creation.

Target flow:

```text
ApplySemanticTypeModel(model, configure)
  -> model.DeriveEfCoreModel(configure)
  -> modelBuilder.ApplyEfCoreSemanticModel(derived.Model)
  -> return diagnostics from derivation + application
```

If separate option types are retained, mapping between them must be explicit and tested.

### 7. Error Surface

Consumers must be able to inspect diagnostics before or during application.

Required behavior:

```text
DeriveEfCoreModel(...) returns source-lineage diagnostics.
ApplySemanticTypeModel(...) returns source-lineage diagnostics.
ApplyEfCoreSemanticModel(...) reports missing/invalid lineage through stable diagnostic/exception surface.
No model authoring issue appears as raw InvalidOperationException from LINQ.
```

If `ApplyEfCoreSemanticModel(...)` must throw for blocking application failures, the message must include the STM diagnostic code and actionable remediation.

### 8. Compile-Time / Generator Diagnostics

Add compile-time diagnostics where the source generator can statically detect invalid shapes, especially:

```text
[SemanticOwned] on unsupported target shape
owned collection without explicit EF policy if policy is statically knowable
semantic value object used as root DbSet only if detectable in generator scope
missing CLR member metadata required for closed EF application, where detectable
```

Do not over-promise generator diagnostics for policy-dependent runtime choices.

### 9. Preserve Prior Behavior

Preserve:

```text
M0049 dictionary key/value extraction fix
M0050 Uri / format-compatible scalar behavior
M0050 role-aware EF owned storage
M0051 inherited SemanticExtensionData regression coverage
M0052 closed EF semantic model application
M0052 ApplyEfCoreSemanticModel source-lineage requirement
```

## Non-Goals

- No broad EF provider-specific redesign.
- No migrations, database creation, DbContext generation, query filters, temporal tables, or provider-specific JSON storage.
- No arbitrary EF convention mapping outside the closed semantic model.
- No general dictionary persistence for EF.
- No extension-data storage in EF.
- No support for semantic `ValueObject` as an independent root entity.
- No unrelated public API redesign except the required derivation application-policy addition and any compatibility mapping needed for path convergence.
- No package publication, tag creation, or GitHub release creation inside this milestone.
- No copied external guide documents, TBPs, issue templates, workflow documents, or non-root README files.

## Regression Scenarios

### 1. Owned Target Missing

A property has ownership annotation but its `property.Type.Id` cannot be resolved.

Expected:

```text
diagnostic EFCORE_OWNED_TARGET_TYPE_NOT_FOUND
no raw Single/First exception
```

### 2. Owned Target Non-Object

A property has ownership annotation but targets scalar, enum, dictionary, array, or union.

Expected:

```text
diagnostic EFCORE_OWNED_TARGET_SHAPE_UNSUPPORTED or owned-collection policy diagnostic
no raw exception
```

### 3. Owned Collection Ambiguity

A collection is marked as owned but lineage creation sees only the collection type id.

Expected:

```text
diagnostic EFCORE_OWNED_COLLECTION_LINEAGE_POLICY_REQUIRED or explicit collection lineage support
no single-object owned mapping assumption
```

### 4. ApplicationMode in Derivation

```csharp
var derived = model.DeriveEfCoreModel(options =>
{
    options.ApplicationMode = EfCoreApplicationMode.SharedTypeModel;
});
```

Expected:

```text
derived.Model.ApplicationPolicy == SharedTypeModel
lineage-required diagnostics are not incorrectly raised as ClosedClrModel errors
```

And:

```csharp
var derived = model.DeriveEfCoreModel(options =>
{
    options.ApplicationMode = EfCoreApplicationMode.ClosedClrModel;
});
```

Expected:

```text
derived.Model.ApplicationPolicy == ClosedClrModel
missing required source lineage is diagnostic/error
```

### 5. ApplySemanticTypeModel Path Convergence

`ApplySemanticTypeModel(...)` and manual `DeriveEfCoreModel(...)` + `ApplyEfCoreSemanticModel(...)` must produce equivalent policy and diagnostics for the same options.

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
docs/specs/ef-core-source-lineage-diagnostics.md
docs/specs/ef-core-closed-modelbuilder-application.md
docs/specs/ef-core-clr-convention-suppression.md
docs/specs/ef-core-role-aware-owned-storage.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-transformation-and-domain-derivation.md
public-docs/guides/ef-core-projection.md
```

### Source and Tests

```text
src/SemanticTypeModel.EFCore/EfCoreSourceLineage.cs
src/SemanticTypeModel.EFCore/EfCoreDerivation.cs
src/SemanticTypeModel.EFCore/EfCoreModelBuilderProjection.cs
src/SemanticTypeModel.EFCore/EfCoreSemanticModel.cs
src/SemanticTypeModel.EFCore/EfCoreModelProjection.cs
src/SemanticTypeModel.DotNet/
src/SemanticTypeModel.Generators/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
tests/unit/SemanticTypeModel.DotNet.Tests.Unit/
tests/unit/SemanticTypeModel.Generators.Tests.Unit/
tests/package-smoke/
samples/code-first-ef-core/
```

## Files or Areas Likely Affected

```text
src/SemanticTypeModel.EFCore/EfCoreSourceLineage.cs
src/SemanticTypeModel.EFCore/EfCoreDerivation.cs
src/SemanticTypeModel.EFCore/EfCoreModelBuilderProjection.cs
src/SemanticTypeModel.EFCore/EfCoreSemanticModel.cs
src/SemanticTypeModel.EFCore/EfCoreModelProjection.cs
src/SemanticTypeModel.EFCore/*Diagnostic*.cs
src/SemanticTypeModel.Generators/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
tests/unit/SemanticTypeModel.Generators.Tests.Unit/
samples/code-first-ef-core/
docs/specs/ef-core-source-lineage-diagnostics.md
docs/specs/ef-core-closed-modelbuilder-application.md
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
./eng/test-filter.sh SourceLineage
./eng/test-filter.sh OwnedTarget
./eng/test-filter.sh ApplicationMode
./eng/test-filter.sh DeriveEfCoreModel
./eng/test-filter.sh ApplySemanticTypeModel
./eng/test-filter.sh Diagnostics
./eng/test-filter.sh EFCore

./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
```

### Tier 2 — Repository Completion

```sh
./eng/check.sh
```

### Tier 3 — 2.4.5 Package and Release Validation

```sh
./eng/package.sh 2.4.5
./eng/package-smoke.sh 2.4.5
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.4.5
```

Inspect package inventory:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

## Acceptance Criteria

### Source Lineage

- `EfCoreSourceLineage.Create(...)` no longer throws raw LINQ exceptions for owned target model-authoring errors.
- Owned targets are resolved through guarded logic.
- Missing, ambiguous, non-object, collection, dictionary, array, union, scalar, and enum owned targets produce diagnostics.
- Lineage diagnostics are included in derivation results.
- Lineage diagnostics are included in `ApplySemanticTypeModel(...)` results.
- Blocking closed-application lineage failures surface stable STM diagnostic codes.

### Application Policy

- `EfCoreDerivationOptions` exposes application mode/policy.
- `DeriveEfCoreModel(...)` stores selected application policy in `EfCoreSemanticModel`.
- `ApplySemanticTypeModel(...)` uses `DeriveEfCoreModel(...)` or otherwise has tested equivalent behavior.
- Manual `DeriveEfCoreModel(...)` + `ApplyEfCoreSemanticModel(...)` behaves consistently with `ApplySemanticTypeModel(...)`.
- Shared-type mode does not incorrectly require closed CLR lineage.
- Closed CLR mode correctly treats missing/invalid required lineage as diagnostic/error.

### Error Surface

- Model authoring problems appear as STM diagnostics, not unhandled runtime exceptions.
- Generator diagnostics are added where invalid shapes are statically knowable.
- Runtime exceptions, where unavoidable, include STM diagnostic code and remediation.

### Regression Preservation

- M0049 through M0052 tests remain passing.
- Inherited extension data remains suppressed for EF.
- Semantic value objects remain non-root EF entities.
- Closed EF application remains the default.

### Documentation

- Docs explain `ApplicationMode` on `DeriveEfCoreModel(...)`.
- Docs explain lineage diagnostics and their severity by application mode.
- Docs document source-lineage diagnostic codes.
- 2.4.5 release notes document the `.Single(...)` lineage bug and application-policy derivation fix.

### Release Readiness

- `./eng/check.sh` passes.
- `./eng/package.sh 2.4.5` produces expected packages.
- `./eng/package-smoke.sh 2.4.5` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.4.5` passes.
- No package is published during milestone implementation.
- Publication remains explicit human-approved follow-up.

## Direct Documentation Impact

Implementation must update:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/specs/ef-core-source-lineage-diagnostics.md
docs/specs/ef-core-closed-modelbuilder-application.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-transformation-and-domain-derivation.md
public-docs/guides/ef-core-projection.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

The package adds:

```text
.guide-sync/pending/m0053-2-4-5-publication-follow-up.md
```

It tracks only later human-approved publication, tag, release, and post-publication verification.

Ordinary implementation agents do not need to read `.guide-sync/`.

## Human Review Requirements

Human review is required for:

- lineage diagnostic code names;
- diagnostic severities by application mode;
- public `EfCoreDerivationOptions.ApplicationMode` naming;
- path convergence compatibility strategy;
- whether `ApplySemanticTypeModel(...)` option type changes are source-compatible;
- generator diagnostic scope;
- 2.4.5 release wording;
- package contents;
- publication approval.

## Out-of-Scope Guide Migration Work

M0053 is not a guide migration.

Do not update, copy, or reference external guide documents as target-repository operational authority.
