# M0024: System.Text.Json Contract Correction for 1.1.0

## Status

Planned.

## Maturity Mode

Public package correction.

The repository has public packages, public documentation, package README sources, package smoke tests, public API baselines, and release validation. This milestone changes consumer-visible behavior in `SemanticTypeModel.SystemTextJson`, so it must update directly affected behavioral authority and implementation tests during the milestone. Broad documentation synchronization is deferred to the documentation synchronization task mode unless explicitly listed as direct documentation impact.

## Task Mode

Milestone authoring / implementation routing.

This milestone is not a broad documentation synchronization task and does not require TBPs or issue templates. Implementation agents must use the focus areas below, read only the listed authority for the selected focus area, and validate at the listed tier.

## Goal

Correct the `SemanticTypeModel.SystemTextJson` 1.0 contract before it becomes a durable burden:

1. remove all `JsonSerializerContext` generation support from `SemanticTypeModel.Generators` and `SemanticTypeModel.SystemTextJson`;
2. make `SemanticTypeModel.SystemTextJson` resolver-centered instead of generator-chaining-centered;
3. support using semantic property names as System.Text.Json serialization names through resolver customization;
4. keep `System.Text.Json` attribute import and annotation preservation;
5. align tests, samples, package smoke tests, specs, and public-facing guidance with the corrected contract.

The corrected 1.1.0 contract is:

```text
SemanticTypeModel.SystemTextJson imports, preserves, validates, and applies System.Text.Json contract metadata where supported.

It does not generate JsonSerializerContext types.

When source generation is desired, the consumer owns the JsonSerializerContext declaration.

SemanticTypeModel may wrap or modify an existing IJsonTypeInfoResolver to apply supported semantic metadata.
```

## Problem Statement

The 1.0 implementation attempted to emit a `JsonSerializerContext` declaration from `SemanticTypeModel.Generators` and expected the `System.Text.Json` source generator to process that emitted declaration. That design is not reliable because source generators are not an ordered pipeline in which one generator can produce input declarations for another generator to consume in the same compilation.

The current repository also supports this direction:

```text
JsonPropertyNameAttribute -> systemTextJson.propertyName -> optional semantic property name
```

but does not properly support the reverse direction:

```text
semantic property name -> System.Text.Json serialization property name
```

The reverse direction is feasible through `IJsonTypeInfoResolver` / `JsonTypeInfo` customization, not through generated `JsonSerializerContext` declarations.

## Required Authority

Read these documents before implementing any focus area:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/SPECS.md
docs/specs/system-text-json-contract-integration.md
docs/decisions/remove-system-text-json-context-generation.md
```

Read these only for focus areas that touch them:

```text
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/PUBLIC-DOCS.md
public-docs/guides/system-text-json.md
public-docs/nuget/SemanticTypeModel.SystemTextJson.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Remove generated `JsonSerializerContext` behavior.
- Remove or reject `SemanticTypeModelGenerateSystemTextJsonContext`.
- Remove or reject `SemanticTypeModelSystemTextJsonContextName`.
- Remove or revise `GenerateJsonSerializerContext`.
- Remove or revise `GeneratedContextName`.
- Remove generator code that emits `JsonSerializerContext` classes.
- Update `System.Text.Json` extraction options and projection options to no longer advertise generated context support.
- Add resolver-centered APIs that can wrap an existing `IJsonTypeInfoResolver`.
- Preserve existing resolver behavior instead of replacing `JsonSerializerOptions.TypeInfoResolver`.
- Add an option to use semantic property names as JSON serialization names.
- Define deterministic name-source precedence.
- Ensure resolver customization works with user-authored source-generated contexts.
- Keep `System.Text.Json` attribute import and annotations.
- Add tests proving the corrected behavior.
- Add package smoke coverage for consumer-style usage.
- Correct directly affected specs and public behavior statements.

### Out of Scope

- Generating `JsonSerializerContext` declarations by Roslyn source generator.
- Generating `JsonSerializerContext` declarations by MSBuild pre-generation.
- Creating a custom JSON serializer.
- Emulating arbitrary `JsonConverter` behavior.
- Changing JSON Schema projection behavior.
- Changing the canonical semantic model into a serializer contract model.
- Broad documentation cleanup unrelated to `SemanticTypeModel.SystemTextJson`.
- Release publication.
- TBP creation.
- Issue-template creation.

