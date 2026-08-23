# Strong Scalar Semantics

## Status

Authoritative behavioral specification for the 4.1 line.

## Purpose

Define a projection-neutral nominal scalar type for CLR wrappers whose domain identity is distinct from their underlying scalar representation.

A Strong Scalar solves the semantic gap between:

```text
object/value-object semantics
```

and:

```text
primitive/canonical scalar representation
```

without making identity, key, persistence, or serializer policy part of the core type meaning.

## Canonical Term

**Strong Scalar:** a distinct nominal semantic type whose complete data representation is exactly one underlying canonical scalar value.

A Strong Scalar is not an Entity, Value Object, Key, Identifier role, ownership boundary, or projection-specific converter declaration.

Examples include strongly typed identifiers and other nominal scalar values such as `SpecificationVersionId(Guid Value)`, but use as an identifier is determined by the containing model and key semantics rather than by Strong Scalar itself.

## Canonical Model Surface

Add the public canonical type kind:

```text
TypeKind.StrongScalar
```

and the public type definition:

```text
StrongScalarTypeDefinition : TypeDefinition
    ValueType : TypeRef
```

`ValueType` MUST resolve to one supported non-null `ScalarTypeDefinition`.

Allowed underlying canonical scalar kinds in this contract are:

```text
Boolean
String
Integer
Number
Decimal
Date
Time
DateTime
DateTimeOffset
Duration
Guid
Binary
```

`Json` and `Unknown` are not valid Strong Scalar value kinds.

The value type MUST NOT be:

```text
Object
Array
Dictionary
Enum
Union
Intersection
Reference to a non-scalar shape
StrongScalar
Any
Never
```

Nested Strong Scalars are deliberately not supported in 4.1.

The Strong Scalar's CLR `Value` member is representation metadata, not a canonical `PropertyDefinition`. A consumer inspecting the canonical model sees one `StrongScalarTypeDefinition`, not an object containing a `Value` property.

Use-site requiredness and nullability remain property/cardinality semantics. The underlying `Value` representation itself must be non-nullable.

## Code-First Authoring

The explicit authoring marker is:

```csharp
[SemanticStrongScalar]
public readonly record struct SpecificationVersionId(Guid Value);
```

`SemanticStrongScalarAttribute` targets structs. The 4.1 authoring contract supports non-generic readonly structs and readonly record structs only.

Automatic inference from a one-property class/struct, a `*Id` suffix, key usage, or a `Value` property is forbidden.

An unannotated wrapper retains its existing 4.0 behavior.

### Required CLR representation shape

An attributed Strong Scalar MUST:

1. be a non-generic readonly struct or readonly record struct;
2. expose exactly one non-ignored public instance data property named `Value`;
3. expose a readable `Value` property with no mutable setter (`init` is allowed);
4. use a non-null CLR type for `Value` that maps to one allowed canonical scalar kind;
5. expose a public single-argument constructor whose parameter type exactly matches the `Value` property type;
6. have no additional non-ignored public instance fields/properties representing data.

Methods, operators, static members, and members marked `[SemanticIgnore]` do not add representation components.

Invalid Strong Scalar declarations emit stable diagnostic `STM5051` and do not produce a `StrongScalarTypeDefinition`.

### Interaction with other semantic authoring

- `[SemanticType]` may coexist only to select/discover the type or provide otherwise-valid type metadata; a non-`Unspecified` semantic role on a Strong Scalar is invalid.
- `[SemanticRole]` on a Strong Scalar is invalid.
- Strong Scalar does not infer `SemanticKey` from its name, CLR type, or use.
- A containing property typed as a Strong Scalar MAY participate in `SemanticKey`, Display Identity, and Access Path semantics normally.
- `SemanticOwned` is invalid on a property whose direct value shape is Strong Scalar because Strong Scalar is atomic rather than structurally owned.
- type-level semantic mutability does not apply to Strong Scalar; the CLR representation is immutable by contract. Property-level mutability of a containing object may still describe whether that property can be replaced.
- property-level scalar format/string/numeric constraints are validated against the underlying scalar kind. Collection/object constraints are not made valid merely because a property is Strong Scalar-valued.

## Compatibility

Strong Scalar is additive in 4.1.

Existing unannotated CLR wrappers continue to follow existing projection behavior. In particular, existing EF direct-column fallback for a supported `Value` + matching-constructor wrapper remains supported for 4.1 and MUST NOT be removed merely because canonical Strong Scalar now exists.

Only the explicit `[SemanticStrongScalar]` opt-in changes canonical and JSON representation from object-like wrapper shape to scalar shape.

Do not rename or remove the existing `SemanticLiteralKind.StrongIdentifier` public enum member in 4.1. Strong Scalar conditional-literal authoring is not expanded by this contract.

## Compile-Time Generation and Manifest

Runtime extraction and compile-time generation MUST produce equivalent canonical Strong Scalar types.

The compile-time semantic manifest MUST represent Strong Scalar kind and its underlying value type without requiring the EF generator to re-infer semantic meaning from an arbitrary CLR one-property shape.

The manifest schema version advances from `1` to `2` for the 4.1 line.

Existing manifest policy remains deliberately simple:

- manifests are generated on build;
- producer and consumer suite versions must match exactly;
- the EF generator consumes only the supported manifest schema version;
- no persisted-manifest or cross-version compatibility negotiation is introduced.

## JSON Representation

The canonical JSON representation of a Strong Scalar is the JSON representation of its underlying scalar value.

For:

```csharp
[SemanticStrongScalar]
public readonly record struct SpecificationVersionId(Guid Value);
```

the JSON representation is a GUID JSON string, not:

```json
{ "Value": "..." }
```

This scalar representation applies at root, property, array-item, and nested owned-JSON locations where the target supports the containing shape.

