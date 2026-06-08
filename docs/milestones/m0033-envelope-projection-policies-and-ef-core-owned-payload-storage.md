# M0033: Envelope Projection Policies and EF Core Owned Payload Storage

## Status

Planned.

## Maturity Mode

Post-2.0 domain projection hardening for a public package set.

The repository has released the code-first semantic model architecture, domain semantic models for JSON Schema, EF Core, and Power BI, and the core `Envelope` semantic. This milestone hardens how envelope payloads are projected across those domains, with the deepest implementation work in EF Core storage policies.

## Task Mode

Milestone implementation routing and cross-domain projection policy implementation.

This milestone extends the M0032 envelope semantic without redefining it. It defines target-specific envelope projection policies for JSON Schema, EF Core, and Power BI, and implements opinionated EF Core owned/JSON storage behavior for envelope payloads and value-object graphs.

Do not introduce TBPs, issue templates, workflow YAML, non-root README files, generated code files, broad public-documentation rewrites, migrations, database creation, service publishing, PBIX generation, or deployment automation in this planning package.

## Goal

After M0033, a consumer can use a single semantic envelope type across JSON Schema, EF Core, and Power BI with explicit, opinionated target behavior.

Canonical example:

```csharp
[SemanticEnvelope(Purpose = SemanticEnvelopePurpose.Management)]
[SemanticEntity]
public sealed class ManagedSpecificationEnvelope
{
    [SemanticKey]
    public required Guid Id { get; init; }

    [SemanticEnvelopeMetadata]
    public required long Revision { get; init; }

    [SemanticEnvelopeMetadata]
    public required string ModifiedBy { get; init; }

    [SemanticEnvelopeMetadata]
    public required DateTimeOffset ModifiedAt { get; init; }

    [SemanticEnvelopePayload]
    public required WorkflowSpecification Specification { get; init; }
}
```

Default target behavior:

| Target | Envelope default | Payload default |
|---|---|---|
| Core | Preserve envelope, metadata, and payload semantics. | No storage/export decision. |
| JSON Schema | Envelope contract root when envelope is selected. | Structured payload schema by `$ref`. |
| EF Core | Envelope persistence entity/table. | Serialized JSON column. |
| Power BI | Envelope metadata table. | Payload body ignored or summarized unless explicitly projected. |

## Required Authority

Read these documents before implementing any focus area:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/SPECS.md
docs/specs/core-semantic-vocabulary.md
docs/specs/envelope-projection-policies.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/decisions/envelope-projection-policies-are-target-specific.md
```

Read these only when the selected focus area touches the relevant component:

```text
docs/specs/type-model-core.md
docs/specs/type-model-annotations.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/diagnostics.md
docs/PUBLIC-DOCS.md
public-docs/guides/core-semantics.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/release-notes.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Add an authoritative envelope projection policy specification.
- Define shared projection policy concepts:
  - envelope-as-root;
  - payload-as-root;
  - envelope-and-payload;
  - payload structured reference;
  - payload inline structured representation;
  - payload serialized JSON;
  - payload opaque JSON/document;
  - payload ignored;
  - payload summary.
- Define JSON Schema envelope projection policies and defaults.
- Define EF Core envelope payload storage policies and defaults.
- Define EF Core owned value-object reference and collection storage policies.
- Define Power BI envelope analytical projection policies and defaults.
- Add diagnostics for ambiguous or unsupported envelope/payload projection policy.
- Add an EF Core managed-specification-envelope sample that can demonstrate:
  - default serialized JSON payload;
  - owned JSON payload where supported;
  - owned same-table columns for `OwnsOne`/complex property style mapping;
  - owned collection mapping using `OwnsMany` where configured.
- Add JSON Schema and Power BI sample coverage for the same envelope model.
- Keep all behavior deterministic and inspectable.

### Out of Scope