## Compatibility Position

This milestone intentionally removes a broken 1.0 feature surface.

The implementation must record the compatibility behavior explicitly:

```text
Generated JsonSerializerContext support is removed in 1.1.0 because it depended on unsupported source-generator chaining and did not produce a reliable consumer feature.
```

Because this is a stable-package correction, the implementation must choose one of these compatibility treatments and document the choice in the changed spec and release notes:

1. remove the public API and document it as a 1.1 compatibility correction; or
2. retain obsolete no-op properties for one minor release if public API baselines require a gentler transition.

Do not leave the feature silently present.

## Focus Areas

### Focus Area 1 — Remove Generated JsonSerializerContext Support

#### Implementation Intent

Remove the generated-context feature from the generator and System.Text.Json options.

Expected removals or behavior changes include:

```text
SemanticTypeModelGenerateSystemTextJsonContext
SemanticTypeModelSystemTextJsonContextName
GenerateJsonSerializerContext
GeneratedContextName
GenerateSystemTextJsonContextSource(...)
SemanticTypeModel.SystemTextJsonContext.g.cs output
STJ004 generated-context missing-root diagnostic
STJ005 object-typed generated-context diagnostic, unless reused for resolver guidance
```

If public API baselines prevent direct deletion, obsolete the surface with diagnostics and make it non-operational.

#### Required Authority

```text
docs/specs/system-text-json-contract-integration.md
docs/specs/type-model-compile-time-generator.md
docs/decisions/remove-system-text-json-context-generation.md
```

#### Validation

- Tier 1 during implementation:
  - affected generator unit tests;
  - affected System.Text.Json unit tests.
- Tier 2 before completing the focus area:
  - `./eng/check.sh`.

#### Direct Documentation Impact

- `docs/specs/system-text-json-contract-integration.md`
- generator-related public docs if they currently claim generated context support.
- package README source for `SemanticTypeModel.SystemTextJson` if it currently advertises generated context support.

#### Deferred Documentation Impact

- Release-wide documentation synchronization.
- Root README package guidance unless changed examples require it.
- Broad sample index cleanup.

### Focus Area 2 — Resolver-Centered System.Text.Json Contract Customization

#### Implementation Intent

Make resolver customization the supported application mechanism.

The package must support wrapping an existing resolver:

```csharp
IJsonTypeInfoResolver resolver =
    AppJsonContext.Default.WithSemanticTypeModelJson(
        AppSemanticTypeModel.Create(),
        options =>
        {
            options.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName;
        });
```

Equivalent APIs may be used if they preserve these invariants:

```text
existing resolver metadata is preserved
user-authored JsonSerializerContext can be used
semantic metadata is applied by modifying JsonTypeInfo before use
options.TypeInfoResolver is not blindly replaced when one already exists
```

#### Required Authority

```text
docs/specs/system-text-json-contract-integration.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
```

#### Validation

- Tier 1:
  - System.Text.Json project tests;
  - package smoke or focused consumer-style test when resolver composition is changed.
- Tier 2:
  - `./eng/check.sh`.

#### Direct Documentation Impact

- `docs/specs/system-text-json-contract-integration.md`
- `public-docs/guides/system-text-json.md`
- `public-docs/nuget/SemanticTypeModel.SystemTextJson.md`

#### Deferred Documentation Impact

- General getting-started pages unless examples change there.
- Full public-docs cross-link normalization.

### Focus Area 3 — Semantic Name as JSON Serialization Name

#### Implementation Intent

Add a supported option that lets the runtime resolver use semantic property names as JSON property names.

The implementation must not conflate these concepts:

```text
CLR property name
semantic property name
semantic display/title metadata
System.Text.Json property name
JsonPropertyNameAttribute value
currently resolved JsonPropertyInfo.Name
```

Recommended public contract:

```csharp
public enum SemanticJsonPropertyNameSource
{
    ExistingJsonContract,
    SystemTextJsonPropertyNameAnnotation,
    SemanticPropertyName
}
```

Recommended options:

```csharp
public SemanticJsonPropertyNameSource PropertyNameSource { get; set; }
```

The final names may differ, but the final API must allow:

```text
use existing System.Text.Json contract name
use systemTextJson.propertyName annotation
use semantic property name
```

