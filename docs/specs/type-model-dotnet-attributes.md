# Type Model .NET Attribute Specification

## Purpose

Define the stable attribute vocabulary for compile-time .NET extraction into the canonical semantic type model.

## Attribute Extensibility Contract

Semantic attributes are the primary code-first declaration mechanism for canonical semantic primitives.

The built-in vocabulary may be extended by custom attributes that declare one of these roles:

- core alias attribute: maps directly to a core primitive such as entity, value object, key, relationship, envelope, display name, description, format, constraint, or category;
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

### `SemanticDescriptionAttribute`

- Targets: class, struct, enum, property, field.
- Semantics:
  - maps to `schema.description`;
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

### `SemanticRelationshipAttribute`

- Targets: property (allow multiple).
- Semantics:
  - marks explicit relationships (`schema.relationship=explicit`);
  - constructor accepts optional principal type metadata name string;
  - `PrincipalKey`, `ForeignKey`, and `Cardinality` map to relationship annotations.

### `SemanticEnvelopeAttribute`

- Targets: class, struct, record class, record struct.
- Semantics:
  - marks a type as an envelope core semantic;
  - maps to `schema.envelope=true`;
  - optional projection-neutral `Purpose` maps to `schema.envelope.purpose`;
  - does not erase the semantics of the payload type;
  - invalid usage is diagnosable.

### `SemanticEnvelopePayloadAttribute`

- Targets: property, field.
- Semantics:
  - marks the distinguished payload property inside an envelope;
  - maps to `schema.envelope.payload=true`;
  - payload semantics remain attached to the payload type;
  - a payload marker outside an envelope is diagnosable unless a transformation explicitly promotes the containing type to an envelope;
  - multiple payloads are diagnosable unless explicit policy allows them.

### `SemanticEnvelopeMetadataAttribute`

- Targets: property, field.
- Semantics:
  - marks a property as envelope lifecycle/context metadata;
  - maps to `schema.envelope.metadata=true`;
  - optional `Kind` or `Purpose` may map to projection-neutral metadata when supported;
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

## Envelope Attribute Example

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
}
```

## Precedence Rules

1. Explicit semantic attributes.
2. XML documentation summaries (when enabled).
3. Naming policy conventions.
4. CLR symbol-name fallback.

Concrete precedence examples:

- `[SemanticName]` overrides naming policy.
- `[SemanticDescription]` overrides XML summary extraction.
- `[SemanticIgnore]` overrides convention discovery inclusion.
- `[SemanticKey]` overrides key inference.
- `[SemanticRelationship]` overrides relationship inference.
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
- envelope-specific diagnostics for invalid envelope target usage, missing payload, duplicate payloads, misplaced payload markers, and misplaced metadata markers must be assigned stable IDs before implementation.

Diagnostics are contractually stable by code; message text is non-authoritative.
