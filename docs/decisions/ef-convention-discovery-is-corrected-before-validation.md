# Decision: EF Convention Discovery Is Corrected Before Validation

## Status

Accepted for M0056.

## Context

The `2.5.0` implementation audits convention-discovered EF entities before removing non-semantic types. It therefore emits a blocking error and returns before its cleanup code executes.

This causes semantic ValueKinds used as JSON properties to appear as keyless or unexpected EF entities.

## Decision

The EF application pipeline will correct convention discovery before validating it.

Required order:

```text
cleanup
apply
cleanup
final audit
```

The final EF entity set must exactly match the semantic Entity allowlist.

Semantic ValueKinds are always non-entities.

## Consequences

- Existing keyless ValueKind failures are corrected.
- `EF_UNEXPECTED_CONVENTION_ENTITY` becomes a residual invariant diagnostic.
- ModelBuilder and SQLite tests must inspect the complete final entity inventory.
- No consumer-facing configuration option is added.

## Rejected Alternatives

### Let consumers call ModelBuilder.Ignore manually

Rejected because the semantic package owns the convention boundary.

### Permit ValueKinds as keyless entities

Rejected because it contradicts the 2.5.0 relational contract.

### Keep the current early audit

Rejected because it prevents the intended cleanup from running.
