# Core Semantic Vocabulary Specification

## Status

Authoritative behavioral specification.

## Purpose

Define the projection-neutral core semantic vocabulary for code-first SemanticTypeModel authoring.

This specification is authoritative for:

- core semantic names and meanings;
- usage guidance for core semantics;
- canonical annotation keys used to preserve authoring intent;
- distinction between core semantics and projection-specific metadata;
- envelope semantics;
- projection-neutral interpretation of core semantics;
- semantic ambiguity diagnostics.

## Core Principle

Core semantics describe domain meaning that remains true before any JSON Schema, EF Core, Power BI, System.Text.Json, or other target projection is selected.

Projection-specific metadata describes target-specific representation. Use target namespaces such as `jsonSchema.*`, `efCore.*`, `powerBi.*`, or `systemTextJson.*` when the meaning exists only for one target.

Use a core semantic when the same meaning should be available to the canonical semantic model, domain semantic models, diagnostics, queries, and inspection. Use projection-specific metadata when the meaning is a storage, serialization, reporting, or export choice.

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
| Type has identity and lifecycle | `Entity` | EF Core may derive entity mapping by policy. |
| Type is value-based | `ValueObject` | EF Core may map owned/converted/JSON by policy. |
| Type has analytical dimensional meaning | `Dimension` | Power BI may choose table/display behavior by policy. |
| Type represents analytical observations | `Fact` | Power BI may choose measure/table behavior by policy. |
| Property identifies an entity | `Key` | EF Core may derive primary/alternate keys by policy. |
| Human recognition uses an ordered property group | `DisplayIdentity` | UI, API, reporting, or selection projections may later choose rendering behavior by explicit target policy. |
| Consumers commonly locate/narrow instances through an ordered property group | `AccessPath` | EF Core may later derive an index candidate; API/UI projections may later derive query/filter affordances by explicit target policy. |
| Property is required | `Required` | JSON Schema/EF Core derive target requiredness by policy. |
| Property has a user-facing label | `DisplayName` | UI/Power BI may consume display metadata by policy. |
| EF Core index | None | `efCore.index` |
| EF Core table name | None | `efCore.tableName` |
| Power BI display folder | None | `powerBi.displayFolder` |
| DAX measure | None by default | `powerBi.measure` or measure builder metadata |
| JSON Schema-only format override | None | `jsonSchema.format` |
| Projection-neutral scalar format | `Format` | Target projections may map when compatible. |
| Wrapper with distinguished payload | `Envelope` + `EnvelopePayload` | Target decides envelope-root/payload-root and payload representation policy. |

`AccessPath` is not an EF Core index declaration. It describes projection-neutral intended access; a physical index remains target-specific unless an EF policy explicitly derives one.

## Canonical Annotation Keys

The following canonical annotation keys preserve authoring intent before or after transformations derive structured model members.

| Key | Scope | Meaning |
|---|---|---|
| `schema.role` | Type | Declared core role alias for an object type. |
| `schema.key` | Member | Declares that a member participates in a key. |
| `schema.key.name` | Member | Groups members into a named key. |
| `schema.key.kind` | Member | Declares primary, alternate, natural, surrogate, or external key intent. |
| `schema.key.order` | Member | Orders members in a composite key. |
| `schema.key.generated` | Member | Declares generated key intent. |
| `schema.displayIdentity` | Member | Non-negative decimal order of this member in the containing object's single Display Identity. |
| `schema.accessPath.<name>` | Member | Non-negative decimal order of this member in the named Access Path scoped to the containing object. |
| `schema.title` | Type/member | Projection-neutral display name. |
| `schema.userDescription` | Type/member | User-facing projection-neutral description. |
| `schema.technicalDescription` | Type/member | Technical projection-neutral description. |
| `schema.format` | Type/member | Projection-neutral scalar format. |
| `schema.minLength`, `schema.maxLength`, `schema.pattern` | Member | String constraints. |
| `schema.minimum`, `schema.maximum`, `schema.exclusiveMinimum`, `schema.exclusiveMaximum`, `schema.multipleOf` | Member | Numeric constraints. |
| `schema.minItems`, `schema.maxItems`, `schema.uniqueItems` | Member | Collection constraints. |
| `schema.envelope` | Type | Marks an object type as an envelope wrapper boundary. |
| `schema.envelope.purpose` | Type | Optional projection-neutral envelope purpose. |
| `schema.envelope.payload` | Member | Marks the envelope payload member. |
| `schema.envelope.metadata` | Member | Marks envelope lifecycle or context metadata. |

