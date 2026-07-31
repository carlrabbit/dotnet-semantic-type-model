# M0050: Format-Compatible Scalar Support and Role-Aware EF Owned Storage

## Status

Planned.

## Goal

Stabilize two defects discovered after the 2.4.1 patch release:

1. `SemanticFormat` rejects semantically valid CLR types such as `System.Uri`.
2. EF Core owned-object projection conflates ownership with flattening and does not respect the owned target type role or selected storage policy.

The milestone must add role-aware EF owned storage behavior, expand supported scalar/format compatibility for `System.Uri`, preserve diagnostics for genuinely invalid usage, update samples and docs, and prepare a non-publishing `2.4.2` patch release candidate.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Published 2.4.1 package set |
| Release target | `2.4.2` |
| Execution mode | `ai-executed-human-reviewed` |
| Capability-provider scope | .NET extraction, generator output, canonical scalar contracts, EF Core domain projection, EF Core ModelBuilder application, JSON Schema/Power BI/System.Text.Json compatibility, tests, samples, package validation, release notes |
| Consumer/dogfood scope | Package-based Order Fulfillment samples verify public package behavior for `Uri`, semantic formats, owned value-object JSON storage, and owned object diagnostics |

## Execution Mode

`ai-executed-human-reviewed`.

The desired behavior is clear, but the implementation affects public projection behavior, EF Core storage policy semantics, scalar contracts, diagnostics, package behavior, and patch-release guidance. AI implements and validates; human review is required for storage policy names, diagnostics, compatibility notes, and release publication.

## Scope

### Format-Compatible Scalar Stabilization

Add `System.Uri` as a supported scalar-compatible CLR type.

Required behavior:

```text
System.Uri
  -> canonical scalar with string-like URI semantics
  -> JSON Schema string output
  -> JSON Schema format "uri" when inferred or explicitly selected
  -> System.Text.Json remains compatible with normal Uri handling
  -> Power BI projects as text
  -> EF Core projects as string/provider-converted scalar or emits a precise diagnostic if converter support is not implemented
```

`SemanticFormat` must accept `Uri` and nullable `Uri?`.

Do not remove `STM5025`. It must still reject nonsensical format applications such as email/URI formats on integer, decimal, Boolean, collection, dictionary, object, and enum members unless the target type is explicitly supported.

### Role-Aware EF Owned Storage

EF Core projection must classify owned properties by both:

```text
property ownership annotation
target type semantic role
```

Ownership is lifecycle/containment semantics. The target type role determines what kind of thing is being owned. EF storage policy determines how it is represented.

Required classification:

| Property | Target role | Default / policy behavior |
|---|---|---|
| `[SemanticOwned]` single object | `ValueObject` | value-object storage policy applies |
| `[SemanticOwned]` single object | `Object` | owned-object storage policy applies or diagnostic if no policy exists |
| `[SemanticOwned]` single object | `Entity` | diagnostic unless an explicit aggregate-owned entity policy exists |
| `[SemanticOwned]` collection | `ValueObject` | explicit owned-collection policy required |
| `[SemanticOwned]` collection | `Object` | explicit owned-collection policy required |
| no ownership annotation | `ValueObject` | configured value-object projection mode applies |
| no ownership annotation | `Object` | unsupported object, relationship, or diagnostic according to existing target rules |

The implementation must remove the hard-coded behavior:

```text
OwnedObject -> FlattenValueObject
```

and replace it with role-aware policy dispatch.

### Owned ValueObject Storage

For `[SemanticOwned]` properties whose target type role is `ValueObject`, `ValueObjectProjectionMode` must apply.

Required cases:

```text
ValueObjectProjectionMode.Flatten
  -> flattened scalar/enum columns

ValueObjectProjectionMode.SerializeJson
  -> one string JSON column

ValueObjectProjectionMode.Owned
  -> true EF owned mapping if implemented;
     otherwise explicit diagnostic that true EF owned navigation is not yet supported

ValueObjectProjectionMode.Diagnose
  -> diagnostic and no projected property
```

If true EF owned navigation is not implemented in this milestone, the implementation must not fake it by appending annotations. It must emit a stable diagnostic.

### Owned Object Storage

A target role of `Object` is not the same as `ValueObject`.

For `[SemanticOwned]` properties whose target role is `Object`:

- do not silently flatten;
- do not silently serialize using value-object policy;
- require explicit owned-object storage policy if one already exists or is introduced narrowly;
- otherwise emit a stable diagnostic.

