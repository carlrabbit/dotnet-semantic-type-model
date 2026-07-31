# EF Core Role-Aware Owned Storage

## Status

Authoritative behavioral specification for M0050.

## Purpose

Define how EF Core projection interprets semantic ownership in combination with the target type's semantic role and selected storage policy.

## Core Principle

Semantic ownership and storage shape are distinct.

```text
[SemanticOwned]
  says the member is lifecycle-owned / contained by the owner.

Target type role
  says what kind of thing is owned.

EF storage policy
  says how the owned thing is represented in EF metadata.
```

Projection must not treat ownership as a synonym for flattening.

## Classification

For every property, EF Core projection classifies:

```text
ownership kind:
  none
  owned object
  owned collection

target role:
  value object
  object
  entity
  unknown

target shape:
  scalar
  enum
  object
  array
  dictionary
  union
```

The classification must occur before flattening, JSON serialization, owned navigation, unsupported-shape diagnostics, or relationship projection.

## Owned ValueObject

An owned single object whose target role is `ValueObject` uses the configured value-object storage mode.

| Mode | Required behavior |
|---|---|
| `Diagnose` | Emit diagnostic and skip projected property. |
| `Flatten` | Project flattenable scalar/enum members as columns. |
| `SerializeJson` | Project one string JSON column for the owned value object. |
| `Owned` | Apply true EF ownership if implemented; otherwise emit explicit diagnostic and do not claim ownership metadata. |

`[SemanticOwned]` must not bypass `ValueObjectProjectionMode`.

## Owned Object

An owned single object whose target role is `Object` is not a value object.

Default behavior:

```text
diagnose unless explicit owned-object storage policy exists
```

The projection must not silently flatten or serialize it using value-object rules.

Supported initial policies, if implemented, may include:

```text
Diagnose
SerializeJsonStringColumn
IgnoreWithWarning
```

## Owned Entity

An owned single object whose target role is `Entity` is diagnostic unless an explicit aggregate-owned entity policy exists.

Do not silently create an independent table.

## Owned Collections

Owned collections require explicit policy.

Default behavior:

```text
diagnostic
no silent flattening
no implicit separate table
no implicit JSON collection
```

## Unowned ValueObject

An unowned object whose target role is `ValueObject` may still use `ValueObjectProjectionMode`, but projection output must not claim lifecycle ownership unless the property is actually annotated as owned.

## ModelBuilder Consistency

EF domain metadata and applied `ModelBuilder` metadata must agree.

- A JSON string projection must apply as one string property.
- A flattened projection must apply as flattened properties.
- A true owned projection must apply EF owned navigation metadata.
- If true owned navigation is not implemented, no metadata may claim that it was.

## Diagnostics

Required diagnostic categories:

```text
owned object role requires explicit policy
owned entity role unsupported without explicit policy
owned collection policy required
true EF owned navigation not implemented
owned value object cannot be flattened because nested member shape is unsupported
owned value object JSON serialization unsupported for target type
```

## Non-Goals

- Provider-specific JSON column configuration.
- Migration SQL generation.
- DbContext generation.
- Owned collection implementation by default.
- Aggregate-owned entity support by default.
- Arbitrary flattening of object-role types.

## Invariants

- Ownership semantics are retained in canonical metadata.
- Storage policy is EF-specific.
- Value object role and object role are distinct.
- No silent table creation for owned object or owned entity roles.
- No fake `OwnsOne` annotations without actual application support.
