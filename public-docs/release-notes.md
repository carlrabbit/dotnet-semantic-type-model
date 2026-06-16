# Release Notes

## 2.2.0

M0038 collapses the `Model` / `Canonical` split so generated models and projection packages share one canonical public model surface.

### Highlights

- Moved canonical semantic model contracts to `SemanticTypeModel.Abstractions.Model`.
- Removed the old `TypeShape` / `ObjectShape` / `PropertyShape` / `ShapeRef` shape graph from shipped source.
- Updated the source generator so `Create()` returns `SemanticTypeModel.Abstractions.Model.TypeSchemaModel`.
- Updated runtime, transformation, query, JSON Schema, EF Core, Power BI, System.Text.Json, and dependency-injection paths to consume the unified model type.

### Compatibility Notes

This is an intentional breaking cleanup for the 2.2.0 line. Consumers should migrate from `SemanticTypeModel.Abstractions.Canonical` and the old shape graph to `SemanticTypeModel.Abstractions.Model` canonical contracts. Public samples and package guidance use annotated .NET code plus generated providers as the supported model authoring path. Human review is required before publishing packages, creating tags, or creating a GitHub release.

## 2.1.0

Candidate release documentation synchronization for M0033, M0034, M0035, and M0036. Packages are not published until human-approved publication completes.

### Highlights

- Documented envelope projection policies across JSON Schema, EF Core, and Power BI.
- Documented EF Core envelope payload storage policy concepts, including serialized JSON, owned JSON, owned same-table columns, owned separate tables, and ignored payloads.
- Documented ownership, versioning, revision, current-version, temporal-validity, lifecycle-state, and extension-data semantics.
- Documented System.Text.Json as a domain projection target and M0035 as the cleanup boundary for removing old model compatibility and stale transition terminology.

### Public Documentation

- Updated the core-semantics guide for ownership, evolution, lifecycle, and extension-data semantics.
- Updated JSON Schema guidance to emphasize code-first derivation and export rather than JSON Schema import as an authoring path.
- Updated EF Core and Power BI guides for envelope and evolution/lifecycle projection behavior.
- Updated System.Text.Json guide and package README source to use 2.1.0 candidate package guidance while distinguishing current resolver helpers from planned M0035 internal realignment.

### Compatibility Notes

M0035 remains a release-review boundary for removing old model compatibility APIs, stale transition terminology, and System.Text.Json paths that bypass the domain-projection architecture. Consumers using old model-shape APIs, JSON Schema import as a canonical source, or old System.Text.Json helper patterns should review compatibility notes before upgrading to a release that includes those removals.

## 2.0.0

Code-first semantic model architecture release.

### Highlights

- Added the projection-neutral core semantic vocabulary and public core-semantics guide.
- Added envelope semantics for wrapper types with distinguished payloads and lifecycle/context metadata.
- Added envelope-oriented code-first attribute guidance: `SemanticEnvelope`, `SemanticEnvelopePayload`, and `SemanticEnvelopeMetadata`.
- Established domain semantic model derivation as the common projection architecture.
- Updated EF Core support around `EfCoreSemanticModel` derivation and provider-neutral `ModelBuilder` configuration.
- Updated Power BI support around `PowerBiSemanticModel` derivation and deterministic local metadata output.
- Clarified that EF Core integration does not own database creation, migrations, provider-specific behavior, DbContext discovery/generation, runtime database validation, or global query filters.
- Clarified that Power BI integration does not own service publishing, workspace management, authentication, refresh scheduling, XMLA operations, PBIX generation, or full TOM parity.

### Public Documentation

- Added `public-docs/guides/core-semantics.md`.
- Updated EF Core and Power BI projection guides for the 2.0.0 domain semantic model architecture.
- Updated package README sources for `SemanticTypeModel.Core`, `SemanticTypeModel.DotNet`, `SemanticTypeModel.EFCore`, and `SemanticTypeModel.PowerBI`.
- Updated public sample documentation to emphasize code-first package-based samples.

### Compatibility Notes

2.0.0 is a major release. Consumers should review the code-first authoring path, domain semantic model derivation APIs, and target-specific projection boundaries before upgrading.

## 1.1.0

System.Text.Json contract correction and consumer sample validation release.

- Removed SemanticTypeModel-generated `JsonSerializerContext` support. Generated JsonSerializerContext support is removed in 1.1.0 because it depended on unsupported source-generator chaining and did not produce a reliable consumer feature.
- Removed public generated-context options from `SemanticTypeModel.SystemTextJson` projection/extraction helpers and the generator options attribute. The legacy MSBuild properties are rejected with explicit STJ004 guidance when set.
- Made resolver customization the supported System.Text.Json application mechanism. Existing `JsonSerializerOptions.TypeInfoResolver` values and user-authored `JsonSerializerContext` resolvers are wrapped instead of replaced.
- Added `SemanticJsonPropertyNameSource` and `SystemTextJsonProjectionOptions.PropertyNameSource` so consumers can explicitly preserve existing JSON names, use imported `systemTextJson.propertyName` values, or use semantic property names as JSON serialization names.
- Added deterministic duplicate final JSON property-name failure during resolver customization.
- Reworked public samples as consumer-facing package-based examples instead of source-tree development harnesses.

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

### Known Limitations

- Projection targets intentionally expose repository-defined metadata and do not provision external services.
- JSON Editor compatibility is an export mode in `SemanticTypeModel.JsonSchema`, not a complete JSON Editor runtime.
- Power BI projection does not authenticate with Power BI, publish datasets, create PBIX files, or manage service resources.
