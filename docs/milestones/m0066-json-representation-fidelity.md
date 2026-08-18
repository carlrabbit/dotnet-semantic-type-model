# M0066 — JSON Representation Fidelity

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-broad |
| Scope size | broad, cross-package |
| Implementation autonomy | high within the resolved JSON representation/fidelity contract |
| Repository role | capability-provider |
| Maturity | published-maintenance |
| Documentation sync | separate pass |
| Release readiness | separate pass |
| Human review | none |
| Recommended implementation branch | `codex/m0066-json-representation-fidelity` |

Implementation work must not be committed directly to `main`.

## Goal

Make the JSON projections coherent around the product workflow:

```text
annotated .NET model
    -> canonical TypeSchemaModel
    -> System.Text.Json JSON
    -> optional validation against JSON Schema derived from the same model
```

M0066 has two connected goals:

1. complete JSON Schema preservation of the current projection-neutral semantic vocabulary where standard JSON Schema does not already represent it;
2. define and prove a bounded one-way System.Text.Json -> JSON Schema output-conformance contract without turning System.Text.Json into a validation framework or adding a production JSON Schema validator.

## Target State

When M0066 is complete:

1. every current public semantic authoring attribute has an explicit JSON Schema classification: native/structural representation, `x-stm` preservation, or intentionally target-specific/acquisition-only;
2. the JSON Schema projection preserves Display Identity, Access Paths, ownership, envelope, lifecycle/evolution, extension-data meaning, and enum-value semantic metadata according to the durable specs;
3. existing native JSON Schema mappings remain authoritative for requiredness, nullability, constraints, formats, enum values, descriptions, titles, and conditional `RequiredWhen`;
4. semantic extension data is not emitted as a normal bag property and uses typed `additionalProperties` when its value shape is representable;
5. deterministic property export honors semantic order metadata before stable name ordering;
6. the System.Text.Json domain model represents imported STJ contract metadata relevant to fidelity inspection/classification instead of reducing the domain model to naming alone;
7. existing System.Text.Json default `ExistingJsonContract` behavior is unchanged;
8. a documented supported System.Text.Json configuration exists under which successfully emitted JSON is proven to validate against the JSON Schema derived from the same canonical model;
9. that guarantee is explicitly one-way and excludes unsupported representation-changing converters, number handling, reference-preservation representation, unmatched noncanonical CLR members, and explicit STJ polymorphism;
10. general semantic validation remains JSON Schema territory; STJ does not acquire pattern/min/max/RequiredWhen validation behavior;
11. production packages gain no JSON Schema validation dependency;
12. JSON Schema and System.Text.Json remain sibling packages with no direct dependency and no new shared JSON package/model;
13. M0066 is suitable for the 4.1 minor-release line and does not prepare or publish a release.

## Scope

### JSON Schema semantic-fidelity pass

Implement the behavior defined by:

- `docs/specs/json-representation-fidelity.md`;
- `docs/specs/json-schema-domain-model-and-export.md`;
- `docs/decisions/json-schema-uses-x-stm-for-selected-semantics.md`.

The implementation must cover all currently supported projection-neutral authoring semantics, including the M0065 Display Identity and Access Path additions already present on `main`.

### `x-stm` expansion

When semantic annotations are enabled, support the approved vocabulary:

```text
role
aggregateRoot
mutability
technicalDescription
keys
unit
ui
displayIdentity
accessPaths
ownership
envelope
versioned
version
revision
currentVersion
temporalValidity
validFrom
validTo
lifecycleState
extensionData
enumValues
```

Do not duplicate standard JSON Schema semantics in `x-stm`.

Use structured target-consumer representations rather than leaking canonical annotation encoding:

```text
Display Identity -> ordered emitted-property-name array
Access Paths     -> path-name -> ordered emitted-property-name array
Envelope         -> purpose/payload/metadata structure
Ownership        -> object|collection marker at the owned property
Evolution        -> declaration-local semantic markers
Extension Data   -> containing-object marker; native additionalProperties owns value shape
Enum metadata    -> entries aligned to native enum values, without duplicating the JSON enum value
```

### Extension data

