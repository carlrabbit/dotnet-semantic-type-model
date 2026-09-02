# 6.0.0

6.0.0 is the intended next stable release and current release-candidate line. It is a breaking semantic release:
CLR single-value wrapper shape no longer carries STM scalar meaning, and Logical Type is the explicit
projection-neutral way to label semantically distinct scalar properties without changing representation.
Publication, tagging, and GitHub Release creation remain separate from release-readiness validation.

## Highlights

- Strong Scalar canonical semantics and CLR single-value-wrapper inference are removed across the suite.
  `SemanticLiteralKind.StrongIdentifier` and EF Core's automatic single-value-wrapper conversion are removed as
  part of the same boundary. Strongly typed CLR IDs remain application/library concerns unless an explicit
  integration is configured outside STM's canonical semantics.
- `[SemanticLogicalType("Name")]` adds an explicit model-local, case-sensitive Logical Type name to an ordinary
  scalar property. The same Logical Type name must map to the same scalar type within one model. Logical Type is
  metadata only: it does not create a canonical type node or change CLR, JSON, EF Core, LINQ, Power BI, or
  built-in TestData representation.
- The aligned suite now contains exactly eleven packages with `SemanticTypeModel.TestData` added to the existing
  ten-package suite. Every `SemanticTypeModel.*` package used together must use exactly the same version.
- `SemanticTypeModel.TestData` adds deterministic, constraint-aware Random generation for canonical value graphs
  with `Simple`, `Moderate`, and `Extreme` size profiles, fixed/configurable safety budgets, deterministic seeds,
  and fail-closed `TESTDATA_*` diagnostics. Built-in regex synthesis remains intentionally unsupported.
- Optional Semantic Terminology Profiles provide a versioned, model-bound JSON sidecar for synthetic scalar
  candidates. Profile-guided generation prefers property terminology, then reusable Logical Type terminology,
  and falls back to deterministic Random generation when no eligible terminology value is available.
- The typed TestData facade adds `model.TestData()`, `Generate<T>()`, `GenerateMany<T>()`, materialization of an
  existing semantic value graph, property/Logical-Type custom scalar generators, and supported public CLR
  construction for the documented scalar, enum, collection, dictionary, nullable, record/class/struct, and
  constructor/member shapes. Invalid custom candidates fail closed.
- The TestData acceptance boundary is exercised through the real code-first generator, CLR materialization,
  STM-configured System.Text.Json, and JSON Schema derived from the same canonical model.
- The compile-time semantic manifest remains ephemeral build transport with exact producer/consumer suite-version
  alignment. The current manifest schema is v3 and is not a persisted interchange format.

## Upgrade guidance from 5.x

1. Upgrade every `SemanticTypeModel.*` package, generator, and analyzer used together to exactly `6.0.0`.
2. Remove `[SemanticStrongScalar]`, `SemanticLiteralKind.StrongIdentifier`, and assumptions that a CLR
   `Value`-property wrapper receives automatic STM scalar or EF conversion behavior.
3. Where representation-neutral scalar identity is useful, put `[SemanticLogicalType("...")]` on the ordinary
   scalar property instead of relying on CLR wrapper shape.
4. Rebuild producer and consuming projects together so generated semantic manifests and consuming generators use
   the same exact suite version.
5. Add `SemanticTypeModel.TestData` only where deterministic semantic test-data generation is needed. Random mode
   requires no terminology profile; terminology profiles and programmatic generators are optional enrichment.
6. Keep target-specific wrapper conversions, strongly typed-ID integration, relationships, serializer contracts,
   and database behavior in application/target-native configuration rather than inferring them from CLR shape.

See [Compatibility](api/compatibility.md), [Core semantics](guides/core-semantics.md), and
[Test data](guides/test-data.md) for the current contracts and boundaries.

# 5.0.1

5.0.1 was the patch-line release candidate before the current 6.0.0 development line. Its publication,
tagging, and GitHub Release history remains separate from the current development work.

- Ordinary `JsonSerializerOptions` remains the primary complete System.Text.Json runtime path. Modeled
  semantic Entity inheritance receives automatic `$type` polymorphism when the application has not supplied
  an explicit System.Text.Json contract; an explicit application contract wins.
- Multiple independently generated SemanticTypeModel models compose through runtime options. Minimal API
  applications configure the global JSON path with `ConfigureHttpJsonOptions`.
- Automatic polymorphic/discriminator output remains outside the JSON Schema/System.Text.Json fidelity
  baseline.
- Consolidated positive cross-package tests now use independent generated Model A and Model B fixtures,
  including multi-model EF Core composition and isolated packed-consumer coverage.
- Scalar representation fidelity is aligned across extraction and projections: `char` and
  `ReadOnlyMemory<byte>` are covered, `Uri` defaults to `uri-reference`, Binary schemas use Base64
  `contentEncoding`, and raw Json is not restricted to object shape.
- EF Core rejects unsupported unsigned integer representations deterministically, while Power BI preserves
  `ulong` values as text and diagnoses unconstrained Decimal precision risk.

# 5.0.0

5.0.0 is the next major release after 4.0.1. It contains the current aligned ten-package suite and the
following consumer-visible boundaries and additions.

- STM-owned Configuration/Options integration is removed: `SemanticTypeModel.Configuration`, former
  Configuration-specific authoring/runtime APIs, and `AddSemanticOptions<TOptions>` are not part of 5.0.0.
  Applications use Microsoft.Extensions.Configuration and Microsoft.Extensions.Options directly when needed;
  `SemanticTypeRole.Configuration` and `SemanticRequiredWhen` remain projection-neutral semantics.
- The suite is exactly `SemanticTypeModel.Abstractions`, `Core`, `JsonSchema`, `DotNet`, `Generators`,
  `DependencyInjection`, `PowerBI`, `EFCore`, `EFCore.Generators`, and `SystemTextJson`, all at 5.0.0.
