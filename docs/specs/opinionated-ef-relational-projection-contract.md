# Opinionated EF Relational Projection Contract

## Status

Authoritative for M0055 and the `2.5.0` EF package reset.

## Core Mapping

```text
Entity -> table
Semantic entity inheritance -> TPT
Scalar -> column
Enum -> string column
Strong identifier -> underlying scalar column
Owned ValueKind object -> JSON object column
Owned ValueKind collection -> JSON array column
ExtensionData -> JSON object column
Entity link -> identifier only
Non-semantic infrastructure -> excluded
Unsupported combination -> diagnostic
```

## Compatibility Policy

`2.5.0` does not preserve the `2.4.x` EF API surface.

```text
No Obsolete attributes
No compatibility aliases
No legacy modes
No forwarding APIs
No compatibility branches
Delete superseded code
```

## Convention Policy

EF conventions are not semantic authority.

Only explicitly projected semantic entities may remain EF entity types.

## Ownership Policy

Ownership applies only to `ValueKind`.

```text
owned object -> JSON object
owned collection -> JSON array
owned entity -> error
```

Do not use `OwnsOne` or `OwnsMany`.

## Inheritance Policy

Use TPT only.

No TPH/TPC options.

## Link Policy

Semantic entity links are stored through identifiers.

Do not infer relational navigations or foreign keys.

## Extension Data

Persist as JSON object.

Do not inspect dictionary internals.

## Validation Policy

Projection must produce diagnostics, not raw EF/LINQ exceptions.

## Entity Keys

Every root semantic entity requires an explicit semantic primary key. Missing keys produce `EF_ENTITY_KEY_REQUIRED`.