The resolver must detect duplicate final JSON names and fail deterministically through an exception or diagnostic/result contract defined in the spec.

#### Required Authority

```text
docs/specs/system-text-json-contract-integration.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-annotations.md
```

#### Validation

- Tier 1:
  - tests for serialization and deserialization using semantic property names;
  - tests for duplicate final JSON names;
  - tests with user-authored source-generated context wrapped by the SemanticTypeModel resolver.
- Tier 2:
  - `./eng/check.sh`.

#### Direct Documentation Impact

- `docs/specs/system-text-json-contract-integration.md`
- `public-docs/guides/system-text-json.md`
- relevant diagnostics reference if a new diagnostic ID is added.

#### Deferred Documentation Impact

- Broad examples pass across all samples and public docs.

### Focus Area 4 — Samples and Package Smoke Corrections

#### Implementation Intent

Fix samples and package smoke tests so they represent the supported 1.1 contract.

Samples must not rely on `SemanticTypeModel` generating a `JsonSerializerContext`.

If source generation is demonstrated, the consumer sample must declare the context itself:

```csharp
[JsonSerializable(typeof(Customer))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
```

Then the sample may wrap the context resolver with SemanticTypeModel customization.

#### Required Authority

```text
docs/specs/system-text-json-contract-integration.md
docs/engineering/command-contract.md
docs/engineering/samples.md
docs/engineering/packaging.md
```

#### Validation

- Tier 1:
  - affected sample command if available;
  - package smoke focused on `SemanticTypeModel.SystemTextJson`.
- Tier 2:
  - `./eng/check.sh`.
- Tier 3 only if the implementation changes packaging layout or package contents:
  - `./eng/package.sh 1.1.0`
  - `./eng/package-smoke.sh 1.1.0`

#### Direct Documentation Impact

- Sample source comments or direct sample docs if they are changed.
- `public-docs/samples.md` only if sample names or entry points change.

#### Deferred Documentation Impact

- Full sample documentation synchronization.

## Required Behavior

### Generated Contexts

The corrected contract is:

```text
SemanticTypeModel does not generate JsonSerializerContext declarations.
```

The implementation must ensure:

- no generator output file declares a type deriving from `JsonSerializerContext`;
- no MSBuild property or assembly option is documented as enabling generated context output;
- no sample depends on `SemanticTypeModel.Generated.*JsonContext`;
- no package smoke test proves a non-supported context-generation path.

### User-Owned Source Generation

Consumers who want System.Text.Json source generation must author the context declaration:

```csharp
[JsonSerializable(typeof(Customer))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
```

SemanticTypeModel may then wrap or compose the user-owned context resolver.

### Resolver Composition

The resolver API must support an existing resolver.

Required behavior:

```text
when options.TypeInfoResolver is null:
  use DefaultJsonTypeInfoResolver as the base resolver

when options.TypeInfoResolver is not null:
  wrap or compose that resolver; do not discard it

when a JsonSerializerContext is supplied:
  allow SemanticTypeModel customization by wrapping the context as an IJsonTypeInfoResolver
```

### Property Name Source

The corrected implementation must support at least these effective name sources:

```text
ExistingJsonContract
SystemTextJsonPropertyNameAnnotation
SemanticPropertyName
```

The default must preserve the existing JSON contract unless the existing 1.0 behavior intentionally documented another default. If the default changes, the milestone implementation must treat it as public behavior change and update the spec and package README source.

### Matching Model Properties to JsonPropertyInfo

The implementation must not rely only on `JsonPropertyInfo.Name` if that value may already have been transformed by a naming policy or source-generated context.

The implementation should use a stable match strategy, such as:

```text
stored CLR member-name annotation from extraction
JsonPropertyInfo.AttributeProvider where available
explicit mapping from semantic property metadata
fallback to current JsonPropertyInfo.Name only when safe
```

If stable matching cannot be guaranteed for a type, the resolver must leave the property unchanged or fail with explicit guidance.

## Diagnostics

The implementation may add diagnostics if required by the final design.

If diagnostics are added or changed:

- follow the diagnostic rules in `docs/ENGINEERING.md`;
- add public constants in the appropriate diagnostic ID owner;
- add or update diagnostic descriptor fields for compile-time diagnostics;
- update the relevant diagnostics reference page;
- add diagnostic stability tests.

Do not reuse retired IDs.