## Baseline Core Semantics

### Entity

**Kind:** Type semantic.

**Description:** A type with identity and lifecycle.

**Best used when:** The object is referenced independently, has stable identity, or participates in cross-object references by identity.

**Avoid when:** The type is embedded only by value, is a DTO fragment with no independent identity, or is only a transport wrapper around another payload.

**Projection implications:** JSON Schema usually exports an object definition. EF Core may treat the type as an entity candidate. Power BI may treat it as a table candidate when analytical projection is enabled.

**Example:**

```csharp
[SemanticType(SemanticTypeRole.Entity)]
public sealed class Customer
{
    [SemanticKey]
    public required string Id { get; init; }
}
```

**Diagnostics / ambiguity notes:** Unsupported role aliases and conflicts with already-derived roles are diagnosable. Keys on non-entity types are diagnosable. Envelope projection-root selection does not erase entity semantics on the envelope or payload.

### ValueObject

**Kind:** Type semantic.

**Description:** A type whose equality and meaning are defined by contained values rather than independent identity.

**Best used when:** The type is embedded in another object, has no independent lifecycle, or groups related scalar values.

**Avoid when:** The type is referenced independently by identity, is a persistence root, or is an envelope with lifecycle metadata.

**Projection implications:** JSON Schema may export an object definition or inline object. EF Core may map it as owned, converted, serialized, or ignored by explicit policy. Power BI may flatten, ignore, or serialize it by analytical policy.

**Example:**

```csharp
[SemanticType(SemanticTypeRole.ValueObject)]
public readonly record struct Money(decimal Amount, string Currency);
```

**Diagnostics / ambiguity notes:** A value object used as an entity root or carrying key metadata is ambiguous unless explicit policy allows it.

### AggregateRoot

**Kind:** Type semantic.

**Description:** An entity that is the consistency and lifecycle boundary for related child entities and value objects.

**Best used when:** The type controls changes to an aggregate and should be the API or persistence boundary for a related object group.

**Avoid when:** The type is only a lookup, a child object, or a payload inside an envelope whose management boundary is the envelope.

**Projection implications:** JSON Schema may be selected as a root schema by policy. EF Core may treat it as a persistence aggregate root by policy. Power BI may treat it as an analytical table candidate.

**Example:**

```csharp
[SemanticType(SemanticTypeRole.Entity)]
public sealed class Order
{
    [SemanticKey]
    public required string Id { get; init; }
}
```

**Diagnostics / ambiguity notes:** Aggregate-root meaning requires entity semantics. If represented through annotations before structured support, conflicting role or key metadata remains diagnosable.

### Dimension

**Kind:** Type semantic role.

**Description:** A descriptive analytical object used for slicing, grouping, or filtering observations.

**Best used when:** The object has analytical classification meaning independent of any one report layout.

**Avoid when:** The only intent is a Power BI table name, display folder, or visual arrangement.

**Projection implications:** JSON Schema exports the object shape. EF Core does not infer storage behavior from this role alone. Power BI may use it as dimensional modeling input.

**Example:**

```csharp
[SemanticRole(SemanticTypeRole.Dimension)]
public sealed class ProductDimension { }
```

**Diagnostics / ambiguity notes:** Conflicting role declarations are diagnosable.

### Fact

**Kind:** Type semantic role.

**Description:** A quantitative analytical event, observation, or measurement set.

**Best used when:** The type carries measures and links to analytical context.

**Avoid when:** The type is a general entity with no analytical measure semantics.

**Projection implications:** JSON Schema exports the object shape. EF Core behavior requires explicit target policy. Power BI may use it as fact-modeling input.

**Example:**

```csharp
[SemanticRole(SemanticTypeRole.Fact)]
public sealed class SalesFact { }
```

**Diagnostics / ambiguity notes:** Conflicting role declarations are diagnosable.

### Lookup

**Kind:** Type semantic role.

**Description:** A reference set or small semantic catalog used to describe valid categories or choices.

