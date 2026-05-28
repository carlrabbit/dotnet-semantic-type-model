# Release Notes

## 0.1.0-alpha

Initial prerelease package publication milestone.

- Added release automation scripts: `./eng/package.sh`, `./eng/publish.sh`, `./eng/release-check.sh`.
- Added manual release workflows: `release-check.yml`, `pack.yml`, and `publish-nuget.yml`.
- Added package smoke tests that consume local `artifacts/nuget` packages.
- Added package metadata and per-package NuGet README sources for:
  - `SemanticTypeModel.Abstractions`
  - `SemanticTypeModel.Core`
  - `SemanticTypeModel.JsonSchema`
  - `SemanticTypeModel.DotNet`
  - `SemanticTypeModel.Generators`
  - `SemanticTypeModel.JsonEditor`
  - `SemanticTypeModel.PowerBI`
  - `SemanticTypeModel.EFCore`
- Added public API baseline files and release-gate validation.
- Hardened `SemanticTypeModel.EFCore` with `ModelBuilder.ApplySemanticTypeModel(...)` and configurable projection options returning diagnostics.
- Added end-to-end code-first schema authoring sample at `samples/code-first-authoring` with JSON Schema, JSON Editor-compatible UI-hint, and EF Core outputs.

## Known Limitations

- Public API surface and package split are still prerelease and may change before 1.0.
