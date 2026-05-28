# M0014: Semantic Type Annotation Usability

## Status

Active preview milestone.

## Goal

Make code-first semantic type extraction pleasant, explicit, projection-neutral by default, and hard to misuse.

This milestone improves the usability of semantic type attribute annotations used by .NET type-system extraction and the compile-time generator. The result should let ordinary C# model authors describe semantic intent without needing to understand the full canonical model object graph or any single downstream projection.

The milestone must preserve the project architecture:

```text
C# types, attributes, and conventions
        ↓
SemanticTypeModel.DotNet extraction
        ↓
canonical TypeSchemaModel
        ↓
transformations and diagnostics
        ↓
JSON Schema / JSON Editor / Power BI / EF Core projections
```

Attributes must primarily describe semantic intent. Projection-specific attributes are allowed only as explicit escape hatches owned by the corresponding projection package.

For the current prerelease repository layout, neutral authoring attributes remain in `SemanticTypeModel.DotNet` so the existing extraction and generator package split stays intact while the public vocabulary matures.

## Background

Previous milestones established:

- the canonical semantic type model;
- JSON Schema runtime import/export;
- transformation and diagnostics hardening;
- JSON Editor / UI projection hints;
- Power BI / TOM projection prototype;
- EF Core projection prototype;
- .NET type-system extraction and generator baseline;
- attribute and convention baseline;
- runtime API and DI integration;
- documentation, samples, and first end-to-end scenarios;
- prerelease NuGet packaging and release automation.

This milestone now improves the public authoring experience for C# developers using attributes to shape the semantic type model.

## Scope

This milestone covers the public annotation model, extraction behavior, generator behavior, diagnostics, documentation, samples, and validation needed to make attribute-driven semantic type extraction usable.

Included areas:

- projection-neutral semantic attributes;
- attribute namespace and package ownership;
- explicit type inclusion and exclusion;
- semantic names, display names, descriptions, categories, and ordering;
- requiredness and nullability overrides;
- scalar format and constraint annotations;
- enum value metadata;
- key annotations;
- relationship annotations;
- entity, value-object, dimension, fact, lookup, event, configuration, and form role annotations;
- attribute-to-canonical-model mapping;
- attribute validation diagnostics;
- generator diagnostics for invalid attribute usage;
- conflict resolution between attributes, conventions, XML docs, and nullable metadata;
- public documentation and examples;
- package smoke coverage for attribute usage.

## Non-Goals

This milestone does not include:

- redesigning the canonical type model;
- replacing convention-based extraction;
- making EF Core annotations the primary annotation model;
- adding EF Core projection hardening beyond the attribute support required by M0015;
- adding Power BI, JSON Editor, or JSON Schema projection features beyond annotation ownership and pass-through mapping;
- runtime CLR type generation;
- automatic inference from arbitrary database schemas;
- stabilization of all APIs for 1.0;
- introducing non-attribute configuration as the primary authoring mechanism.

## Required Reading

Before implementation, read:

- `docs/TERMINOLOGY.md`
- `docs/SPECS.md`
- `docs/MILESTONES.md`
- `docs/ENGINEERING.md`
- `docs/PUBLIC-DOCS.md`
- `docs/GUARDRAILS.md`
- `docs/guardrails/implementation.md`
- `docs/guardrails/testing.md`
- `docs/engineering/command-contract.md`
- `docs/engineering/packaging.md`
- `docs/tbps/feature-implementation.md`
- `docs/tbps/public-documentation-update.md`
- `docs/research/project-setup-guide-v5.md`
- `docs/research/engineering-guide-v4.md`

Also review milestone and issue history for:

- M0003 Type Model Hardening
- M0005 Transformation Pipeline and Diagnostics Hardening
- M0008 EF Core DbModel Projection Prototype
- M0009 .NET Type System Extraction and Compile-Time Generator Baseline
- M0010 Attribute and Convention Model for .NET Type Extraction
- M0011 Runtime API Surface and DI Integration
- M0013 Prerelease NuGet Packaging and Release Automation

## Attribute Design Principles

### Projection-neutral first