**Best used when:** The same reference values are reused by other types.

**Avoid when:** A simple enum or scalar constraint completely expresses the domain.

**Projection implications:** JSON Schema may export an object or enum-like shape according to model structure. EF Core and Power BI need explicit policy for table or target-owned association decisions.

**Example:**

```csharp
[SemanticRole(SemanticTypeRole.Lookup)]
public sealed class Country { }
```

**Diagnostics / ambiguity notes:** Conflicting role declarations are diagnosable.

### Event

**Kind:** Type semantic role.

**Description:** A domain occurrence recorded at a point in a process.

**Best used when:** The type represents an immutable occurrence or message describing something that happened.

**Avoid when:** The type is a mutable aggregate state container.

**Projection implications:** JSON Schema exports the event payload shape. EF Core persistence and Power BI analytical treatment require explicit target policy.

**Example:**

```csharp
[SemanticRole(SemanticTypeRole.Event)]
public sealed record OrderSubmitted(string OrderId);
```

**Diagnostics / ambiguity notes:** Conflicting role declarations are diagnosable.

### Configuration

**Kind:** Type semantic role.

**Description:** A type describing configurable behavior or settings.

**Best used when:** Values are authored or changed to affect behavior rather than record domain transactions.

**Avoid when:** The type is target-specific options for a projection implementation.

**Projection implications:** JSON Schema may use the role for configuration contracts. EF Core and Power BI require explicit target policy.

**Example:**

```csharp
[SemanticRole(SemanticTypeRole.Configuration)]
public sealed class PricingOptions { }
```

**Diagnostics / ambiguity notes:** Conflicting role declarations are diagnosable.

### Form

**Kind:** Type semantic role.

**Description:** A user-input-oriented object.

**Best used when:** The domain meaning is an input contract rather than a stored entity.

**Avoid when:** The only intent is a specific UI control library setting.

**Projection implications:** JSON Schema may export an input object. Generic UI hints remain separate annotations. EF Core and Power BI do not infer storage or analytics behavior from this role alone.

**Example:**

```csharp
[SemanticRole(SemanticTypeRole.Form)]
public sealed class RegistrationForm { }
```

**Diagnostics / ambiguity notes:** Conflicting role declarations are diagnosable.

### Key

**Kind:** Property or key-group semantic.

**Description:** A property or property group that identifies an entity or semantic object.

**Best used when:** The value participates in stable identity and cross-object references.

**Avoid when:** The value is only a display number, transient sequence, database index, or storage clustering choice.

**Projection implications:** JSON Schema may preserve key metadata and requiredness. EF Core may map primary, alternate, natural, surrogate, or external keys by policy. Power BI may preserve keys as analytical identity metadata.

**Example:**

```csharp
[SemanticKey(Kind = KeyKind.Primary, IsGenerated = true)]
public required string Id { get; init; }
```

**Diagnostics / ambiguity notes:** Keys on non-entity types and unsupported multiple primary keys are diagnosable.

### AlternateKey

**Kind:** Property or key-group semantic.

**Description:** A secondary unique identity for an entity.

**Best used when:** A business identifier uniquely identifies the entity in addition to the primary key.

**Avoid when:** The property is merely indexed, sorted, or frequently queried.

**Projection implications:** JSON Schema may preserve uniqueness metadata but generally cannot enforce model-wide uniqueness. EF Core may map an alternate key by policy. Power BI may preserve it as key metadata.

**Example:**

```csharp
[SemanticKey(Kind = KeyKind.Alternate, Name = "CustomerNumber")]
public required string CustomerNumber { get; init; }
```

**Diagnostics / ambiguity notes:** Alternate keys without entity context are ambiguous and may emit diagnostics.

### DisplayIdentity

**Kind:** Property-group semantic.

**Description:** The single ordered property group that defines how a human should recognize an instance.

**Best used when:** A selector, list, form, report, API, log, or other consumer benefits from stable human-recognition components that are distinct from machine identity.

**Avoid when:** The intent is machine/domain identity, uniqueness, a property caption, arbitrary display formatting, or a target-specific layout rule.

**Projection implications:** This milestone adds no target-specific behavior. Future UI, API, reporting, or selection projections may use Display Identity as input to explicit target policy. EF Core must not infer keys, indexes, or storage changes from it.

