## 3.0.0 (release candidate)

- Added `SemanticTypeModel.EFCore.Generators`, manifest schema version 1, explicit model selection, entity configurations, partial hooks, and deterministic registration.
- Replaced the supported runtime global EF cleanup/application path. This is a breaking migration: install the analyzer in the persistence project and call the generated apply extension.
- Generated models configure only owned CLR Entities; multiple models and manual entities compose. Duplicate CLR ownership is `STM5041`.
- Not published; release validation is required.

# 2.6.1

2.6.1 is a non-publishing patch release candidate. Publication, tagging, and GitHub release creation require separate human approval.

- EF Core enum scalar columns now stamp finalized provider CLR metadata as `string` for required and nullable enum members when semantic enum storage is string-based.
- Added a CLR-extracted, provider-backed SQLite regression that finalizes the EF model, audits entity/navigation/foreign-key/shadow/join metadata, creates the schema, saves, and reloads enum values plus enum-guarded ValueKind JSON data.

# 2.6.0

2.6.0 is a non-publishing release candidate. Publication, tagging, and GitHub release creation require separate human approval.

- Adds projection-neutral typed literals and resolved conditional-constraint references for `SemanticRequiredWhen`.
- Normalizes enum member names, Boolean and invariant numeric values, nullable `null`, GUID, and date/time literals against the source property type, with stable STM5026-STM5036 diagnostics for invalid and unsupported cases.
- Treats strong-identifier condition sources without a resolved provider-scalar contract as unsupported (`STM5027`) rather than silently comparing their text as strings.
- Emits deterministic JSON Schema `if`/`const`/`then` conditional required rules while keeping RequiredWhen metadata out of EF Core entity discovery.

# 2.5.3

2.5.3 is a non-publishing patch release candidate. Publication, tagging, and GitHub release creation require separate human approval.

- Separates CLR/semantic member declaration from CLR/semantic relational storage in EF column metadata.
- Configures inherited non-semantic-base members on the first semantic storage entity and avoids reconfiguring semantic-base members on derived TPT entities.
- Adds deterministic member declaration, storage resolution, and declaring-type mismatch diagnostics.
- Adds the permanent 15-shape EF fixture inventory and focused declaration, storage, TPT, exact-entity, ValueKind, and SQLite regression coverage.

## Upgrade guidance

No consumer configuration change is required. Existing semantic models with inherited members no longer need placement workarounds. Human review remains required before publication.

# 2.5.1

2.5.1 is a non-publishing patch release candidate. Publication, tagging, and GitHub release creation require separate human approval.

- Fixed semantic ValueKinds being convention-discovered as keyless entities before they could be mapped as JSON-converted properties.
- Corrected `ApplySemanticRelationalModel(...)` ordering so deterministic suppression and cleanup occur before the residual `EF_UNEXPECTED_CONVENTION_ENTITY` diagnostic.
- Added an exact final CLR entity allowlist audit: only projected semantic Entity types remain, while JSON-owned objects and collection items remain converted properties.
- Added polluted-`ModelBuilder` and SQLite schema and round-trip coverage for JSON objects, JSON arrays, TPT entities, and exact entity inventories.

## Upgrade guidance

No consumer configuration change is required. Remove workarounds that call `ModelBuilder.Ignore(...)` for semantic ValueKinds; the package now owns that convention boundary. The relational contract and public application APIs remain unchanged from 2.5.0.

## Publication status

Human review is required for the mutable-model cleanup strategy, final entity audit, SQLite results, package inventory, release notes, and publication approval.

## 2.5.0

- Reset `SemanticTypeModel.EFCore` to the opinionated relational contract: entities/tables, TPT, scalar columns, JSON-owned ValueKinds, JSON extension data, and identifier-only entity links.
- Removed the 2.4.x application modes, shared-type projection, source-lineage graph, ownership navigations, alternative storage and inheritance strategies, relationship projection, compatibility aliases, and forwarding APIs.
- Added real CLR ModelBuilder and SQLite create/insert/load acceptance coverage for the anonymized fixtures.

## 2.4.6

2.4.6 is a non-publishing patch release candidate. Publication, tagging, and GitHub release creation require separate human approval.

- Limits EF source lineage to projected root entities and owned/value-object types reachable through EF mappings, excluding interfaces, helper infrastructure, DTOs, and repository abstractions.
- Adds anonymized order-intake specification and fulfillment-run-state regression fixtures that retain realistic record, inheritance, ownership, extension-data, identifier, collection, dictionary, and binary-payload shapes.
- Establishes three EF compatibility layers: unit projection/lineage tests, real CLR `DbContext`/`ModelBuilder` tests, and SQLite in-memory schema and persistence tests.
- Reports and suppresses closed-CLR members that still require explicit conversion or storage policy, including record-struct identifiers and `ReadOnlyMemory<byte>` payloads; SQLite tests round-trip supported state and assert unsupported members are absent from metadata.