```text
provider-specific JSON path indexes
computed columns over JSON paths
SQL Server/PostgreSQL-specific JSON tuning
migration generation
database creation
query optimization
automatic database performance recommendations
complex owned inheritance
shared owned instances
provider-specific conversion magic
Power BI Service publishing
PBIX generation
Power BI REST orchestration
full TOM parity
DAX generation from payload graphs
JSON Schema runtime validation
```

## Package Boundary

| Package | Responsibility |
|---|---|
| `SemanticTypeModel.Core` | Preserve envelope/payload/metadata semantics and shared projection policy concepts if needed by public contracts. |
| `SemanticTypeModel.JsonSchema` | JSON Schema envelope root and payload representation policies. |
| `SemanticTypeModel.EFCore` | EF Core envelope payload storage policies, owned mapping policies, `ModelBuilder` configuration, diagnostics, and inspection. |
| `SemanticTypeModel.PowerBI` | Power BI envelope analytical projection policies, diagnostics, and inspection. |
| `SemanticTypeModel.DotNet` / `SemanticTypeModel.Generators` | Existing envelope attribute extraction and generated metadata support when sample/generated tests require it. |

## Focus Areas

### Focus Area 1 — Shared Envelope Projection Policy Model

#### Intent

Define a common conceptual model so JSON Schema, EF Core, and Power BI make consistent envelope/payload decisions while keeping representation target-specific.

#### Required Authority

```text
docs/specs/core-semantic-vocabulary.md
docs/specs/envelope-projection-policies.md
docs/decisions/envelope-projection-policies-are-target-specific.md
```

#### Implementation Requirements

- Preserve the M0032 invariant: envelope semantics do not erase payload semantics.
- Define target-neutral policy concepts in core or shared abstractions only if needed by public API:
  - `EnvelopeAsRoot`;
  - `PayloadAsRoot`;
  - `EnvelopeAndPayload`;
  - `Structured`;
  - `Reference`;
  - `Inline`;
  - `Serialized`;
  - `Opaque`;
  - `Ignored`;
  - `Summary`.
- Keep concrete representation choices in target packages.
- Emit diagnostics when both envelope and payload are selected as roots without explicit policy.
- Keep inspection output deterministic.

#### Validation

- Tier 1:
  - shared policy model tests if new shared API is introduced;
  - diagnostics tests for ambiguous root selection;
  - inspection tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/envelope-projection-policies.md
docs/decisions/envelope-projection-policies-are-target-specific.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/core-semantics.md
public-docs/release-notes.md
```

### Focus Area 2 — JSON Schema Envelope Projection Policies

#### Intent

Make JSON Schema contracts explicit for envelope vs payload views.

#### Required Authority

```text
docs/specs/envelope-projection-policies.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
```

#### Required Policies

```text
EnvelopeAsRoot
PayloadAsRoot
EnvelopeWithPayloadRef
EnvelopeWithInlinePayload
EnvelopeWithJsonDocumentPayload
EnvelopeWithSerializedJsonStringPayload
EnvelopeWithOpaquePayload
```

#### Defaults

```text
Envelope selected as root:
  export envelope object.

Envelope payload default:
  structured payload schema by $ref.

Payload selected as root:
  export payload schema only.

Opaque or serialized payload:
  explicit only.
```

#### Candidate API Shape

Equivalent behavior is required:

```csharp
var result = semanticModel.DeriveJsonSchemaModel(options =>
{
    options.UseDefaultTransformations();

    options.Envelopes.For<ManagedSpecificationEnvelope>()
        .UseEnvelopeAsRoot()
        .Payload(x => x.Specification)
        .RepresentAsStructuredReference();
});
```

Explicit opaque/serialized behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsRoot()
    .Payload(x => x.Specification)
    .RepresentAsJsonDocument();

options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsRoot()
    .Payload(x => x.Specification)
    .RepresentAsSerializedJsonString();
```

#### Validation