**Example:**

```csharp
[SemanticDisplayIdentity(Order = 0)]
public required string CustomerNumber { get; init; }

[SemanticDisplayIdentity(Order = 1)]
public required string Name { get; init; }
```

**Diagnostics / ambiguity notes:** An object has at most one Display Identity. Component order is non-negative and unique within the object's effective extracted property set; gaps are allowed. Display Identity does not define separators, formatting, null rendering, uniqueness, or fallback behavior. It is not inferred from Key, DisplayName, SemanticOrder, or AccessPath.

### AccessPath

**Kind:** Named property-group semantic.

**Description:** A named ordered property sequence expressing an intended way consumers may locate or narrow instances of an object.

**Best used when:** A property or composite property sequence is an expected lookup/filter route that could be useful to data stores, APIs, list/grid filters, selectors, forms, or analytical consumers.

**Avoid when:** The intent is machine identity, uniqueness, a physical database index, a query operator, sort order, UI presentation order, or a target-specific query contract.

**Projection implications:** This milestone adds no target-specific behavior. Future EF Core policy may use Access Paths as index candidates; future API/UI/reporting policies may use them as query/filter or prominence hints. Such behavior requires a separate target contract.

**Example:**

```csharp
[SemanticAccessPath("ByDeviceAndTimestamp", Order = 0)]
public required Guid DeviceId { get; init; }

[SemanticAccessPath("ByDeviceAndTimestamp", Order = 1)]
public required DateTimeOffset Timestamp { get; init; }
```

**Diagnostics / ambiguity notes:** Access Path names are case-sensitive and scoped to the containing object. Names must match `[A-Za-z][A-Za-z0-9_.-]*`. Orders are non-negative and unique within one named path; gaps are allowed. A property may participate in multiple paths. An Access Path does not define equality/range/prefix semantics, whether all members must be supplied by a query, index prefix behavior, uniqueness, frequency, or performance guarantees. No Access Path is inferred from Key or Display Identity.

### Semantic Mutability

**Kind:** Object-type and property semantic.

**Description:** Optional lifecycle mutability intent expressed only as `Mutable` or `Immutable`; absence means mutability is not part of the semantic contract.

**Declared mutability:** `ObjectTypeDefinition.Mutability` and `PropertyDefinition.Mutability` preserve only declarations made at that location.

**Effective mutability:** `property.Mutability ?? containingObject.Mutability ?? unspecified`. A mutable property in an immutable object is valid and intentional.

**CLR boundary:** Setter presence/accessibility, `init`, getter-only members, `readonly`, record syntax, and similar CLR access shape never infer semantic mutability.

**Authoring:** `[SemanticMutable]` and `[SemanticImmutable]` apply to classes, structs, properties, and fields. Applying both to one target is invalid and diagnosable.

**Projection implications:** JSON Schema emits only declared values at their declared nodes beneath `x-stm.mutability`; projections must not flatten inherited values into child declarations.

### Required

**Kind:** Property semantic.

**Description:** A property must be present for a valid model instance.

**Best used when:** Presence is required by the domain contract.

**Avoid when:** Presence is required only by one projection target.

**Projection implications:** JSON Schema may emit the property in `required`. EF Core may map a required property. Power BI may carry non-nullable metadata where representable.

**Example:**

```csharp
public required string Name { get; init; }
```

**Diagnostics / ambiguity notes:** Required and nullable are distinct. Required nullable means the member must be present but may hold null.

### Nullable

**Kind:** Property/type semantic.

**Description:** A value may explicitly be null.

**Best used when:** Null is a valid domain state.

**Avoid when:** The property is optional or absent rather than explicitly nullable.

**Projection implications:** JSON Schema uses the configured nullable representation. EF Core maps optional property metadata where applicable. Power BI maps null-capable column metadata where representable.

**Example:**

```csharp
public string? MiddleName { get; init; }
```

**Diagnostics / ambiguity notes:** Nullability must not be conflated with requiredness or collection cardinality.

### Collection

**Kind:** Property/type semantic.

**Description:** A property or type contains multiple values.

**Best used when:** The domain value is a list, set, array, or collection.

**Avoid when:** A scalar string or JSON value happens to contain multiple serialized values.