## 2.4.5

2.4.5 is a non-publishing patch release candidate. Publication, tagging, and GitHub release creation require separate human approval.

- Replaced unchecked owned-target lineage resolution with guarded, shape-aware resolution and stable `EFCORE_*` diagnostics.
- Added `EfCoreDerivationOptions.ApplicationMode`; derived `EfCoreSemanticModel` instances now carry `ApplicationPolicy`.
- CLR type/member lineage failures are errors for `ClosedClrModel` and warnings for `SharedTypeModel`; semantic ownership contradictions remain errors.
- `ApplySemanticTypeModel(...)` now derives through `DeriveEfCoreModel(...)`, applies the resulting model, and returns merged projection and lineage diagnostics.
- Added compile-time `STM5025` reporting when an explicit `[SemanticOwned]` kind contradicts the statically known CLR member shape.

## 2.4.2

2.4.2 is a non-publishing patch release candidate correcting two 2.4.1 behavior gaps. Publication, tagging, and GitHub release creation require separate human approval.

### Corrections

- `System.Uri` and nullable `Uri` members are supported as string-compatible canonical scalars. `Uri` implies the `uri` semantic format by default, while an explicit supported format can override that convention. JSON Schema emits string/`uri`, Power BI emits text, System.Text.Json retains normal `Uri` handling, and provider-neutral EF Core stores the scalar as a string.
- `STM5025` remains strict for formats applied to unsupported targets such as integer, object, collection, dictionary, and enum members.
- EF Core now classifies semantic ownership by ownership kind, target role, and target shape before choosing storage. Owned value objects respect `ValueObjectProjectionMode`: `Flatten` emits scalar/enum columns and `SerializeJson` emits one string JSON column.
- Object-role owned members, entity-role owned members, and owned collections require explicit policies and are diagnosed rather than silently flattened or serialized.
- True EF owned navigation remains deferred: selecting `Owned` emits `EFCORE_TRUE_OWNED_NAVIGATION_NOT_SUPPORTED` and creates no fake `OwnsOne` domain metadata.

### Release status

Packages are prepared and validated as a 2.4.2 release candidate only. Human review is required for policy names, diagnostics, samples, package inventory, wording, and publication approval.

## 2.4.1

2.4.1 is an emergency patch release-preparation line for the dictionary type extraction defect introduced in the 2.4.0 package set. Packages are prepared as a release candidate only; publication, tag creation, and GitHub release creation require separate human approval.

### Correction

- Fixed the 2.4.0 .NET extraction defect where dictionary key type definitions could be omitted while dictionary descriptors still referenced the key type.
- The most visible affected scenario was `[SemanticExtensionData] Dictionary<string, JsonElement>?`, where the generated dictionary shape referenced a string key type that was not registered and canonical validation reported `STM0002`.
- Dictionary key and value types are now both normalized, extracted, registered, and emitted into generated providers so canonical validation succeeds without weakening `STM0002`.
- EF Core now ignores extension-data properties before property type lookup and dictionary shape diagnostics, preserving the existing default that extension data is not projected into EF properties.
- Regression coverage now proves valid extension-data dictionaries and ordinary dictionaries register supported key/value type definitions, and malformed dictionary references still produce `STM0002`.

### Package Inventory

The intended 2.4.1 package set is resolved from packable projects during release validation and is expected to include the same package family as 2.4.0. Human review is required for the final produced package inventory and archive contents.

## 2.4.0

2.4.0 is the documentation-synchronization and release-preparation line for the shared Order Fulfillment samples, scalar/nullability compatibility hardening, Configuration package documentation, and audience-specific descriptions. Packages are prepared as a release candidate only; publication, tag creation, and GitHub release creation require separate human approval.

### Highlights

