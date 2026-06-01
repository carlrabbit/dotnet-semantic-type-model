# M0020: System.Text.Json Contract Integration

## Status

Draft milestone content document.

## Goal

Add a dedicated `System.Text.Json` integration layer that allows `SemanticTypeModel` to understand, preserve, validate, and optionally generate `System.Text.Json` serialization contract metadata without making the canonical semantic model depend on `System.Text.Json`.

The milestone establishes a clear boundary:

```text
Semantic model
  remains projection-neutral

System.Text.Json integration
  imports and exports serialization contract metadata through namespaced annotations, generator hooks, and runtime helper APIs
```

The canonical rule is:

```text
System.Text.Json metadata may inform JSON serialization behavior, but the canonical semantic model must not become a System.Text.Json contract model.
```

## Scope

This milestone covers:

- adding a dedicated `SemanticTypeModel.SystemTextJson` package;
- importing relevant `System.Text.Json.Serialization` attributes during .NET type extraction;
- representing imported metadata as namespaced annotations;
- distinguishing semantic names from JSON serialization names;
- adding source-generator support for optional `JsonSerializerContext` generation;
- adding runtime helper APIs for `JsonSerializerOptions` and `IJsonTypeInfoResolver` integration where practical;
- adding diagnostics for conflicting, unsupported, or ambiguous `System.Text.Json` metadata;
- adding package smoke tests for `System.Text.Json` usage;
- documenting the relationship between semantic metadata and `System.Text.Json` serialization contracts.

## Non-Goals

This milestone does not cover:

- replacing `System.Text.Json`;
- implementing a custom JSON serializer;
- making the canonical model a `System.Text.Json` contract model;
- forcing `System.Text.Json` dependencies into `SemanticTypeModel.Abstractions`;
- generating JSON Schema directly from `System.Text.Json` metadata;
- full support for every custom converter behavior;
- runtime mutation of all possible `JsonTypeInfo` contracts;
- Newtonsoft.Json integration;
- making `JsonSerializerOptions` the primary semantic configuration system.

## Implementation Router

Read only the authoritative documents needed for the focus area being implemented:

- relevant specs from `docs/specs/`;
- `docs/ENGINEERING.md` and `docs/engineering/command-contract.md` for validation-tier selection;
- `docs/PUBLIC-DOCS.md` and affected `public-docs/` pages only when the change is consumer-facing;
- architecture or decision records only when the change alters structure or rationale.

Historical research guide copies are non-authoritative references and are not required milestone reading.

## Focus Areas

Use the milestone scope to choose one or more focused implementation slices instead of treating the whole milestone as a single work item:

| Focus area | Validation tier | Documentation impact |
|---|---|---|
| Behavior or API implementation | Tier 1 during development, Tier 2 before completion | Direct when behavior is consumer-facing; otherwise update specs only when contracts change. |
| Tests and diagnostics | Tier 1 for the affected test project or diagnostic filter, Tier 2 before completion | Direct for public diagnostics; deferred only when examples require a later feature slice. |
| Public documentation, samples, or release readiness | Tier 0 for documentation checks, Tier 3 for package/release readiness | Direct for changed public docs and package README sources; record deferred docs explicitly. |

## Validation Tier

- Default implementation focus areas: Tier 1 during the inner loop, then Tier 2 before completion.
- Documentation-only focus areas: Tier 0 plus `./eng/public-docs.sh` when public documentation changes.
- Packaging or release focus areas: Tier 3 or Tier 4 as described by the release-readiness documents.

## Background

`System.Text.Json` uses two complementary mechanisms:

1. Type/member attributes such as `JsonPropertyNameAttribute`, `JsonIgnoreAttribute`, `JsonIncludeAttribute`, `JsonConverterAttribute`, `JsonNumberHandlingAttribute`, `JsonRequiredAttribute`, and related attributes.
2. Source-generation contexts derived from `JsonSerializerContext`, typically configured through `JsonSerializableAttribute` and `JsonSourceGenerationOptionsAttribute`.

