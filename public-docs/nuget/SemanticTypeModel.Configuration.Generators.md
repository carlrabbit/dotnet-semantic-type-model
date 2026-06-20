# SemanticTypeModel.Configuration.Generators

## What this package does

`SemanticTypeModel.Configuration.Generators` provides source-generator support for deterministic configuration options registration helpers that delegate to the runtime `AddSemanticOptions<TOptions>` adapter.

## Install

```sh
dotnet add package SemanticTypeModel.Configuration.Generators --version 2.2.0
```

## Use when

- Install this package when you need source-generator support for deterministic configuration options registration helpers that delegate to the runtime `AddSemanticOptions<TOptions>` adapter.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.
- Generate optional convenience methods only when a configuration type explicitly requests them.

## Minimal example

```csharp
<ItemGroup>
  <PackageReference Include="SemanticTypeModel.Configuration.Generators" Version="2.2.0" PrivateAssets="all" />
</ItemGroup>

// Build the project to emit explicitly requested helpers such as:
// services.AddColdStorageOptions(configuration);
// The generated helper delegates to services.AddSemanticOptions<ColdStorageOptions>(configuration, AppSemanticTypeModel.Create()).
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

- It does not define configuration semantics, bind options without the runtime configuration package, register every Configuration type automatically, or maintain skip/exclude lists.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [Configuration guide](../guides/configuration.md)
- [Getting started](../getting-started.md)
