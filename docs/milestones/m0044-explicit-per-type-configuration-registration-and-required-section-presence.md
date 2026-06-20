# M0044: Explicit Per-Type Configuration Registration and Required Section Presence

## Status

Planned.

## Goal

Make Configuration registration explicit per options type and complete the runtime adapter that binds configuration, applies semantic validation, enforces required section presence, and optionally enables startup validation.

The primary consumer contract must be type-selective:

```csharp
builder.Services.AddSemanticOptions<ColdStorageOptions>(
    builder.Configuration,
    AppSemanticTypeModel.Create());
```

The application consciously selects each options type it uses. The library must not register every configuration type found in a solution-wide semantic model.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Post-2.2.0 public package set with Configuration domain model implemented |
| Capability-provider scope | The repository implements configuration semantics, selected-type derivation, Options registration, validation, diagnostics, generated convenience methods, tests, samples, and public docs. |
| Consumer/dogfood scope | Samples explicitly register only the configuration types required by the sample service. |

## Execution Mode

`ai-executed-human-reviewed`.

The design authority is clear, but human review is required because M0044 introduces new public APIs, changes the primary Configuration registration contract, adds runtime validation behavior, and may affect generated extension methods.

## Scope

### In Scope

- Add Configuration section-presence semantics with `Optional` and `Required` values.
- Keep `Optional` as the backwards-compatible default.
- Add selected-type Configuration derivation for a single options CLR type.
- Add the primary runtime adapter:

```csharp
OptionsBuilder<TOptions> AddSemanticOptions<TOptions>(
    IServiceCollection services,
    IConfiguration configuration,
    TypeSchemaModel model,
    Action<SemanticOptionsRegistration>? configure = null)
```

- Make registration explicit per options type.
- Bind only the selected options type.
- Apply required-section presence validation.
- Apply DataAnnotations validation when declared.
- Apply semantic validators such as `RequiredWhen`.
- Apply `ValidateOnStart` when declared or explicitly strengthened at the call site.
- Return `OptionsBuilder<TOptions>` for standard .NET Options composition.
- Add bounded call-site overrides for deployment-specific registration concerns.
- Add optional generated convenience methods that delegate to the runtime adapter.
- Keep full-model derivation for inspection/tooling only.
- Add diagnostics, tests, samples, package docs, usage guide updates, compatibility notes, and public API documentation for the new behavior.

### Out of Scope

```text
automatic registration of every configuration type in a semantic model
skip/exclude lists for model-wide registration
configuration provider setup
secret management
appsettings file generation
arbitrary validation expression parsing
removing semantic validators at the call site
changing property requiredness at the call site
provider-specific configuration behavior
release publication
NuGet publishing
workflow YAML changes unless required by package validation
copied guide documents
TBPs
issue templates
non-root README files
```

## Non-Goals

- Do not make complete-model registration the primary API.
- Do not require consumers to maintain exclusion lists.
- Do not duplicate binding and validation logic between runtime and source-generated implementations.
- Do not promote section presence to core semantics.
- Do not interpret a syntactically empty JSON object as a provider-independent proof that a section exists.
- Do not silently register configuration types merely because they exist in the semantic model.

## Locked Design Decisions

### Explicit Type Inclusion

The application selects each options type:

```csharp
builder.Services.AddSemanticOptions<ColdStorageOptions>(
    builder.Configuration,
    model);
```

Only `ColdStorageOptions` is registered. Other configuration types in `model` are ignored unless registered explicitly.

### Runtime Adapter Is Canonical

The runtime adapter owns binding and validation behavior. Generated convenience methods must delegate to it rather than reimplementing behavior.

### Full-Model Derivation Is Not the Primary Registration Path

`DeriveConfigurationModel()` may remain for inspection, tooling, documentation generation, and bulk analysis.

Application registration must use selected-type derivation.

### Section Presence Is Configuration-Specific

Use:

```csharp
public enum ConfigurationSectionPresence
{
    Optional,
    Required,
}
```

Reserved annotation key:

```text
configuration.section.presence
```

`Optional` is the default.

### Required Section Meaning