Bring JSON Schema output into line with current canonical Extension Data semantics:

- omit the extension-data bag from normal properties;
- represent openness through `additionalProperties`;
- when the canonical extension-data value type is representable, use a typed `additionalProperties` schema;
- when it is not representable, remain safely permissive and emit a projection diagnostic rather than inventing a false type contract;
- preserve `x-stm.extensionData = true` when semantic annotations are enabled.

Do not add general `unevaluatedProperties` behavior.

### RequiredWhen

Keep the existing supported canonical `RequiredWhen` equality mapping and make the specification/output unambiguous:

```text
if source property equals typed canonical literal
then target property is required
```

This does not authorize general user-authored `if/then/else` composition.

### Deterministic order

Use semantic order metadata for deterministic JSON Schema property emission when available, then stable emitted property name.

This is document-order determinism only. JSON object order remains non-semantic for validation.

### System.Text.Json domain fidelity

The System.Text.Json domain model must carry deterministic metadata relevant to representation/fidelity analysis for imported:

```text
propertyName
ignore / ignoreCondition
include
converter
numberHandling
required
extensionData
objectCreationHandling
unmappedMemberHandling
polymorphism marker
```

The exact internal/public record decomposition is implementation-owned as long as current public contracts remain compatible and the domain model/inspection/tests can observe the required information.

Do not reimplement the base System.Text.Json resolver/context.

The base resolver remains authoritative for serializer-native behavior except for existing explicit STM customization such as property-name projection.

### Supported tandem configuration

The fidelity guarantee is tested and documented for a bounded configuration, not for arbitrary System.Text.Json options.

The baseline requires:

- canonical semantic property names as JSON names;
- ordinary supported System.Text.Json string-enum serialization matching canonical string enum values;
- no serializer option that can omit required members;
- no representation-changing custom converters;
- no representation-changing number handling;
- no reference-preservation output shape;
- no explicit STJ polymorphism/discriminator behavior;
- compatible canonical/STJ extension-data member and value shape;
- no extra serialized CLR members outside the canonical model unless the effective base contract also omits them.

No new dedicated public fidelity-mode/profile API is required by this milestone. Existing APIs and standard System.Text.Json configuration may be composed to produce the baseline.

### Cross-target proof

Add real boundary tests that:

```text
annotated CLR source
    -> real generated or extracted canonical model
    -> STJ domain model/resolver
    -> JsonSerializer output
    -> JSON Schema domain model/export
    -> standards-compliant Draft 2020-12 validation
```

The successful tandem path must not be proved solely from hand-built `TypeSchemaModel` instances.

A test-only JSON Schema validator dependency is permitted.

It must not enter any publishable package dependency graph.

### Projection capability truth

Do not expand the global `SemanticModelFeature` enum with every newer semantic family in M0066.

The detailed JSON semantic coverage matrix lives in `docs/specs/json-representation-fidelity.md`.

Existing projection capability/public documentation must not make claims that contradict the implemented JSON Schema/STJ behavior. Broader cross-target capability-taxonomy redesign remains future work.

## Resolved Semantic and Architecture Decisions

### One-way conformance, not equivalence

The supported guarantee is:

> successful JSON output under the bounded STJ configuration validates against the derived JSON Schema.

The reverse is not promised.

This deliberately allows JSON Schema to describe optional inputs or semantic validity more broadly than STJ write behavior.

### Validation belongs to JSON Schema

STJ does not enforce:

```text
min/max
length
pattern
collection cardinality
RequiredWhen
```

Those remain canonical constraints projected into JSON Schema.

STJ serializer-native `required` metadata and canonical semantic requiredness are related but distinct: serializer read behavior does not replace schema validation.

### Existing STJ default remains

`ExistingJsonContract` remains the default property-name policy.

M0066 does not silently turn all existing consumers into canonical-name JSON serializers.

The fidelity guarantee therefore requires an explicit canonical-name configuration.

### Enums require string wire representation in the baseline

The generated canonical model represents .NET enums as string semantic values.

The tandem configuration must therefore use ordinary supported STJ string-enum serialization so actual wire values match native JSON Schema `enum` values.

M0066 does not change default System.Text.Json enum behavior globally.

