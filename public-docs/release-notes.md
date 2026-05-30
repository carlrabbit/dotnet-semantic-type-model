# Release Notes

## 1.0.0

First stable SemanticTypeModel release.

### Final Package Set

- `SemanticTypeModel.Abstractions`
- `SemanticTypeModel.Core`
- `SemanticTypeModel.JsonSchema`
- `SemanticTypeModel.DotNet`
- `SemanticTypeModel.Generators`
- `SemanticTypeModel.DependencyInjection`
- `SemanticTypeModel.PowerBI`
- `SemanticTypeModel.EFCore`
- `SemanticTypeModel.SystemTextJson`

`SemanticTypeModel.JsonEditor` is not a standalone package. JSON Editor compatibility is provided by `SemanticTypeModel.JsonSchema` through JSON Schema UI-hint export using `JsonSchemaUiMode.JsonEditorCompatible`.

### Supported Scenarios

- Canonical semantic type model contracts and compatibility metadata.
- Runtime model building, validation, and transformation pipelines.
- JSON Schema Draft 2020-12 import/export.
- JSON Editor-compatible UI-hint export through JSON Schema options.
- .NET code-first extraction and source-generator-backed model providers.
- System.Text.Json contract annotation import and resolver helpers.
- Runtime dependency-injection composition for model providers, transformations, and projections.
- EF Core-oriented metadata and `ModelBuilder` projection.
- Power BI-oriented metadata projection.

### Installation

Install the package or packages required by your scenario, for example:

```sh
dotnet add package SemanticTypeModel.JsonSchema --version 1.0.0
dotnet add package SemanticTypeModel.DependencyInjection --version 1.0.0
dotnet add package SemanticTypeModel.DotNet --version 1.0.0
dotnet add package SemanticTypeModel.SystemTextJson --version 1.0.0
```

See [packages.md](packages.md) and [installation.md](installation.md) for package combinations.

### Compatibility

Compatibility policy is documented in [api/compatibility.md](api/compatibility.md). `1.0.0` is the first stable release; prerelease APIs before 1.0 were not compatibility-stable.

### Migration from 0.1.0-alpha

- Update package references to `1.0.0`.
- Use the documented package set above; do not reference a standalone `SemanticTypeModel.JsonEditor` package.
- Prefer the current public APIs documented in [api/public-api.md](api/public-api.md) and package-specific guides.
- Validate applications with the scenario guides and sample flows under [samples.md](samples.md).

### Known Limitations

- Projection targets intentionally expose repository-defined metadata and do not provision external services.
- JSON Editor compatibility is an export mode in `SemanticTypeModel.JsonSchema`, not a complete JSON Editor runtime.
- Power BI projection does not authenticate with Power BI, publish datasets, create PBIX files, or manage service resources.

## 1.0.0-rc.1

Release-candidate validation package set for the first stable release.

- Confirmed the final package set listed in the `1.0.0` entry.
- Validated local package production, package smoke tests, public API baselines, public docs, and samples through the canonical release gate.
- Confirmed JSON Editor compatibility is provided through `SemanticTypeModel.JsonSchema`, not through a standalone package.

## M0019 — Projection Capability Matrix and Compatibility Contracts

Defined projection capability and compatibility contracts across JSON Schema, JSON Editor compatibility mode, EF Core, and Power BI projections.

- Added projection capability metadata contracts in `SemanticTypeModel.Abstractions.Hardening`.
- Added deterministic `ProjectionCapabilityCatalog` with core feature coverage for each supported projection target.
- Added `IProjectionCapabilityProvider.GetCapabilities()` to runtime projection implementations.
- Added capability coverage and determinism tests.
- Added public guide: `public-docs/guides/projection-capabilities.md`.

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
  - `SemanticTypeModel.DependencyInjection`
  - `SemanticTypeModel.PowerBI`
  - `SemanticTypeModel.EFCore`
- Added public API baseline files and release-gate validation.
- Hardened `SemanticTypeModel.EFCore` with `ModelBuilder.ApplySemanticTypeModel(...)` and configurable projection options returning diagnostics.
- Added end-to-end code-first schema authoring sample at `samples/code-first-authoring` with JSON Schema, JSON Editor-compatible UI-hint, and EF Core outputs.
