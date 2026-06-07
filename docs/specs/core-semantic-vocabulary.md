# Core Semantic Vocabulary Specification

## Status

Authoritative behavioral specification.

## Purpose

Define the core semantic vocabulary for code-first SemanticTypeModel authoring.

This specification is authoritative for:

- core semantic names and meanings;
- usage guidance for core semantics;
- distinction between core semantics and projection-specific metadata;
- envelope semantics;
- projection-neutral interpretation of core semantics;
- semantic ambiguity diagnostics.

## Core Principle

Core semantics describe projection-neutral meaning.

Projection-specific metadata describes target-specific representation.

Use core semantics when the meaning is true independently of JSON Schema, EF Core, Power BI, System.Text.Json, or any other target.

Use target-specific metadata when the meaning exists only for a projection target.

## Entry Format

Each semantic entry defines:

```text
Name
Kind
Description
Best used when
Avoid when
Projection implications
Example
Diagnostics / ambiguity notes
```

## Core vs Projection-Specific Examples

| Intent | Core semantic | Projection-specific metadata |
|---|---|---|
| Type has identity and lifecycle | `Entity` | EF Core may derive entity mapping. |
| Property identifies an entity | `Key` | EF Core may derive primary key. |
| Property is required | `Required` | JSON Schema/EF Core derive requiredness. |
| Property has user-facing label | `DisplayName` | UI/Power BI may consume display metadata. |
| EF Core index | None | `efCore.index` |
| EF Core table name | None | `efCore.tableName` |
| Power BI display folder | None | `powerBi.displayFolder` |
| DAX measure | Usually none | `powerBi.measure` / measure builder |
| JSON Schema format override | Maybe `Format` if projection-neutral | `jsonSchema.format` if JSON Schema-specific |
| Wrapper with distinguished payload | `Envelope` + `EnvelopePayload` | Target decides envelope/payload root policy. |

## Baseline Core Semantics

### Entity

Kind: Type semantic.

Description: A type with identity and lifecycle.

Best used when:

```text
The object is referenced independently.
The object has a stable identity.
The object may be persisted, projected, or related by identity.
```

Avoid when:

```text
The type is embedded only by value.
The type is a DTO fragment with no independent identity.
The type is only a transport wrapper around another payload.
```

Projection implications:

```text
JSON Schema: usually becomes an object definition.
EF Core: entity candidate.
Power BI: table candidate when analytical projection is enabled.
```

Example:

```csharp
[SemanticEntity]
public sealed class Customer
{
    [SemanticKey]
    public required CustomerId Id { get; init; }
}
```

Diagnostics / ambiguity notes:

```text
Entity without key may be diagnostic depending configuration.
Entity and ValueObject on the same type is a conflict unless explicitly allowed.
Envelope may override projection root selection without erasing entity semantics.
```

### ValueObject

Kind: Type semantic.

Description: A type whose equality and meaning are defined by its contained values rather than identity.

Best used when:

```text
The type is embedded in another object.
The type has no independent lifecycle.
The type groups related scalar values.
```

Avoid when:

```text
The type is referenced independently by identity.
The type is a persistence root.
The type is an envelope with lifecycle metadata.
```

Projection implications:

```text
JSON Schema: object definition or inline object.
EF Core: owned/value-object candidate when explicitly configured.
Power BI: usually flattened, ignored, or serialized according to analytical options.
```

Example:

```csharp
[SemanticValueObject]
public readonly record struct Money(decimal Amount, string Currency);
```

Diagnostics / ambiguity notes:

```text
ValueObject as root entity is diagnostic unless explicitly configured.
```

### AggregateRoot

Kind: Type semantic.

Description: An entity that is the consistency and lifecycle boundary for an aggregate.

Best used when the type controls changes to related child entities/value objects or acts as the API/persistence boundary for a group of objects.

Avoid when the type is only a lookup, a child object, or a payload inside an envelope whose management boundary is the envelope.

Projection implications:

```text
JSON Schema: may be selected as root schema by default.
EF Core: entity candidate and likely aggregate root.
Power BI: analytical table candidate.
```

Example:

```csharp
[SemanticEntity]
[SemanticRole("AggregateRoot")]
public sealed class Order { }
```

Diagnostics / ambiguity notes: AggregateRoot without Entity semantics may be diagnostic.

### Key

Kind: Property semantic.

Description: A property or property group that identifies an entity or semantic object.

Best used when the property participates in stable identity and relationships.

Avoid when the value is only a display number or transient sequence number.

Projection implications:

```text
JSON Schema: may become required and annotated as identity metadata.
EF Core: primary or alternate key candidate.
Power BI: key column and relationship endpoint candidate.
```