The default annotation vocabulary must describe semantic intent, not a single projection's implementation details.

Prefer:

```csharp
[SemanticType(SemanticTypeRole.Entity)]
public sealed partial record Customer;

[SemanticKey]
public required string CustomerId { get; init; }
```

Avoid making projection-specific attributes the common path:

```csharp
[EfPrimaryKey]
[JsonSchemaRequired]
[PowerBiRelationshipColumn]
public required string CustomerId { get; init; }
```

Projection-specific attributes may exist, but they must be opt-in overrides.

### Small core vocabulary

The neutral attribute model should be small enough that users can learn it quickly.

The core vocabulary should cover:

- type inclusion;
- member inclusion/exclusion;
- naming;
- description;
- semantic role;
- key;
- relationship;
- scalar constraints;
- collection constraints;
- enum metadata;
- custom annotations.

Avoid one attribute for every downstream projection property.

### Explicit escape hatches

When projection-specific behavior is required, attributes should live in the projection package and clearly indicate their target.

Examples:

```csharp
[SemanticEfTable("customers")]
[SemanticPowerBiTableRole(PowerBiTableRole.Dimension)]
[SemanticJsonEditorOrder(10)]
```

Projection-specific attributes must not be required for ordinary semantic extraction.

### Deterministic extraction

The same annotated source code must produce the same canonical semantic type model across builds.

Ordering must be stable for:

- discovered types;
- properties;
- enum values;
- keys;
- relationships;
- annotations;
- diagnostics.

### Diagnostics over silent behavior

Invalid or ambiguous annotation usage must produce diagnostics. Silent fallbacks are allowed only when behavior is obvious, documented, and deterministic.

## Package Ownership

### `SemanticTypeModel.DotNet`

Owns the current prerelease neutral authoring attributes, extraction logic, and shared public enums used by source-generation/extraction scenarios.

Expected neutral attributes:

```text
SemanticTypeAttribute
SemanticIgnoreAttribute
SemanticNameAttribute
SemanticDisplayNameAttribute
SemanticDescriptionAttribute
SemanticCategoryAttribute
SemanticOrderAttribute
SemanticRoleAttribute
SemanticKeyAttribute
SemanticRelationshipAttribute
SemanticFormatAttribute
SemanticStringConstraintsAttribute
SemanticNumericConstraintsAttribute
SemanticCollectionConstraintsAttribute
SemanticEnumValueAttribute
SemanticAnnotationAttribute
```

The exact names may differ if repository naming conventions already define alternatives, but the resulting public API must be consistent, documented, and stable enough for prerelease use.

Responsibilities:

- projection-neutral authoring attributes for the current prerelease package split;
- Roslyn symbol reading;
- attribute reading;
- nullable metadata interpretation;
- XML documentation extraction where supported;
- convention application;
- diagnostic production;
- canonical model construction.

### `SemanticTypeModel.Abstractions`

Owns the canonical semantic model, annotations, constraints, and shared runtime contracts consumed by extraction and projections.

### `SemanticTypeModel.Generators`

Owns compile-time generator integration.

Responsibilities:

- incremental generator input discovery;
- generated model provider output;
- generator diagnostics;
- generated-code determinism;
- package-smoke validation as analyzer/source-generator package.

### Projection packages

Projection-specific override attributes belong to their packages:

```text
SemanticTypeModel.EFCore
SemanticTypeModel.PowerBI
SemanticTypeModel.JsonEditor
```

Examples:

```text
SemanticEfTableAttribute
SemanticEfColumnAttribute
SemanticEfOwnedAttribute
SemanticPowerBiTableRoleAttribute
SemanticPowerBiDataCategoryAttribute
SemanticJsonEditorWidgetAttribute
SemanticJsonEditorOrderAttribute
```

Projection-specific attributes must map to namespaced canonical annotations rather than bypass the canonical model.

## Required Attribute Vocabulary

The implementation may adjust exact names to match repository conventions, but the milestone must define and document an equivalent vocabulary.

### Type inclusion

Provide an attribute for explicit type roots.

Example:

