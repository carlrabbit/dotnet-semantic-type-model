# Type Model .NET Attribute Specification

## Purpose

Define the stable attribute vocabulary for compile-time .NET extraction into the canonical semantic type model.

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

## Diagnostics

Extraction/generator diagnostics in `STM5xxx` include:

- `STM5001` invalid attribute target/usage;
- `STM5002` conflicting duplicate semantic attributes;
- `STM5016` invalid composite key ordering;
- `STM5017` unsupported/invalid semantic attribute argument values.

Diagnostics are contractually stable by code; message text is non-authoritative.

## Deferred Attributes

The following are explicitly deferred in M0010:

- `SemanticFormatAttribute`
- `SemanticUnitAttribute`
- `SemanticConstraintAttribute`
- `SemanticAnnotationAttribute`

These remain non-goals until concrete cross-projection requirements are validated.
