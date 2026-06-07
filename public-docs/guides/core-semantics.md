# Core Semantics Guide

Core semantics describe projection-neutral meaning in annotated .NET code. They are the concepts the canonical semantic model should carry before any JSON Schema, EF Core, Power BI, or System.Text.Json target is selected.

## Use Core Semantics For Meaning

Use core semantics when the meaning is true across targets:

| Meaning | Core semantic |
|---|---|
| Type has identity and lifecycle | Entity |
| Type is value-based | ValueObject |
| Property identifies an entity | Key |
| Property associates two types | Relationship |
| Property must be present | Required |
| Value may be null | Nullable |
| User-facing label | DisplayName |
| Domain description | Description |
| Projection-neutral scalar format | Format |
| Wrapper boundary with distinguished payload | Envelope + EnvelopePayload |

Use target-specific metadata when the meaning belongs to one target only:

| Target-specific intent | Use |
|---|---|
| EF Core table name | `efCore.tableName` |
| EF Core index | `efCore.index` |
| Power BI display folder | `powerBi.displayFolder` |
| DAX measure | Power BI measure metadata or measure builder |
| JSON Schema-only keyword override | `jsonSchema.*` metadata |

## Envelope

Use an envelope when a wrapper type carries, manages, versions, transports, persists, audits, authorizes, caches, or contextualizes one distinguished payload.

```csharp
[SemanticEnvelope("management")]
public sealed class ManagedSpecificationEnvelope<TSpecification>
{
    [SemanticEnvelopePayload]
    public required TSpecification Specification { get; init; }

    [SemanticEnvelopeMetadata]
    public required long Revision { get; init; }

    [SemanticEnvelopeMetadata]
    public required string ModifiedBy { get; init; }
}
```

Envelope rules:

- An envelope normally has exactly one payload.
- Envelope metadata describes the wrapper lifecycle or context, not the payload's domain state.
- Envelope semantics do not erase payload semantics.
- Projection policy decides whether the envelope or payload is the root for a target.

## Projection Implications

### JSON Schema

JSON Schema can export the envelope as the root wrapper object or export only the payload schema. Payload representation can be inline, referenced, serialized, or opaque depending on target policy.

### EF Core

EF Core can map the envelope as the persistence/cache entity and metadata as columns. The payload can be mapped as owned, serialized, converted, ignored, or separately mapped by explicit policy.

### Power BI

Power BI can expose envelope metadata as reporting columns and treat the payload as ignored, flattened, referenced, serialized, or opaque by analytical policy.

## Diagnostics

Unsupported or ambiguous semantic usage is diagnostic. Common envelope diagnostics include missing payload, multiple payloads without policy, payload marker outside envelope, metadata marker outside envelope, and ambiguous envelope-versus-payload projection root.
