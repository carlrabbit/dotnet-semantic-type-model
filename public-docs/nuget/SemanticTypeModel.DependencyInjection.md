# SemanticTypeModel.DependencyInjection

## What this package does

`SemanticTypeModel.DependencyInjection` provides Microsoft.Extensions.DependencyInjection registration helpers for semantic model providers and projection services.

## Install

```sh
dotnet add package SemanticTypeModel.DependencyInjection --version 2.3.0
```

## Use when

- Install this package when you need Microsoft.Extensions.DependencyInjection registration helpers for semantic model providers and projection services.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.

## Minimal example

```csharp
using Microsoft.Extensions.DependencyInjection;
using SemanticTypeModel.DependencyInjection;

var services = new ServiceCollection();
services.AddSemanticTypeModelProvider(AppSemanticTypeModel.Create);
```

## Main APIs

| API | Purpose |
| --- | --- |
| `AddSemanticTypeModelProvider` | Registers a model provider. |
| `Projection registrations` | Register target services supplied by projection packages. |
| `IServiceCollection integration` | Uses standard Microsoft dependency injection. |

## Works with

- SemanticTypeModel.Core and whichever projection package your app registers.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not discover assemblies automatically or install every projection package transitively.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [Runtime DI sample](../samples/runtime-di.md)