**Projection implications:** JSON Schema exports an array. EF Core may map an owned collection, JSON/serialized value, or unsupported case by policy. Power BI may require flattening, serialization, or omission by policy.

**Example:**

```csharp
public IReadOnlyList<OrderLine> Lines { get; init; } = [];
```

**Diagnostics / ambiguity notes:** Collections of entities and collections of value objects have different projection implications.

### Enumeration

**Kind:** Type semantic.

**Description:** A closed set of named values.

**Best used when:** Allowed values are known and finite.

**Avoid when:** The value set is externally managed, user-defined, or open-ended.

**Projection implications:** JSON Schema exports `enum`. EF Core maps string or numeric enum storage according to policy. Power BI treats it as categorical data.

**Example:**

```csharp
public enum OrderStatus
{
    New,
    Submitted,
    Shipped,
}
```

**Diagnostics / ambiguity notes:** Duplicate enum names or duplicate payloads are diagnosable.

### Scalar

**Kind:** Type semantic.

**Description:** A primitive or scalar-like value.

**Best used when:** The value has no properties relevant to semantic model traversal.

**Avoid when:** The type is a value object with meaningful internal structure.

**Projection implications:** JSON Schema exports a scalar type and format when available. EF Core maps a scalar property or converted value. Power BI maps a column data type.

### Strong Scalar

**Kind:** Projection-neutral nominal atomic type whose complete representation is one underlying canonical scalar; it does not imply identity, key, ownership, or target-specific conversion.

**Projection implications:** JSON Schema and System.Text.Json use the underlying scalar representation, EF Core maps the underlying provider scalar including inside supported STM-owned JSON, and Power BI uses the underlying scalar classification; explicit authoring and supported CLR shape are defined by `docs/specs/strong-scalar-semantics.md`.

**Example:**

```csharp
public string Email { get; init; } = string.Empty;
```

**Diagnostics / ambiguity notes:** Custom scalar-like domain types may require explicit format or conversion metadata.

### Identifier

**Kind:** Property/type semantic.

**Description:** A value identifies something but is not necessarily the primary key of the current entity.

**Best used when:** Modeling external IDs, correlation IDs, tenant IDs, or reference IDs.

**Avoid when:** The value is the entity key; use `Key` instead.

**Projection implications:** JSON Schema may preserve identifier annotation or format metadata. EF Core treats it as an ordinary property unless key metadata is also present. Power BI may use it as category or key-like column metadata by policy.

**Example:**

```csharp
public string CorrelationId { get; init; } = string.Empty;
```

**Diagnostics / ambiguity notes:** Identifier does not imply uniqueness unless key semantics are present.

### DisplayName

**Kind:** Type/property metadata semantic.

**Description:** A stable user-facing label.

**Best used when:** A human-readable label should be shown by UI, documentation, or analytical tools across targets.

**Avoid when:** Only a serialized name, database column name, or report-specific caption must change.

**Projection implications:** JSON Schema may map it to `title`. EF Core does not rename tables or columns unless target policy chooses display names. Power BI may use it as table or column display metadata by policy.

**Example:**

```csharp
[SemanticName("Customer name")]
public string Name { get; init; } = string.Empty;
```

**Diagnostics / ambiguity notes:** Display names should not replace canonical identifiers. Conflicting display annotations are diagnosable during extraction.

### Description

**Kind:** Type/property metadata semantic.

**Description:** Human-readable explanatory text.

**Best used when:** Consumers need stable documentation or tooltips.

**Avoid when:** The text encodes behavior or validation rules that should be modeled explicitly.

**Projection implications:** JSON Schema may map it to `description`. Power BI may use description metadata where supported. EF Core ignores it unless target-specific comments are configured.

**Example:**

```csharp
[SemanticUserDescription("The customer-facing display name.")]
public string Name { get; init; } = string.Empty;
```

**Diagnostics / ambiguity notes:** Conflicting description sources are diagnosable during extraction.

### Category

**Kind:** Metadata semantic.

**Description:** Stable grouping/category metadata.

**Best used when:** Properties or types should be grouped in a projection-neutral way.

**Avoid when:** The grouping is target-specific, such as a Power BI display folder.

