# Type Model .NET Attribute Specification

## Purpose

Define the stable attribute vocabulary for compile-time .NET extraction into the canonical semantic type model.

## Attribute Extensibility Contract

Semantic attributes are the primary code-first declaration mechanism for canonical semantic primitives.

The built-in vocabulary may be extended by custom attributes that declare one of these roles:

- core alias attribute: maps directly to a core primitive such as entity, value object, key, envelope, ownership, display name, description, format, constraint, or category;
- core extension attribute: carries projection-neutral metadata that a transformation normalizes into canonical annotations or primitives;
- domain attribute: carries domain-specific metadata for a domain semantic model such as JSON Schema, EF Core, Power BI, or System.Text.Json.

Custom attributes do not mutate the canonical model directly. Extraction preserves intent, transformations derive meaning, and invalid or ambiguous derivation emits diagnostics.

## Attribute Vocabulary

### `SemanticTypeAttribute`

- Targets: class, struct, enum.
- Semantics:
  - marks explicit extraction roots in `ExplicitAttributes` and `ReachableFromRoots` discovery modes;
  - optional `Name` maps to `schema.title`;
  - optional `Role` maps to `schema.role`.

### `SemanticIgnoreAttribute`

- Targets: class, struct, enum, property, field.
- Semantics:
  - excludes attributed symbols from extraction;
  - overrides namespace/convention discovery.

### `SemanticNameAttribute`

- Targets: class, struct, enum, property, field.
- Semantics:
  - maps to canonical display name metadata (`schema.title`) for types;
  - maps member/enum output names for properties and enum values;
  - overrides naming policy.

### `SemanticUserDescriptionAttribute` and `SemanticTechnicalDescriptionAttribute`

- Targets: class, struct, enum, property, field.
- Semantics:
  - maps to `schema.userDescription` or `schema.technicalDescription`;
  - overrides XML documentation summaries.

### `SemanticDisplayNameAttribute`

- Targets: class, struct, enum, property, field.
- Semantics:
  - maps to user-facing display metadata (`ui.title`);
  - does not replace canonical member naming.

### `SemanticCategoryAttribute`

- Targets: class, struct, enum, property, field.
- Semantics:
  - maps to generic UI categorization metadata (`ui.category`).

### `SemanticOrderAttribute`

- Targets: class, struct, enum, property, field.
- Semantics:
  - maps to deterministic UI/property ordering metadata (`ui.order`).

### `SemanticRoleAttribute`

- Targets: class, struct, enum.
- Semantics:
  - maps to `schema.role`.

### `SemanticKeyAttribute`

- Targets: property (allow multiple).
- Semantics:
  - marks key members (`schema.key=true`);
  - `Kind` maps to `schema.key.kind`;
  - `Name` + `Order` support composite keys through shared key-name grouping;
  - `IsGenerated` maps to `schema.key.generated`.

### `SemanticDisplayIdentityAttribute`

- Targets: property (allow multiple: false).
- Public shape:
  - `int Order { get; init; }`, default `0`.
- Semantics:
  - marks the property as one component of the containing object's single Display Identity;
  - maps to `schema.displayIdentity=<order>`;
  - the non-negative `Order` value defines component ordering;
  - order values must be unique within the effective extracted property set of each object;
  - gaps are allowed;
  - there are no named Display Identity variants in this contract.
- Boundaries:
  - does not imply `SemanticKey`, uniqueness, `SemanticDisplayName`, `SemanticOrder`, string concatenation, or any target-specific rendering behavior;
  - can coexist with key and Access Path semantics on the same property.

### `SemanticAccessPathAttribute`

- Targets: property (allow multiple).
- Public shape:
  - constructor `SemanticAccessPathAttribute(string name)`;
  - `string Name { get; }`;
  - `int Order { get; init; }`, default `0`.
- Semantics:
  - each attribute adds the property to one named Access Path scoped to the containing object;
  - maps to `schema.accessPath.<name>=<order>`;
  - names are case-sensitive ordinal semantic identifiers and must match `[A-Za-z][A-Za-z0-9_.-]*`;
  - the non-negative `Order` value defines property order inside that named path;
  - order values must be unique within each path;
  - gaps are allowed;
  - a property may participate in multiple differently named Access Paths.