A required section exists when the effective `IConfiguration` subtree contains a value or at least one descendant value.

The implementation must not depend on an empty JSON object being distinguishable from an absent section across providers.

### Validation Timing

- Model/programming errors fail during derivation or registration.
- Deployed configuration errors fail through Options validation.
- `ValidateOnStart` determines whether deployed-value validation is forced during host startup.

## Focus Areas

### Focus Area 1 — Add Required Section Presence Semantics

#### Intent

Allow a configuration contract to state that effective configuration data must exist beneath its selected section path.

#### Implementation Requirements

- Add `ConfigurationSectionPresence` with `Optional` and `Required`.
- Add `configuration.section.presence` metadata.
- Add section presence to `ConfigurationType`.
- Add authoring support on the section attribute or equivalent cohesive Configuration attribute surface.
- Default to `Optional` when metadata is absent.
- Preserve section presence in inspection output.
- Add diagnostics for:
  - required presence without a section name;
  - required presence with root binding;
  - required presence when binding is disabled;
  - unsupported presence value;
  - conflicting declarations.

#### Validation

- Focused Configuration domain derivation tests.
- DotNet extraction tests.
- Generator metadata tests.
- Inspection snapshot tests.

### Focus Area 2 — Add Selected-Type Derivation

#### Intent

Derive one `ConfigurationType` from the solution-wide semantic model without deriving or registering every configuration type.

#### Required API Shape

```csharp
public static ConfigurationTypeResult DeriveConfigurationType<TOptions>(
    this TypeSchemaModel model);
```

Expected result shape:

```csharp
public sealed record ConfigurationTypeResult(
    ConfigurationType? Type,
    IReadOnlyList<SchemaDiagnostic> Diagnostics);
```

A non-generic internal/helper overload may resolve by canonical CLR type identity.

#### Implementation Requirements

- Resolve `TOptions` deterministically from canonical CLR metadata.
- Fail or emit error diagnostics for:
  - type not found;
  - ambiguous CLR type match;
  - type is not a Configuration type;
  - invalid Configuration metadata;
  - missing bindable section when required by policy.
- Reuse the same per-type derivation logic from `DeriveConfigurationModel()` to prevent behavioral drift.
- Do not derive unrelated Configuration types for the registration path.

#### Validation

- Generic selected-type derivation tests.
- Ambiguous/missing/non-configuration type tests.
- Full-model and selected-type parity tests for the selected type.

### Focus Area 3 — Implement Explicit Runtime Options Registration

#### Intent

Provide the primary consumer-facing API for consciously registering one options type.

#### Required API Shape

```csharp
public static OptionsBuilder<TOptions> AddSemanticOptions<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    TypeSchemaModel model,
    Action<SemanticOptionsRegistration>? configure = null)
    where TOptions : class;
```

#### Registration Override Shape

```csharp
public sealed class SemanticOptionsRegistration
{
    public string? Name { get; set; }
    public string? SectionName { get; set; }
    public bool? ValidateOnStart { get; set; }
    public ConfigurationSectionPresence? SectionPresence { get; set; }
}
```

Names may be refined during implementation, but the behavior and precedence must remain explicit.

#### Precedence

```text
explicit call-site override
  > Configuration domain metadata
  > documented default
```

#### Allowed Call-Site Overrides

```text
named-options name
section path
ValidateOnStart activation
section-presence strengthening
```

Call-site overrides must not remove semantic validators, redefine `RequiredWhen`, or change property requiredness/nullability.

#### Runtime Behavior

For the selected type:

1. derive `ConfigurationType`;
2. fail immediately for invalid model/programming setup;
3. resolve effective name and section path;
4. call `AddOptions<TOptions>(name)` or the unnamed equivalent;
5. bind the selected `IConfigurationSection`;
6. add section-presence validation when `Required`;
7. add DataAnnotations validation when enabled;
8. add semantic validators such as `RequiredWhen`;
9. add `ValidateOnStart` when enabled;
10. return `OptionsBuilder<TOptions>`.

#### Required Section Validation

Equivalent behavior:

```csharp
IConfigurationSection section = configuration.GetSection(sectionName);

builder.Validate(
    _ => section.Exists(),
    $"Configuration section '{sectionName}' is required.");
```

