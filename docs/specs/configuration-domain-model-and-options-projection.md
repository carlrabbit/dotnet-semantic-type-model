# Configuration Domain Model and Options Projection Specification

## Status

Authoritative behavioral specification.

## Purpose

Define the Configuration domain semantic model and the Microsoft.Extensions.Options registration projection.

This specification is authoritative for configuration-domain derivation, options type discovery, configuration sections, bind policies, options validation, `ValidateOnStart`, generated registration helpers, diagnostics, and inspection.

## Core Principle

Configuration is a domain projection.

The canonical semantic model describes projection-neutral meaning. The Configuration package derives a Configuration Domain Model and then projects it into Microsoft.Extensions.Options registration behavior or generated registration helpers.

The Configuration package must not own application configuration sources. Host applications still own configuration provider setup, environment layering, secret management, and deployment-specific configuration loading.

## Package Boundary

Expected packages:

```text
SemanticTypeModel.Configuration
SemanticTypeModel.Configuration.Generators
```

`SemanticTypeModel.Configuration` owns domain derivation, inspection, diagnostics, and runtime application of a Configuration Domain Model.

`SemanticTypeModel.Configuration.Generators` owns generated consumer glue such as `IServiceCollection` extension methods.

## Configuration Domain Model

A Configuration Domain Model contains:

```text
ConfigurationSemanticModel
  ConfigurationTypes
    ConfigurationType
      OptionsClrType
      Section
      BindPolicy
      ValidationModel
      StartupValidationPolicy
      Properties
      Diagnostics
```

The model must be deterministic and inspectable.

## Core Semantics Consumed by Configuration

| Core semantic | Configuration interpretation |
|---|---|
| `Configuration` role | Candidate options type and configuration contract root. |
| `Required` | Required options value or required section/member. |
| `Nullable` | Null is valid when present; do not conflate with optional presence. |
| `Constraint` | Options validation rule when representable. |
| `RequiredWhen` | Conditional options validation rule. |
| `Enumeration` | Allowed configuration values and validation operands. |
| `Format` | Validation/documentation hint when supported. |
| `Description` / `DisplayName` | Generated docs, diagnostics, and sample appsettings metadata. |
| `Category` / `Order` | Generated docs and deterministic example ordering. |
| `Ownership` / `OwnedObject` / `OwnedCollection` | Nested section/object binding and recursive validation candidates. |
| `Version` / `Revision` / `CurrentVersion` | Configuration contract version fields; no automatic migration behavior. |
| `LifecycleState` | Status/configuration-state value; no workflow behavior. |
| `ExtensionData` | Unknown configuration key preservation/allowance policy when supported. |

## Configuration-Specific Semantics

Configuration-specific semantics are not core semantics unless a separate core spec promotes them.

Reserved configuration metadata keys:

```text
configuration.options
configuration.section
configuration.section.name
configuration.bind
configuration.bind.policy
configuration.namedOptions
configuration.namedOptions.name
configuration.validateDataAnnotations
configuration.validateOnStart
configuration.registration.generateExtensionMethod
configuration.registration.extensionMethodName
```

These keys describe Options/configuration behavior and must not force JSON Schema, EF Core, Power BI, or System.Text.Json to generate target behavior.

## Options Type Discovery

A type may be treated as a configuration options type when one of these conditions holds:

- the type has the core `Configuration` role and configuration projection is enabled;
- the type carries configuration-specific options metadata;
- the type is explicitly included by Configuration derivation options.

The Configuration domain must not treat every object type as an options type by default.

## Section Name Resolution

Section name resolution order:

1. Explicit configuration metadata such as `configuration.section.name`.
2. A configured static member lookup policy, if selected by Configuration derivation options.
3. Naming convention policy, if explicitly enabled.
4. Diagnostic when no section can be resolved and section binding is required.

A `SectionName` constant is a supported source only when explicit policy enables static member lookup or explicit metadata points to it.

## Validation Model

The Configuration domain validation model includes data-annotation validation when enabled, core requiredness/nullability validation when representable, projection-neutral constraints when representable, conditional constraints such as `RequiredWhen`, and explicit startup validation policy.

`ValidateDataAnnotations` and `ValidateOnStart` are configuration-domain policies. They are not core semantics.

## Options Registration Projection

The primary consumer output is generated or applied Options registration equivalent to:

```csharp
builder.Services
    .AddOptions<ColdStorageOptions>()
    .Bind(builder.Configuration.GetSection(ColdStorageOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options =>
    {
        return options.Provider != ColdStorageProvider.File
               || !string.IsNullOrWhiteSpace(options.TargetFilePath);
    }, "TargetFilePath is required when ColdStorage:Provider is File.")
    .ValidateOnStart();
```

Generated helper code may reduce consumer code to:

```csharp
builder.Services.AddColdStorageOptions(builder.Configuration);
```

Generated code must be deterministic and must not hide diagnostics.

## ColdStorage Baseline Scenario

M0040 must include a Cold Storage sample or test scenario equivalent to:

```csharp
[SemanticRole(SemanticTypeRole.Configuration)]
[SemanticConfigurationSection("ColdStorage")]
[SemanticValidateOnStart]
public sealed class ColdStorageOptions
{
    public const string SectionName = "ColdStorage";

    public ColdStorageProvider Provider { get; init; }

    [SemanticRequiredWhen(nameof(Provider), "File")]
    public string? TargetFilePath { get; init; }
}
```

The expected generated registration must apply conditional validation requiring `TargetFilePath` when `Provider` is `File`.

## Diagnostics

The Configuration package must emit diagnostics for missing section names, duplicate sections, invalid section paths, invalid section-name member references, unsupported metadata targets, invalid `RequiredWhen` references, incompatible comparison values, unsupported generated output, `ValidateOnStart` without bindable options, and generated method collisions.

## Invariants

- Configuration semantics must not load configuration providers.
- Configuration semantics must not own secret-management behavior.
- Configuration semantics must not publish or mutate appsettings files as the primary feature.
- Options registration projection must be deterministic.
- Unsupported validation rules must not be silently dropped.
- Configuration-specific metadata must not be interpreted as core semantics.
- Core semantics consumed by Configuration must remain available to other domain projections.

## Public Documentation Expectations

Public docs must explain how to mark a type as configuration options, specify a section, use generated helpers, understand core semantic effects, and understand what the package does not own.