## JSON Schema

A Strong Scalar definition projects as the underlying scalar schema while retaining its own nominal schema definition/reference identity.

For a Guid-backed Strong Scalar, the effective schema is:

```text
type: string
format: uuid
```

A property typed as that Strong Scalar continues to reference the Strong Scalar definition rather than being rewritten to the primitive type identity.

When STM semantic annotations are enabled, the Strong Scalar definition includes:

```json
{
  "x-stm": {
    "strongScalar": true
  }
}
```

The underlying scalar kind/format remains authoritative native JSON Schema and is not redundantly serialized into `x-stm`.

The `Value` CLR property is never emitted as a JSON Schema object property for a Strong Scalar.

Use-site scalar constraints continue to apply normally.

## System.Text.Json

The supported STM Strong Scalar wire behavior serializes the wrapper as its underlying scalar and reconstructs the wrapper through its declared single-argument constructor.

For Strong Scalar models, the M0066 one-way JSON representation-fidelity baseline uses the `JsonSerializerOptions.AddSemanticTypeModelJson(...)` configuration path (or behavior demonstrably equivalent to it) so STM can establish Strong Scalar conversion in addition to resolver customization.

The resolver-only `WithSemanticTypeModelJson(...)` path remains supported for its existing metadata/name behavior. It satisfies Strong Scalar wire fidelity only when the serializer options already contain an equivalent compatible Strong Scalar converter.

An explicit user-owned representation-changing converter that conflicts with Strong Scalar scalar representation remains outside the M0066 fidelity guarantee. STM does not need to infer or reverse-engineer such converters.

System.Text.Json MUST NOT serialize an STM-configured Strong Scalar as an object merely because the CLR wrapper exposes a `Value` property.

## EF Core

### Direct scalar columns

An explicitly modeled Strong Scalar property maps to the provider scalar representation selected for its underlying scalar kind.

Existing direct-column compatibility for unannotated supported wrapper shapes remains intact.

### Owned JSON

When a Strong Scalar occurs anywhere inside an STM-owned JSON object or collection, it is a supported JSON scalar leaf.

EF JSON serialization/deserialization MUST recursively use the Strong Scalar's underlying scalar representation and reconstruct the CLR wrapper during materialization.

The presence of a Strong Scalar inside owned JSON MUST NOT emit `EF_JSON_VALUE_TYPE_NOT_SERIALIZABLE` merely because its CLR type is a custom struct.

Strong Scalar does not change the existing ownership policy: `[SemanticOwned]` structural objects/collections remain JSON storage in this contract; no `OwnsOne`/`OwnsMany` relational storage is introduced.

## Power BI

Power BI type/data classification treats a supported Strong Scalar as its underlying scalar kind while retaining nominal type identity where the Power BI domain/inspection model can represent it.

Strong Scalar does not infer table keys, relationships, display behavior, or analytical role.

## Configuration / Options

The 4.1 branch retains `SemanticTypeModel.Configuration`, but Strong Scalar Options binding is not added in this milestone.

A selected Configuration/Options model containing a Strong Scalar-valued member is outside the supported Configuration binding contract for 4.1 and MUST be rejected or diagnosed deterministically rather than silently claiming supported scalar binding.

Do not add TypeConverter, custom Configuration Binder, generated Options converter, or application-host behavior for Strong Scalar as part of this contract.

## Query, Inspection, Transformation, and Cloning

Generic canonical-model infrastructure that handles `TypeDefinition`/`TypeKind` MUST preserve Strong Scalar identity and `ValueType`.

Inspection output must distinguish Strong Scalar from Object/ValueObject and show its underlying scalar type deterministically.

This milestone does not introduce a new persisted-model compatibility promise. Existing exact/current-model snapshot behavior, where supported, must understand the current Strong Scalar type without adding cross-version compatibility shims.

## Required Regression Scenario

The motivating regression is structurally equivalent to:

```text
SpecificationState
└── Entries : List<SpecificationStateEntry>   [SemanticOwned collection]
    └── SpecificationVersionId : SpecificationVersionId

[SemanticStrongScalar]
readonly record struct SpecificationVersionId(Guid Value)
```

The intended EF representation is owned JSON. Relational `OwnsMany`/table mapping is out of scope and must not be introduced as a workaround.

The regression evidence must prove the real code-first boundary:

1. CLR extraction/generation produces `StrongScalarTypeDefinition<Guid>`;
2. the canonical Strong Scalar has no semantic `Value` property;
3. generated manifest v2 preserves Strong Scalar meaning;
4. JSON Schema represents `SpecificationVersionId` as a GUID scalar definition;
5. STM-configured System.Text.Json emits a scalar GUID and round-trips the wrapper;
6. EF relational derivation/generation accepts the nested Strong Scalar in owned JSON without `EF_JSON_VALUE_TYPE_NOT_SERIALIZABLE`;
7. a real SQLite-backed `DbContext` can create/save/reload the containing entity/value shape;
8. persisted JSON contains the Strong Scalar as the bare underlying GUID JSON value, not a `{ "Value": ... }` object;
9. package smoke proves the packed generator/runtime package path handles manifest v2 and the Strong Scalar scenario.

Hand-built canonical models alone are insufficient regression evidence.

## Non-Goals

This contract does not add:

- automatic Strong Scalar inference;
- a semantic Identifier role/type;
- key inference from Strong Scalar;
- Strong Scalar classes or mutable structs;
- nested Strong Scalars;
- enum-backed Strong Scalars;
- Strong Scalar conditional-literal parsing;
- Configuration/Options scalar-wrapper binding;
- relational `OwnsOne` / `OwnsMany` storage;
- arbitrary user converter inference;
- a shared JSON runtime package/model;
- persisted manifest compatibility or negotiation.