If a narrow policy is introduced, supported initial storage may be:

```text
SerializeJsonStringColumn
IgnoreWithWarning
Diagnose
```

Do not add complex owned-table or nested aggregate behavior unless already supported by the EF model/application layer.

### Owned Collections

Keep owned collection behavior conservative.

Required behavior:

```text
[SemanticOwned] collection
  -> explicit policy required
```

Do not implement owned collections in this milestone unless a minimal existing policy already exists and is broken. The objective is to prevent silent wrong mappings.

### EF ModelBuilder Consistency

The EF domain semantic model and the applied `ModelBuilder` metadata must agree.

If the EF domain model says:

```text
one JSON string property
```

then `ModelBuilder` must contain one corresponding string property.

If the EF domain model says:

```text
true owned navigation
```

then `ModelBuilder` must apply true EF ownership metadata.

If true ownership cannot be applied, the domain model must not claim it.

## Non-Goals

- No broad EF Core redesign.
- No provider-specific SQL Server/PostgreSQL/SQLite JSON column behavior.
- No migrations, database creation, DbContext generation, query filters, or temporal table support.
- No owned collection implementation unless narrowly required by an existing documented policy.
- No aggregate-owned entity implementation unless already supported.
- No arbitrary object flattening for target role `Object`.
- No removal of `STM5025`.
- No “any type with ToString” format compatibility.
- No custom scalar plug-in framework.
- No changes to audience-specific descriptions.
- No unrelated documentation cleanup.
- No package publication, tag creation, or GitHub release creation inside the milestone.

## Focus Areas

### 1. Reproduce `SemanticFormat` Failure on `Uri`

Add a failing test for:

```csharp
[SemanticFormat(SemanticScalarFormat.Uri)]
public Uri? Website { get; init; }
```

Required post-fix assertions:

- no `STM5025`;
- canonical scalar exists;
- nullability is preserved;
- format is retained or inferred according to the final policy;
- JSON Schema emits `type: string` and `format: uri`;
- invalid target types still emit `STM5025`.

Also test:

```csharp
public Uri Website { get; init; }
public Uri? OptionalWebsite { get; init; }
[SemanticFormat(SemanticScalarFormat.Uri)] public string WebsiteText { get; init; }
[SemanticFormat(SemanticScalarFormat.Uri)] public int InvalidWebsite { get; init; }
```

### 2. Add `Uri` Scalar Semantics

Update runtime extraction, source-generator output, canonical scalar mapping, JSON Schema mapping, Power BI mapping, System.Text.Json compatibility, EF Core scalar handling, and docs.

Decide explicitly whether:

```text
Uri without [SemanticFormat(Uri)]
```

automatically receives URI format.

Recommended default:

```text
System.Uri implies URI scalar format unless an explicit compatible format overrides it.
```

If explicit override is not allowed, emit a stable diagnostic.

### 3. Preserve Format Diagnostics

Keep `STM5025` and clarify it:

```text
[SemanticFormat] is supported only on string-like or format-compatible scalar members.
```

The compatibility table must include:

```text
string
Uri
Guid
DateOnly
TimeOnly
DateTime
DateTimeOffset
TimeSpan
JsonElement/JsonDocument/JsonNode only if a format is semantically meaningful
```

Do not accept arbitrary object types.

### 4. Define Role-Aware EF Owned Classification

Implement a helper equivalent to:

```text
ClassifyOwnedProperty(property, targetType):
  ownership kind: none / object / collection
  target role: value object / object / entity / unknown
  target shape: scalar / enum / object / collection / dictionary / union
```

Use this classification before choosing flatten/JSON/owned/diagnose behavior.

### 5. Fix Owned ValueObject + SerializeJson

Add a failing test:

```csharp
[SemanticType(SemanticTypeRole.Entity)]
public sealed class Order
{
    [SemanticKey]
    public required Guid Id { get; init; }

    [SemanticOwned]
    public required Address ShippingAddress { get; init; }
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class Address
{
    public required string Street { get; init; }
    public required string City { get; init; }
}
```

With:

```csharp
ValueObjectProjectionMode = ValueObjectEfProjectionMode.SerializeJson
```

Required result:

```text
ShippingAddress
  -> one projected EF property
  -> ClrType string
  -> Conversion "Json"
  -> correct required/nullability metadata
  -> ModelBuilder has one matching property
  -> no flattened ShippingAddress_Street / ShippingAddress_City columns
```

### 6. Preserve Owned ValueObject + Flatten