The semantic model should understand both surfaces where relevant.

The semantic model should not treat `System.Text.Json` as the same thing as JSON Schema. `System.Text.Json` describes a serializer contract. JSON Schema describes a schema document. They overlap, but neither should own the canonical semantic model.

## Package Design

Add a new package:

```text
SemanticTypeModel.SystemTextJson
```

The package owns:

- `System.Text.Json` attribute import;
- `systemTextJson.*` annotation keys;
- `System.Text.Json` projection options;
- optional `JsonSerializerContext` generation support;
- `JsonSerializerOptions` / `IJsonTypeInfoResolver` helper APIs;
- `System.Text.Json` diagnostics;
- `System.Text.Json` package docs and smoke tests.

Do not add `System.Text.Json` dependencies to:

```text
SemanticTypeModel.Abstractions
```

unless a later decision explicitly changes the dependency policy.

## Dependency Rules

`SemanticTypeModel.Abstractions` must remain projection-neutral.

Recommended package responsibilities:

```text
SemanticTypeModel.Abstractions
  projection-neutral semantic model and attributes

SemanticTypeModel.DotNet
  .NET type extraction and general .NET metadata mapping

SemanticTypeModel.Generators
  compile-time semantic model generation

SemanticTypeModel.SystemTextJson
  System.Text.Json-specific import, annotations, options, helpers, and generator integration

SemanticTypeModel.JsonSchema
  JSON Schema import/export

SemanticTypeModel.JsonEditor
  UI/JSON-editor projection hints

SemanticTypeModel.EFCore
  EF Core projection

SemanticTypeModel.PowerBI
  Power BI/TOM projection
```

The new package may depend on the packages needed for extraction/generator integration, but dependencies must remain acyclic.

## Canonical Design Rules

### Rule 1: Semantic names and serialization names are different

A semantic property name describes the model member identity.

A `System.Text.Json` property name describes the JSON serialization contract.

Example:

```csharp
public sealed record Customer
{
    [SemanticName("Customer ID")]
    [JsonPropertyName("customer_id")]
    public required string Id { get; init; }
}
```

Expected interpretation:

```text
SemanticName
  affects semantic/display metadata according to semantic rules

JsonPropertyName
  affects System.Text.Json serialization metadata

Neither automatically overwrites the other unless an explicit policy says so
```

### Rule 2: System.Text.Json attributes are imported as namespaced annotations

`System.Text.Json` metadata should be represented using namespaced annotations, for example:

```text
systemTextJson.propertyName
systemTextJson.ignore
systemTextJson.include
systemTextJson.converter
systemTextJson.numberHandling
systemTextJson.required
systemTextJson.extensionData
systemTextJson.objectCreationHandling
systemTextJson.unmappedMemberHandling
```

The exact keys must be documented in a spec.

### Rule 3: Existing System.Text.Json attributes should be recognized

Do not create duplicate wrapper attributes for standard `System.Text.Json` concepts unless there is a specific reason.

Prefer:

```csharp
[JsonPropertyName("customer_id")]
public required string Id { get; init; }
```

over:

```csharp
[SemanticSystemTextJsonPropertyName("customer_id")]
public required string Id { get; init; }
```

Semantic attributes should describe semantic intent.

`System.Text.Json` attributes should describe `System.Text.Json` behavior.

### Rule 4: JsonSerializerContext generation is opt-in

The semantic source generator must not emit `System.Text.Json` contexts unless explicitly configured.

Valid opt-in mechanisms may include:

```xml
<PropertyGroup>
  <SemanticTypeModelGenerateSystemTextJsonContext>true</SemanticTypeModelGenerateSystemTextJsonContext>
</PropertyGroup>
```

or a dedicated marker/configuration attribute in the `SystemTextJson` package.

### Rule 5: Unsupported converter behavior is preserved or diagnosed

A custom converter may imply behavior that cannot be represented in the semantic model.

The implementation must either:

- preserve converter metadata as annotation; or
- report a stable diagnostic if the converter is required for a requested operation but cannot be represented.

Do not attempt to infer arbitrary custom converter semantics.

## Required Public APIs

The exact names may change during implementation, but the milestone must provide equivalent capabilities.

### Options

Provide an options type similar to:

```csharp
public sealed class SystemTextJsonProjectionOptions
{
    public bool ImportSystemTextJsonAttributes { get; set; } = true;

    public bool UseJsonPropertyNameAsSerializationName { get; set; } = true;

    public bool UseJsonPropertyNameAsSemanticName { get; set; } = false;

    public bool PreserveUnsupportedConverterMetadata { get; set; } = true;

    public bool GenerateJsonSerializerContext { get; set; } = false;

    public string? GeneratedContextName { get; set; }
}
```

The options must make the semantic-name vs serialization-name boundary explicit.

### Annotation Constants

Provide typed constants for annotation keys, for example:

```csharp
public static class SystemTextJsonAnnotationNames
{
    public const string PropertyName = "systemTextJson.propertyName";
    public const string Ignore = "systemTextJson.ignore";
    public const string Include = "systemTextJson.include";
    public const string Converter = "systemTextJson.converter";
    public const string NumberHandling = "systemTextJson.numberHandling";
    public const string Required = "systemTextJson.required";
}
```

If the repository has an existing annotation-name pattern, follow it.

### Extraction Integration

Provide integration with .NET type extraction so that extraction can import `System.Text.Json` metadata.

Possible API shape:

```csharp
options.UseSystemTextJson();
```

or:

```csharp
options.SystemTextJson.ImportAttributes = true;
```

The final API must fit the existing extraction/convention options.

### Source Generator Integration

The source generator should support opt-in generation of a `JsonSerializerContext` or context-compatible metadata.

Potential generated shape:

```csharp
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Customer))]
[JsonSerializable(typeof(Order))]
internal partial class AppSemanticJsonContext : JsonSerializerContext
{
}
```

The implementation must define:

- how root types are selected;
- whether reachable types are included;
- how generic/collection types are represented;
- how generated context names are configured;
- where generated code is emitted;
- how conflicts with user-defined contexts are diagnosed.

### Runtime Helper API

Provide helper APIs only where they are technically sound.

Potential API shape:

```csharp
public static class SemanticTypeModelJsonSerializerOptionsExtensions
{
    public static JsonSerializerOptions AddSemanticTypeModelJson(
        this JsonSerializerOptions options,
        TypeSchemaModel model,
        Action<SystemTextJsonProjectionOptions>? configure = null);
}
```

Potential resolver API shape:

```csharp
public static class SemanticTypeModelJsonTypeInfoResolver
{
    public static IJsonTypeInfoResolver Create(
        TypeSchemaModel model,
        SystemTextJsonProjectionOptions? options = null);
}
```

If runtime resolver customization is not sufficiently reliable for the first implementation, the milestone may restrict runtime helpers to documented, limited scenarios and prefer generated contexts.

The limitation must be documented.

## Attribute Import Requirements

The implementation must recognize at least the following attributes when present:

```text
System.Text.Json.Serialization.JsonPropertyNameAttribute
System.Text.Json.Serialization.JsonIgnoreAttribute
System.Text.Json.Serialization.JsonIncludeAttribute
System.Text.Json.Serialization.JsonConverterAttribute
System.Text.Json.Serialization.JsonNumberHandlingAttribute
System.Text.Json.Serialization.JsonRequiredAttribute
System.Text.Json.Serialization.JsonExtensionDataAttribute
```

Where available in the target .NET version, also consider:

```text
JsonObjectCreationHandlingAttribute
JsonUnmappedMemberHandlingAttribute
JsonPolymorphicAttribute
JsonDerivedTypeAttribute
```

If an attribute is not available for the target framework or not supported by the package baseline, document the limitation and diagnostic behavior.

## Mapping Requirements

### JsonPropertyName

