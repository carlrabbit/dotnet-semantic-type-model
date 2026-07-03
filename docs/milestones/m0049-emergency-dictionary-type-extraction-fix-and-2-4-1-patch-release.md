# M0049: Emergency Dictionary Type Extraction Fix and 2.4.1 Patch Release

## Status

Planned.

## Goal

Correct the released 2.4.0 defect where dictionary key types are referenced but not extracted into the canonical model, causing valid dictionary properties—especially `[SemanticExtensionData] Dictionary<string, JsonElement>?`—to fail canonical validation with `STM0002`.

The milestone must:

1. reproduce and fix the canonical dictionary-key extraction defect;
2. preserve extension-data semantics across projections;
3. make EF Core ignore extension-data properties before target type resolution;
4. add focused regression tests and package-based sample coverage;
5. synchronize patch-release documentation;
6. prepare and validate version `2.4.1`;
7. stop before publication unless explicit human approval is provided separately.

This is an emergency patch milestone. Scope must remain limited to the defect, its directly exposed projection behavior, regression coverage, and 2.4.1 release readiness.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Published 2.4.0 package set |
| Release target | `2.4.1` |
| Execution mode | `ai-executed-human-reviewed` |
| Severity | Released canonical-model validation defect |
| Capability-provider scope | .NET extraction, generator parity, canonical validation, EF Core projection, extension-data behavior, tests, samples, package validation, release notes |
| Consumer/dogfood scope | Package-based Order Fulfillment samples verify extension-data behavior through public package APIs |

## Execution Mode

`ai-executed-human-reviewed`.

The defect and intended behavior are well defined, but the change affects canonical extraction, generated model output, multiple projections, diagnostics, and a released package line. AI performs the implementation and validation; humans review the patch boundary, package inventory, compatibility wording, and publication decision.

## Scope

### Defect Reproduction

Reproduce the released failure with a model equivalent to:

```csharp
[SemanticExtensionData]
public Dictionary<string, JsonElement>? ExtensionData { get; init; }
```

Required observed pre-fix behavior:

```text
dictionary TypeRef exists
dictionary key TypeRef points to global::System.String
canonical string scalar type is missing
TypeSchemaModelValidator emits STM0002
EF Core never reaches its intended extension-data ignore policy
```

The implementation must prove the root cause with a focused failing test before applying the fix.

### Canonical Dictionary Extraction Fix

For every extracted dictionary type:

- normalize both key and value types;
- extract/register both key and value type definitions;
- assign both normalized type IDs to the dictionary descriptor/model;
- preserve deterministic type IDs;
- preserve existing supported-key diagnostics;
- ensure the final canonical model resolves both `KeyType` and `ValueType`;
- ensure runtime extraction and source-generator output remain equivalent.

Required invariant:

```text
DictionaryTypeDefinition.KeyType resolves in TypeSchemaModel.TypesById.
DictionaryTypeDefinition.ValueType resolves in TypeSchemaModel.TypesById.
```

This invariant applies to ordinary dictionaries and extension-data dictionaries.

### EF Core Defensive Ordering

The EF Core projection default for extension data remains:

```text
ignored by default
```

Move or add the extension-data early-return so it runs before target property-type lookup, nullability resolution, converter resolution, unsupported-shape handling, or other EF-specific processing.

Required order:

```text
identify schema.extensionData
  -> return no EF property
  -> perform no dictionary type projection
  -> emit no unsupported dictionary diagnostic
```

This defensive change does not replace the canonical extraction fix. The canonical model must still validate independently of EF Core.

### Cross-Projection Regression Coverage

Verify:

- Core validation no longer emits `STM0002`.
- EF Core omits extension-data properties by default.
- JSON Schema uses extension data to control openness/additional properties rather than exporting the bag as an ordinary property.
- System.Text.Json extension-data metadata remains available and supported.
- Power BI ignores extension data by default.
- canonical inspection identifies the extension-data property and dictionary shape.
- ordinary dictionary properties still project or diagnose according to existing target policy.

### Accepted Extension-Data Shapes

Add regression coverage for at least:

```csharp
Dictionary<string, JsonElement>
IDictionary<string, JsonElement>
Dictionary<string, object>
IReadOnlyDictionary<string, object>
```