### Custom converters remain opaque

Do not inspect converter implementation code or execute converters during schema derivation to infer a schema.

Representation-changing converter metadata excludes the affected contract from the baseline fidelity guarantee unless a future explicit converter wire-shape contract is added.

### Explicit STJ polymorphism is deferred

Current extraction preserves only a coarse polymorphism marker/diagnostic and does not carry a faithful discriminator/derived-type contract.

M0066 must not guess that contract.

Explicit `JsonPolymorphic`/`JsonDerivedType` behavior remains outside tandem fidelity.

Canonical JSON Schema `oneOf`/`anyOf` continues to work for canonical union models independently.

### No new shared JSON layer

Do not add:

```text
SemanticTypeModel.Json
JsonRepresentationModel in Abstractions/Core
JsonSchema -> SystemTextJson dependency
SystemTextJson -> JsonSchema dependency
```

A shared layer requires a later architecture decision backed by concrete pressure.

### `x-stm` remains optional and unversioned

`IncludeSemanticAnnotations=false` produces plain JSON Schema without `x-stm`.

Native JSON Schema semantics remain present.

No x-stm protocol version or compatibility negotiation is added.

Unknown future `x-stm` fields remain tolerated by consumers.

### Additive minor compatibility

This is additive 4.1-line projection work.

No existing public API is removed or renamed.

Expected intentional output changes include:

- new `x-stm` members when semantic annotations are enabled;
- typed `additionalProperties` for representable extension data;
- semantic-order-aware deterministic property ordering.

These are not 4.0.x patch changes.

No canonical persisted-model schema/version or EF manifest version change is required solely for M0066.

## Non-Goals

- production JSON Schema validator or validation API;
- JSON Schema import/roundtrip authoring;
- exact/bidirectional System.Text.Json <-> JSON Schema equivalence;
- new shared JSON package or canonical wire model;
- automatic arbitrary converter schema inference;
- full STJ polymorphism/discriminator support;
- reference-handler `$id`/`$ref` schema modeling;
- automatic compatibility with arbitrary `JsonSerializerOptions`;
- making `ExistingJsonContract` schema-compatible by default;
- generic STJ runtime validation of semantic constraints;
- general `unevaluatedProperties` support;
- unrestricted JSON Schema `if/then/else`, `not`, `dependentSchemas`, dynamic refs, or full Draft 2020-12 parity;
- OpenAPI;
- schema registry/remote reference loading;
- JSON Editor-specific behavior;
- EF Core, Power BI, or Configuration behavior changes;
- global projection capability taxonomy redesign for every newer semantic;
- release preparation or publication.

## Required Project Authority

Implementation reads:

- `AGENTS.md`
- `docs/TERMINOLOGY.md`
- `docs/ARCHITECTURE.md`
- `docs/architecture/code-first-domain-projection-pipeline.md`
- `docs/SPECS.md`
- `docs/specs/core-semantic-vocabulary.md`
- `docs/specs/current-canonical-model-surface.md`
- `docs/specs/evolution-ownership-and-lifecycle-semantics.md`
- `docs/specs/json-representation-fidelity.md`
- `docs/specs/json-schema-domain-model-and-export.md`
- `docs/specs/system-text-json-domain-model-and-resolver-projection.md`
- `docs/specs/system-text-json-contract-integration.md`
- `docs/specs/type-model-projection-capabilities.md`
- `docs/decisions/json-schema-uses-x-stm-for-selected-semantics.md`
- `docs/ENGINEERING.md`
- `docs/engineering/dotnet.md`
- `docs/engineering/command-contract.md`
- `docs/engineering/packaging.md`
- this milestone

Implementation inspects the live JSON Schema, System.Text.Json, .NET extraction/generator, canonical model, tests, fixtures, and package-smoke code required to choose mechanics.

Ordinary implementation must **not** read:

- the external guide repository;
- the planning conversation;
- `.guide-profile.json`;
- `.guide-sync/`;
- copied setup/engineering guides or research;
- completed historical milestones unless current authority/live code cannot resolve a concrete behavior.

## Acceptance Criteria

### JSON Schema semantic coverage