## Files Likely Affected

Implementation agents should inspect these likely affected files, but the list is not exhaustive:

```text
src/SemanticTypeModel.Generators/SemanticTypeModelSourceGenerator.cs
src/SemanticTypeModel.DotNet/DotNetExtractionContracts.cs
src/SemanticTypeModel.DotNet/SemanticTypeAttributes.cs
src/SemanticTypeModel.DotNet/RoslynDotNetTypeExtractor.cs
src/SemanticTypeModel.SystemTextJson/SystemTextJsonProjectionOptions.cs
src/SemanticTypeModel.SystemTextJson/SemanticTypeModelJsonSerializerOptionsExtensions.cs
src/SemanticTypeModel.SystemTextJson/SemanticTypeModelJsonTypeInfoResolver.cs
tests/unit/SemanticTypeModel.Generators.Tests.Unit/*
tests/unit/SemanticTypeModel.SystemTextJson.Tests.Unit/*
tests/package-smoke/*
samples/system-text-json-basic/*
public-docs/guides/system-text-json.md
public-docs/nuget/SemanticTypeModel.SystemTextJson.md
public-docs/release-notes.md
```

## Validation Plan

Use the smallest useful validation tier during implementation.

### Tier 1 Inner Loop

Use affected-area validation for:

```text
generator tests
System.Text.Json tests
package smoke tests when package behavior changes
sample validation when samples change
diagnostic stability tests when diagnostics change
```

Use repository command wrappers from `docs/engineering/command-contract.md`. Do not invent alternative commands.

### Tier 2 Completion Gate

Run:

```sh
./eng/check.sh
```

before completing implementation work, unless an explicit environment limitation prevents it.

### Tier 3 Conditional Validation

Run package validation only when packaging layout, package contents, package smoke tests, or release package behavior are touched:

```sh
./eng/package.sh 1.1.0
./eng/package-smoke.sh 1.1.0
```

Do not run publish validation. This is not a release publication milestone.

## Acceptance Criteria

The milestone is complete when:

- `SemanticTypeModel` no longer generates `JsonSerializerContext` declarations.
- Generator code no longer emits `SemanticTypeModel.SystemTextJsonContext.g.cs` or equivalent context output.
- Generated-context MSBuild properties and assembly options are removed, obsolete, or explicitly rejected according to the chosen compatibility treatment.
- `docs/specs/system-text-json-contract-integration.md` states that generated contexts are unsupported.
- Consumers can use a user-authored `JsonSerializerContext` together with SemanticTypeModel resolver customization.
- `JsonSerializerOptions.AddSemanticTypeModelJson(...)` or equivalent APIs preserve an existing resolver instead of blindly replacing it.
- A public option exists to use semantic property names as JSON serialization names.
- Tests cover serialization and deserialization with semantic property names.
- Tests cover resolver composition over a user-authored source-generated context.
- Tests cover duplicate final JSON property names.
- Samples no longer depend on SemanticTypeModel-generated JSON contexts.
- Package smoke coverage exercises the supported System.Text.Json path.
- Directly affected public docs and package README source are corrected.
- Tier 2 validation passes, or any inability to run it is explicitly reported with the exact lower-tier validation that did run.
- No TBPs, issue templates, broad documentation cleanup, generated code files, implementation source patches, or workflow YAML changes are introduced by the planning package itself.

## Direct Documentation Impact

The implementation should update these when the corresponding behavior changes are implemented:

```text
docs/specs/system-text-json-contract-integration.md
public-docs/guides/system-text-json.md
public-docs/nuget/SemanticTypeModel.SystemTextJson.md
public-docs/release-notes.md
public-docs/api/compatibility.md
public-docs/diagnostics/*.md when diagnostics change
```

## Deferred Documentation Impact

The implementation should leave explicit notes for a later documentation synchronization pass covering:

```text
README.md if examples or package positioning should change
public-docs/getting-started.md if the System.Text.Json quick path is mentioned there
public-docs/packages.md if package capability wording changes
public-docs/samples.md and public-docs/samples/* if sample entry points or behavior change
docs/MILESTONES.md index update for M0024
docs/SPECS.md index wording if the spec title or purpose changes
docs/DECISIONS.md index update for the new decision record
```

Do not perform broad documentation synchronization as part of a narrow implementation slice unless the implementation directly touches that documentation surface.