- Tier 1:
  - envelope root schema tests;
  - payload root schema tests;
  - structured `$ref` payload tests;
  - inline payload tests;
  - JSON document/opaque payload tests;
  - serialized JSON string payload tests;
  - ambiguity diagnostics tests;
  - deterministic schema snapshot tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/envelope-projection-policies.md
docs/specs/json-schema-domain-model-and-export.md only if behavior changes directly
```

#### Deferred Documentation Impact

```text
public-docs/guides/core-semantics.md
public-docs/release-notes.md
```

### Focus Area 3 — EF Core Envelope Payload Storage Policies

#### Intent

Give EF Core consumers a solid, opinionated way to store envelope payloads while leaving advanced database tuning to user code.

#### Required Authority

```text
docs/specs/envelope-projection-policies.md
docs/specs/type-model-ef-core-projection.md
docs/decisions/ef-core-integration-stops-at-modelbuilder-configuration.md
```

#### Required Policies

```text
SerializedJson
OwnedJson
OwnedSameTable
OwnedSeparateTable
Ignored
```

#### Defaults

```text
Envelope:
  map as EF entity/table when selected for EF projection.

Envelope metadata:
  map as normal scalar columns.

Envelope payload:
  SerializedJson by default.
```

#### Candidate API Shape

Equivalent behavior is required:

```csharp
var result = semanticModel.DeriveEfCoreModel(options =>
{
    options.UseDefaultTransformations();

    options.Envelopes.For<ManagedSpecificationEnvelope>()
        .UseEnvelopeAsEntity()
        .Payload(x => x.Specification)
        .StoreAsSerializedJson(columnName: "SpecificationJson");
});
```

Owned JSON:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsEntity()
    .Payload(x => x.Specification)
    .StoreAsOwnedJson(columnName: "Specification");
```

Owned same-table columns:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsEntity()
    .Payload(x => x.Specification)
    .StoreAsOwnedColumns(prefix: "Specification");
```

Ignored payload:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsEntity()
    .Payload(x => x.Specification)
    .IgnorePayload();
```

#### Implementation Requirements

- `SerializedJson` uses a configured serializer/converter and maps the payload as one scalar column.
- `OwnedJson` uses EF Core owned JSON mapping where supported by the referenced EF Core version and configured provider capabilities.
- `OwnedSameTable` maps owned reference members as deterministic same-table columns with configurable prefix.
- `OwnedSeparateTable` maps owned reference or collection payloads to separate table(s) when explicitly configured.
- Provider-specific column types and tuning are user configuration, not default behavior.
- Payload semantics remain available in the semantic model even when EF stores the payload as serialized JSON.

#### Validation

- Tier 1:
  - default serialized JSON envelope tests;
  - serializer/converter tests;
  - owned JSON tests where supported;
  - same-table owned columns tests;
  - ignored payload tests;
  - provider-neutral `ModelBuilder` metadata tests;
  - diagnostics tests for unsupported storage policy;
  - deterministic inspection tests.
- Tier 2 before completion if code changes.
- Tier 3 only if package consumption/sample behavior changes.

#### Direct Documentation Impact

```text
docs/specs/envelope-projection-policies.md
docs/specs/type-model-ef-core-projection.md only if behavior changes directly
```

#### Deferred Documentation Impact

```text
public-docs/guides/ef-core-projection.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/samples.md
public-docs/release-notes.md
```

### Focus Area 4 — EF Core Owned Value-Object Reference and Collection Policies

#### Intent

Extend the EF Core package beyond envelope payloads so value-object graphs can be stored consistently.

#### Required Authority

```text
docs/specs/envelope-projection-policies.md
docs/specs/type-model-ef-core-projection.md
docs/specs/core-semantic-vocabulary.md
```

#### Required Policies

```text
OwnedReferenceSameTable
OwnedReferenceJson
OwnedReferenceSeparateTable
OwnedCollectionJson
OwnedCollectionSeparateTable
SerializedJson
Ignored
```