```csharp
[SemanticType]
public sealed partial record Customer;
```

Optional role:

```csharp
[SemanticType(SemanticTypeRole.Entity)]
public sealed partial record Customer;
```

Required behavior:

- marks a type as a semantic extraction root;
- supports role declaration;
- supports optional explicit semantic name;
- works with classes, records, record structs, and structs where supported;
- produces a diagnostic on unsupported targets.

### Type/member exclusion

Example:

```csharp
[SemanticIgnore]
public string InternalCacheKey { get; init; } = "";
```

Required behavior:

- excludes a type or member from semantic extraction;
- exclusion must be deterministic;
- exclusion must win over ordinary convention-based inclusion;
- conflicts with required semantic annotations must produce diagnostics.

### Naming and display metadata

Examples:

```csharp
[SemanticName("customer")]
[SemanticDisplayName("Customer")]
[SemanticDescription("A customer account.")]
public sealed partial record Customer;
```

Members:

```csharp
[SemanticName("email")]
[SemanticDisplayName("Email address")]
[SemanticDescription("Primary contact email address.")]
public required string Email { get; init; }
```

Required behavior:

- `SemanticName` controls canonical stable semantic name;
- display name is user-facing metadata;
- description is documentation metadata;
- XML documentation may populate description when no explicit attribute exists;
- explicit attributes override XML docs;
- naming conflicts after normalization must produce diagnostics.

### Categorization and ordering

Examples:

```csharp
[SemanticCategory("Contact")]
[SemanticOrder(20)]
public string? PhoneNumber { get; init; }
```

Required behavior:

- maps to generic UI or schema annotations as projection-neutral metadata;
- must not require JSON Editor dependency;
- order must be deterministic;
- duplicate order values are allowed unless the existing model forbids them;
- invalid order values must produce diagnostics.

### Roles

Examples:

```csharp
[SemanticRole(SemanticTypeRole.Entity)]
public sealed partial record Customer;

[SemanticRole(SemanticTypeRole.ValueObject)]
public sealed partial record Address;
```

Expected roles should align with existing semantic model role vocabulary, for example:

```text
Unspecified
Entity
ValueObject
Dimension
Fact
Lookup
Event
Configuration
Form
```

Required behavior:

- type role maps to canonical entity/type semantics;
- role conflicts must produce diagnostics;
- projection packages may interpret role differently but must not redefine core meaning.

### Requiredness and nullability

Requiredness and nullability must remain separate concepts.

The model must distinguish:

```text
member may be absent
member may be present with null
member value may be empty
collection may have zero items
```

Examples:

```csharp
public required string Name { get; init; }

[SemanticRequired]
public string? LegacyRequiredButNullable { get; init; }

[SemanticOptional]
public required string? OptionalButCompilerRequiredForConstruction { get; init; }
```

If the project decides not to introduce `SemanticRequiredAttribute` and `SemanticOptionalAttribute`, it must document the alternative clearly.

Required behavior:

- nullable reference type metadata is respected;
- C# `required` is respected where available;
- explicit semantic required/optional attributes override conventions;
- impossible or contradictory combinations produce diagnostics;
- generated model records requiredness and nullability separately.

### Scalar format

Examples:

```csharp
[SemanticFormat(SemanticScalarFormat.Email)]
public required string Email { get; init; }

[SemanticFormat("uri")]
public required string Website { get; init; }
```

Required behavior:

- supports common predefined formats;
- supports custom string formats if the model supports them;
- format must remain separate from scalar kind;
- invalid format usage must produce diagnostics.

### String constraints

Example:

```csharp
[SemanticStringConstraints(MinLength = 1, MaxLength = 200, Pattern = "^[A-Z]")]
public required string Code { get; init; }
```

Required behavior:

- maps to canonical string constraints;
- validates impossible combinations such as `MinLength > MaxLength`;
- validates target type compatibility;
- produces stable diagnostics.

### Numeric constraints

Example:

```csharp
[SemanticNumericConstraints(Minimum = 0, Maximum = 100)]
public decimal Percent { get; init; }
```

Required behavior:

