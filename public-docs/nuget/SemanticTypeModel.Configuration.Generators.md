# SemanticTypeModel.Configuration.Generators

## What this package does

`SemanticTypeModel.Configuration.Generators` is the reserved package for deterministic configuration options registration helpers. The 2.3.0 package is present for package-inventory and documentation alignment; use runtime `AddSemanticOptions<TOptions>` registration unless your build contains generated helper output.

## Install

```sh
dotnet add package SemanticTypeModel.Configuration.Generators --version 2.3.0
```

## Use when

- Install this package only when you are validating the reserved generator package or a build that includes generated helper output.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.
- Treat generated convenience methods as optional wrappers that must delegate to the runtime `AddSemanticOptions<TOptions>` adapter.

## Minimal example

```csharp
<ItemGroup>
  <PackageReference Include="SemanticTypeModel.Configuration.Generators" Version="2.3.0" PrivateAssets="all" />
</ItemGroup>

// In 2.3.0, prefer the runtime adapter unless generated helper output is present:
// services.AddSemanticOptions<ColdStorageOptions>(configuration, AppSemanticTypeModel.Create());
// Any generated helper must delegate to the same runtime adapter.
```

## Main APIs

| API | Purpose |
| --- | --- |
| `Reserved generator package` | Package identity reserved for generated options-registration helpers. |
| `Generated registration helpers` | Optional compile-time helper surface when generated output is present in a build. |
| `PrivateAssets=all` | Keeps generator package from flowing transitively. |

## Works with

- SemanticTypeModel.Configuration and generated semantic model providers when helper output is present.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- In 2.3.0 it does not by itself emit helper code in package smoke coverage; use the runtime adapter unless generated output is present.
- It does not define configuration semantics, bind options without the runtime configuration package, register every Configuration type automatically, or maintain skip/exclude lists.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [Configuration guide](../guides/configuration.md)
- [Getting started](../getting-started.md)