#### Defaults

```text
Value-object reference:
  OwnedSameTable.

Value-object collection:
  diagnostic unless a package default or explicit policy is configured.
```

#### Implementation Requirements

- Support `OwnsOne`/complex-property-style mapping for value-object references.
- Support `OwnsMany` for value-object collections when explicit policy supplies enough key/table/order information.
- Support JSON aggregate mapping for owned reference graphs where configured.
- Apply deterministic column/table naming and configurable prefixes.
- Enforce depth limit and emit diagnostics for unsupported nesting.
- Avoid provider-specific tuning by default.

#### Validation

- Tier 1:
  - value-object reference same-table tests;
  - value-object reference JSON tests;
  - value-object collection JSON tests;
  - value-object collection separate-table tests;
  - deterministic naming tests;
  - duplicate column/table diagnostics tests;
  - max-depth diagnostics tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/envelope-projection-policies.md
docs/specs/type-model-ef-core-projection.md only if behavior changes directly
```

#### Deferred Documentation Impact

```text
public-docs/guides/ef-core-projection.md
public-docs/samples.md
public-docs/release-notes.md
```

### Focus Area 5 — Power BI Envelope Analytical Projection Policies

#### Intent

Make Power BI envelope behavior opinionated and safe for analytical models.

#### Required Authority

```text
docs/specs/envelope-projection-policies.md
docs/specs/type-model-powerbi-tom-projection.md
docs/decisions/power-bi-integration-stops-at-local-metadata-projection.md
```

#### Required Policies

```text
MetadataOnly
MetadataWithPayloadSummary
FlattenPayload
PayloadAsSeparateTables
PayloadAsRoot
IgnoreEnvelope
IgnorePayload
```

#### Defaults

```text
Envelope:
  project envelope metadata table.

Envelope payload:
  ignore payload body by default or expose configured summary metadata only.

Payload analysis:
  explicit opt-in.
```

#### Candidate API Shape

Equivalent behavior is required:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeMetadataOnly();
```

Payload summary:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeMetadataWithPayloadSummary(summary =>
    {
        summary.IncludePayloadType();
        summary.IncludePayloadVersion();
        summary.IncludePayloadHash();
    });
```

Payload analysis:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .ProjectPayloadAsSeparateTables();

options.Envelopes.For<ManagedSpecificationEnvelope>()
    .FlattenPayload(prefix: "Specification");

options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UsePayloadAsAnalyticalRoot();
```

#### Validation

- Tier 1:
  - metadata-only envelope projection tests;
  - payload summary tests;
  - flatten payload tests;
  - payload-as-separate-tables tests if implemented;
  - payload-as-root tests;
  - deterministic local metadata export tests;
  - unsupported/ambiguous policy diagnostics tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/envelope-projection-policies.md
docs/specs/type-model-powerbi-tom-projection.md only if behavior changes directly
```

#### Deferred Documentation Impact

```text
public-docs/guides/power-bi-projection.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/samples.md
public-docs/release-notes.md
```

### Focus Area 6 — Unified Managed Specification Envelope Sample

#### Intent

Give users one realistic sample that demonstrates the same envelope model across JSON Schema, EF Core, and Power BI.

#### Required Authority

```text
docs/specs/envelope-projection-policies.md
docs/engineering/samples.md
public-docs/samples.md
```

#### Sample Scenario

```text
ManagedSpecificationEnvelope
  Id
  Revision
  ModifiedBy
  ModifiedAt
  Specification: WorkflowSpecification
```

The sample should demonstrate:

```text
JSON Schema:
  envelope contract with structured payload reference.

EF Core:
  envelope entity with default serialized JSON payload column.
  optional owned JSON payload variant.
  optional owned same-table payload variant.

Power BI:
  envelope metadata table by default.
  optional payload summary or payload analysis policy.
