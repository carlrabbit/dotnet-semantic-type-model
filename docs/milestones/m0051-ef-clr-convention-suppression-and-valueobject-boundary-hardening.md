# M0051: EF CLR Convention Suppression and ValueObject Boundary Hardening

## Status

Implemented.

## Goal

Fix the 2.4.2 EF Core interop gap where EF Core conventions can still discover semantic-only members and value-object CLR types when consumers use CLR-backed `DbSet<T>` entities together with SemanticTypeModel EF projection.

Target version: `2.4.3`.

The milestone must:

1. distinguish STM-owned shared-type EF projection from CLR-backed EF convention augmentation;
2. ensure semantic `ValueObject` types do not become root EF entities through STM projection or accidental reachability;
3. suppress semantic-only members such as inherited `[SemanticExtensionData]` from EF CLR convention mapping;
4. support non-semantic abstract base classes that contribute inherited semantic members;
5. add real `DbContext` regression tests that reproduce EF Core relationship/navigation discovery failures;
6. document supported and unsupported EF integration modes;
7. prepare a non-publishing `2.4.3` patch release.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Published 2.4.2 package set |
| Release target | `2.4.3` |
| Execution mode | `ai-executed-human-reviewed` |
| Capability-provider scope | EF Core projection, EF Core ModelBuilder application, CLR-backed EF convention suppression, inherited semantic member extraction, diagnostics, samples, public docs, package validation |
| Consumer/dogfood scope | Package-based EF sample validates a real CLR-backed `DbContext` with `DbSet<TEntity>`, reachable value objects, and inherited extension data |

## Execution Mode

`ai-executed-human-reviewed`.

The bug affects EF runtime behavior in consuming applications, not only the STM semantic projection model. Implementation must be constrained and regression-driven. Human review is required for public API shape, EF-mode terminology, diagnostics, documentation wording, and release approval.

## Scope

### Problem Constellation

The regression scenario is:

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

A consumer then uses a normal CLR-backed EF context:

```csharp
public sealed class AppDbContext : DbContext
{
    public DbSet<A> Items => Set<A>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplySemanticTypeModel(AppSemanticTypeModel.Create());
    }
}
```

Observed failure:

```text
EF Core convention discovery sees B.ExtensionData inherited from ExtensibleObject.
EF Core does not understand SemanticExtensionData.
EF Core attempts to map the dictionary as a property/navigation/relationship.
Model building fails with an "unable to determine relationship" style exception.
```

Expected behavior:

```text
SemanticExtensionData is preserved in the canonical model.
SemanticExtensionData is ignored by EF projection.
SemanticExtensionData is also suppressed from CLR-backed EF convention mapping.
B is a semantic ValueObject and does not become a root EF entity through STM.
ExtensibleObject has no SemanticType and does not become an EF entity.
The DbContext model builds.
```

### EF Integration Modes

Define and document two distinct modes.

#### Mode 1 — STM-Owned Shared-Type Projection

STM owns EF shape.

```text
TypeSchemaModel
  -> EfModelDefinition
  -> shared-type EF entities / projected metadata
```

Rules:

- consumer should not expose the same CLR types as `DbSet<T>` unless CLR-backed augmentation is explicitly used;
- STM projection determines which objects become EF entities;
- semantic `ValueObject` types are not root EF entities;
- semantic extension-data properties are ignored by EF projection.

#### Mode 2 — CLR-Backed EF Convention Augmentation

EF discovers CLR types first. STM augments the EF model.

```text
DbSet<T> / modelBuilder.Entity<T>()
  -> EF convention model
  -> STM semantic suppression and augmentation
```

Rules:

- STM must suppress semantic-only members from EF conventions;
- STM must detect semantic `ValueObject` types used as root `DbSet<T>` entities and diagnose or ignore according to policy;
- inherited semantic members must be considered;
- value-object members reachable from entities are configured as owned/flattened/serialized according to EF storage policy.

If complete CLR-backed augmentation is not fully implemented in this milestone, the public docs must say exactly what is supported and what remains consumer responsibility.

## Non-Goals

- No broad EF provider-specific redesign.
- No migrations, database creation, DbContext generation, query filters, or temporal table support.
- No requirement to make CLR-backed augmentation feature-complete for every EF projection feature.
- No automatic support for `DbSet<ValueObject>` as a normal entity.
- No arbitrary dictionary mapping for EF.
- No new extension-data storage mode.
- No changes to JSON Schema extension-data semantics except regression coverage.
- No package publication, tag creation, or GitHub release creation inside this milestone.
- No copied external guide documents, TBPs, issue templates, workflow documents, or non-root README files.

