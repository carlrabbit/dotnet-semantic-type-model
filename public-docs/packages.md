# Packages

## Initial Prerelease Package Set (0.1.0-alpha)

- `SemanticTypeModel.Abstractions`
- `SemanticTypeModel.Core`
- `SemanticTypeModel.JsonSchema`
- `SemanticTypeModel.DotNet`
- `SemanticTypeModel.Generators`
- `SemanticTypeModel.JsonEditor`
- `SemanticTypeModel.PowerBI`
- `SemanticTypeModel.EFCore`

## EF Core projection

- Guide: [guides/ef-core-projection.md](guides/ef-core-projection.md)
- NuGet README source: [nuget/SemanticTypeModel.EFCore.md](nuget/SemanticTypeModel.EFCore.md)

## Projection capability matrix

- Guide: [guides/projection-capabilities.md](guides/projection-capabilities.md)

## Code-first sample path

- End-to-end sample: [samples/code-first.md](samples/code-first.md)
- Uses `SemanticTypeModel.DotNet`, `SemanticTypeModel.Generators`, `SemanticTypeModel.JsonSchema`, and `SemanticTypeModel.EFCore`.

## Prerelease Notes

- This is the first prerelease package set.
- Public APIs may change before 1.0.
- Package boundaries may be refined before 1.0.
- Known limitations are documented in release notes.
- `SemanticTypeModel.JsonEditor` is currently produced from the `src/SemanticTypeModel.DependencyInjection` project.
- `SemanticTypeModel.SystemTextJson` - System.Text.Json annotation import, generated-context opt-in, and runtime helper APIs.

- [Power BI projection guide](guides/power-bi-projection.md)