**Projection implications:** JSON Schema/UI projections may preserve grouping metadata. Power BI may use it for display folders only when configured. EF Core normally ignores it.

**Example:**

```csharp
[SemanticCategory("Contact")]
public string Email { get; init; } = string.Empty;
```

**Diagnostics / ambiguity notes:** Category is presentation metadata and should not imply type role or ownership.

### Order

**Kind:** Metadata semantic.

**Description:** Deterministic presentation/order hint.

**Best used when:** A stable order should be preserved across generated documentation, schemas, or simple UI surfaces.

**Avoid when:** The order is target-specific, such as database column order or a single report visual order.

**Projection implications:** JSON Schema/UI projections may preserve order metadata. Power BI may map it to display order by policy. EF Core normally ignores it.

**Example:**

```csharp
[SemanticOrder(10)]
public string Name { get; init; } = string.Empty;
```

**Diagnostics / ambiguity notes:** Order must be deterministic and should not change model identity.

### Format

**Kind:** Scalar metadata semantic.

**Description:** Projection-neutral scalar format such as email, URI, date, time, date-time, duration, or UUID.

**Best used when:** The format has cross-target meaning.

**Avoid when:** The format is JSON Schema-specific or serializer-specific only.

**Projection implications:** JSON Schema maps compatible formats. EF Core may use conversions by target policy. Power BI may map supported data categories or formatting by policy.

**Example:**

```csharp
[SemanticFormat(SemanticScalarFormat.Email)]
public string Email { get; init; } = string.Empty;
```

**Diagnostics / ambiguity notes:** Invalid format arguments or incompatible member types are diagnosable during extraction.

### Constraint

**Kind:** Constraint semantic.

**Description:** Projection-neutral validation bounds or predicates on scalar, collection, or object values.

**Best used when:** The constraint is part of the domain contract.

**Avoid when:** The constraint is only a UI validation hint or target-specific database constraint.

**Projection implications:** JSON Schema maps compatible constraints. EF Core may map length/precision/nullability where policy supports it. Power BI may ignore constraints or preserve metadata.

**Example:**

```csharp
[SemanticStringConstraints(MinLength = 1, MaxLength = 200)]
public string Name { get; init; } = string.Empty;
```

**Diagnostics / ambiguity notes:** Invalid constraint ranges and constraints on incompatible member types are diagnosable.

### Computed

**Kind:** Member semantic.

**Description:** A member whose value is derived from an expression rather than directly supplied state.

**Best used when:** Computation is part of the semantic model and can be described projection-neutrally.

**Avoid when:** The computation is only DAX, SQL, JSON Schema, or application code specific.

**Projection implications:** JSON Schema may preserve read-only or annotation metadata. EF Core calculated columns are not implied. Power BI DAX measures are target-specific and must use Power BI metadata.

**Example:**

```csharp
[SemanticAnnotation("schema.computed", "true")]
public decimal Total { get; init; }
```

**Diagnostics / ambiguity notes:** Computed does not imply any target can execute the expression.

## Envelope Semantics

### Envelope

**Kind:** Type semantic.

**Description:** A wrapper boundary that carries, manages, versions, transports, persists, audits, authorizes, caches, or otherwise contextualizes another semantic payload.

**Best used when:** The wrapper has lifecycle, transport, audit, management, persistence, versioning, authorization, or context meaning independent of the payload.

**Avoid when:** The wrapper exists only because one serializer, database, message bus, report, or endpoint requires a shape.

**Projection implications:** JSON Schema can export the envelope as root wrapper object, or export the payload as root schema and keep the envelope as context by target policy. EF Core can map the envelope as a persistence/cache entity with metadata columns while mapping the payload as owned, JSON/serialized, converted, ignored, or separately mapped by explicit policy. Power BI can expose envelope metadata as reporting columns and ignore, flatten, reference, or serialize payload by analytical policy.

**Example:**

```csharp
[SemanticEnvelope("management")]
public sealed class ManagedEnvelope<TPayload>
{
    [SemanticEnvelopePayload]
    public required TPayload Payload { get; init; }

    [SemanticEnvelopeMetadata]
    public required long Revision { get; init; }

    [SemanticEnvelopeMetadata]
    public required string ModifiedBy { get; init; }
}
```

