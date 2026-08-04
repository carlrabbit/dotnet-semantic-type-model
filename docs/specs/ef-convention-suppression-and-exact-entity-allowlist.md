# EF Convention Suppression and Exact Entity Allowlist

## Status

Authoritative for M0056 and `2.5.1`.

## Invariant

After applying an `EfRelationalModel`:

```text
final EF entity CLR types
==
projected semantic Entity CLR types
```

Semantic `ValueKind` types never become EF entities, including keyless entities.

## Required Order

```text
suppress/remove convention-discovered non-entities
apply semantic entities and properties
suppress/remove newly discovered non-entities
audit exact entity set
report residual violations
```

Do not report convention-discovery failures before attempting deterministic correction.

## ValueKind Rule

A semantic `ValueKind` may appear as:

```text
JSON-converted object property
JSON-converted collection property
nested JSON value
```

It may not appear as:

```text
EF entity
keyless entity
table
DbSet
navigation
owned EF entity
```

## Diagnostic Timing

`EF_UNEXPECTED_CONVENTION_ENTITY` is emitted only after final correction and exact-set audit.

## Application Responsibility

Convention suppression is handled by `SemanticTypeModel.EFCore`.

Consumers do not configure or manually ignore semantic ValueKinds.
