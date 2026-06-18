# Configuration Options Projection

## Goal

Derive Microsoft.Extensions.Options registrations from configuration semantics without moving provider setup or configuration-file loading into SemanticTypeModel.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.2.0`.

## Packages

- `SemanticTypeModel.Configuration` for configuration model derivation and options registration.
- `SemanticTypeModel.Configuration.Generators` when deterministic generated registration helpers are desired.
- `Microsoft.Extensions.Options` and `Microsoft.Extensions.DependencyInjection` for runtime options registration.

## Minimal path

1. Mark configuration option types with configuration annotations.
2. Generate the semantic model.
3. Call `DeriveConfigurationModel()`.
4. Check model diagnostics.
5. Call `services.AddSemanticConfigurationOptions(configurationModel)` and configure providers separately.

## Full example

```csharp
using Microsoft.Extensions.DependencyInjection;
using SemanticTypeModel.Configuration;

ConfigurationSemanticModel configuration = AppSemanticTypeModel.Create()
    .DeriveConfigurationModel();

foreach (var diagnostic in configuration.Diagnostics)
{
    Console.Error.WriteLine(diagnostic.Message);
}

services.AddSemanticConfigurationOptions(configuration);
```

## How it works

Annotated .NET code is extracted by the generator into a `TypeSchemaModel`. Core transformations normalize projection-neutral semantics. The target package derives a domain semantic model and then exports or applies target-specific output when that target supports it.

## Options and policies

Configuration annotations can describe section names, binding policy, named options, data-annotations validation, validate-on-start behavior, generated extension-method intent, and conditional requiredness. Application code still owns configuration providers and host setup.

## Diagnostics

Configuration diagnostics report missing section names, unresolved conditional-required source properties, invalid required-when operators, duplicate generated registration names, and option types that cannot be projected safely.

## Common mistakes

- Treating JSON Schema files as the canonical authoring source for new models.
- Mixing target-specific metadata with projection-neutral semantics.
- Skipping diagnostic inspection before using projected output.
- Using stale pre-2.2 model namespace or shape names in current examples.

## Limitations

Configuration projection is limited to options-registration metadata and helper generation. It does not load `appsettings.json`, choose providers, validate arbitrary live configuration outside registered options, or replace application startup code.

## Related docs

- [SemanticTypeModel.Configuration package](../nuget/SemanticTypeModel.Configuration.md)
- [SemanticTypeModel.Configuration.Generators package](../nuget/SemanticTypeModel.Configuration.Generators.md)
- [Packages](../packages.md)
