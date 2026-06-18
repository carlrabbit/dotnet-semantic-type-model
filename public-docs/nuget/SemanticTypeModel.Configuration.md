# SemanticTypeModel.Configuration

## What this package does

`SemanticTypeModel.Configuration` provides configuration options domain-model derivation and Microsoft.Extensions.Options registration projection.

## Install

```sh
dotnet add package SemanticTypeModel.Configuration --version 2.2.0
```

## Use when

- Install this package when you need configuration options domain-model derivation and Microsoft.Extensions.Options registration projection.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.
- Derive Microsoft.Extensions.Options registrations from configuration semantics.

## Minimal example

```csharp
using SemanticTypeModel.Configuration;

ConfigurationSemanticModel configuration = AppSemanticTypeModel.Create()
    .DeriveConfigurationModel();
services.AddSemanticConfigurationOptions(configuration);
```

## Main APIs

| API | Purpose |
| --- | --- |
| `DeriveConfigurationModel` | Derives configuration options metadata. |
| `AddSemanticConfigurationOptions` | Registers options according to derived metadata. |
| `ConfigurationAnnotationKeys` | Names for configuration annotations. |
| `ConfigurationSemanticModel.Inspect` | Produces deterministic inspection text. |

## Works with

- Microsoft.Extensions.Options, Microsoft.Extensions.DependencyInjection, and generated model providers.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not read configuration files, own provider setup, or validate arbitrary runtime configuration outside the projected options registrations.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [Configuration guide](../guides/configuration.md)
