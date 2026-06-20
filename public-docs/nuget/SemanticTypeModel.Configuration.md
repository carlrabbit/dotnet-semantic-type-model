# SemanticTypeModel.Configuration

## What this package does

`SemanticTypeModel.Configuration` derives an inspectable Configuration domain model from a `TypeSchemaModel` and registers one explicitly selected options type with Microsoft.Extensions.Options.

## Install

```sh
dotnet add package SemanticTypeModel.Configuration --version 2.3.0
```

## Minimal example

```csharp
using SemanticTypeModel.Configuration;

OptionsBuilder<ColdStorageOptions> builder = services.AddSemanticOptions<ColdStorageOptions>(
    configuration,
    AppSemanticTypeModel.Create());
```

The application chooses each options type it uses. A complete semantic model can contain `ColdStorageOptions`, `QueueOptions`, and `UnusedOptions`, but only calls to `AddSemanticOptions<TOptions>` create options registrations for the selected `TOptions`.

## Main APIs

| API | Purpose |
| --- | --- |
| `DeriveConfigurationType<TOptions>` | Derives one selected `ConfigurationType` and diagnostics from a complete semantic model. |
| `AddSemanticOptions<TOptions>` | Binds and validates one selected options type and returns `OptionsBuilder<TOptions>`. |
| `SemanticOptionsRegistration` | Allows deployment-specific overrides for options name, section path, `ValidateOnStart`, and section-presence strengthening. |
| `ConfigurationSectionPresence` | Declares whether effective data under the selected section is optional or required. |
| `DeriveConfigurationModel` | Derives the complete Configuration domain model for inspection, tooling, and bulk analysis. |
| `ConfigurationSemanticModel.Inspect` | Produces deterministic inspection text, including section presence. |

`AddSemanticConfigurationOptions(ConfigurationSemanticModel)` is obsolete. Use explicit per-type registration with `AddSemanticOptions<TOptions>` for application startup.

## Validation behavior

`AddSemanticOptions<TOptions>` fails during registration for model or programming errors such as a missing selected type, ambiguous CLR type match, non-Configuration selected type, missing section path, root required section binding, or unsupported bind policy.

Deployed configuration errors fail through Options validation. Required section presence, DataAnnotations, and `RequiredWhen` rules are attached to the selected options registration; enabling `ValidateOnStart` moves those validation failures to host startup.

## Does not do

- It does not register every Configuration type in a complete model.
- It does not provide skip or exclusion lists because selected-type registration is explicit.
- It does not read configuration files, own provider setup, or manage secrets.
- It does not treat an empty JSON object as provider-independent proof that a section exists.

## More documentation

- [Configuration guide](../guides/configuration.md)
- [Compatibility](../api/compatibility.md)
- [SemanticTypeModel.Configuration.Generators](SemanticTypeModel.Configuration.Generators.md)