**Diagnostics / ambiguity notes:** Envelope without payload, envelope with multiple payloads, unrepresented payload type, ambiguous projection-root selection, and unsupported target payload representation are diagnosable. Envelope semantics do not erase payload semantics.

### EnvelopePayload

**Kind:** Property semantic.

**Description:** The distinguished member inside an envelope that carries the wrapped semantic payload.

**Best used when:** Exactly one member is the wrapped domain payload.

**Avoid when:** The member is metadata about transport, auditing, tenancy, versioning, management, persistence, or cache context.

**Projection implications:** JSON Schema, EF Core, and Power BI can embed, reference, serialize, flatten, ignore, or treat the payload as opaque by explicit target policy.

**Example:**

```csharp
[SemanticEnvelopePayload]
public required OrderSubmitted Payload { get; init; }
```

**Diagnostics / ambiguity notes:** Payload markers outside an envelope, duplicate payloads, and payload type references absent from the canonical model are diagnosable.

### EnvelopeMetadata

**Kind:** Property semantic.

**Description:** A member that describes envelope lifecycle or context rather than payload domain state.

**Best used when:** The member describes transport, correlation, version, tenant, audit, authorization, cache, management, or persistence context of the envelope.

**Avoid when:** The member belongs to the payload domain state.

**Projection implications:** JSON Schema includes metadata when the envelope is root. EF Core may map metadata as columns by policy. Power BI may expose metadata as report columns by policy.

**Example:**

```csharp
[SemanticEnvelopeMetadata]
public DateTimeOffset ReceivedAt { get; init; }
```

**Diagnostics / ambiguity notes:** Metadata markers outside an envelope are diagnosable. Metadata must not be interpreted as payload state.

## Envelope Invariants

- An envelope is a wrapper boundary.
- An envelope normally has exactly one payload.
- Envelope metadata describes envelope lifecycle or context, not payload domain state.
- Envelope semantics do not erase payload semantics.
- Projection policy decides whether the envelope or payload is the projection root for a target.
- Unsupported or ambiguous projection-root choices must emit diagnostics.

## Envelope Projection Policy Concepts

| Concept | Meaning |
|---|---|
| envelope-as-root | The wrapper object is the target projection root. |
| payload-as-root | The wrapped payload is the target projection root. |
| payload embedded | The payload appears inline in the envelope projection. |
| payload reference | The payload is represented by a reference to a separately projected artifact. |
| payload serialized | The payload is represented as serialized data. |
| payload opaque | The target projection intentionally does not interpret the payload. |

Domain packages may expose target-specific options for these choices, but those options do not change the core meaning of `Envelope`, `EnvelopePayload`, or `EnvelopeMetadata`.

## Diagnostic Requirements

Core semantic vocabulary diagnostics must be queryable by code, stage, path, and projection target when applicable.

Required envelope diagnostic classes include:

- envelope with no payload;
- envelope with multiple payloads without explicit policy;
- payload marker outside envelope;
- envelope metadata marker outside envelope;
- envelope payload not represented in canonical model;
- envelope and payload both selected as projection root without explicit policy;
- unsupported payload representation for a target.

Display Identity and Access Path extraction diagnostics are defined in `docs/specs/type-model-dotnet-attributes.md`; malformed/ambiguous groups must not survive as valid reserved `schema.*` semantics.

## M0034 Evolution, Ownership, Lifecycle, Temporal Validity, and Extension Data Semantics

The core semantic vocabulary includes projection-neutral annotation keys for `Ownership`, `OwnedObject`, `OwnedCollection`, `Versioned`, `Version`, `Revision`, `CurrentVersion`, `TemporalValidity`, `ValidFrom`, `ValidTo`, `LifecycleState`, and `ExtensionData`.

These semantics preserve model meaning independently of JSON Schema, EF Core, Power BI, and System.Text.Json; target packages may derive safe metadata or diagnostics but must not generate query filters, workflow transitions, temporal-table configuration, automatic primary-key replacement, DAX measures, or arbitrary extension-data flattening by default.

## M0050 URI and Ownership Clarification

`System.Uri` is a string-compatible scalar and implies the projection-neutral URI format by default. Ownership continues to mean lifecycle containment; it does not choose flattening, JSON serialization, or EF owned-navigation storage.