## Focus Areas

### 1. Reproduce EF Convention Failure

Add a failing regression using a real EF `DbContext`, not only `EfModelDefinition`.

Required shape:

```csharp
[SemanticType(SemanticTypeRole.Entity)]
public sealed class Order
{
    [SemanticKey]
    public required Guid Id { get; init; }

    [SemanticOwned]
    public required Money Amount { get; init; }
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class Money : ExtensibleObject
{
    public required decimal Value { get; init; }
    public required string Currency { get; init; }
}

public abstract class ExtensibleObject
{
    [SemanticExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
```

The pre-fix model should fail because EF conventions discover `Money.ExtensionData`. The post-fix model must build.

### 2. Define Public EF Mode APIs or Options

Decide the minimal public surface for 2.4.3.

Acceptable designs:

```csharp
public enum EfCoreApplicationMode
{
    SharedTypeProjection,
    ClrConventionAugmentation
}
```

or separate APIs:

```csharp
modelBuilder.ApplySemanticTypeModelAsSharedTypes(model);
modelBuilder.ApplySemanticTypeModelToClrTypes(model);
```

or a current-API option:

```csharp
modelBuilder.ApplySemanticTypeModel(model, options =>
{
    options.ClrConventions.SuppressSemanticOnlyMembers = true;
});
```

The milestone may choose a conservative minimal API, but behavior must be explicit and documented.

### 3. Suppress Semantic-Only Members in CLR-Backed Mode

Implement suppression for at least:

```text
SemanticExtensionData
```

Suppression must work for root entity types, owned/value-object types reachable from root entities, directly declared properties, inherited properties, abstract base-class properties, nullable dictionary properties, generated providers, and runtime extraction when CLR metadata is available.

If required CLR metadata is missing, emit a clear diagnostic and document the consumer workaround.

### 4. Harden ValueObject Root Boundary

Add diagnostics or suppression for:

```csharp
public DbSet<Money> MoneyValues => Set<Money>();
```

where `Money` has:

```csharp
[SemanticType(SemanticTypeRole.ValueObject)]
```

Expected behavior:

```text
semantic ValueObject is not configured as root EF entity by STM;
CLR-backed mode reports unsupported DbSet<ValueObject> or ignores it if safe;
docs state DbSet<T> is for semantic Entity/AggregateRoot roles.
```

### 5. Support Non-Semantic Abstract Base Members

Verify extraction and generated model output for inherited semantic members from a base type without `[SemanticType]`.

Required assertions:

```text
derived ValueObject contains inherited ExtensionData property;
ExtensionData carries semantic extension-data annotation;
base type is not projected as root semantic type unless existing implementation requires internal representation;
no EF entity is generated for the abstract base class.
```

### 6. Preserve STM EF Projection Behavior

Existing STM projection expectations must remain:

```text
ValueObject is not root entity;
ExtensionData produces no EfPropertyDefinition;
Owned value object respects M0050 storage policy;
Owned collections remain explicit-policy-required.
```

### 7. Add Real DbContext Tests

Add tests that build `context.Model` for:

1. shared-type projection mode only;
2. CLR-backed `DbSet<Order>` augmentation mode;
3. inherited `ExtensionData`;
4. direct `ExtensionData`;
5. accidental `DbSet<ValueObject>`;
6. owned value object serialized as JSON string column;
7. owned value object flattened;
8. object-role owned member diagnostic remains conservative.

The key regression assertion:

```text
ExtensionData is not an EF property.
ExtensionData is not an EF navigation.
ExtensionData is not an EF relationship.
context.Model builds successfully.
```

### 8. Document Consumer Workarounds and Boundaries

Docs must state clearly:

```text
If you use EF CLR conventions directly without STM CLR-backed suppression,
EF Core does not understand SemanticExtensionData.
Use [NotMapped] or modelBuilder.Ignore until/if CLR-backed suppression is enabled.
```

For the supported 2.4.3 path, docs must show the preferred configuration.

### 9. Release Notes and Compatibility

2.4.3 release notes must state:

```text
2.4.2 issue:
  EF Core conventions could still discover inherited SemanticExtensionData
  on reachable semantic ValueObject CLR types when consumers used DbSet<TEntity>.

2.4.3 correction:
  CLR-backed EF convention suppression handles SemanticExtensionData.
  ValueObject root boundaries are diagnosed/hardened.
  EF integration modes are documented.
```

## Implementation Constraints

