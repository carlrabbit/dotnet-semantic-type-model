# SemanticTypeModel.PackageReadme

## What this package does

This page is the generic package README source used for repository-level package documentation checks. Package-specific README sources live beside it and are the source of truth for individual NuGet packages.

## Install

```sh
dotnet add package SemanticTypeModel.Core --version 2.3.0
```

## Use when

- Check the expected README shape for package documentation validation.
- Link from repository packaging scripts that need a generic README source.
- Keep package-specific pages short and consistent.

## Minimal example

```csharp
TypeSchemaModel model = AppSemanticTypeModel.Create();
Console.WriteLine(model.Types.Count);
```

## Main APIs

| API | Purpose |
| --- | --- |
| `public-docs/nuget/*.md` | Package README source files. |
| `docs/engineering/package-documentation.md` | Authoritative README and guide standard. |

## Works with

- Package-specific README sources under this directory.
- `./eng/public-docs.sh` validation.

## Does not do

- It is not the README for a separately shipped NuGet package.
- It does not replace package-specific README sources.

## More documentation

- [Package documentation standard](../../docs/engineering/package-documentation.md)
- [Package list](../packages.md)