Automated tests prove that every public semantic authoring attribute is classified by the cross-target spec.

For projection-neutral semantics that apply to JSON Schema:

- native JSON Schema behavior remains native;
- approved STM-only semantics appear under `x-stm`;
- target-specific/acquisition-only attributes do not leak into `x-stm`.

### Display Identity and Access Paths

For a real generated model:

```text
Display Identity -> ordered emitted property names
Access Paths -> deterministic named path object + ordered emitted property names
```

No internal `schema.displayIdentity` / `schema.accessPath.*` annotation encoding leaks into exported JSON.

### Lifecycle / ownership / envelope semantics

Tests cover deterministic preservation of:

```text
ownership
envelope purpose/payload/metadata
versioned
version
revision
currentVersion
temporalValidity
validFrom
validTo
lifecycleState
extensionData
```

The projection does not add business behavior beyond semantic preservation/native structural schema.

### Enum metadata

- native `enum` values remain authoritative;
- semantic enum-value display/user/technical metadata is preserved through `x-stm.enumValues` when applicable;
- enum metadata ordering is deterministic and aligned with native enum ordering;
- native enum values are not duplicated inside x-stm.

### Extension data

Tests prove:

- the extension-data bag is not emitted as a normal property;
- representable value type -> typed `additionalProperties`;
- unsupported value shape -> explicit diagnostic + safe permissive fallback;
- semantic annotations enabled -> `x-stm.extensionData = true`;
- semantic annotations disabled -> no x-stm marker while native openness behavior remains.

### RequiredWhen

Tests prove the supported equality case produces valid conditional requiredness using the typed canonical literal.

General arbitrary JSON Schema conditional composition is not added.

### Determinism

- property output uses semantic order where present, then stable emitted name;
- x-stm object/path/member ordering is deterministic;
- repeated export produces byte-equivalent deterministic content under the same export formatting contract.

### System.Text.Json domain model

For imported STJ metadata, domain derivation/inspection can observe the required representation metadata listed in Scope.

Existing name matching and resolver composition remain working.

`ExistingJsonContract` remains the default.

### Tandem output conformance

A real annotated CLR fixture proves the full supported boundary:

```text
generated/extracted canonical model
+ STJ canonical-name configuration
+ string enum serialization
-> actual JSON
-> derived/exported JSON Schema
-> Draft 2020-12 validation succeeds
```

Minimum successful scenarios include:

- nested object;
- required/optional/nullable properties;
- enum;
- scalar and collection constraints on valid values;
- collection;
- ownership;
- extension data using compatible STJ/core declaration;
- current semantic metadata including Display Identity and Access Paths.

JSON Schema-focused validation tests separately prove invalid semantic values are rejected for representable constraints; STJ itself is not expected to reject them.

### Fidelity exclusions are explicit

Automated negative coverage proves no false guarantee for at least:

- representation-changing custom converter;
- representation-changing number handling;
- explicit STJ polymorphism;
- serializer behavior capable of omitting a schema-required member.

Existing diagnostics may be reused where they already express the conflict. New public diagnostics, if implementation proves necessary, must follow repository diagnostic allocation/stability rules and become project/public truth before milestone completion.

### Package/dependency boundary

- `SemanticTypeModel.JsonSchema` does not reference `SemanticTypeModel.SystemTextJson`;
- `SemanticTypeModel.SystemTextJson` does not reference `SemanticTypeModel.JsonSchema`;
- no new shared JSON package is introduced;
- any Draft 2020-12 validator dependency is test-only and absent from publishable package dependency metadata.

### Regression compatibility

- existing JSON Schema tests remain valid except snapshots intentionally changed by the resolved new fidelity behavior;
- existing STJ default contract-preservation tests remain green;
- user-owned `JsonSerializerContext` composition remains supported;
- models without newly covered semantic annotations retain their prior native schema semantics;
- `IncludeSemanticAnnotations=false` remains a plain-schema escape hatch.

### Packed consumer boundary

Packed-package smoke consumes the packed:

```text
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
SemanticTypeModel.SystemTextJson
SemanticTypeModel.JsonSchema
```

packages from `artifacts/nuget`.