- Do not weaken canonical model semantics.
- Do not remove `SemanticExtensionData`.
- Do not require `[NotMapped]` as the only fix for supported CLR-backed mode.
- Do not make semantic `ValueObject` a valid root EF entity by default.
- Do not silently ignore consumer `DbSet<ValueObject>` without diagnostic if detectable.
- Preserve M0050 role-aware EF owned storage behavior.
- Preserve M0049 dictionary extraction correctness.
- Keep provider-specific EF behavior out of scope.
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

### EF Integration Authority

```text
docs/specs/ef-core-clr-convention-suppression.md
docs/specs/ef-core-role-aware-owned-storage.md
docs/specs/type-model-ef-core-projection.md
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-compile-time-generator.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
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
docs/specs/ef-core-clr-convention-suppression.md
docs/specs/ef-core-role-aware-owned-storage.md
docs/specs/type-model-ef-core-projection.md
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-dotnet-extraction.md
public-docs/guides/ef-core-projection.md
public-docs/guides/core-semantics.md
public-docs/api/compatibility.md
public-docs/samples.md
public-docs/release-notes.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
.guide-sync/pending/
```

## Validation Tiers and Concrete Commands

### Tier 1 — Focused Loop

```sh
./eng/test-filter.sh ExtensionData
./eng/test-filter.sh ClrConvention
./eng/test-filter.sh ValueObject
./eng/test-filter.sh DbSet
./eng/test-filter.sh EFCore

./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
```

### Tier 2 — Repository Completion

```sh
./eng/check.sh
```

### Tier 3 — 2.4.3 Package and Release Validation

```sh
./eng/package.sh 2.4.3
./eng/package-smoke.sh 2.4.3
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.4.3
```

Inspect package inventory:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

## Acceptance Criteria

### EF Convention Suppression

- A real CLR-backed `DbContext` with `DbSet<Order>` and inherited `SemanticExtensionData` builds successfully.
- `ExtensionData` is not an EF property.
- `ExtensionData` is not an EF navigation.
- `ExtensionData` is not an EF relationship.
- Suppression works for directly declared and inherited extension-data properties.
- Suppression works when the base class has no `[SemanticType]`.
- Suppression works for value objects reachable from semantic entities.
- If CLR metadata is missing, a clear diagnostic or documented workaround exists.

### ValueObject Boundary

- Semantic `ValueObject` types are not root EF entities by STM projection.
- Reachability from an entity property does not make a `ValueObject` a root entity.
- `DbSet<ValueObject>` is diagnosed or explicitly documented as unsupported.
- Non-semantic abstract base classes do not become EF entities.
- M0050 owned value-object storage policy behavior remains intact.

### Documentation

- EF docs distinguish STM-owned shared-type projection from CLR-backed EF convention augmentation.
- Docs explain that EF conventions do not understand STM annotations unless CLR-backed suppression is enabled.
- Docs describe `[NotMapped]` / manual `Ignore` as workaround only for unsupported or disabled suppression paths.
- Release notes document the 2.4.2 issue and 2.4.3 correction.
- Compatibility docs specify valid use of `DbSet<T>` for semantic roles.

### Release Readiness

- `./eng/check.sh` passes.
- `./eng/package.sh 2.4.3` produces expected packages.
- `./eng/package-smoke.sh 2.4.3` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.4.3` passes.
- No package is published during milestone implementation.
- Publication remains explicit human-approved follow-up.

## Direct Documentation Impact

Implementation must update:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/specs/ef-core-clr-convention-suppression.md
docs/specs/ef-core-role-aware-owned-storage.md
docs/specs/type-model-ef-core-projection.md
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-dotnet-extraction.md
public-docs/guides/ef-core-projection.md
public-docs/guides/core-semantics.md
public-docs/api/compatibility.md
public-docs/samples.md
affected public-docs/samples/*.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

The package adds:

```text
.guide-sync/pending/m0051-2-4-3-publication-follow-up.md
```

It tracks only later human-approved publication, tag, release, and post-publication verification.

Ordinary implementation agents do not need to read `.guide-sync/`.

## Human Review Requirements

Human review is required for:

- EF integration mode names;
- public API shape;
- whether CLR-backed suppression is default-on or opt-in;
- diagnostics for `DbSet<ValueObject>`;
- behavior when CLR source metadata is incomplete;
- documentation boundary wording;
- sample clarity;
- 2.4.3 release wording;
- package contents;
- publication approval.

## Out-of-Scope Guide Migration Work

M0051 is not a guide migration.

Do not update, copy, or reference external guide documents as target-repository operational authority.
