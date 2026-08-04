# Decision: EF Requires a Surgical Model-Shape Test Matrix

## Status

Accepted for M0057.

## Context

Real-life fixtures found repeated EF projection failures, but they are too large to isolate specific rules. Recent failures involve inherited CLR properties and EF application attempting to ignore or configure a property from the wrong declaring type.

## Decision

The repository will add a permanent EF model-shape fixture matrix. Each fixture isolates one semantic/CLR shape and verifies both derivation and final EF `IModel` application.

The EF relational model will carry declaration and storage metadata so inherited members are applied to the correct EF entity builder.

## Consequences

- EF tests become more numerous but more diagnostic.
- Real-life fixtures remain acceptance tests, not the only coverage.
- Property placement becomes explicit model data.
- Hidden or ambiguous member declarations produce deterministic diagnostics.

## Rejected Alternatives

### Add only a regression test for the current model

Rejected because it would not cover the underlying shape class.

### Keep relying on CLR reflection loops

Rejected because reflection exposure and EF storage placement differ under inheritance.

### Let EF decide inherited property placement

Rejected because EF conventions are not semantic authority.