- Added shared Order Fulfillment sample coverage so JSON Schema, EF Core, Power BI, System.Text.Json, runtime DI, and Configuration examples consume one complete generated semantic model while each projection selects only its target-specific metadata.
- Documented deliberate cross-sample overlap: samples are representative package canaries, while unit tests and package smoke tests provide exhaustive compatibility coverage for scalar and nullability combinations.
- Fixed EF Core nullable value-type projection so nullable scalar and numeric enum properties remain `Nullable<T>` in projected EF metadata and applied EF Core `IProperty` metadata.
- Retained 2.3.0 Configuration behavior in the 2.4.0 package set: explicit per-options-type registration, selected-type derivation, required section presence, and generated-helper delegation to runtime registration.
- Replaced the former general description contract with `UserDescription` and `TechnicalDescription` across model contracts, generated output, query/inspection text, and projection documentation.
- Added `SemanticUserDescriptionAttribute` for user-facing text and `SemanticTechnicalDescriptionAttribute` for technical text; XML `<summary>` is an automatic technical-description fallback only.
- JSON Schema uses user descriptions for `description` and can emit technical descriptions through an opt-in `x-*` extension; Power BI uses user descriptions; EF Core maps technical descriptions to table and column comments.
- Configuration, query, and inspection output expose audience-specific descriptions instead of one generic description field.

### Compatibility and Migration Notes

- `SemanticDescriptionAttribute`, the generic canonical `Description`, `IncludeXmlDocumentation`, `SemanticTypeModelIncludeXmlDocumentation`, and `RequireXmlDocumentation` are removed and unsupported in 2.4.0.
- Use `RequireTechnicalDescription` when generator validation must require either an explicit technical description or XML `<summary>` fallback.
- User-facing projections do not silently fall back to technical descriptions, and technical projections do not silently fall back to user descriptions.
- Existing general description text requires manual classification as user-facing text, technical text, or both because audience intent cannot be inferred safely.
- The Order Fulfillment sample's `Customer.Name` demonstrates the mapping: XML `<summary>` becomes `TechnicalDescription` and EF Core column comment, while `SemanticUserDescription` becomes `UserDescription`, JSON Schema `description`, and Power BI description.

### Package Inventory

The intended 2.4.0 package set is resolved from packable projects during release validation and is expected to include `SemanticTypeModel.Abstractions`, `SemanticTypeModel.Core`, `SemanticTypeModel.JsonSchema`, `SemanticTypeModel.DotNet`, `SemanticTypeModel.Generators`, `SemanticTypeModel.DependencyInjection`, `SemanticTypeModel.Configuration`, `SemanticTypeModel.Configuration.Generators`, `SemanticTypeModel.PowerBI`, `SemanticTypeModel.EFCore`, and `SemanticTypeModel.SystemTextJson`. Human review is required for the final produced package inventory and archive contents.

### Known Limitations and Publication Status

- 2.4.0 is release-preparation documentation until human-approved publication completes.
- The Configuration generator package remains a package-inventory/documentation-alignment package unless generated helper output is present in the consuming build.
- Human review is required for breaking compatibility wording, migration guidance, XML-summary fallback wording, projection description mappings, diagnostics, package contents, release evidence, publication approval, tag creation, and GitHub release creation.

## 2.3.0

2.3.0 was the Configuration release-preparation line prepared by M0040 through M0045. It introduced the Configuration domain model, runtime options registration adapter, source-generator helper package, projection-neutral `RequiredWhen`, explicit per-options-type registration, selected-type derivation, required section presence validation, and package documentation standardization.

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

## 2.4.4

- Corrects the 2.4.3 authority model: EF CLR convention augmentation partially treated EF conventions as a source to augment rather than behavior to constrain.
- Makes `EfCoreSemanticModel` the complete, lineage-preserving EF application contract and makes closed CLR application the default.
- Makes `ApplySemanticTypeModel` a convenience wrapper over the same closed application engine used by `ApplyEfCoreSemanticModel`.
- Adds explicit `EFCORE_SOURCE_LINEAGE_REQUIRED` failure for lineage-free closed application and keeps shared-type application explicit through `SharedTypeModel` / `ApplyEfCoreSemanticModelAsSharedTypes`.
- Obsoletes the `ClrConventionAugmentation` and `SharedTypeProjection` names as compatibility aliases.
- Preserves inherited extension-data suppression, value-object root rejection, dictionary extraction, `Uri` scalar handling, and role-aware owned-storage projection.

## 2.4.3

- Corrects a 2.4.2 interop issue where EF Core conventions could discover directly declared or inherited `[SemanticExtensionData]` dictionaries on reachable semantic value-object CLR types and fail relationship discovery.
- Adds explicit CLR-backed convention augmentation and STM-owned shared-type projection modes. CLR-backed augmentation is the default `ApplySemanticTypeModel` mode and suppresses semantic extension data only from EF mapping.
- Rejects semantic `ValueObject` CLR types used as root `DbSet<T>` entities while preserving owned reachability, canonical extension-data semantics, M0049 dictionary extraction, and M0050 role-aware owned storage behavior.
