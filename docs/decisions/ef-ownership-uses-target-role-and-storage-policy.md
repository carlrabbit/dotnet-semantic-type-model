# Decision: EF Ownership Uses Target Role and Storage Policy

## Status

Accepted for M0050.

## Context

The EF Core projection currently treats `[SemanticOwned]` on a single object as a hard-coded flattening signal. That is too blunt.

Ownership describes lifecycle containment. It does not decide whether the value is stored as flattened columns, an EF owned navigation, a JSON column, a separate table, or omitted with diagnostics.

The target type role also matters. Owning a `ValueObject` is different from owning an `Object` or `Entity`.

## Decision

EF Core projection will classify owned members using:

```text
property ownership annotation
target type semantic role
target shape
EF storage policy
```

Owned value objects use value-object storage policy.

Owned object-role targets require explicit owned-object policy or emit diagnostics.

Owned entity-role targets require explicit aggregate-owned entity policy or emit diagnostics.

Owned collections remain explicit-policy-required.

`[SemanticOwned]` no longer means “flatten this object.”

## Consequences

- Existing consumers relying on implicit flattening for `[SemanticOwned]` value objects must use `ValueObjectProjectionMode.Flatten`.
- Consumers can serialize owned value objects as one JSON string column by selecting `ValueObjectProjectionMode.SerializeJson`.
- Object-role owned members are safer because the projection will not silently flatten rich object graphs.
- EF domain metadata must not claim true `OwnsOne` unless `ModelBuilder` applies it.

## Alternatives Rejected

### Keep hard-coded flattening

Rejected because it conflates ownership with storage.

### Treat every owned object as EF `OwnsOne`

Rejected unless true `ModelBuilder` ownership support is implemented end to end.

### Use value-object policy for all owned target roles

Rejected because object-role and entity-role targets are semantically different from value objects.

### Implement owned collections immediately

Rejected for this milestone because collection storage requires a separate explicit policy surface.
