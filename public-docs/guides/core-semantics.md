# Core Semantics Guide

Core semantics describe projection-neutral meaning in annotated .NET code. They are the concepts the canonical semantic model carries before any JSON Schema, EF Core, Power BI, or System.Text.Json target is selected.

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
| Lifecycle containment | Ownership / OwnedObject / OwnedCollection |
| Instance or contract evolution | Versioned / Version / Revision / CurrentVersion |
| Effective interval | TemporalValidity / ValidFrom / ValidTo |
| Status or lifecycle phase | LifecycleState |
| Unknown compatibility members | ExtensionData |

Use target-specific metadata when the meaning belongs to one target only:

| Target-specific intent | Use |
|---|---|
| EF Core table name | `efCore.tableName` |
| EF Core index | `efCore.index` |
| Power BI display folder | `powerBi.displayFolder` |
| DAX measure | Power BI measure metadata or measure builder |
| JSON Schema-only keyword override | `jsonSchema.*` metadata |
| System.Text.Json serialization name | `systemTextJson.propertyName` metadata |

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

## Ownership

Use ownership when an object-valued or collection-valued member is part of the containing owner's composition boundary and does not stand independently by default.

```csharp
[SemanticEntity]
public sealed class Customer
{
    [SemanticKey]
    public required string Id { get; init; }

    [SemanticOwnedObject]
    public required Address BillingAddress { get; init; }

    [SemanticOwnedCollection]
    public IReadOnlyList<ContactMethod> ContactMethods { get; init; } = [];
}
```

Ownership is not the same as an envelope. An envelope identifies wrapper/payload semantics. Ownership identifies lifecycle containment.

## Evolution and Lifecycle

Use evolution and lifecycle semantics when a type or member participates in versioning, revision history, effective dating, lifecycle state, or compatibility preservation.

```csharp
[SemanticVersioned]
public sealed class WorkflowSpecification
{
    [SemanticRevision]
    public required long Revision { get; init; }

    [SemanticLifecycleState]
    public required string State { get; init; }

    [SemanticValidFrom]
    public required DateTimeOffset ValidFrom { get; init; }

    [SemanticValidTo]
    public DateTimeOffset? ValidTo { get; init; }

    [SemanticExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
```

`ExtensionData` represents instance-level unknown or forward-compatible data. It is different from annotations, which are metadata about the model.

## Projection Implications

### JSON Schema

JSON Schema can export the envelope as the root wrapper object or export only the payload schema. Payload representation can be inline, referenced, serialized, or opaque depending on target policy.

Ownership usually exports owned values as structured nested schemas or local references. `ExtensionData` usually controls `additionalProperties` or `unevaluatedProperties` rather than appearing as a normal modeled property.

### EF Core

EF Core can map the envelope as the persistence/cache entity and metadata as columns. The payload can be mapped as owned, serialized, converted, ignored, or separately mapped by explicit policy.

Ownership maps to owned reference or owned collection policies. Version/revision, lifecycle state, and temporal-validity members map as regular scalar members with optional configured keys or indexes. `ExtensionData` is ignored by default unless configured as serialized JSON or summary metadata.

### Power BI

Power BI can expose envelope metadata as reporting columns and treat the payload as ignored, flattened, referenced, serialized, or opaque by analytical policy.

Ownership can flatten owned objects or project owned collections as child tables. Version, revision, lifecycle state, and temporal-validity members are useful analytical columns. `ExtensionData` is ignored by default unless configured as summary fields such as `HasExtensionData` or `ExtensionDataCount`.

### System.Text.Json

System.Text.Json metadata remains projection-specific. Semantic names and JSON property names remain separate unless a resolver customization policy explicitly uses semantic names as JSON serialization names.

## Diagnostics

Unsupported or ambiguous semantic usage is diagnostic. Common cases include missing envelope payloads, multiple payloads without policy, ownership cycles, invalid temporal endpoints, duplicate lifecycle state members, invalid extension-data property types, and ambiguous target projection policies.