For nullable reference declarations, ensure nullability affects the containing property cardinality and does not prevent dictionary key/value type extraction.

### Ordinary Dictionary Coverage

Add non-extension-data coverage for at least:

```csharp
Dictionary<string, string>
Dictionary<int, decimal>
Dictionary<Guid, JsonElement>
```

Verify:

- supported key types resolve;
- unsupported keys retain the existing stable extraction diagnostic;
- dictionary key/value resolution works even when a target later rejects or ignores the shape.

### Sample Coverage

Update the shared Order Fulfillment domain with a natural extension-data property on an appropriate envelope, event, or externally supplied contract.

The package-based samples must prove:

- the generated canonical model validates;
- JSON Schema represents extension-data openness correctly;
- System.Text.Json recognizes the extension-data contract where applicable;
- EF Core does not project the extension-data bag;
- Power BI does not project arbitrary extension-data keys.

Keep exhaustive shape coverage in tests. Samples should assert one representative scenario.

### Documentation and 2.4.1 Release Preparation

Update:

- current release notes;
- compatibility documentation;
- affected .NET extraction guidance;
- extension-data documentation;
- EF Core projection guidance;
- JSON Schema and System.Text.Json guidance where behavior is described;
- sample documentation;
- package version guidance where the current patch version is shown.

The 2.4.1 release notes must state:

```text
2.4.0 defect:
  dictionary key type definitions could be omitted during .NET extraction,
  producing STM0002 for valid dictionary models.

most visible affected scenario:
  [SemanticExtensionData] Dictionary<string, JsonElement>?

2.4.1 correction:
  dictionary key and value types are both extracted;
  canonical validation succeeds;
  EF Core ignores extension data before target type processing;
  cross-projection regression coverage is added.
```

Do not imply that `[SemanticIgnore]` is the recommended workaround.

## Non-Goals

- No redesign of dictionary semantics.
- No new dictionary key-type support beyond existing documented support.
- No new EF Core extension-data storage mode.
- No new JSON Schema openness policy.
- No arbitrary extension-data flattening.
- No Power BI extension-data expansion.
- No changes to audience-specific descriptions.
- No unrelated diagnostics renumbering.
- No broad documentation cleanup.
- No package publication, tag creation, or GitHub release creation within the implementation milestone.
- No copied external guides, TBPs, issue templates, workflow documents, or non-root README files.

## Focus Areas

### 1. Reproduce the Canonical Validation Failure

Add a focused extraction/generator test using:

```csharp
[SemanticType(SemanticTypeRole.Entity)]
public sealed class ExternalRecord
{
    [SemanticKey]
    public required Guid Id { get; init; }

    [SemanticExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
```

Assert before the fix that validation produces an unresolved dictionary key reference.

Do not encode the broken behavior as a permanent expected snapshot after the fix.

### 2. Correct Dictionary Descriptor Construction

Inspect both runtime extraction and source-generator conversion paths.

The corrected extraction flow must be equivalent to:

```text
discover implemented dictionary interface
normalize key type
normalize value type
extract key type
extract value type
create dictionary descriptor with both type IDs
```

Special attention:

- `string` must become the canonical string scalar definition;
- `JsonElement` must become the canonical JSON scalar definition;
- nullable annotations on the property must not mutate dictionary key/value identities;
- interface and concrete dictionary declarations must normalize consistently;
- the generated provider must not rely on runtime repair.

### 3. Preserve Canonical Validation Strictness

Do not suppress `STM0002` for dictionary references.

`STM0002` must continue to identify genuinely unresolved references.

The fix is to produce a valid model, not weaken validation.

Add tests proving that manually malformed dictionary models still receive `STM0002`.

### 4. Apply EF Core Ignore Before Type Resolution

In EF Core property projection:

1. detect `CoreSemanticAnnotationKeys.ExtensionData`;
2. return no projected property;
3. skip property type lookup;
4. skip nullability/converter/provider-type processing;
5. skip unsupported dictionary diagnostics.

Add both domain-projection and applied `ModelBuilder` tests.

Assert that all non-extension-data entity properties still project normally.

### 5. Verify JSON Schema Behavior

For extension-data properties:

- do not export the bag as a normal named property by default;
- apply the existing `additionalProperties` / `unevaluatedProperties` policy;
- preserve key-type restrictions;
- preserve value-schema behavior;
- add regression assertions using the shared model.

No new JSON Schema policy is introduced.

### 6. Verify System.Text.Json Behavior

Ensure both:

```text
[SemanticExtensionData]
[JsonExtensionData]
```

continue to normalize or project according to existing policy.

Verify supported shapes and prevent duplicate/conflicting extension-data members according to existing diagnostics.

### 7. Verify Power BI Behavior

Confirm extension data is ignored by default and does not introduce:

- arbitrary columns;
- unsupported dictionary diagnostics;
- relationships;
- flattening of dynamic keys.

### 8. Extend Shared Sample Canary

Add one representative extension-data property to the Order Fulfillment domain.

Recommended location:

```text
an external order message
an order event envelope
an imported customer update contract
```

The sample must naturally represent forward-compatible unknown members.

Required sample assertions:

```text
canonical validation has no STM0002
JSON Schema openness behavior is present
EF Core property is absent
Power BI property is absent
System.Text.Json extension-data contract remains functional where demonstrated
```

### 9. Synchronize Patch Documentation

Update active docs without rewriting unrelated 2.4.0 material.

Required documentation areas:

```text
public-docs/release-notes.md
public-docs/api/compatibility.md
public-docs/guides/core-semantics.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/guides/system-text-json.md
public-docs/samples.md
affected public-docs/samples/*.md
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-ef-core-projection.md
```

Update README/current-version references only where the repository convention changes them for patch releases.

### 10. Prepare and Validate 2.4.1

Run all required package, sample, documentation, and release gates using version `2.4.1`.

Inspect the final package inventory and confirm the affected packages at minimum include the packages that own or embed:

```text
.NET runtime extraction
source generator output
core validation contracts
EF Core projection
JSON Schema/System.Text.Json behavior when changed
sample/package smoke dependencies
```

Do not publish.

## Implementation Constraints

- Fix model construction; do not weaken canonical validation.
- Preserve projection-neutral extension-data semantics.
- Preserve target-specific ignore/mapping policies.
- Keep EF Core defensive handling in addition to canonical correctness.
- Keep runtime extraction and generator output equivalent.
- Use existing diagnostic IDs unless a genuinely new diagnostic is required.
- Do not introduce a compatibility workaround based on `[SemanticIgnore]`.
- Keep samples package-based.
- Do not add source-project references from public samples.
- Use canonical `eng/` scripts.
- Treat any unrelated failing test as out of scope unless caused by the patch.
- Do not publish packages.

## Required Authority Documents

### Always Read

```text
AGENTS.md
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/packaging.md
docs/engineering/release-readiness.md
docs/engineering/public-documentation.md
docs/engineering/samples.md
public-docs/versioning.md
public-docs/release-notes.md
public-docs/api/compatibility.md
```

### Dictionary and Extension-Data Authority

```text
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/type-model-powerbi-tom-projection.md
public-docs/guides/core-semantics.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/guides/system-text-json.md
```

### Source and Tests

```text
src/SemanticTypeModel.DotNet/RoslynDotNetTypeExtractor.cs
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.Core/Validation/TypeSchemaModelValidator.cs
src/SemanticTypeModel.EFCore/EfCoreModelProjection.cs
src/SemanticTypeModel.EFCore/EfCoreModelBuilderProjection.cs
src/SemanticTypeModel.JsonSchema/
src/SemanticTypeModel.SystemTextJson/
src/SemanticTypeModel.PowerBI/
tests/unit/SemanticTypeModel.DotNet.Tests.Unit/
tests/unit/SemanticTypeModel.Generators.Tests.Unit/
tests/unit/SemanticTypeModel.Core.Tests.Unit/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/
tests/unit/SemanticTypeModel.SystemTextJson.Tests.Unit/
tests/unit/SemanticTypeModel.PowerBI.Tests.Unit/
tests/package-smoke/
samples/OrderFulfillment.Domain/
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
src/SemanticTypeModel.DotNet/RoslynDotNetTypeExtractor.cs
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.Core/
src/SemanticTypeModel.EFCore/
src/SemanticTypeModel.JsonSchema/
src/SemanticTypeModel.SystemTextJson/
src/SemanticTypeModel.PowerBI/
tests/unit/SemanticTypeModel.*.Tests.Unit/
tests/package-smoke/
samples/OrderFulfillment.Domain/
samples/code-first-json-schema/
samples/code-first-ef-core/
samples/code-first-powerbi/
samples/system-text-json-resolver/
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-ef-core-projection.md
public-docs/guides/core-semantics.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/guides/system-text-json.md
public-docs/api/compatibility.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/release-notes.md
docs/MILESTONES.md
.guide-sync/pending/
```

