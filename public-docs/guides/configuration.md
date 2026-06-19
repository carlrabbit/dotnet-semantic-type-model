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
3. Call `DeriveConfigurationModel()`.
4. Check model diagnostics.
5. Register options from the derived metadata or write the equivalent `AddOptions<T>().Bind(...).Validate...` code.

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

ConfigurationSemanticModel configurationModel = AppSemanticTypeModel.Create()
    .DeriveConfigurationModel();

configurationModel.Diagnostics.ThrowIfErrors();

// Runtime registration path supported by SemanticTypeModel.Configuration metadata.
builder.Services
    .AddOptions<ColdStorageOptions>()
    .Bind(builder.Configuration.GetSection(ColdStorageOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options =>
        options.Provider != ColdStorageProvider.File ||
        !string.IsNullOrWhiteSpace(options.TargetFilePath),
        "TargetFilePath is required when ColdStorage:Provider is File.")
    .ValidateOnStart();

// Planned generated-helper shape when SemanticTypeModel.Configuration.Generators emits it:
// builder.Services.AddColdStorageOptions(builder.Configuration);
```

## How it works

The configuration projection reads configuration-specific annotations and relevant core semantics from the generated `TypeSchemaModel`. It derives a `ConfigurationSemanticModel` containing options CLR types, section names, bind policy, validation flags, generated-helper intent, and conditional validation rules. The package does not choose configuration providers, load `appsettings.json`, or own secret management.

The generated helper, when available, must be equivalent to the explicit runtime registration: `AddOptions<ColdStorageOptions>()`, bind to `ColdStorage`, apply data-annotations validation, add the `RequiredWhen` validation delegate, and call `ValidateOnStart()`. Until the generator emits that helper in your build, keep the explicit runtime registration in application startup.

## Options and policies

| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
| Configuration options marker | No type is an options type by default | `SemanticTypeRole.Configuration` or configuration metadata | Makes the type a configuration options candidate | Non-options object types are ignored by default. |
| Section name | No default when section binding is required | `SemanticConfigurationSection("Name")` or equivalent metadata | Selects `IConfiguration.GetSection("Name")` for binding | Missing, empty, duplicate, or invalid section paths are diagnostic conditions. |
| Bind policy | `Section` | Current public behavior binds a named section; other policies are not documented as shipped | Determines the source section for `Bind(...)` | Unsupported bind policies must be diagnosed rather than silently ignored. |
| Named options | No named options | `configuration.namedOptions.name` metadata when present | Registers a named options instance | Duplicate or invalid names are reported by configuration diagnostics. |
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

## Limitations

Configuration projection is limited to options-registration metadata and equivalent registration behavior. It does not load configuration files, choose providers, validate arbitrary live configuration outside registered options, replace host startup code, or currently guarantee generated helpers from the placeholder generator package.

## Related docs

- [SemanticTypeModel.Configuration package](../nuget/SemanticTypeModel.Configuration.md)
- [SemanticTypeModel.Configuration.Generators package](../nuget/SemanticTypeModel.Configuration.Generators.md)
- [Packages](../packages.md)