The smoke consumer must exercise at least one supported tandem serialization/schema scenario from real annotated CLR source.

The package smoke must not use project references for the packages being proven.

## Validation

### Tier 1 — focused

Run affected projects through canonical wrappers:

```sh
./eng/test-project.sh tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/SemanticTypeModel.JsonSchema.Tests.Unit.csproj
./eng/test-project.sh tests/unit/SemanticTypeModel.SystemTextJson.Tests.Unit/SemanticTypeModel.SystemTextJson.Tests.Unit.csproj
./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit/SemanticTypeModel.DotNet.Tests.Unit.csproj
./eng/test-project.sh tests/unit/SemanticTypeModel.Core.Tests.Unit/SemanticTypeModel.Core.Tests.Unit.csproj
```

Useful filters during iteration:

```sh
./eng/test-filter.sh JsonRepresentation
./eng/test-filter.sh JsonSchema
./eng/test-filter.sh SystemTextJson
./eng/test-filter.sh ExtensionData
```

Exact test names/files are implementation-owned.

### Tier 2 — completion gate

Required:

```sh
./eng/check.sh
```

### Tier 3 — packed boundary

Required non-publishing candidate validation:

```sh
./eng/package.sh 4.1.0-m0066
./eng/package-smoke.sh 4.1.0-m0066
```

If implementation changes runnable sample projects, also run:

```sh
./eng/samples.sh
```

Do not publish.

## Validation Execution Mode

| Validation | Mode |
|---|---|
| Tier 1 focused tests | direct |
| Tier 2 repository check | direct |
| Tier 3 package + smoke | direct |
| Optional sample validation when sample source changes | direct |

No resumable/sharded validation infrastructure is required.

If an agent runtime cannot complete an aggregate command, run that unchanged command in CI or another capable environment. Partial child-command output is not aggregate success evidence.

## Capability-Provider vs Consumer Validation

M0066 is capability-provider work.

Required proof is bounded to the library's published package behavior and packed consumer smoke.

Do not broaden validation into an application/product-specific API, database, UI, or deployment scenario.

The tandem smoke consumer is a capability-consumer fixture used only to prove the package boundary.

## Direct Documentation Impact

Planning directly updates:

- the architecture boundary for coordinated JSON projections;
- the cross-target fidelity specification;
- the authoritative JSON Schema export specification;
- the accepted x-stm decision;
- terminology and spec routing;
- milestone routing.

Implementation should update those same authority documents only if live mechanics reveal a non-material clarification that does not change the resolved contract.

Consumer-facing documentation synchronization is deferred through the guide-sync hint.

## Deferred Documentation Synchronization

See:

```text
.guide-sync/pending/m0066-json-representation-fidelity.md
```

Ordinary implementation agents do not read or resolve this hint.

## Human Review

Applicability: **none**.

Reason: acceptance is objectively decidable through deterministic model/export assertions, real serializer/schema validation, dependency inspection, Tier 2 validation, and packed-package smoke.

No `.review/` request or human completion gate is required.

## Constrained Runtime

No long-running or resumable suite is introduced.

The required aggregate commands are repository-standard and must complete as aggregate commands.

Do not add milestone-specific shard/receipt infrastructure.

## Escalation Boundary

Return M0066 to planning before implementation changes any of these decisions:

- adding a new shared JSON package/model;
- introducing a direct JsonSchema <-> SystemTextJson package dependency;
- changing the default STJ property-name policy away from `ExistingJsonContract`;
- adding a production JSON Schema validator/API/dependency;
- promising bidirectional JSON Schema/STJ equivalence;
- automatically inferring arbitrary converter wire shape;
- implementing explicit STJ polymorphism/discriminator fidelity;
- adding general reference-preservation schema support;
- changing the canonical persisted model solely for JSON fidelity;
- changing EF semantic manifest schema/version;
- expanding M0066 into EF, Power BI, Configuration, OpenAPI, or UI behavior;
- redesigning the global projection-capability taxonomy for all targets.

Local record/helper structure, test organization, diagnostic message wording, transformation decomposition, and implementation sequence remain implementation-owned unless they would alter the contracts above.