With `ValueObjectProjectionMode.Flatten`, required result:

```text
ShippingAddress_Street
ShippingAddress_City
```

No independent Address table.

No true EF owned navigation claim unless it is actually applied.

### 7. Clarify Owned ValueObject + Owned

With `ValueObjectProjectionMode.Owned`, choose one:

A. Implement true EF owned navigation end to end; or  
B. Emit an explicit diagnostic that true EF owned navigation is not implemented by the provider-neutral projection.

Do not claim `efCore.ownership = OwnsOne` without applying corresponding `ModelBuilder` behavior.

### 8. Diagnose Owned Object Role

Add a test:

```csharp
[SemanticOwned]
public OrderAuditTrail? AuditTrail { get; init; }

[SemanticType(SemanticTypeRole.Object)]
public sealed class OrderAuditTrail
{
    public DateTimeOffset CreatedAt { get; init; }
    public string? SourceSystem { get; init; }
}
```

Required default behavior:

```text
diagnostic unless explicit owned-object storage policy exists
no silent flattening
no silent value-object JSON serialization
```

If explicit owned-object JSON storage is added, test it separately and keep the default conservative.

### 9. Preserve Owned Collection Diagnostic

For owned collections:

```csharp
[SemanticOwned]
public IReadOnlyList<Address> Addresses { get; init; }
```

Required behavior:

```text
explicit policy required diagnostic
no silent flatten
no implicit table creation
no implicit JSON collection unless configured
```

### 10. Update Shared Samples

Update the Order Fulfillment sample domain and EF sample to demonstrate:

```text
Uri property with semantic format
owned value object serialized as JSON column
owned value object flattened under another mode or test
owned object role diagnostic
```

Keep public samples representative; keep exhaustive matrix in tests.

### 11. Documentation and Release Notes

Update:

```text
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-ef-core-projection.md
docs/specs/core-semantic-vocabulary.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/guides/core-semantics.md
public-docs/api/compatibility.md
public-docs/samples.md
public-docs/release-notes.md
```

2.4.2 release notes must state:

```text
2.4.1 defect:
  Uri was not handled as a format-compatible scalar.

2.4.1 behavior gap:
  EF owned object projection treated SemanticOwned as flattening
  instead of considering target role and storage policy.

2.4.2 correction:
  Uri is supported as scalar/format-compatible type.
  Owned value objects respect ValueObjectProjectionMode.
  Owned object role is not silently flattened.
  Owned collections remain explicit-policy-required.
```

Adjust wording to the final verified implementation.

### 12. 2.4.2 Patch Release Preparation

Run package, smoke, sample, public-doc, and release checks for `2.4.2`.

Do not publish packages.

## Implementation Constraints

- Fix behavior at the canonical/projection boundary, not by sample workarounds.
- Keep `SemanticFormat` diagnostic strict.
- Do not make arbitrary object types format-compatible.
- Do not conflate ownership and flattening.
- Do not conflate target role `Object` and `ValueObject`.
- Do not claim true EF ownership unless `ModelBuilder` applies it.
- Keep provider-specific EF behavior out of scope.
- Keep owned collections conservative.
- Preserve package-based samples.
- Use canonical `eng/` scripts.
- Treat unrelated failures as out of scope unless introduced by the change.
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

### Scalar Format Authority

```text
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-schema-model.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/type-model-powerbi-tom-projection.md
```

### EF Ownership Authority

```text
docs/specs/ef-core-role-aware-owned-storage.md
docs/specs/core-semantic-vocabulary.md
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-ef-core-projection.md
public-docs/guides/ef-core-projection.md
```

### Source and Tests

