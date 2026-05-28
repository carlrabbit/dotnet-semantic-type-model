# Release Notes

## M0018 — Diagnostics Documentation and Analyzer Experience

Stabilized the diagnostic framework across all packages.

- Added `StmDiagnosticIds` in `SemanticTypeModel.Core` with public constants for all STM0xxx (model validation) and STM3xxx (JSON Schema runtime projection) codes.
- Added `DotNetExtractionDiagnosticIds` in `SemanticTypeModel.DotNet` with public constants for all STM5xxx (.NET extraction and source-generator) codes.
- Added `GeneratorDiagnosticDescriptors` in `SemanticTypeModel.Generators` with stable static `DiagnosticDescriptor` fields for STM5008, STM5018, and STM5019. The source generator now uses these static descriptors instead of inline construction.
- Added diagnostic ID stability tests verifying uniqueness and format across all STM packages.
- Published full diagnostic reference pages under `public-docs/diagnostics/`.
- Published `docs/specs/diagnostics.md` defining the diagnostic contract: ID scheme, severity policy, source/model location guidance, descriptor requirements, and maintenance rules.

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
