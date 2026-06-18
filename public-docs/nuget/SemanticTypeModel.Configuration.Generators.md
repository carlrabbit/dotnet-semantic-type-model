# SemanticTypeModel.Configuration.Generators

## What this package does

`SemanticTypeModel.Configuration.Generators` provides source-generator support for deterministic configuration options registration helpers.

## Install

```sh
dotnet add package SemanticTypeModel.Configuration.Generators --version 2.2.0
```

## Use when

- Install this package when you need source-generator support for deterministic configuration options registration helpers.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.
- Derive Microsoft.Extensions.Options registrations from configuration semantics.

## Minimal example

```csharp
<ItemGroup>
  <PackageReference Include="SemanticTypeModel.Configuration.Generators" Version="2.2.0" PrivateAssets="all" />
</ItemGroup>

// Build the project to emit deterministic options registration helpers.
```

## Main APIs

| API | Purpose |
| --- | --- |
| `Generated registration helpers` | Compile-time helper surface for options registration. |
| `MSBuild integration` | Runs with the project build. |
| `PrivateAssets=all` | Keeps generator package from flowing transitively. |

## Works with

- SemanticTypeModel.Configuration and generated semantic model providers.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not define configuration semantics or bind options without the runtime configuration package.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [Configuration guide](../guides/configuration.md)
- [Getting started](../getting-started.md)