- maps to canonical numeric constraints;
- supports inclusive/exclusive bounds if the canonical model supports them;
- validates incompatible targets;
- validates impossible ranges.

### Collection constraints

Example:

```csharp
[SemanticCollectionConstraints(MinItems = 1, MaxItems = 10)]
public IReadOnlyList<string> Tags { get; init; } = [];
```

Required behavior:

- maps to canonical array or collection constraints;
- validates collection-compatible target type;
- validates impossible ranges.

### Enum value metadata

Example:

```csharp
public enum CustomerStatus
{
    [SemanticEnumValue(DisplayName = "Active customer", Description = "Customer can place orders.")]
    Active,

    [SemanticEnumValue(DisplayName = "Suspended customer")]
    Suspended
}
```

Required behavior:

- maps enum value display metadata to canonical enum metadata;
- preserves enum storage information where already supported;
- supports generated model and runtime extraction;
- produces diagnostics for invalid targets.

### Keys

Examples:

```csharp
[SemanticKey]
public required string CustomerId { get; init; }
```

Composite key option:

```csharp
[SemanticKey("customer_region_key", Order = 0)]
public required string CustomerId { get; init; }

[SemanticKey("customer_region_key", Order = 1)]
public required string RegionId { get; init; }
```

Required behavior:

- supports simple keys;
- supports composite keys if canonical model already supports them;
- supports key kind if the model supports primary, alternate, natural, surrogate, or external keys;
- detects duplicate or incomplete composite key definitions;
- produces diagnostics when keys reference ignored or unsupported members.

### Relationships

Example:

```csharp
[SemanticRelationship(
    TargetType = typeof(Customer),
    Cardinality = SemanticRelationshipCardinality.ManyToOne,
    ForeignKey = nameof(CustomerId))]
public Customer? Customer { get; init; }

public required string CustomerId { get; init; }
```

Alternative simple usage may be supported if conventions can infer the rest.

Required behavior:

- maps to canonical relationship definitions;
- supports relationship cardinality;
- supports target type reference;
- supports principal/dependent property references where needed;
- validates referenced members;
- produces diagnostics for missing target types, ignored members, ambiguous keys, and unsupported relationship shapes.

### Custom annotations

Example:

```csharp
[SemanticAnnotation("ui.placeholder", "Enter customer name")]
public required string Name { get; init; }
```

Required behavior:

- supports namespaced annotation keys;
- validates annotation key syntax;
- preserves simple value types supported by the annotation model;
- prevents invalid or reserved namespace usage;
- deterministic merge behavior when duplicate annotations exist.

## Attribute Conflict Resolution

Define and implement deterministic precedence.

Recommended precedence:

```text
Explicit ignore
  > explicit semantic attributes
  > projection-specific attributes
  > generator/extraction configuration
  > XML documentation
  > conventions
  > default model behavior
```

Required conflict diagnostics:

- member is both ignored and semantically required;
- multiple semantic names conflict;
- duplicate normalized property names;
- key attribute on ignored member;
- relationship references missing member;
- constraint attribute applied to incompatible member type;
- enum-value attribute applied outside enum value;
- projection-specific attribute used without corresponding package support;
- duplicate custom annotation keys with incompatible values;
- role conflicts between attributes and conventions.

## Diagnostics

Add stable diagnostic IDs for annotation misuse.

The exact prefix must follow repository diagnostic conventions. If no convention exists, define one before implementing.

Required diagnostic categories:

```text
invalid attribute target
conflicting attributes
invalid annotation key
duplicate semantic name
unsupported member shape
invalid constraint range
invalid key definition
invalid relationship definition
projection-specific attribute misuse
generator extraction failure
```

Diagnostics must include:

- stable ID;
- severity;
- message;
- model path or source location where applicable;
- actionable remediation;
- tests.

Generator diagnostics must point at the annotated symbol where possible.

## Extraction Behavior

### Runtime and compile-time consistency

If runtime extraction and generator extraction both exist, attribute behavior must be consistent.

Required behavior:

- same attributes produce equivalent canonical model output;
- unsupported runtime-only or compile-time-only behavior must be documented;
- diagnostics should be equivalent where source-location differences allow.

### Incremental generator behavior

The generator must:

- detect relevant attribute changes;
- regenerate only affected output where practical;
- produce deterministic generated code;
- expose the generated model through the established M0009/M0011 API shape;
- include diagnostics for invalid annotation usage.

### Attribute inheritance

Define whether attributes are inherited from base types and interfaces.

Recommended baseline:

- type-level semantic attributes are not inherited unless explicitly documented;
- member-level attributes apply to the declaring member;
- overridden members use the most derived declaration;
- interface member attributes are not merged by default unless the extraction model already supports interface contracts.

If a different policy is implemented, document and test it.

### Partial types

Attributes across partial type declarations must merge deterministically.

Required behavior:

- duplicate compatible metadata is allowed only if values match;
- duplicate incompatible metadata produces diagnostics;
- source-order dependence is not allowed.

## Projection-Specific Attribute Policy

Projection-specific attributes are allowed only when they map into namespaced annotations or projection options.

### EF Core

Package:

```text
SemanticTypeModel.EFCore
```

Examples:

```text
SemanticEfTableAttribute
SemanticEfColumnAttribute
SemanticEfSchemaAttribute
SemanticEfOwnedAttribute
SemanticEfPrecisionAttribute
SemanticEfDeleteBehaviorAttribute
```

Policy:

- EF attributes must not become required for semantic extraction;
- EF attributes map to `efCore.*` annotations or EF projection configuration;
- EF attributes should be minimal and focused on overrides that cannot be expressed neutrally.

### Power BI

Package:

```text
SemanticTypeModel.PowerBI
```

Examples:

```text
SemanticPowerBiTableRoleAttribute
SemanticPowerBiDataCategoryAttribute
SemanticPowerBiFormatStringAttribute
SemanticPowerBiSummarizeByAttribute
```

Policy:

- Power BI attributes map to `powerBi.*` or `tom.*` annotations;
- they must not redefine canonical roles.

### JSON Editor

Package:

```text
SemanticTypeModel.JsonEditor
```

Examples:

```text
SemanticJsonEditorWidgetAttribute
SemanticJsonEditorOrderAttribute
SemanticJsonEditorOptionsAttribute
```

Policy:

- JSON Editor attributes map to `jsonEditor.*` annotations;
- generic UI intent should stay in neutral `ui.*` annotations where possible.

## Public API Requirements

The public attribute API must be:

- discoverable;
- XML documented;
- stable enough for prerelease usage;
- small enough to explain in public docs;
- analyzable by generator tests;
- compatible with central package management and existing package split.

Public API docs must explain intent, not implementation mechanics.

Examples should show ordinary C# usage, not internal model construction.

## Documentation Requirements

Update or create:

```text
docs/specs/semantic-type-annotations.md
docs/engineering/dotnet-type-extraction.md
docs/engineering/source-generator.md
public-docs/guides/attribute-annotations.md
public-docs/api/semantic-attributes.md
public-docs/diagnostics.md
public-docs/diagnostics/
public-docs/samples/attribute-annotations.md
public-docs/release-notes.md
```

Update existing indexes:

```text
docs/SPECS.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
```

The implementation must also update `README.md` if attribute annotations become part of the recommended quick start.

## Sample Requirements

Add or update a sample that demonstrates code-first attribute usage.

Recommended sample:

```text
samples/AttributeAnnotations/
```

The sample should demonstrate:

- type root annotation;
- semantic role;
- semantic name/display/description;
- required and nullable members;
- scalar constraints;
- enum value metadata;
- key;
- relationship;
- generated model access;
- JSON Schema export from the generated model.

If samples are validated through `eng/samples.sh`, include this sample.

Do not create `samples/AttributeAnnotations/README.md`.

Document the sample under:

```text
docs/engineering/samples.md
public-docs/samples/attribute-annotations.md
```

## Test Requirements

Add tests for:

### Attribute model tests

- valid neutral attributes compile;
- attributes reject invalid targets where possible;
- XML documentation exists for public attributes;
- public API baseline includes intended attributes only.

