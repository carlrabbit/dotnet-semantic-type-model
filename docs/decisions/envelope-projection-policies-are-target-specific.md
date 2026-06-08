# Decision: Envelope Projection Policies Are Target-Specific

## Status

Accepted for M0033.

## Context

M0032 introduced `Envelope`, `EnvelopePayload`, and `EnvelopeMetadata` as core projection-neutral semantics.

A single envelope model is commonly projected to multiple targets:

```text
JSON Schema for external or management contracts.
EF Core for persistence/cache storage.
Power BI for analytical reporting over envelope metadata and, sometimes, payload content.
```

Those targets need different representation choices. JSON Schema chooses contract shape, EF Core chooses storage shape, and Power BI chooses analytical shape.

## Decision

Core envelope semantics remain projection-neutral.

Target packages own target-specific envelope projection policies:

```text
SemanticTypeModel.JsonSchema owns envelope contract/payload schema policies.
SemanticTypeModel.EFCore owns envelope payload storage and owned mapping policies.
SemanticTypeModel.PowerBI owns envelope analytical projection policies.
```

The default target behavior is:

| Target | Envelope default | Payload default |
|---|---|---|
| Core | Preserve semantics. | No representation decision. |
| JSON Schema | Envelope object as root when selected. | Structured payload `$ref`. |
| EF Core | Envelope entity/table when selected. | Serialized JSON column. |
| Power BI | Envelope metadata table. | Ignore body or expose configured summary only. |

## Rationale

- The envelope semantic identifies a wrapper boundary and distinguished payload; it does not define storage, schema, or analytical representation.
- EF Core storage concerns are materially different from JSON Schema contract concerns and Power BI analytical modeling concerns.
- Consumers who use envelopes usually need coherent behavior across all major projections, so defaults must be explicit.
- EF Core needs the most concrete policy surface because it must configure `ModelBuilder` for payload persistence.
- JSON Schema and Power BI must avoid silent shape changes: structured payload schemas, opaque payloads, flattened analytical payloads, and payload-as-root views have different meanings.

## Consequences

- Domain packages must not reinterpret `Envelope` itself; they refine representation through target-specific options.
- Selecting both envelope and payload as projection roots requires explicit policy.
- JSON Schema defaults to an envelope object with payload `$ref` when the envelope is selected as root.
- EF Core defaults envelope payload storage to serialized JSON.
- Power BI defaults to metadata-oriented envelope projection and does not flatten payloads implicitly.
- Advanced database tuning, provider-specific JSON path indexing, Power BI service operations, PBIX generation, and JSON Schema runtime validation remain out of scope.

## Alternatives Considered

### EF Core Only Milestone

Rejected because envelopes are core semantics and consumers commonly use the same envelope with JSON Schema, EF Core, and Power BI.

### Core Defines All Envelope Representation

Rejected because storage, schema, and analytical representation are target-specific.

### Automatic Payload Flattening

Rejected because flattening can silently change contracts, create bad analytical models, and generate ambiguous database columns.

### Payload Always as Root

Rejected because management/audit/revision envelopes are often the correct API, storage, or analytical root.