Example:

```csharp
[SemanticKey]
public required CustomerId Id { get; init; }
```

Diagnostics / ambiguity notes: Key on non-entity may be diagnostic unless configured.

### AlternateKey

Kind: Property or key-group semantic.

Description: A secondary unique identity for an entity.

Best used when a business identifier uniquely identifies the entity in addition to the primary key.

Avoid when the property is merely indexed or frequently queried.

Projection implications:

```text
EF Core: alternate key candidate.
Power BI: possible relationship endpoint metadata.
JSON Schema: uniqueness may be annotated but is not generally enforceable.
```

Example:

```csharp
[SemanticKey(Kind = SemanticKeyKind.Alternate, Name = "CustomerNumber")]
public required string CustomerNumber { get; init; }
```

Diagnostics / ambiguity notes: Alternate key without entity context may be diagnostic.

### Relationship

Kind: Property or relationship semantic.

Description: A semantic association between model elements.

Best used when a property references another entity or collection of entities and the association matters independently of object nesting.

Avoid when the property is just embedded value-object composition or the target/cardinality cannot be resolved.

Projection implications:

```text
JSON Schema: may become reference metadata or object graph reference.
EF Core: explicit relationship candidate.
Power BI: relationship candidate.
```

Example:

```csharp
[SemanticRelationship(typeof(Customer), Cardinality = SemanticCardinality.ManyToOne)]
public required CustomerId CustomerId { get; init; }
```

Diagnostics / ambiguity notes: Ambiguous cardinality, missing target, or invalid target kind emit diagnostics.

### Required

Kind: Property semantic.

Description: A property must be present for a valid model instance.

Best used when the value is required by the domain contract.

Avoid when the property is required only by one projection target.

Projection implications:

```text
JSON Schema: required property.
EF Core: required property/relationship candidate.
Power BI: non-nullable metadata where representable.
```

Diagnostics / ambiguity notes: Required and Nullable are distinct; required nullable means present but may be null.

### Nullable

Kind: Property/type semantic.

Description: A value may explicitly be null.

Best used when null is a valid domain state.

Avoid when the property is merely optional/absent rather than explicitly nullable.

Projection implications:

```text
JSON Schema: nullable representation according to strategy.
EF Core: optional property/relationship where applicable.
Power BI: null-capable column metadata where representable.
```

Diagnostics / ambiguity notes: Nullability must not be conflated with collection cardinality or requiredness.

### Collection

Kind: Property/type semantic.

Description: A property contains multiple values.

Best used when the domain value is a list, set, array, or collection.

Avoid when the property is serialized text that happens to contain multiple values.

Projection implications:

```text
JSON Schema: array.
EF Core: relationship or unsupported collection depending explicit metadata.
Power BI: often relationship table or unsupported shape.
```

Diagnostics / ambiguity notes: Collections of entities and collections of value objects have different projection implications.

### Enumeration

Kind: Type semantic.

Description: A closed set of named values.

Best used when allowed values are known and finite.

Avoid when the set is externally managed or open-ended.

Projection implications:

```text
JSON Schema: enum.
EF Core: string or numeric enum storage according to policy.
Power BI: categorical column.
```

### Scalar

Kind: Type semantic.

Description: A primitive or scalar-like value.

Best used when the value has no properties relevant to semantic model traversal.

Avoid when the type is a value object with meaningful internal structure.

Projection implications:

```text
JSON Schema: scalar type.
EF Core: scalar property or converted value.
Power BI: column data type.
```

Diagnostics / ambiguity notes: Custom scalar-like domain types may require explicit format/conversion metadata.

### Identifier

Kind: Property/type semantic.

Description: A value identifies something but is not necessarily the primary key of the current entity.

Best used for external IDs, correlation IDs, tenant IDs, or reference IDs.

Avoid when the value is the entity key; use Key instead.

Projection implications:

```text
JSON Schema: identifier annotation/format.
EF Core: ordinary property unless also key/foreign key.
Power BI: category or key-like column depending projection options.
```

Diagnostics / ambiguity notes: Identifier does not imply uniqueness unless key semantics are present.

### DisplayName

Kind: Type/property metadata semantic.

Description: User-facing label.

Best used when a stable human-readable label should be shown by UI, documentation, or analytical tools.

Avoid when serialized or database names need to change for one target only.

Projection implications:

```text
JSON Schema: title when configured.
EF Core: no default table/column rename unless option chooses display names.
Power BI: table/column display name candidate.
```

Diagnostics / ambiguity notes: DisplayName should not replace canonical identifiers.

### Description

