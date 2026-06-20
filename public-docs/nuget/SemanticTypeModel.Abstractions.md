# SemanticTypeModel.Abstractions

## What this package does

`SemanticTypeModel.Abstractions` provides shared model contracts, diagnostics contracts, and projection-neutral data structures used by the SemanticTypeModel package set.

## Install

```sh
dotnet add package SemanticTypeModel.Abstractions --version 2.3.0
```

## Use when

- Install this package when you need shared model contracts, diagnostics contracts, and projection-neutral data structures used by the SemanticTypeModel package set.
- Keep package boundaries explicit in an application or library.
- Pair generated semantic models with the target runtime you are configuring.

## Minimal example

```csharp
using SemanticTypeModel.Abstractions.Model;

TypeSchemaModel model = AppSemanticTypeModel.Create();
foreach (TypeDefinition type in model.Types)
{
    Console.WriteLine(type.Name);
}
```

## Main APIs

| API | Purpose |
| --- | --- |
| `TypeSchemaModel` | Canonical semantic model root. |
| `TypeDefinition` | Base contract for modeled types. |
| `ObjectTypeDefinition` | Object type contract with properties, keys, relationships, and annotations. |
| `SchemaDiagnostic` | Projection-neutral diagnostic contract. |

## Works with

- SemanticTypeModel.Core, SemanticTypeModel.DotNet, and every projection package.
- `SemanticTypeModel.Abstractions.Model` for the current unified model surface.
- `public-docs/samples/` projects that demonstrate package-based usage.

## Does not do

- It does not contain source generators, projection exporters, or framework-specific integration.
- It does not make milestone plans or historical research documents part of the public API.
- It does not change compatibility rules described in the compatibility documentation.

## More documentation

- [Package list](../packages.md)
- [Getting started](../getting-started.md)
- [Compatibility](../api/compatibility.md)
