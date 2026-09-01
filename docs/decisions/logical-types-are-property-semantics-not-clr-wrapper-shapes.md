# Decision: Logical Types Are Property Semantics, Not CLR Wrapper Shapes

## Status

Accepted for M0077.

This decision supersedes `remove-canonical-strong-scalar-semantics.md`.

## Context

M0076 removed Strong Scalar as a canonical semantic type because making a CLR single-value wrapper behave like its underlying primitive required every target to simulate primitive transparency.

M0076 deliberately left two exceptions in place:

- `SemanticLiteralKind.StrongIdentifier`;
- EF Core target-specific inference for CLR types with one public `Value` property and a matching constructor.

After Strong Scalar removal, those exceptions no longer serve a coherent semantic contract.

`StrongIdentifier` has no positive canonical behavior: ordinary CLR wrapper sources are unsupported by typed-literal normalization.

The EF exception still gives the same CLR wrapper shape privileged behavior by silently detecting it and generating provider-scalar conversion. This retains hidden structural inference for exactly the shape the canonical model no longer recognizes.

At the same time, there is still useful semantic information in distinguishing two scalar values with identical representation, for example:

```text
CustomerId : Guid
OrderId    : Guid
```

That information does not require CLR wrapper types.

## Decision

SemanticTypeModel assigns no special semantic or target-specific meaning to CLR single-value wrapper shape.

A type does not become a scalar, identifier, EF-convertible value, serializable primitive, or test-data scalar because it has:

```text
Value property + matching constructor
```

Strongly typed CLR identifiers remain an application/library concern unless a future explicit integration says otherwise.

Projection-neutral scalar interpretation that is distinct from representation is expressed instead as an optional Logical Type name on a scalar property.

Canonical representation:

```text
Property.Type -> ordinary ScalarTypeDefinition
Property.Annotations["schema.logicalType"] -> logical type name
```

Code-first authoring:

```csharp
[SemanticLogicalType("CustomerId")]
public Guid CustomerId { get; init; }
```

Logical Type is semantic metadata over an underlying scalar representation. It is not a canonical type node and does not imply behavioral substitutability.

`SemanticLiteralKind.StrongIdentifier` is removed. Typed literals continue to use the actual underlying supported scalar/enum kind.

## Durable Principle

```text
semantic interpretation
is independent from
CLR wrapper representation
```

and:

```text
Logical Type
adds meaning
without changing representation
```

Targets continue operating on the ordinary scalar representation unless a future explicit target contract chooses to consume Logical Type metadata.

## Rationale

This keeps SemanticTypeModel focused on semantic meaning that survives across representations.

A property-level Logical Type can be useful to schema/documentation consumers, validation policy, future terminology/value sources, test-data customization, UI generation, model inspection, and future code generation without forcing current targets to change how the value is represented or queried.

For example:

```csharp
[SemanticLogicalType("CustomerId")]
public Guid Id { get; init; }
```

remains an ordinary Guid to CLR, EF Core, LINQ, System.Text.Json, JSON Schema's standard type system, Power BI, and TestData's built-in scalar generator.

STM additionally knows that the value is semantically a `CustomerId`.

This avoids the architectural failure mode of Strong Scalar: target packages no longer need wrapper constructors, converters, special query rules, or parallel value representations merely to preserve semantic identity.

## Consequences

- CLR wrapper shape has no special handling in any STM package.
- EF Core automatic single-value-wrapper conversion is removed.
- `SemanticLiteralKind.StrongIdentifier` is removed.
- `SemanticLogicalTypeAttribute` becomes the explicit code-first authoring surface.
- Canonical `schema.logicalType` is member metadata over an ordinary scalar `TypeRef`.
- No `LogicalTypeDefinition`, `TypeKind.LogicalType`, or equivalent graph node is introduced.
- Logical Type names are case-sensitive and model-local.
- Reuse of one Logical Type name in a model requires the same scalar `TypeRef`.
- Constraints, format, unit, nullability, key semantics, display identity, and access paths remain independent use-site semantics.
- JSON Schema may preserve Logical Type in optional `x-stm.logicalType` without changing standard representation.
- EF Core, System.Text.Json, Power BI, and TestData gain no Logical Type-specific behavior in M0077.
- The EF compile-time manifest remains schema v3 because Logical Type is irrelevant to EF generation.
- Future external terminology or code generation may build on Logical Type, but must be designed separately.

## Alternatives Considered

### Retain EF wrapper convenience as target-specific behavior

Rejected.

Even when target-specific, structural wrapper inference preserves the old assumption that a `Value`-wrapper deserves privileged behavior. Applications or specialized strongly typed-ID libraries can configure EF explicitly without STM maintaining hidden shape inference.

### Keep `StrongIdentifier` as a reserved literal kind

Rejected.

There is no positive typed-literal normalization contract that uses it after Strong Scalar removal. A Logical Type over a scalar already has the correct ordinary literal kind.

### Add a canonical nominal scalar type

Rejected.

That recreates a second scalar type hierarchy and risks reintroducing representation/behavior obligations across targets.

### Create a Logical Type registry immediately

Deferred.

A registry would require definitions for intrinsic constraints, provenance, cross-model identity, merging, external authoring, and conflict resolution. M0077 needs only explicit property-level semantic identity.

### Allow the same Logical Type over different scalar representations

Rejected for the initial contract.

Within one model, a semantic identity with contradictory underlying `TypeRef`s is ambiguous. Cross-model federation is intentionally undefined.

### Infer Logical Type from keys or property names

Rejected.

Logical Type is semantic meaning and must be explicit. Key semantics and naming conventions do not establish nominal semantic identity.