```text
src/SemanticTypeModel.DotNet/RoslynDotNetTypeExtractor.cs
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.Abstractions/
src/SemanticTypeModel.Core/
src/SemanticTypeModel.JsonSchema/
src/SemanticTypeModel.EFCore/
src/SemanticTypeModel.SystemTextJson/
src/SemanticTypeModel.PowerBI/
tests/unit/SemanticTypeModel.DotNet.Tests.Unit/
tests/unit/SemanticTypeModel.Generators.Tests.Unit/
tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
tests/unit/SemanticTypeModel.SystemTextJson.Tests.Unit/
tests/unit/SemanticTypeModel.PowerBI.Tests.Unit/
samples/OrderFulfillment.Domain/
samples/code-first-ef-core/
samples/code-first-json-schema/
samples/code-first-powerbi/
samples/system-text-json-resolver/
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
src/SemanticTypeModel.DotNet/
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.Abstractions/
src/SemanticTypeModel.Core/
src/SemanticTypeModel.JsonSchema/
src/SemanticTypeModel.EFCore/
src/SemanticTypeModel.SystemTextJson/
src/SemanticTypeModel.PowerBI/
tests/unit/SemanticTypeModel.*.Tests.Unit/
samples/OrderFulfillment.Domain/
samples/code-first-ef-core/
samples/code-first-json-schema/
samples/code-first-powerbi/
samples/system-text-json-resolver/
docs/specs/ef-core-role-aware-owned-storage.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-ef-core-projection.md
docs/specs/core-semantic-vocabulary.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/guides/core-semantics.md
public-docs/api/compatibility.md
public-docs/samples.md
public-docs/release-notes.md
docs/MILESTONES.md
.guide-sync/pending/
```

## Validation Tiers and Concrete Commands

### Tier 1 — Focused Loop

```sh
./eng/test-filter.sh Uri
./eng/test-filter.sh SemanticFormat
./eng/test-filter.sh Owned
./eng/test-filter.sh ValueObject
./eng/test-filter.sh SerializeJson
./eng/test-filter.sh EFCore

./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
```

Run System.Text.Json and Power BI test projects when mapping tests are added.

### Tier 2 — Repository Completion

```sh
./eng/check.sh
```

### Tier 3 — 2.4.2 Package and Release Validation

```sh
./eng/package.sh 2.4.2
./eng/package-smoke.sh 2.4.2
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.4.2
```

Inspect package inventory:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

## Acceptance Criteria

### SemanticFormat and Uri

- `Uri` and `Uri?` are extracted as supported scalar-compatible types.
- `[SemanticFormat(Uri)]` on `Uri`, `Uri?`, and `string` succeeds.
- JSON Schema emits string/URI format for supported URI properties.
- EF Core handles `Uri` as string/provider-converted scalar or emits a precise documented diagnostic.
- Power BI handles `Uri` as text.
- System.Text.Json compatibility is preserved.
- `STM5025` still rejects invalid target types.

### Role-Aware EF Ownership

- `[SemanticOwned]` no longer hard-codes flattening.
- Owned target role `ValueObject` respects `ValueObjectProjectionMode`.
- Owned value object + `SerializeJson` creates one JSON string column.
- Owned value object + `Flatten` creates flattened columns.
- Owned value object + `Owned` either applies true EF ownership end to end or emits a precise diagnostic.
- Owned target role `Object` is not silently flattened or value-object serialized.
- Owned target role `Entity` is diagnostic unless an explicit policy exists.
- Owned collections remain explicit-policy-required.
- EF domain model and applied `ModelBuilder` metadata agree.

### Samples and Documentation

- Order Fulfillment sample includes a representative `Uri`.
- EF sample verifies owned value object JSON-column behavior.
- EF sample or tests verify owned object role diagnostics.
- Docs describe role-aware ownership classification.
- Docs do not imply `[SemanticOwned]` means flatten.
- 2.4.2 release notes document the corrections.

### Release Readiness

- `./eng/check.sh` passes.
- `./eng/package.sh 2.4.2` produces expected packages.
- `./eng/package-smoke.sh 2.4.2` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.4.2` passes.
- No package is published during milestone implementation.
- Publication remains an explicit human-approved follow-up.

## Direct Documentation Impact

Implementation must update:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/specs/ef-core-role-aware-owned-storage.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-ef-core-projection.md
docs/specs/core-semantic-vocabulary.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/guides/core-semantics.md
public-docs/api/compatibility.md
public-docs/samples.md
affected public-docs/samples/*.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

The package adds:

```text
.guide-sync/pending/m0050-2-4-2-publication-follow-up.md
```

It tracks only later human-approved publication, tag, release, and post-publication verification.

Ordinary implementation agents do not need to read `.guide-sync/`.

## Human Review Requirements

Human review is required for:

- `Uri` scalar/format compatibility policy;
- whether `Uri` implies URI format by default;
- EF owned storage policy names;
- diagnostics for owned object and owned entity roles;
- whether true EF `OwnsOne` is implemented or explicitly deferred;
- sample clarity;
- 2.4.2 compatibility wording;
- final affected package inventory;
- package contents;
- release-gate evidence;
- publication approval.

## Out-of-Scope Guide Migration Work

M0050 is not a guide migration.

Do not update, copy, or reference external guide documents as target-repository operational authority.
