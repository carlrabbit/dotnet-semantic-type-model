# Decision: Shared Order Fulfillment Sample Domain

## Status

Accepted for M0046.

## Context

Independent miniature sample models are easy to read but do not show reuse of one solution-wide semantic model, obscure projection overlap, fail to demonstrate explicit Configuration selection, and allow common compatibility gaps to escape sample validation. The released EF Core nullable-value-type defect was not detected because the EF sample contained only required strings.

## Decision

All code-first public samples use one shared annotated Order Fulfillment domain under:

```text
samples/OrderFulfillment.Domain/
```

Projection-specific executables reference the shared domain, consume `SemanticTypeModel.*` through package references, create the same generated model, invoke only their relevant target, and assert representative output.

The JSON Schema import/roundtrip sample may remain independent because it demonstrates a distinct workflow.

## Required Overlap

```text
Customer:
  EF Core, JSON Schema/editing, System.Text.Json, Power BI

Order and OrderLine:
  EF Core, JSON Schema, Power BI, event/serialization where appropriate

Configuration types:
  present in the complete model
  explicitly selected by the Configuration consumer
  not automatically registered elsewhere
```

## Composition over Artificial Inheritance

Use shared domain types and composition. Reject a universal base class introduced only to share identifiers or audit fields because it creates accidental inheritance semantics. Inheritance is allowed only when domain-meaningful and intentionally tested.

## Samples and Tests

Samples are executable public documentation and representative compatibility canaries. Tests remain responsible for exhaustive matrices. A focused `ProjectionProbe` may exist, but sample narratives should primarily use natural business properties.

## Package Boundary

The shared domain is not a shipped package. Projection samples may project-reference it, but no sample may reference `src/*`; all SemanticTypeModel dependencies remain package references.

## Consequences

- Sample models are no longer duplicated.
- Shared-domain changes expose cross-projection inconsistencies earlier.
- Each sample must clearly explain the subset it consumes.
- Configuration samples demonstrate explicit per-type registration.
- Samples become stronger canaries while tests retain exhaustive coverage.