Equivalent provider-independent behavior is acceptable if it treats a section as present only when the effective subtree contains a value or descendant value.

#### Validation

- DI registration tests.
- Named-options tests.
- Required-section absent/present tests.
- Optional-section absent tests.
- DataAnnotations tests.
- `RequiredWhen` tests.
- `ValidateOnStart` integration tests.
- Multiple explicit registrations from one complete model.
- Proof that unrelated Configuration types are not registered.

### Focus Area 4 — Add Generated Convenience Methods

#### Intent

Provide optional ergonomic wrappers without introducing a second implementation of binding and validation behavior.

#### Required Behavior

A generated helper may look like:

```csharp
public static OptionsBuilder<ColdStorageOptions> AddColdStorageOptions(
    this IServiceCollection services,
    IConfiguration configuration)
{
    return services.AddSemanticOptions<ColdStorageOptions>(
        configuration,
        AppSemanticTypeModel.Create());
}
```

#### Implementation Requirements

- Generate helpers only for types that explicitly request generation.
- Delegate to `AddSemanticOptions<TOptions>`.
- Preserve deterministic names and collision diagnostics.
- Do not generate a method that registers every Configuration type.
- Keep generated methods optional convenience APIs.

#### Validation

- Generated-source snapshots.
- Generated helper compile tests.
- Generated helper behavior parity with direct runtime registration.
- Collision diagnostics.

### Focus Area 5 — Deprecate or Reframe Model-Wide Registration

#### Intent

Prevent model-wide registration from remaining the apparent primary consumer API.

#### Implementation Requirements

- Remove `AddSemanticConfigurationOptions(ConfigurationSemanticModel)` if it has no established shipped behavior and removal is compatible with current version policy; otherwise obsolete it and document it as inspection/tooling-only or unsupported for application registration.
- Remove package README and guide examples that suggest registering the complete Configuration model.
- Keep `DeriveConfigurationModel()` available for inspection/tooling unless there is a separate reason to remove it.
- Do not add skip/exclude filtering APIs.

#### Human Review Gate

Human review must decide removal versus obsoletion based on current shipped package compatibility.

### Focus Area 6 — Update Samples and Public Documentation

#### Intent

Teach explicit registration and required section presence accurately.

#### Required Documentation Changes

Update:

```text
public-docs/guides/configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.Generators.md
public-docs/packages.md
public-docs/samples.md
public-docs/release-notes.md
public-docs/api/compatibility.md
```

The Configuration guide must include:

- explicit registration of two selected options types from one complete model;
- proof that a third type remains unregistered;
- required versus optional section behavior;
- required section plus `ValidateOnStart`;
- call-site section override;
- named options;
- generated helper delegation;
- model/programming failures versus deployed configuration failures.

#### Validation

```sh
./eng/public-docs.sh
./eng/samples.sh
```

## Implementation Constraints

- Use the runtime adapter as the single behavior implementation.
- Do not add complete-model auto-registration as the primary API.
- Do not add exclusion or skip-list configuration.
- Keep section presence Configuration-specific.
- Preserve projection-neutral core semantics.
- Use canonical `eng/` scripts.
- Avoid opportunistic refactoring.
- Do not copy external guide documents.
- Do not add TBPs, issue templates, or non-root README files.
- Unsupported semantic validation must not be silently dropped.

## Required Authority Documents

Always read:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/specs/configuration-domain-model-and-options-projection.md
docs/specs/core-conditional-constraint-semantics.md
docs/decisions/configuration-domain-is-options-registration-projection.md
docs/decisions/configuration-registration-is-explicit-per-options-type.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

Read when modifying .NET authoring, extraction, or generation:

```text
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-compile-time-generator.md
src/SemanticTypeModel.DotNet/SemanticTypeAttributes.cs
src/SemanticTypeModel.DotNet/RoslynDotNetTypeExtractor.cs
```

Read when modifying Configuration runtime behavior:

```text
src/SemanticTypeModel.Configuration/
src/SemanticTypeModel.Configuration.Generators/
public-docs/guides/configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.Generators.md
```