Kind: Type/property metadata semantic.

Description: Human-readable explanatory text.

Best used when consumers need documentation or tooltips.

Avoid when the text encodes behavior or validation rules that should be modeled explicitly.

Projection implications:

```text
JSON Schema: description.
Power BI: description metadata where supported.
EF Core: ignored unless target-specific comments are configured.
```

### Category

Kind: Metadata semantic.

Description: Stable grouping/category metadata.

Best used when properties or types should be grouped in a projection-neutral way.

Avoid when the grouping is target-specific, such as a Power BI display folder.

Projection implications:

```text
JSON Schema/UI: may become grouping metadata.
Power BI: may inform display folder only when configured.
EF Core: normally ignored.
```

### Order

Kind: Metadata semantic.

Description: Deterministic presentation/order hint.

Best used when a stable order should be preserved across generated outputs.

Avoid when the ordering is target-specific and should not affect other outputs.

Projection implications:

```text
JSON Schema: property order metadata or deterministic ordering.
Power BI: column order where supported.
EF Core: generally ignored.
```

Diagnostics / ambiguity notes: Duplicate order values must be resolved deterministically.

### Format

Kind: Scalar metadata semantic.

Description: Projection-neutral scalar format hint.

Best used when the format is a domain-level scalar meaning, such as email, URI, date, currency, percentage, or duration.

Avoid when the format string is target-specific.

Projection implications:

```text
JSON Schema: format where supported.
Power BI: data category or format candidate when configured.
EF Core: normally ignored unless conversion policy uses it.
```

Diagnostics / ambiguity notes: Invalid format for the target type emits diagnostics.

### Constraint

Kind: Validation semantic.

Description: Projection-neutral restriction on valid values.

Best used when the domain contract restricts length, range, pattern, cardinality, or allowed values.

Avoid when the constraint exists only in one storage or UI target.

Projection implications:

```text
JSON Schema: validation keywords.
EF Core: max length, precision, or limited metadata where supported.
Power BI: may be ignored or documented.
```

### Computed

Kind: Member semantic.

Description: A value derived from an expression or other model state.

Best used when the value is not directly stored as normal domain state and the expression is projection-neutral or explicitly carries a language.

Avoid when the expression is target-specific DAX, SQL, or JSONPath; use target-specific metadata unless a core expression model is explicitly supported.

Projection implications:

```text
Power BI: explicit DAX measures are target-specific, not generic Computed by default.
EF Core: calculated columns are not implied by core Computed.
JSON Schema: usually annotation only.
```

Diagnostics / ambiguity notes: Computed does not imply a target can execute the expression.

### Ignored

Kind: Type/property semantic.

Description: Exclude an element from semantic extraction or downstream consideration.

Best used when a member should not appear in the semantic model.

Avoid when the member should be present in the canonical model but ignored only by one projection.

Projection implications:

```text
All targets: element absent or marked ignored depending extraction policy.
Target-specific ignore should use target-specific metadata instead.
```

## Envelope Semantics

### Envelope

Kind: Type semantic.

Description: A type whose primary role is to carry, manage, version, transport, persist, authorize, audit, or otherwise contextualize another semantic payload.

Best used when:

```text
A wrapper has one distinguished payload.
The wrapper has metadata about lifecycle, transport, management, auditing, status, revision, authorization, or persistence context.
The wrapper may become the projection root for one target while the payload remains the semantic domain value.
```

Avoid when:

```text
The type is normal composition with several equally important properties.
The wrapper has no distinguished payload.
The type is merely an entity with related children.
```

Projection implications:

```text
JSON Schema: envelope root exports wrapper object; payload root exports only payload schema; payload can be referenced, inlined, or serialized according to projection policy.
EF Core: envelope can become persistence/cache entity; metadata maps as columns; payload may be owned, serialized, converted, ignored, or separately mapped by explicit policy.
Power BI: envelope metadata may become reporting columns; payload may be ignored, flattened, or serialized by analytical policy.
```

Example:

```csharp
[SemanticEnvelope(Purpose = SemanticEnvelopePurpose.Management)]
public sealed class ManagedSpecificationEnvelope<TSpecification>
{
    [SemanticEnvelopePayload]
    public required TSpecification Specification { get; init; }

    [SemanticEnvelopeMetadata]
    public required long Revision { get; init; }

    [SemanticEnvelopeMetadata]
    public required string ModifiedBy { get; init; }

    [SemanticEnvelopeMetadata]
    public required DateTimeOffset ModifiedAt { get; init; }
}
```

Diagnostics / ambiguity notes:

```text
Envelope without payload is diagnostic by default.
Envelope with multiple payloads is diagnostic unless explicit policy allows it.
Envelope does not erase payload semantics.
Projection root ambiguity must emit diagnostics.
```

### EnvelopePayload

Kind: Property semantic.

Description: The distinguished property inside an envelope that carries the semantic value being transported, managed, persisted, cached, or contextualized.

Best used when one property is the real business/domain/configuration payload and the surrounding type carries metadata about that payload.

Avoid when several properties are equally important and no single payload exists.

Projection implications:

```text
JSON Schema: payload property can be object, $ref, inline, or serialized according to envelope policy.
EF Core: payload property can be owned, JSON/serialized, converted, ignored, or separately mapped according to EF policy.
Power BI: payload may be flattened, ignored, or serialized according to analytical policy.
```

Diagnostics / ambiguity notes:

```text
Payload marker outside an envelope is diagnostic unless a transform promotes the containing type to Envelope.
Payload property must resolve to a modeled type unless explicit opaque/serialized policy is configured.
```

### EnvelopeMetadata

Kind: Property semantic.

Description: A property on an envelope that describes the envelope lifecycle/context rather than the payload domain state.

Best used when the property describes revision, audit, transport status, error state, management state, tenant/correlation context, or authorization context.

Avoid when the property belongs to the payload's own domain state.

Projection implications:

```text
JSON Schema: ordinary metadata property on envelope root.
EF Core: column on envelope entity/cache record when envelope is root.
Power BI: reporting column if envelope metadata is analytically relevant.
```

Diagnostics / ambiguity notes: Metadata marker outside an envelope is diagnostic unless explicitly allowed.

## Envelope Projection Policy Concepts

Envelope semantics introduce projection root choice.

Allowed policy concepts:

```text
EnvelopeAsRoot
PayloadAsRoot
PayloadEmbedded
PayloadReference
PayloadSerialized
PayloadOpaque
```

Rules:

- Envelope semantics do not erase payload semantics.
- Projection configuration decides whether envelope or payload is root for a target.
- Domain packages may expose target-specific options for payload representation.
- Ambiguous root selection emits diagnostics.
- Payload serialization is a representation policy, not a change to the payload's core semantics.

## Envelope Use Cases

### Operation Result Envelope

```csharp
[SemanticEnvelope(Purpose = SemanticEnvelopePurpose.OperationResult)]
public sealed class OperationResultEnvelope<T>
{
    [SemanticEnvelopePayload]
    public T? Data { get; init; }

    [SemanticEnvelopeMetadata]
    public bool Failed { get; init; }

    [SemanticEnvelopeMetadata]
    public string? ErrorCode { get; init; }

    [SemanticEnvelopeMetadata]
    public string? CorrelationId { get; init; }
}
```

Use when the result wrapper carries status/error/correlation metadata around a payload.

### Managed Specification Envelope

```csharp
[SemanticEnvelope(Purpose = SemanticEnvelopePurpose.Management)]
public sealed class ManagedSpecificationEnvelope<TSpecification>
{
    [SemanticEnvelopePayload]
    public required TSpecification Specification { get; init; }

    [SemanticEnvelopeMetadata]
    public required long Revision { get; init; }

    [SemanticEnvelopeMetadata]
    public required string ModifiedBy { get; init; }

    [SemanticEnvelopeMetadata]
    public required DateTimeOffset ModifiedAt { get; init; }
}
```

Use when a subsystem specification/configuration is the payload, while the envelope is the management, cache, persistence, audit, or revision boundary.

## Canonical Envelope Annotation Keys

The final representation may use dedicated primitives or annotations. If annotations are used, these keys are reserved:

```text
schema.envelope
schema.envelope.purpose
schema.envelope.payload
schema.envelope.metadata
schema.envelope.payloadRepresentation
```

Required values are deterministic and projection-neutral.

`schema.envelope.payloadRepresentation` may express broad representation intent such as:

```text
Embedded
Reference
Serialized
Opaque
```

Target-specific storage details remain target-specific metadata.

## Diagnostics

Required diagnostic classes include:

```text
envelope has no payload
envelope has multiple payloads without explicit policy
payload marker outside envelope
metadata marker outside envelope
payload type not modeled
payload representation unsupported
envelope and payload both selected as projection root without explicit policy
envelope metadata conflicts with payload semantics
target cannot represent selected envelope policy
```

Diagnostics must include model path and related model paths where available.

## Inspection

Inspection output must show:

```text
Envelope type
Envelope purpose when present
Envelope payload property
Envelope metadata properties
Envelope projection policy when selected
Diagnostics related to envelope semantics
```

Inspection output must be deterministic and suitable for snapshot tests.