- Boundaries:
  - does not imply a database index, uniqueness, key semantics, query operator, equality/range/prefix behavior, sort order, query completeness, frequency guarantee, UI order, or API surface;
  - target packages do not gain behavior from this attribute until a target-specific contract explicitly adopts it.

### `SemanticMutableAttribute` and `SemanticImmutableAttribute`

- Targets: class, struct, property, field.
- Semantics: declare optional lifecycle mutability on an object type or member.
- Applying both to one target emits `STM5048`; neither attribute means unspecified.
- Member declarations override the containing object declaration, including mutable members in immutable objects.

### `SemanticEnvelopeAttribute`

- Targets: class, struct, record class, record struct.
- Semantics:
  - marks a type as an envelope core semantic;
  - maps to `schema.envelope=true`;
  - optional projection-neutral `Purpose` maps to `schema.envelope.purpose`;
  - does not erase the semantics of the payload type;
  - invalid usage is diagnosable.

### `SemanticEnvelopePayloadAttribute`

- Targets: property.
- Semantics:
  - marks the distinguished payload property inside an envelope;
  - maps to `schema.envelope.payload=true`;
  - payload semantics remain attached to the payload type;
  - a payload marker outside an envelope is diagnosable unless a transformation explicitly promotes the containing type to an envelope;
  - multiple payloads are diagnosable unless explicit policy allows them.

### `SemanticEnvelopeMetadataAttribute`

- Targets: property.
- Semantics:
  - marks a property as envelope lifecycle/context metadata;
  - maps to `schema.envelope.metadata=true`;
  - metadata marker outside an envelope is diagnosable unless explicitly allowed.

### `SemanticFormatAttribute`

- Targets: property, field.
- Semantics:
  - maps to `schema.format`;
  - supports common predefined `SemanticScalarFormat` values and custom strings;
  - invalid target usage is diagnosable.

### `SemanticStringConstraintsAttribute`

- Targets: property, field.
- Semantics:
  - maps to `schema.minLength`, `schema.maxLength`, and `schema.pattern`;
  - invalid ranges are diagnosable.

### `SemanticNumericConstraintsAttribute`

- Targets: property, field.
- Semantics:
  - maps to `schema.minimum`, `schema.maximum`, `schema.exclusiveMinimum`, `schema.exclusiveMaximum`, and `schema.multipleOf`;
  - invalid ranges are diagnosable.

### `SemanticCollectionConstraintsAttribute`

- Targets: property, field.
- Semantics:
  - maps to `schema.minItems`, `schema.maxItems`, and `schema.uniqueItems`;
  - invalid ranges are diagnosable.

### `SemanticEnumValueAttribute`

- Targets: enum fields.
- Semantics:
  - preserves enum display/description metadata as deterministic annotations on the owning enum shape.

### `SemanticAnnotationAttribute`

- Targets: class, struct, enum, property, field.
- Semantics:
  - preserves custom namespaced annotations;
  - invalid keys and conflicting duplicate values are diagnosable.

## Display Identity and Access Path Examples

```csharp
[SemanticType(SemanticTypeRole.Entity)]
public sealed class Customer
{
    [SemanticKey]
    public required Guid Id { get; init; }

    [SemanticDisplayIdentity(Order = 0)]
    [SemanticAccessPath("ByCustomerNumber")]
    public required string CustomerNumber { get; init; }

    [SemanticDisplayIdentity(Order = 1)]
    public required string Name { get; init; }
}
```

Composite Access Path:

```csharp
[SemanticAccessPath("ByDeviceAndTimestamp", Order = 0)]
public required Guid DeviceId { get; init; }

[SemanticAccessPath("ByDeviceAndTimestamp", Order = 1)]
public required DateTimeOffset Timestamp { get; init; }
```

A property may simultaneously be a key, Display Identity component, and member of one or more Access Paths. No semantic is inferred from another.

## Envelope Attribute Example

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

## Precedence Rules

1. Explicit semantic attributes.
2. XML documentation summaries (when enabled).
3. Naming policy conventions.
4. CLR symbol-name fallback.

Concrete precedence examples:

- `[SemanticName]` overrides naming policy.
- `[SemanticTechnicalDescription]` overrides XML summary technical fallback; `[SemanticUserDescription]` is independent.
- `[SemanticIgnore]` overrides convention discovery inclusion.
- `[SemanticKey]` overrides key inference.
- `[SemanticDisplayIdentity]` is explicit only; no key/display-name convention infers it.
- `[SemanticAccessPath]` is explicit only; no key/index/query convention infers it.
- `[SemanticEnvelope]` declares envelope semantics explicitly.
- `[SemanticEnvelopePayload]` declares the distinguished payload explicitly.
- `[SemanticEnvelopeMetadata]` declares envelope metadata explicitly.

## Diagnostics

Extraction/generator diagnostics in `STM5xxx` include:

- `STM5001` invalid attribute target/usage;
- `STM5002` conflicting duplicate semantic attributes;
- `STM5016` invalid composite key ordering;
- `STM5017` unsupported/invalid semantic attribute argument values.
- `STM5020` invalid semantic annotation key;
- `STM5021` invalid constraint target or order value;
- `STM5022` invalid string constraint range;
- `STM5023` invalid numeric constraint range;
- `STM5024` invalid collection constraint range;
- `STM5025` invalid scalar format usage.
- `STM5049` invalid or ambiguous Display Identity definition.
- `STM5050` invalid or ambiguous Access Path definition.
- envelope-specific diagnostics use stable core transformation IDs for missing payload, duplicate payloads, misplaced payload markers, misplaced metadata markers, missing payload type representation, and ambiguous projection-root declarations.

Diagnostics are contractually stable by code; message text is non-authoritative.

For M0065:

- negative Display Identity order or duplicate effective Display Identity order emits `STM5049`;
- invalid Access Path name, negative Access Path order, duplicate membership in the same named path, or duplicate effective order in one named path emits `STM5050`;
- an invalid Display Identity is omitted as a whole for the affected object;
- an invalid named Access Path is omitted as a whole for the affected object while unrelated valid Access Paths remain;
- generator diagnostics follow the existing `STM5xxx` extraction warning policy; this milestone does not introduce a new diagnostic-severity policy.

## Evolution, Ownership, Lifecycle, Temporal Validity, and Extension Data Attributes

## Logical Type

`SemanticLogicalTypeAttribute` targets properties and fields and emits the validated `schema.logicalType` annotation. The target must be an ordinary scalar, and the name must match `[A-Za-z][A-Za-z0-9_.-]*`. The annotation is projection-neutral; invalid names, non-scalar targets, and conflicting same-name scalar mappings report `STM5052`.

M0034 semantics can be authored with projection-neutral attributes in `SemanticTypeModel.DotNet`.

| Attribute | Target | Canonical annotation emitted |
|---|---|---|
| `SemanticOwnedAttribute` | property | `schema.ownership=true`, `schema.ownership.kind`, plus `schema.ownedObject=true` or `schema.ownedCollection=true` |
| `SemanticVersionedAttribute` | class or struct | `schema.versioned=true` |
| `SemanticVersionAttribute` | property | `schema.version=true` |
| `SemanticRevisionAttribute` | property | `schema.revision=true` |
| `SemanticCurrentVersionAttribute` | property | `schema.currentVersion=true` |
| `SemanticTemporalValidityAttribute` | class or struct | `schema.temporalValidity=true` |
| `SemanticValidFromAttribute` | property | `schema.validFrom=true` |
| `SemanticValidToAttribute` | property | `schema.validTo=true` |
| `SemanticLifecycleStateAttribute` | property | `schema.lifecycleState=true` |
| `SemanticExtensionDataAttribute` | property | `schema.extensionData=true`, with `schema.extensionData.keyType` and `schema.extensionData.valueType` when the dictionary shape can be identified |

`SemanticOwnedAttribute.Kind` may explicitly select `Object` or `Collection`; `Inferred` derives the kind from the property shape.
System.Text.Json `[JsonExtensionData]` import also normalizes to `schema.extensionData=true` when System.Text.Json attribute import is enabled.

## M0050 SemanticFormat Compatibility

`SemanticFormat` supports `string`, `Uri`, `Guid`, `DateOnly`, `TimeOnly`, `DateTime`, `DateTimeOffset`, and `TimeSpan`. `System.Uri` implies URI format by convention. `STM5025` MUST continue to diagnose integer, decimal, Boolean, collection, dictionary, object, and enum targets.