Read when modifying public docs or samples:

```text
docs/PUBLIC-DOCS.md
docs/engineering/package-documentation.md
public-docs/api/compatibility.md
public-docs/release-notes.md
public-docs/samples/*.md
samples/
```

Do not treat `docs/research/` guide copies as operational authority.

## Files or Areas Likely Affected

```text
src/SemanticTypeModel.Configuration/
src/SemanticTypeModel.Configuration.Generators/
src/SemanticTypeModel.DotNet/
tests/unit/SemanticTypeModel.Configuration.Tests.Unit/
tests/unit/SemanticTypeModel.Configuration.Generators.Tests.Unit/
tests/unit/SemanticTypeModel.DotNet.Tests.Unit/
samples/
docs/specs/configuration-domain-model-and-options-projection.md
docs/decisions/configuration-registration-is-explicit-per-options-type.md
docs/TERMINOLOGY.md
docs/DECISIONS.md
docs/MILESTONES.md
public-docs/guides/configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.Generators.md
public-docs/packages.md
public-docs/samples.md
public-docs/release-notes.md
public-docs/api/compatibility.md
```

## Validation Tiers and Concrete Repository Commands

Use Tier 1 during implementation with the actual project paths confirmed from the repository:

```sh
./eng/test-filter.sh Configuration
./eng/test-project.sh tests/unit/SemanticTypeModel.Configuration.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Configuration.Generators.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit
```

Completion gate:

```sh
./eng/check.sh
./eng/package.sh 0.0.0-m0044
./eng/package-smoke.sh 0.0.0-m0044
./eng/samples.sh
./eng/public-docs.sh
```

Do not publish packages.

## Acceptance Criteria

- `ConfigurationSectionPresence.Optional` and `.Required` exist.
- `Optional` is the default.
- Required section presence is represented in Configuration metadata and `ConfigurationType`.
- Required section validation uses effective Configuration data, not empty-object token assumptions.
- `DeriveConfigurationType<TOptions>()` derives exactly one selected Configuration type.
- The primary registration API is `AddSemanticOptions<TOptions>(...)` or an equivalent explicitly type-selective API.
- Registration returns `OptionsBuilder<TOptions>`.
- The application explicitly selects each registered options type.
- Unselected Configuration types in the same semantic model are not registered.
- Runtime registration binds the selected section and applies declared validation behavior.
- Required section, DataAnnotations, `RequiredWhen`, named options, and `ValidateOnStart` have focused tests.
- Call-site overrides are limited to deployment-specific concerns and follow documented precedence.
- Generated convenience methods delegate to the runtime adapter.
- No generated or runtime API auto-registers every Configuration type by default.
- Model/programming errors fail during derivation/registration.
- Deployed configuration errors fail through Options validation.
- Configuration package docs and guide show explicit type selection.
- Tier 2, package, smoke, sample, and public-doc validation pass.

## Direct Documentation Impact

Implementation must update:

```text
docs/specs/configuration-domain-model-and-options-projection.md
docs/decisions/configuration-registration-is-explicit-per-options-type.md
docs/TERMINOLOGY.md
docs/DECISIONS.md
docs/MILESTONES.md
public-docs/guides/configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.Generators.md
public-docs/packages.md
public-docs/samples.md
public-docs/release-notes.md
public-docs/api/compatibility.md
```

## Deferred Documentation Synchronization Hints

A deferred documentation-sync hint is included at:

```text
.guide-sync/pending/m0044-explicit-configuration-registration-and-section-presence.md
```

Ordinary implementation agents do not need to read `.guide-sync/`.

## Human Review Requirements

Human review is required for:

- the final public names of selected-type derivation and registration APIs;
- removal versus obsoletion of model-wide registration;
- call-site override scope and precedence;
- section-existence semantics across Configuration providers;
- generated helper API shape;
- named-options behavior;
- public API and compatibility implications;
- sample and guide accuracy.

## Out-of-Scope Guide Migration Work

M0044 is not a guide migration.

Do not read the external guide repository during implementation. Do not copy guide documents into the repository. Do not make target repository docs reference guide documents as operational authority.