## Validation Tiers and Concrete Commands

### Tier 1 — Focused Defect Loop

Confirm actual project paths before running:

```sh
./eng/test-filter.sh ExtensionData
./eng/test-filter.sh Dictionary
./eng/test-filter.sh STM0002
./eng/test-filter.sh EFCore

./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Core.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
```

Run JSON Schema, System.Text.Json, and Power BI projects when their regression tests are added or changed.

### Tier 2 — Repository Completion

```sh
./eng/check.sh
```

### Tier 3 — 2.4.1 Package and Release Validation

```sh
./eng/package.sh 2.4.1
./eng/package-smoke.sh 2.4.1
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.4.1
```

Inspect package inventory:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

Inspect selected `.nupkg` files when package contents or generator/analyzer assets are not otherwise proven.

## Acceptance Criteria

### Canonical Extraction

- The released failure is reproduced by a regression test.
- Dictionary key and value types are both extracted and registered.
- `Dictionary<string, JsonElement>` validates without `STM0002`.
- Interface and concrete accepted extension-data shapes validate.
- Ordinary supported dictionary key types validate.
- Unsupported key types retain the existing specific diagnostic.
- Manually malformed unresolved dictionary references still produce `STM0002`.
- Runtime extraction and generated providers are equivalent.

### EF Core

- Extension-data properties are ignored before target type resolution.
- No EF property is created for the extension-data bag.
- No unsupported dictionary diagnostic is emitted for ignored extension data.
- Applied EF `ModelBuilder` metadata contains no extension-data property.
- Other entity properties continue to project correctly.

### Other Projections

- JSON Schema applies existing openness/additional-properties behavior.
- System.Text.Json retains extension-data semantics.
- Power BI ignores extension data by default.
- Inspection reports the extension-data semantic and dictionary shape.
- No projection silently removes the property from the canonical model.

### Samples and Documentation

- The Order Fulfillment sample contains a natural extension-data scenario.
- Package-based samples detect the original regression.
- 2.4.1 release notes identify the 2.4.0 defect and correction.
- Compatibility documentation does not recommend `[SemanticIgnore]`.
- Active docs remain consistent with existing extension-data policy.

### Release Readiness

- `./eng/check.sh` passes.
- `./eng/package.sh 2.4.1` produces the expected package set.
- `./eng/package-smoke.sh 2.4.1` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.4.1` passes.
- Package inventory and affected package contents are reviewed.
- No package is published during milestone implementation.
- Publication remains an explicit human-approved follow-up.

## Direct Documentation Impact

Implementation must update:

```text
docs/MILESTONES.md
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-ef-core-projection.md
public-docs/guides/core-semantics.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/guides/system-text-json.md
public-docs/api/compatibility.md
public-docs/samples.md
affected public-docs/samples/*.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

The package adds:

```text
.guide-sync/pending/m0049-2-4-1-publication-follow-up.md
```

It tracks only the later human-approved publication, tag, release, and post-publication verification steps.

Ordinary implementation agents do not need to read `.guide-sync/`.

## Human Review Requirements

Human review is required for:

- proof of the root cause;
- dictionary extraction implementation;
- preservation of `STM0002` strictness;
- EF Core early-ignore ordering;
- cross-projection behavior;
- accepted and unsupported dictionary shapes;
- 2.4.1 compatibility and release-note wording;
- final affected package inventory;
- package contents;
- release-gate evidence;
- publication approval.

## Out-of-Scope Guide Migration Work

M0049 is not a guide migration.

Do not update, copy, or reference external guide documents as target-repository operational authority.
