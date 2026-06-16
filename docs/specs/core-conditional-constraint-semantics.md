# Core Conditional Constraint Semantics Specification

## Status

Authoritative behavioral specification.

## Purpose

Define the projection-neutral conditional constraint vocabulary for simple cross-property validation rules.

This specification is authoritative for conditional constraint meaning, the `RequiredWhen` semantic primitive, canonical annotation keys, cross-domain obligations, validation, diagnostics, and inspection.

## Core Principle

A conditional constraint is core only when it describes model validity independent of a projection target.

Do not promote target-only validation behavior to core. Use target metadata namespaces such as `configuration.*`, `jsonSchema.*`, `systemTextJson.*`, `efCore.*`, or `powerBi.*` when the condition exists only to drive one target's implementation mechanics.

## Initial Scope

M0040 defines only a narrow first conditional semantic:

```text
RequiredWhen
```

`RequiredWhen` states that a target property must be present when a source property equals a supported literal value.

The initial rule shape is intentionally limited:

```text
target property
source property
operator: equals
literal value: string, boolean, integer, number, or enum symbolic value
message: optional stable diagnostic/user-facing text
```

Arbitrary expressions, callbacks, custom delegates, and multi-property boolean expression trees are out of scope.

## Canonical Annotation Keys

If implementation represents conditional constraints as canonical annotations before or instead of structured primitives, reserve these keys:

```text
schema.condition.requiredWhen
schema.condition.requiredWhen.source
schema.condition.requiredWhen.operator
schema.condition.requiredWhen.value
schema.condition.requiredWhen.message
```

Projection-specific registration or validation behavior must use target namespaces, for example:

```text
configuration.validation.*
jsonSchema.condition.*
systemTextJson.validation.*
efCore.*
powerBi.*
```

## RequiredWhen

### Kind

Property constraint semantic.

### Description

A property is required when another modeled property equals a supported literal value.

### Best used when

The condition is part of the semantic contract and should be available to validation, schema generation, documentation, query, and inspection.

### Avoid when

The condition exists only because a specific framework, serializer, database, report, or UI requires it.

### Example

```csharp
[SemanticRequiredWhen(nameof(Provider), "File")]
public string? TargetFilePath { get; init; }
```

### Projection implications

| Target | Expected behavior |
|---|---|
| Configuration | Generate or apply conditional options validation when the Configuration domain projection is selected. |
| JSON Schema | Map to conditional schema constructs such as `if`/`then` when supported by the selected JSON Schema dialect and policy. |
| System.Text.Json | Preserve metadata or use explicit validation policy; do not silently add runtime validation unless selected. |
| EF Core | Ignore or preserve metadata by default; do not generate check constraints unless an explicit EF policy is selected. |
| Power BI | Ignore or preserve metadata by default; do not generate measures or filters. |
| Query/Inspection | Show conditional constraint semantics deterministically. |

## Invariants

- The target property and source property must belong to the same object type unless a future spec explicitly defines nested paths.
- The source property must resolve to a modeled property.
- The literal value must be compatible with the source property's semantic type.
- Enum comparison values must resolve to a declared enum value when the source property is enum-like.
- Requiredness and nullability remain distinct.
- Conditional constraints must not generate target-specific behavior without an explicit target projection or policy.

## Diagnostics

Implementations must emit diagnostics for unresolved source property, unresolved target property, unsupported operator, unsupported literal kind, incompatible literal value, enum literal mismatch, unsupported declaration target, duplicate/conflicting rules, and unsupported target projection.

## Inspection

Inspection output must include target property, condition source property, operator, literal value, message when present, and target projection support or unsupported reason when inspecting a domain model.
