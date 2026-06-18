# SemanticTypeModel.Generators

## What this package does

`SemanticTypeModel.Generators` provides incremental source generation for compile-time semantic model providers from annotated .NET code.

## Install

```sh
dotnet add package SemanticTypeModel.Generators --version 2.2.0
```

## Use when

- Install this package when you need incremental source generation for compile-time semantic model providers from annotated .NET code.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.

## Minimal example

```csharp
<ItemGroup>
  <PackageReference Include="SemanticTypeModel.Generators" Version="2.2.0" PrivateAssets="all" />
</ItemGroup>

TypeSchemaModel model = AppSemanticTypeModel.Create();
```

## Main APIs

| API | Purpose |
| --- | --- |
| `Semantic model provider generation` | Build-time provider generated from annotated source. |
| `AppSemanticTypeModel.Create()` | Typical generated entry point used by samples. |
| `MSBuild properties` | Control generator and extraction behavior. |

## Works with

- SemanticTypeModel.DotNet, SemanticTypeModel.Core, and scenario projection packages.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not run target projections or create JSON serializer contexts.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
- [Getting started](../getting-started.md)