```

#### Validation

- Tier 1:
  - sample compile/run tests;
  - deterministic output snapshot tests;
  - no live database requirement;
  - no Power BI Service/PBIX requirement.
- Tier 2 before completion if code changes.
- Tier 3 only if package/sample consumption behavior changes.

#### Direct Documentation Impact

```text
docs/specs/envelope-projection-policies.md
```

#### Deferred Documentation Impact

```text
public-docs/samples.md
public-docs/samples/*.md
public-docs/release-notes.md
```

## Required Acceptance Criteria

M0033 is complete when:

- `docs/specs/envelope-projection-policies.md` exists and is authoritative.
- The target-specific envelope projection decision is documented.
- Shared envelope projection concepts are defined without moving target-specific representation into core semantics.
- JSON Schema supports envelope-as-root, payload-as-root, structured reference payload, inline payload, JSON document payload, serialized JSON string payload, and opaque payload policies.
- JSON Schema defaults to structured payload `$ref` when envelope is selected as root.
- EF Core supports serialized JSON, owned JSON, owned same-table, owned separate-table, and ignored payload policies for envelope payloads.
- EF Core defaults envelope payload storage to serialized JSON.
- EF Core maps envelope metadata as normal scalar columns.
- EF Core supports owned value-object reference storage and configured collection storage policies.
- Power BI supports metadata-only, metadata-with-summary, flatten-payload, payload-as-separate-tables, payload-as-root, ignore-envelope, and ignore-payload policies.
- Power BI defaults envelope projection to metadata-oriented behavior and does not flatten payloads unless explicitly configured.
- Ambiguous envelope/payload root selection emits diagnostics.
- Unsupported payload representation emits diagnostics.
- Inspection output is deterministic for configured envelope policies.
- A managed specification envelope sample demonstrates JSON Schema, EF Core, and Power BI behavior.
- Tier 2 validation passes if code changes, or inability to run it is explicitly reported with exact lower-tier validation performed.
- No TBPs, issue templates, non-root README files, workflow YAML, broad public-doc rewrites, migrations, database creation, service deployment, PBIX generation, or generated code files are introduced by the planning package itself.

## Validation Plan

Use the smallest validation tier that can catch the expected regression.

### Tier 1

Use focused validation for:

```text
envelope projection policy tests
JSON Schema envelope policy tests
EF Core envelope storage policy tests
EF Core owned value-object tests
Power BI envelope analytical policy tests
diagnostics tests
inspection snapshot tests
managed specification envelope sample tests
```

Expected command shape:

```sh
./eng/test-filter.sh <envelope-projection-policy-filter>
./eng/test-project.sh <json-schema-test-project>
./eng/test-project.sh <ef-core-test-project>
./eng/test-project.sh <powerbi-test-project>
./eng/check-affected.sh src/SemanticTypeModel.JsonSchema src/SemanticTypeModel.EFCore src/SemanticTypeModel.PowerBI tests samples
```

Use actual repository project names after inspecting the solution.

### Tier 2

Run before completing implementation work when code changes:

```sh
./eng/check.sh
```

### Tier 3

Run only if package layout, package README generation, public API baseline, or package consumption behavior changes:

```sh
./eng/package.sh <version>
./eng/package-smoke.sh <version>
./eng/public-api.sh
./eng/samples.sh
```

This is not a release publication milestone; do not run publish validation.

## Direct Documentation Impact

The implementation should directly update:

```text
docs/specs/envelope-projection-policies.md
docs/decisions/envelope-projection-policies-are-target-specific.md
```

Update these only when implementation changes their behavior directly:

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/diagnostics.md
```

## Deferred Documentation Impact

Leave explicit notes for a later documentation synchronization pass covering:

```text
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
README.md
public-docs/guides/core-semantics.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md if new diagnostics are public
public-docs/release-notes.md
```

Do not perform broad public documentation synchronization as part of this implementation milestone unless a consumer-facing behavior change directly requires it.
