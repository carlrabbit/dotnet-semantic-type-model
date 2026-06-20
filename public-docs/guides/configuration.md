# Configuration Options Projection

## Goal

Derive Microsoft.Extensions.Options registrations from configuration semantics without moving provider setup or configuration-file loading into SemanticTypeModel.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.2.0`.

## Packages

- `SemanticTypeModel.Configuration` for configuration model derivation and runtime options registration metadata.
- `SemanticTypeModel.Configuration.Generators` for planned deterministic registration helpers. In the current package this project is a placeholder, so use runtime registration unless your build includes a generated helper.
- `Microsoft.Extensions.Options`, `Microsoft.Extensions.Configuration`, and `Microsoft.Extensions.DependencyInjection` for host-owned registration.

## Minimal path

1. Mark options types with configuration annotations.
2. Generate the semantic model.
3. Call `services.AddSemanticOptions<TOptions>(configuration, model)` once for each options type used by the service.
4. Use `DeriveConfigurationModel()` only for inspection, tooling, or bulk analysis.
5. Check diagnostics and let Options validation report deployed configuration errors.

## Full example

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemanticTypeModel.Configuration;
using SemanticTypeModel.DotNet;

public enum ColdStorageProvider
{
    Blob,
    File,
}

[SemanticRole(SemanticTypeRole.Configuration)]
[SemanticConfigurationSection(SectionName)]
[SemanticValidateDataAnnotations]
[SemanticValidateOnStart]
[SemanticGenerateOptionsRegistration(ExtensionMethodName = "AddColdStorageOptions")]
public sealed class ColdStorageOptions
{
    public const string SectionName = "ColdStorage";

    [Required]
    public ColdStorageProvider Provider { get; init; }

    [SemanticRequiredWhen(nameof(Provider), nameof(ColdStorageProvider.File),
        Message = "TargetFilePath is required when ColdStorage:Provider is File.")]
    public string? TargetFilePath { get; init; }
}

TypeSchemaModel model = AppSemanticTypeModel.Create();

OptionsBuilder<ColdStorageOptions> options = builder.Services.AddSemanticOptions<ColdStorageOptions>(
    builder.Configuration,
    model);

// Register a second selected type from the same complete model when this service uses it.
builder.Services.AddSemanticOptions<QueueOptions>(builder.Configuration, model);

// A third Configuration type in the model remains unregistered until the application calls
// AddSemanticOptions<ThatType>(builder.Configuration, model).

// Generated helpers, when explicitly requested, delegate to the same runtime adapter:
// builder.Services.AddColdStorageOptions(builder.Configuration);
```

## How it works

The configuration projection reads configuration-specific annotations and relevant core semantics from the generated `TypeSchemaModel`. It derives a `ConfigurationSemanticModel` containing options CLR types, section names, bind policy, validation flags, generated-helper intent, and conditional validation rules. The package does not choose configuration providers, load `appsettings.json`, or own secret management.

`AddSemanticOptions<TOptions>` derives only the selected `TOptions`, binds the selected section, applies required-section validation when configured, applies DataAnnotations and `RequiredWhen`, enables `ValidateOnStart` when configured, and returns `OptionsBuilder<TOptions>` for normal Options composition. Generated helpers, when available, must delegate to `AddSemanticOptions<TOptions>` rather than reimplementing binding or validation.

## Options and policies

| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
| Configuration options marker | No type is an options type by default | `SemanticTypeRole.Configuration` or configuration metadata | Makes the type a configuration options candidate | Non-options object types are ignored by default. |
| Section name | No default when section binding is required | `SemanticConfigurationSection("Name")` or equivalent metadata; call-site section override | Selects `IConfiguration.GetSection("Name")` for binding | Missing, empty, duplicate, or invalid section paths are diagnostic conditions. |
| Section presence | `Optional` | `ConfigurationSectionPresence.Optional` or `Required`; `configuration.section.presence`; call site may strengthen Optional to Required | Required presence validates that effective configuration data exists beneath the selected section | Required presence without a section name, with root binding, or with disabled binding is a diagnostic/registration error. |
| Bind policy | `Section` | Current public behavior binds a named section; other policies are not documented as shipped | Determines the source section for `Bind(...)` | Unsupported bind policies must be diagnosed rather than silently ignored. |
| Named options | No named options | `configuration.namedOptions.name` metadata or call-site name override | Registers a named options instance | Duplicate or invalid names are reported by configuration diagnostics. |
| Data annotations validation | Disabled | `SemanticValidateDataAnnotations` | Adds `ValidateDataAnnotations()` to equivalent registration | Unsupported validation metadata remains a diagnostic or must be handled manually. |
| Validate on start | Disabled | `SemanticValidateOnStart` | Adds `ValidateOnStart()` to equivalent registration | `ValidateOnStart` without a bindable options type is diagnostic. |
| Generated extension method | Planned; not emitted by the placeholder generator package | `SemanticGenerateOptionsRegistration`, optional `ExtensionMethodName` | Requests a helper such as `AddColdStorageOptions` | Treat the helper as planned unless generated code exists in your build; collisions are diagnostic. |
| Conditional requiredness | No conditional rule | `SemanticRequiredWhen(sourceProperty, value)` with equality comparison | Adds a validation rule that requires the target property when the source equals the literal | Unresolved source property, unsupported operator, or incompatible literal uses STM1020-STM1024 style diagnostics. |

## Diagnostics

| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
| Missing section diagnostic | Options type has configuration role but no section metadata | Add `SemanticConfigurationSection` or configure explicit metadata before binding. |
| Duplicate section diagnostic | Two options types project to the same section unintentionally | Use distinct section names or intentional named options. |
| `RequiredWhen` source unresolved | The source member name does not match a property on the options type | Use `nameof(SourceProperty)` and rebuild the generated model. |
| Unsupported generated output | The guide example assumes a generated helper but the generator package did not emit it | Use explicit runtime registration and keep the generated-helper call commented until generated. |
| Validate-on-start failure at startup | Bound configuration violates `[Required]` or conditional rules | Fix the configuration value or remove the startup-validation policy. |

## Common mistakes

- Calling a planned generated helper such as `AddColdStorageOptions` when no generated source file exists in the build.
- Forgetting that `ColdStorageOptions.SectionName` is only a CLR constant unless configuration metadata points to the same value.
- Treating `RequiredWhen` as a JSON Schema-only rule; Configuration uses it as options validation.
- Expecting SemanticTypeModel to add configuration providers or read secrets.
- Calling model-wide registration for application startup instead of explicitly selecting each options type.
- Expecting an unselected Configuration type in the complete model to be registered automatically.

## Limitations

Configuration projection is limited to options-registration metadata and equivalent registration behavior. It does not load configuration files, choose providers, validate arbitrary live configuration outside registered options, replace host startup code, or currently guarantee generated helpers from the placeholder generator package.

## Related docs

- [SemanticTypeModel.Configuration package](../nuget/SemanticTypeModel.Configuration.md)
- [SemanticTypeModel.Configuration.Generators package](../nuget/SemanticTypeModel.Configuration.Generators.md)
- [Packages](../packages.md)
