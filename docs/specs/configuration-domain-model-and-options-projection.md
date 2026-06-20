# Configuration Domain Model and Options Projection Specification

## Status

Authoritative behavioral specification.

## Purpose

Define the Configuration domain semantic model, selected-type derivation, required section presence, and Microsoft.Extensions.Options registration projection.

This specification is authoritative for configuration-domain derivation, options type discovery, configuration sections, section presence, bind policies, options validation, `ValidateOnStart`, explicit per-type registration, generated registration helpers, diagnostics, and inspection.

## Core Principle

Configuration is a domain projection.

The canonical semantic model describes projection-neutral meaning. The Configuration package derives Configuration domain information and projects one explicitly selected options type into Microsoft.Extensions.Options registration behavior.

The Configuration package must not own application configuration sources. Host applications own configuration provider setup, environment layering, secret management, deployment-specific section paths, and the decision about which options types a service registers.

## Package Boundary

```text
SemanticTypeModel.Configuration
SemanticTypeModel.Configuration.Generators
```

`SemanticTypeModel.Configuration` owns domain derivation, selected-type registration, validation, inspection, and diagnostics.

`SemanticTypeModel.Configuration.Generators` owns optional generated consumer glue that delegates to the runtime registration adapter.

## Configuration Domain Model

```text
ConfigurationSemanticModel
  ConfigurationTypes
    ConfigurationType
      OptionsClrType
      Section
      SectionPresence
      BindPolicy
      NamedOptionsName
      ValidationModel
      StartupValidationPolicy
      GeneratedRegistrationPolicy
      Properties
      Diagnostics
```

The model must be deterministic and inspectable.

## Configuration-Specific Metadata

Reserved metadata keys:

```text
configuration.options
configuration.section
configuration.section.name
configuration.section.presence
configuration.bind
configuration.bind.policy
configuration.namedOptions
configuration.namedOptions.name
configuration.validateDataAnnotations
configuration.validateOnStart
configuration.registration.generateExtensionMethod
configuration.registration.extensionMethodName
```

These keys are Configuration-specific and must not force JSON Schema, EF Core, Power BI, or System.Text.Json behavior.

## Section Presence

```csharp
public enum ConfigurationSectionPresence
{
    Optional,
    Required,
}
```

`Optional` is the default.

A required section exists when the effective `IConfiguration` subtree contains a value or at least one descendant value.

The implementation must not depend on a syntactically empty JSON object being preserved as a meaningful node by all Configuration providers.

Section presence is distinct from:

- section name;
- property requiredness;
- nullability;
- `RequiredWhen`;
- DataAnnotations validation;
- `ValidateOnStart`.

## Options Type Discovery

A type is a Configuration type when it has the core `Configuration` role or explicit Configuration metadata.

Discovery in a complete model does not imply application registration.

The consuming application must explicitly select each options type it registers.

## Selected-Type Derivation

The Configuration package must expose selected-type derivation equivalent to:

```csharp
ConfigurationTypeResult DeriveConfigurationType<TOptions>(
    this TypeSchemaModel model);
```

The result must contain the selected `ConfigurationType` and diagnostics.

Selected-type derivation must not derive unrelated Configuration types as a prerequisite for registration.

`DeriveConfigurationModel()` may remain for inspection, tooling, and bulk analysis.

## Primary Options Registration Projection

The primary consumer API must be explicitly type-selective:

```csharp
OptionsBuilder<TOptions> AddSemanticOptions<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    TypeSchemaModel model,
    Action<SemanticOptionsRegistration>? configure = null)
    where TOptions : class;
```

Equivalent naming is acceptable if the API preserves explicit per-type selection.

The method must return `OptionsBuilder<TOptions>`.

## Registration Precedence

```text
explicit call-site override
  > Configuration domain metadata
  > documented default
```

Permitted deployment-specific overrides:

```text
named-options name
section path
ValidateOnStart activation
section-presence strengthening
```

The call site must not remove semantic validators, redefine `RequiredWhen`, or change property requiredness/nullability.

## Runtime Registration Behavior

For the selected type, the runtime adapter must:

1. derive exactly one Configuration type;
2. fail for invalid model/programming setup;
3. resolve effective name and section path;
4. register named or unnamed options;
5. bind the selected section;
6. add required-section validation when configured;
7. add DataAnnotations validation when configured;
8. add semantic validators such as `RequiredWhen`;
9. enable `ValidateOnStart` when configured;
10. return `OptionsBuilder<TOptions>`.

## Required Section Validation

Equivalent behavior:

```csharp
IConfigurationSection section = configuration.GetSection(sectionName);

builder.Validate(
    _ => section.Exists(),
    $"Configuration section '{sectionName}' is required.");
```

Equivalent provider-independent behavior is acceptable when it tests effective values or descendants.

With `ValidateOnStart`, missing required section data must fail host startup.

Without `ValidateOnStart`, failure occurs when the options instance is validated or resolved.

## Generated Registration Helpers

Generated helpers are optional convenience APIs.

They must delegate to the runtime adapter:

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

Generated helpers must be emitted only when explicitly requested by type metadata or generator policy.

No generated helper may auto-register every Configuration type by default.

## Model-Wide Registration

Complete-model registration is not the primary application API.

The package must not require exclusion or skip lists to select a service-specific subset.

Any pre-existing model-wide registration API must be removed, obsoleted, or clearly limited to non-application tooling based on compatibility review.

## Failure Semantics

Registration-time failures:

```text
TOptions not found in model
ambiguous CLR type match
TOptions is not a Configuration type
missing required section name
unsupported bind policy
invalid semantic validation metadata
required section with root binding
```

Options-validation failures:

```text
required section has no effective data
required property validation fails
DataAnnotations validation fails
RequiredWhen validation fails
```

## Diagnostics

The Configuration package must diagnose:

- missing or ambiguous selected CLR type;
- selected type is not a Configuration type;
- required section presence without a section name;
- required section presence with root binding;
- required section presence when binding is disabled;
- unsupported section-presence value;
- conflicting presence declarations;
- invalid named-options metadata;
- invalid call-site override;
- unsupported bind policy;
- invalid `RequiredWhen` metadata;
- generated helper collision;
- generated helper unable to delegate safely to runtime registration.

## Invariants

- Application registration is explicit per options type.
- Unselected Configuration types are not registered.
- Runtime registration is the canonical behavior implementation.
- Generated helpers delegate to runtime registration.
- Configuration semantics do not load providers or manage secrets.
- Section presence is Configuration-specific.
- Unsupported validation is not silently dropped.
- Full-model derivation does not imply full-model registration.

## Public Documentation Expectations

Public docs must explain:

- how to mark an options type as Configuration;
- how to declare section name and section presence;
- how to register one selected options type;
- how to register multiple selected types from one complete model;
- how named options and call-site section overrides work;
- how required section validation interacts with `ValidateOnStart`;
- how generated helpers delegate to runtime registration;
- why other Configuration types in the model remain unregistered.