### Extraction tests

- type root extraction;
- ignored type/member behavior;
- naming and display metadata;
- XML doc fallback;
- requiredness/nullability;
- scalar formats;
- string constraints;
- numeric constraints;
- collection constraints;
- enum value metadata;
- simple key;
- composite key if supported;
- relationship;
- custom annotation;
- conflict diagnostics;
- deterministic ordering.

### Generator tests

- generator recognizes annotated types;
- generated model includes expected semantic metadata;
- invalid attribute usage produces diagnostics at expected locations;
- incremental changes affect expected generated output;
- generated code compiles in a consumer project.

### Package smoke tests

Update package smoke tests to verify:

- `SemanticTypeModel.Abstractions` exposes neutral attributes;
- `SemanticTypeModel.DotNet` consumes attributes;
- `SemanticTypeModel.Generators` works with annotated consumer types;
- projection packages can expose projection-specific attributes without polluting neutral attributes.

## Validation Commands

Required validation:

```sh
./eng/check.sh
```

If release readiness scripts exist:

```sh
./eng/release-check.sh 0.1.0-alpha
```

If package smoke tests are separated:

```sh
./eng/package-smoke.sh 0.1.0-alpha
```

If samples are supported:

```sh
./eng/samples.sh
```

The implementation report must state which commands were run and whether any were skipped with justification.

## Acceptance Criteria

The milestone is complete when:

- projection-neutral semantic attribute vocabulary is implemented and documented;
- attribute ownership between neutral and projection-specific packages is clear;
- .NET extraction maps attributes into the canonical `TypeSchemaModel`;
- compile-time generator handles annotated code and emits diagnostics for invalid usage;
- requiredness and nullability remain separate in extracted models;
- naming, descriptions, roles, constraints, enum metadata, keys, relationships, and custom annotations are supported or explicitly documented as unsupported;
- conflicts produce stable diagnostics;
- deterministic extraction tests pass;
- public API validation includes the intended attribute surface;
- public docs explain attribute usage from a consumer perspective;
- at least one runnable sample demonstrates annotation-driven extraction;
- package smoke tests cover attribute usage from packed packages where package smoke infrastructure exists;
- no non-root README files are introduced;
- `docs/MILESTONES.md` references this milestone document.

## Risks

### Attribute surface grows too quickly

Mitigation:

- keep neutral vocabulary small;
- prefer custom namespaced annotations for rare cases;
- keep projection-specific overrides in projection packages.

### EF Core needs leak into neutral attributes

Mitigation:

- require semantic intent first;
- use EF-specific attributes only for EF-specific overrides;
- map EF attributes to `efCore.*` annotations.

### Generator behavior diverges from runtime extraction

Mitigation:

- share extraction logic where practical;
- maintain equivalent fixture tests;
- document any unavoidable difference.

### Ambiguous requiredness/nullability

Mitigation:

- test nullable metadata, `required`, and explicit attributes together;
- keep canonical model fields separate;
- produce diagnostics for impossible combinations.

## Completion Report

When closing this milestone, report:

- attribute types added or changed;
- package ownership decisions;
- extraction behavior implemented;
- generator behavior implemented;
- diagnostics added;
- public docs updated;
- samples added or updated;
- package smoke coverage added;
- validation commands run;
- known limitations remaining for M0015 EF Core projection hardening.

## Authority

This document is authoritative for:

- M0014 milestone scope;
- M0014 deliverables;
- M0014 acceptance criteria;
- semantic type annotation usability implementation expectations.

This document is not authoritative for:

- permanent semantic type model behavior outside this milestone;
- long-term public API stability guarantees;
- EF Core projection behavior beyond annotation usability requirements;
- NuGet release policy.

## Document Contract

When this document changes, review and update:

- `docs/MILESTONES.md`
- related GitHub tracking issue;
- `docs/specs/semantic-type-annotations.md`
- `docs/PUBLIC-DOCS.md`
- `public-docs/guides/attribute-annotations.md`
- `public-docs/release-notes.md`