`JsonPropertyNameAttribute` maps to:

```text
systemTextJson.propertyName
```

It must not automatically replace semantic member name unless explicitly configured.

### JsonIgnore

`JsonIgnoreAttribute` maps to:

```text
systemTextJson.ignore
systemTextJson.ignoreCondition
```

If a member is semantically required but ignored by `System.Text.Json`, report a diagnostic.

### JsonInclude

`JsonIncludeAttribute` maps to:

```text
systemTextJson.include
```

If a non-public member is included for serialization but excluded from semantic extraction by current extraction policy, report a diagnostic or preserve metadata according to options.

### JsonConverter

`JsonConverterAttribute` maps to:

```text
systemTextJson.converter
```

The converter type name should be preserved as metadata.

Do not infer arbitrary converter semantics.

### JsonNumberHandling

`JsonNumberHandlingAttribute` maps to:

```text
systemTextJson.numberHandling
```

If number handling changes the effective serialized form in a way that conflicts with scalar constraints or JSON Schema export, produce a diagnostic during the relevant projection.

### JsonRequired

`JsonRequiredAttribute` maps to:

```text
systemTextJson.required
```

It must be reconciled with:

- C# `required`;
- nullable reference type metadata;
- semantic required/presence settings;
- DataAnnotations `[Required]`, if imported.

Conflicts must produce stable diagnostics.

### JsonExtensionData

`JsonExtensionDataAttribute` maps to:

```text
systemTextJson.extensionData
```

If the extension-data member type is unsupported, report a diagnostic.

## Diagnostics

Add stable diagnostics for `System.Text.Json` integration.

Suggested diagnostic IDs may be renamed to match repository diagnostic policy.

### STJ001: Conflicting semantic name and JSON property name policy

Occurs when configuration requires `JsonPropertyName` to define semantic names, but the member also has explicit semantic naming metadata that conflicts.

### STJ002: Unsupported JsonConverter metadata

Occurs when a requested projection requires converter semantics that cannot be represented.

### STJ003: JsonIgnore conflicts with required semantic member

Occurs when a member is semantically required but ignored for `System.Text.Json`.

### STJ004: Generated context missing required root type

Occurs when context generation is requested but no valid root types can be included.

### STJ005: Object-typed member requires explicit JsonSerializable root

Occurs when a member typed as `object`, interface, or open polymorphic surface cannot be safely included in the generated context without explicit roots.

### STJ006: JsonRequired conflicts with nullable or optional semantic member

Occurs when `JsonRequired` metadata conflicts with semantic optional/nullability rules.

### STJ007: Unsupported JsonExtensionData member type

Occurs when extension data is present but member type cannot be represented safely.

### STJ008: Polymorphism metadata cannot be represented

Occurs when `JsonPolymorphic` / `JsonDerivedType` metadata cannot be represented in the canonical model or generated context.

Diagnostics must include:

- stable diagnostic ID;
- severity;
- message;
- source location where available;
- model path where available;
- actionable guidance.

## Tests

Add unit tests for attribute import:

- `JsonPropertyName` import;
- `JsonIgnore` import;
- `JsonInclude` import;
- `JsonConverter` metadata preservation;
- `JsonNumberHandling` import;
- `JsonRequired` import;
- `JsonExtensionData` import;
- semantic name vs serialization name separation;
- explicit policy that maps JSON names into semantic names;
- conflict diagnostics.

Add generator tests for:

- opt-in context generation;
- no context generation by default;
- generated context includes configured roots;
- generated context handles reachable nested types;
- generated context handles collections;
- generated context reports unsupported polymorphic/object cases;
- generated code compiles.

Add runtime tests for:

- helper API behavior;
- `JsonSerializerOptions` integration where supported;
- `IJsonTypeInfoResolver` integration where supported;
- serialization using generated context;
- deserialization using generated context.

Add package smoke tests for:

- consuming `SemanticTypeModel.SystemTextJson` from a packed package;
- importing STJ attributes in a clean consumer;
- using generated context in a clean consumer;
- serializing/deserializing a simple annotated model;
- proving package usage does not require project references.

## Sample Requirements

Add at least one sample under `samples/`, for example:

```text
samples/SystemTextJson.Basic
```

The sample should show:

- annotated C# model;
- semantic attributes and `System.Text.Json` attributes side by side;
- semantic model generation/extraction;
- generated or configured `JsonSerializerContext`;
- serialization/deserialization with `System.Text.Json`;
- distinction between semantic name and JSON property name.

If the repository uses a different sample naming convention, follow the existing convention.

## Documentation Requirements

Create or update:

```text
docs/specs/system-text-json-contract-integration.md
docs/engineering/packaging.md
docs/MILESTONES.md
docs/SPECS.md
docs/PUBLIC-DOCS.md
public-docs/packages.md
public-docs/concepts.md
public-docs/guides/system-text-json.md
public-docs/nuget/SemanticTypeModel.SystemTextJson.md
public-docs/diagnostics.md
public-docs/release-notes.md
```

If the public docs structure differs, update the equivalent documents.

Documentation must explain:

- why `System.Text.Json` integration is a separate package;
- why semantic names and JSON names differ;
- which STJ attributes are imported;
- how generated contexts are enabled;
- how runtime helpers are used;
- what is unsupported;
- how diagnostics are interpreted.

## Public Documentation Impact

Public documentation surfaces affected:

- `README.md`, if package list or quick start changes;
- `public-docs/packages.md`;
- `public-docs/concepts.md`;
- `public-docs/guides/system-text-json.md`;
- `public-docs/nuget/SemanticTypeModel.SystemTextJson.md`;
- `public-docs/diagnostics.md`;
- `public-docs/release-notes.md`.

## Package and Release Impact

Add the package to prerelease packaging once implemented:

```text
SemanticTypeModel.SystemTextJson
```

Update package metadata:

```text
PackageId
Title
Description
Authors
RepositoryUrl
RepositoryType
PackageTags
PackageReadmeFile
PackageLicenseExpression
PublishRepositoryUrl
EmbedUntrackedSources
IncludeSymbols
SymbolPackageFormat
```

Update package smoke tests and release documentation so the package is included in the release pipeline when ready.

## Implementation Notes

The implementation should prefer existing repository extension points.

Do not introduce a parallel extraction pipeline if the existing .NET extraction and generator options can be extended.

Do not special-case `System.Text.Json` in the canonical model core.

The canonical model should only receive typed or namespaced annotations.

Runtime helper APIs should be conservative. If an operation cannot be represented reliably through `JsonSerializerOptions` or `IJsonTypeInfoResolver`, prefer documentation and diagnostics over pretending full support exists.

## Acceptance Criteria

The milestone is complete when:

- `SemanticTypeModel.SystemTextJson` package exists and builds;
- `SemanticTypeModel.Abstractions` remains free of `System.Text.Json` dependencies;
- relevant STJ attributes are imported into namespaced annotations;
- semantic names and JSON serialization names are preserved separately by default;
- explicit options exist for any policy that maps JSON names into semantic names;
- source generator supports opt-in `JsonSerializerContext` generation or an explicitly documented equivalent;
- generated context code compiles in tests;
- runtime helper APIs exist for supported scenarios or limitations are documented;
- STJ diagnostics are stable and tested;
- unit tests cover attribute import and conflicts;
- generator tests cover context generation;
- package smoke tests consume packed packages;
- public documentation explains package purpose and usage;
- `docs/MILESTONES.md` is updated;
- no non-root README files are introduced;
- `./eng/check.sh` passes;
- release-relevant validation passes if release scripts are available.

## Completion Report

When closing this milestone, report:

- package added;
- public APIs added;
- STJ attributes supported;
- annotation keys added;
- generator behavior implemented;
- runtime helper behavior implemented;
- diagnostics added;
- tests added;
- sample added;
- public docs updated;
- validation commands run.
