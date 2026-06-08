# Envelope Projection Policies Specification

## Status

Authoritative behavioral specification for envelope projection policies.

## Purpose

Define how the core `Envelope`, `EnvelopePayload`, and `EnvelopeMetadata` semantics are projected by JSON Schema, EF Core, and Power BI.

This specification is authoritative for:

- shared envelope projection policy concepts;
- JSON Schema envelope contract policies;
- EF Core envelope payload storage policies;
- EF Core owned value-object storage policies;
- Power BI envelope analytical projection policies;
- cross-target diagnostics for ambiguous or unsupported envelope projection.

## Core Principle

Core envelope semantics describe the wrapper boundary, distinguished payload, and envelope metadata.

Target packages decide representation:

```text
JSON Schema decides contract shape.
EF Core decides storage shape.
Power BI decides analytical shape.
```

Envelope projection policies must not erase payload semantics.

## Canonical Example

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

`ManagedSpecificationEnvelope` is the management, audit, revision, persistence, or transport boundary.

`WorkflowSpecification` remains the semantic payload.

## Default Target Matrix

| Target | Envelope default | Payload default |
|---|---|---|
| Core | Preserve envelope, metadata, and payload semantics. | No storage/export decision. |
| JSON Schema | Envelope object as root when envelope is selected. | Structured payload schema by `$ref`. |
| EF Core | Envelope entity/table when selected for EF projection. | Serialized JSON column. |
| Power BI | Envelope metadata table. | Ignore payload body or expose configured summary only. |

## Shared Policy Concepts

Target packages may expose target-specific enum names, but these conceptual roles must be preserved.

### Projection Root

```text
EnvelopeAsRoot
PayloadAsRoot
EnvelopeAndPayload
```

Rules:

- `EnvelopeAsRoot` projects the wrapper as the target root.
- `PayloadAsRoot` projects the payload as the target root and omits envelope metadata unless explicitly configured.
- `EnvelopeAndPayload` projects both but requires explicit relationship/linking policy.
- Selecting both envelope and payload as roots without explicit policy emits diagnostics.

### Payload Representation

```text
Structured
Reference
Inline
Serialized
Opaque
Ignored
Summary
```

Rules:

- `Structured` preserves the payload schema/model shape.
- `Reference` uses target reference semantics, such as JSON Schema `$ref`.
- `Inline` embeds payload members in the envelope/root output.
- `Serialized` stores or exposes the payload as serialized data.
- `Opaque` indicates payload content exists but is not described structurally.
- `Ignored` omits payload content from the target output.
- `Summary` exposes limited metadata such as payload type, version, hash, or count.

## JSON Schema Policies

### Supported Policies

```text
EnvelopeAsRoot
PayloadAsRoot
EnvelopeWithPayloadRef
EnvelopeWithInlinePayload
EnvelopeWithJsonDocumentPayload
EnvelopeWithSerializedJsonStringPayload
EnvelopeWithOpaquePayload
```

### Defaults

```text
Envelope selected as root:
  export envelope object.

Payload default:
  structured payload schema by $ref.

Payload selected as root:
  export payload schema only.

Opaque/serialized payload:
  explicit only.
```

### Envelope with Structured Payload Reference

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsRoot()
    .Payload(x => x.Specification)
    .RepresentAsStructuredReference();
```

Expected schema shape:

```text
ManagedSpecificationEnvelope
  type: object
  properties:
    id
    revision
    modifiedBy
    modifiedAt
    specification: $ref WorkflowSpecification
```

### Envelope with Inline Payload

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsRoot()
    .Payload(x => x.Specification)
    .RepresentInline();
```

Rules:

- payload properties are represented inline under the payload property unless flattening is explicitly configured;
- duplicate names emit diagnostics;
- circular graphs emit diagnostics or references according to JSON Schema policy.

### JSON Document Payload

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsRoot()
    .Payload(x => x.Specification)
    .RepresentAsJsonDocument();
```

Expected schema shape is an open JSON value/object according to configured policy.

### Serialized JSON String Payload

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsRoot()
    .Payload(x => x.Specification)
    .RepresentAsSerializedJsonString();
```

Expected schema shape:

```text
payload:
  type: string
  contentMediaType: application/json
```

### Payload as Root

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UsePayloadAsRoot(x => x.Specification);
```

Expected schema shape is the payload schema without envelope metadata.

## EF Core Policies

### Supported Envelope Payload Storage Policies

```text
SerializedJson
OwnedJson
OwnedSameTable
OwnedSeparateTable
Ignored
```

### Defaults

```text
Envelope:
  map as EF entity/table when selected for EF projection.

Envelope metadata:
  map as normal scalar columns.

Envelope payload:
  SerializedJson.
```

### SerializedJson

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsEntity()
    .Payload(x => x.Specification)
    .StoreAsSerializedJson(columnName: "SpecificationJson");
```

ModelBuilder intent:

```csharp
entity.Property(x => x.Specification)
    .HasColumnName("SpecificationJson")
    .HasConversion(
        payload => serializer.Serialize(payload),
        json => serializer.Deserialize<WorkflowSpecification>(json));
```

Rules:

- maps the payload as one scalar column;
- uses configured serializer/converter;
- does not map nested payload members;
- preserves payload semantics in the semantic model;
- provider-specific column type is optional user configuration, not the default.

### OwnedJson

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsEntity()
    .Payload(x => x.Specification)
    .StoreAsOwnedJson(columnName: "Specification");