- Strong Scalar is explicit `[SemanticStrongScalar]` nominal scalar semantics. Supported JSON Schema,
  STM-configured System.Text.Json, EF scalar/owned-JSON, and Power BI paths use the underlying scalar
  representation. It does not imply Identifier, Key, Entity, ownership, or one-property inference.
- Display Identity and Access Path provide projection-neutral ordered recognition/locate semantics. They do
  not generate EF indexes, API queries, UI behavior, Power BI behavior, or relationships.
- JSON representation fidelity is a bounded one-way contract: supported STM-configured System.Text.Json
  output validates against derived JSON Schema. Bidirectional serializer/schema equivalence and
  representation-changing custom contracts are outside the guarantee. JSON Schema `x-stm` preserves the
  implemented semantic vocabulary, and the ephemeral compile-time manifest is schema v2 with exact suite
  version alignment.
- `STM5051` diagnoses invalid Strong Scalar declarations; `STM5049` and `STM5050` diagnose invalid Display
  Identity and Access Path definitions.

## Upgrade from 4.0.x

Remove the `SemanticTypeModel.Configuration` package and former STM Configuration/Options APIs. Replace their
application binding, registration, and validation with application-owned Microsoft.Extensions.Configuration /
Options policy. Keep the projection-neutral Configuration role and `SemanticRequiredWhen` where applicable.
Upgrade every SemanticTypeModel package and analyzer together to exactly 5.0.0. The chronology is 4.0.1
directly to 5.0.0; there is no 4.1.0 release in this line.

# 4.0.1

4.0.1 is a patch release for EF Core nullability regressions found after 4.0.0.

- Fixed generated EF Core configuration for nullable JSON-owned reference and collection properties so generated `ValueConverter<TModel, string>` / `ValueComparer<TModel>` usages preserve the declared nullable CLR property type and compile against nullable `PropertyBuilder<T?>` APIs.
- Fixed nullable `ReadOnlyMemory<byte>?` EF conversion so null and non-null values use the correct generated conversion path.
- Expanded EF Core compatibility coverage across provider-neutral projection, real source-generator compilation, finalized EF metadata, SQLite persistence/change tracking, and packed-package generator smoke.
- No public API, semantic nullability contract, relationship model, or JSON storage policy changed in this patch.
- This nullable owned-JSON correction is the 4.0.1 patch-line fix; it adds no new storage model or public
  configuration API.

## Upgrade guidance

Upgrade every `SemanticTypeModel.*` runtime, projection, generator, and analyzer package used together to the exact same `4.0.1` version. No application configuration change is required for consumers already using the 4.0.0 generated EF Core application model.

# 4.0.0

4.0.0 is the released major compatibility boundary that consolidates the previously unreleased M0062 work and
the unpublished 3.0.0 candidate. The latest package version verified on NuGet before preparing 4.0.0 was 2.6.0.

## Highlights

- EF Core application now uses generated `IEntityTypeConfiguration<TEntity>` implementations selected
  explicitly with `[assembly: GenerateSemanticEfModel(typeof(ModelMarker))]`; the application applies the
  generated extension and continues to own `DbContext`, unrelated/manual entities, providers, migrations, and
  target-specific relationships.
- The ephemeral model-assembly manifest has an exact producer/consumer suite-version contract; a mismatch is
  reported as `STM5047`.
- `SemanticTypeModel.Configuration.Generators` and JSON Schema import were removed without compatibility
  packages or authoring-path shims. Configuration registration uses the runtime
  `AddSemanticOptions<TOptions>` adapter.
- Lifecycle mutability is optional and projection-neutral. `[SemanticMutable]` and `[SemanticImmutable]` are the
  only declarations; a property declaration overrides its containing type, and CLR setter/init shape does not
  infer mutability.
- The general relationship model, relationship attribute, and relationship inference were removed. Structural
  references, keys, ownership, aggregate roots, and envelopes remain distinct; applications configure
  target-specific relationships.
- JSON Schema Draft 2020-12 export can preserve the supported STM-only vocabulary in one optional `x-stm`
  object: `role`, `aggregateRoot`, `mutability`, `technicalDescription`, `keys`, `unit`, and open `ui.*` data.
- Standard JSON Schema `description` uses the user description. Technical descriptions remain separate in
  `x-stm.technicalDescription`, and JSON-compatible `ui.*` annotations pass through under `x-stm.ui`.
- JSON Editor compatibility, widget inference, and its configuration APIs were removed.

## Migration

1. Set every `SemanticTypeModel.*` runtime, projection, generator, and analyzer package used together to the
   exact same `4.0.0` version.
2. In each EF model project, run `SemanticTypeModel.Generators`; in the persistence project, reference
   `SemanticTypeModel.EFCore.Generators`, select each semantic model explicitly, and call its generated apply
   extension from `OnModelCreating`. Replace retired runtime global `ModelBuilder` application/cleanup calls.
3. Remove `SemanticTypeModel.Configuration.Generators` and register every selected options type explicitly with
   `AddSemanticOptions<TOptions>`.
4. Replace JSON Schema import as a canonical authoring path with annotated .NET code and generated canonical
   models.
5. Replace relationship attributes/inference with target-owned configuration, including ordinary EF
   configuration where applicable.
6. Replace old mutability APIs with optional `[SemanticMutable]`/`[SemanticImmutable]` lifecycle declarations.
7. Replace JSON Editor compatibility options with standard JSON Schema plus optional `x-stm` semantic
   annotations.

See [Compatibility](api/compatibility.md), [EF Core](guides/ef-core.md), and
[JSON Schema](guides/json-schema.md) for detailed current boundaries.

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
