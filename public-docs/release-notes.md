# Release Notes

## 2.3.0

2.3.0 is the Configuration release-candidate line prepared by M0040 through M0045. It introduces the Configuration domain model, runtime options registration adapter, source-generator helper package, projection-neutral `RequiredWhen`, and documentation updates needed to validate the package set before human-approved publication.

### Highlights

- Added `SemanticTypeModel.Configuration` for deriving inspectable Configuration metadata from the canonical model and registering selected options types with Microsoft.Extensions.Options.
- Added `SemanticTypeModel.Configuration.Generators` for generated registration helpers that delegate to the runtime `AddSemanticOptions<TOptions>` adapter.
- Added explicit per-type options registration through `AddSemanticOptions<TOptions>`; applications opt in each selected options type instead of automatically registering every Configuration type in a complete model.
- Added selected-type derivation through `DeriveConfigurationType<TOptions>` so one complete semantic model can be reused across multiple services while unselected Configuration types remain unregistered.
- Added `ConfigurationSectionPresence.Optional` as the compatibility default and `ConfigurationSectionPresence.Required` for provider-independent required section data checks.
- Added required-section validation that runs through Options validation; `ValidateOnStart` moves missing required-section, DataAnnotations, and `RequiredWhen` failures to host startup.
- Added support for named options, call-site section overrides, and call-site strengthening from optional to required section presence.
- Added projection-neutral `RequiredWhen` metadata and Configuration-specific attributes for section binding, DataAnnotations validation, startup validation, and generated registration helpers.
- Removed stale fake public API baseline files from the release process; compatibility is reviewed through package smoke tests, samples, documentation, release notes, and human review.
- Updated package READMEs, usage guides, diagnostics, samples, versioning, compatibility, and release-readiness documentation for the 2.3.0 package inventory.

### Compatibility Notes

- Configuration application registration is explicit per options type. Use `services.AddSemanticOptions<TOptions>(configuration, model)` or a generated helper that delegates to that adapter.
- Complete-model Configuration derivation remains useful for inspection and tooling. Complete-model application registration through `AddSemanticConfigurationOptions(ConfigurationSemanticModel)` is obsolete and retained only for compatibility pending human review.
- `ConfigurationSectionPresence.Optional` preserves prior optional-section behavior. `Required` validates that effective configuration data exists under the selected section; an empty section with no value and no children fails when required.
- Required section presence without a section name, with root binding, or with disabled binding is a registration-time model/programming error. Missing deployed values, DataAnnotations failures, and `RequiredWhen` failures are options-validation failures.
- Diagnostics remain compatibility-reviewed as part of the 2.3.0 release candidate. Human review is required before publication.

### Upgrade Guidance

- Replace model-wide Configuration startup registration with one `AddSemanticOptions<TOptions>` call per options type used by the service.
- If a service needs more than one options type from the same generated model, reuse the complete model and call `AddSemanticOptions<TOptions>` for each selected type.
- Use `[SemanticConfigurationSection(..., Presence = SemanticConfigurationSectionPresence.Required)]` or equivalent metadata for required sections, and add `[SemanticValidateOnStart]` when startup-time validation is desired.
- Use `SemanticOptionsRegistration` for deployment-specific options name, section path, `ValidateOnStart`, or optional-to-required section-presence overrides.

### Known Limitations and Publication Status

- Model-wide Configuration application registration is obsolete rather than removed in this compatibility boundary.
- Generated Configuration helpers are ergonomic wrappers, not a separate behavior source; the runtime adapter is canonical.
- 2.3.0 packages are prepared as a release candidate only. Publication, tag creation, and GitHub release creation require separate human approval.

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

## 0.0.0-m0046 preview

- Added shared Order Fulfillment samples so EF Core, JSON Schema, Power BI, System.Text.Json, runtime DI, and Configuration examples all consume one generated semantic model while each projection selects only the target metadata it needs.
- Fixed EF Core nullable value-type projection so nullable scalar and numeric enum properties are represented as `Nullable<T>` in both projected EF metadata and applied EF Core `IProperty` metadata. This hardens the 2.3.0 compatibility gap where nullable value types could be projected with non-nullable CLR types.