```

ModelBuilder intent:

```csharp
entity.OwnsOne(x => x.Specification, owned =>
{
    owned.ToJson("Specification");
    owned.OwnsMany(x => x.Steps);
});
```

Rules:

- uses EF Core owned JSON aggregate mapping where supported;
- `ToJson` is configured at the aggregate root;
- nested `OwnsOne` and `OwnsMany` are configured below the aggregate root where supported;
- unsupported provider/version/shape emits diagnostics.

### OwnedSameTable

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsEntity()
    .Payload(x => x.Specification)
    .StoreAsOwnedColumns(prefix: "Specification");
```

ModelBuilder intent:

```csharp
entity.OwnsOne(x => x.Specification, owned =>
{
    owned.Property(x => x.Name).HasColumnName("Specification_Name");
});
```

Rules:

- maps owned reference members as same-table columns;
- deterministic prefix is required;
- duplicate column names emit diagnostics;
- max nesting depth is configurable and diagnosable.

### OwnedSeparateTable

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsEntity()
    .Payload(x => x.Specification)
    .StoreAsOwnedTable("SpecificationPayloads");
```

Rules:

- explicit table name is required unless deterministic naming policy is configured;
- owner foreign key is configured deterministically;
- collections require key/order policy.

### Ignored

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeAsEntity()
    .Payload(x => x.Specification)
    .IgnorePayload();
```

Rules:

- payload is not mapped by EF Core;
- payload semantics remain present in the semantic model;
- use only when payload is handled outside EF.

## EF Core Owned Value-Object Policies

### Supported Policies

```text
OwnedReferenceSameTable
OwnedReferenceJson
OwnedReferenceSeparateTable
OwnedCollectionJson
OwnedCollectionSeparateTable
SerializedJson
Ignored
```

### Defaults

```text
Value-object reference:
  OwnedSameTable.

Value-object collection:
  diagnostic unless explicit policy or package default is configured.
```

### Required Behavior

- `OwnsOne`/complex-property-style mapping for value-object references.
- `OwnsMany` for value-object collections when explicit policy supplies enough key/table/order information.
- JSON aggregate mapping for owned reference graphs where configured.
- Deterministic column/table naming and configurable prefixes.
- Diagnostics for duplicate names, unsupported nesting, unsupported inheritance, and ambiguous ownership.

## Power BI Policies

### Supported Policies

```text
MetadataOnly
MetadataWithPayloadSummary
FlattenPayload
PayloadAsSeparateTables
PayloadAsRoot
IgnoreEnvelope
IgnorePayload
```

### Defaults

```text
Envelope:
  project envelope metadata table.

Payload:
  ignore payload body or expose configured summary metadata only.

Payload analysis:
  explicit opt-in.
```

### MetadataOnly

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeMetadataOnly();
```

Expected analytical shape:

```text
ManagedSpecifications
  Id
  Revision
  ModifiedBy
  ModifiedAt
```

### MetadataWithPayloadSummary

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UseEnvelopeMetadataWithPayloadSummary(summary =>
    {
        summary.IncludePayloadType();
        summary.IncludePayloadVersion();
        summary.IncludePayloadHash();
    });
```

Expected analytical shape:

```text
ManagedSpecifications
  Id
  Revision
  ModifiedBy
  ModifiedAt
  PayloadType
  PayloadVersion
  PayloadHash
```

### FlattenPayload

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .FlattenPayload(prefix: "Specification");
```

Rules:

- only deterministic scalar/value-object payload paths are flattened;
- collection payload paths require explicit policy;
- duplicate names emit diagnostics;
- default behavior must not flatten payloads implicitly.

### PayloadAsSeparateTables

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .ProjectPayloadAsSeparateTables();
```

Rules:

- payload tables are linked back to the envelope through deterministic relationship metadata;
- collection expansion is explicit and diagnosable;
- output remains local metadata only.

### PayloadAsRoot

Candidate behavior:

```csharp
options.Envelopes.For<ManagedSpecificationEnvelope>()
    .UsePayloadAsAnalyticalRoot();
```

Rules:

- envelope metadata is omitted unless configured as contextual columns or related table;
- ambiguous dual-root projections emit diagnostics.

## Diagnostics

Required diagnostic classes:

```text
envelope and payload both selected as projection root without explicit policy
payload representation unsupported by target
envelope payload policy conflicts with target package boundary
JSON Schema serialized payload requested without media type policy
JSON Schema inline payload creates duplicate property names
EF Core serialized JSON payload has no serializer policy
EF Core owned JSON requested but provider/version/shape is unsupported
EF Core owned same-table mapping creates duplicate column names
EF Core owned mapping exceeds configured max depth
EF Core owns-many collection lacks key/order policy
Power BI payload flattening creates duplicate names
Power BI payload collection requires explicit expansion policy
Power BI payload analysis requested for unsupported shape
provider-specific tuning requested without provider policy
```

Diagnostics must include model path and related model paths where available.

## Inspection

Inspection output must show:

```text
envelope type
payload property
metadata properties
target projection root policy
target payload representation/storage/analytical policy
serializer/converter policy when relevant
owned mapping strategy when relevant
diagnostics related to envelope policy
```

Inspection output must be deterministic and suitable for snapshot tests.

## Sample Requirement

A managed specification envelope sample must demonstrate the same semantic model across:

```text
JSON Schema:
  envelope contract with structured payload reference.

EF Core:
  default serialized JSON payload.
  optional owned JSON payload.
  optional owned same-table payload.

Power BI:
  metadata-only envelope table.
  optional payload summary or payload analysis.
```

The sample must not require a live database, migration generation, Power BI Desktop, Power BI Service, PBIX generation, XMLA, credentials, or network access.
