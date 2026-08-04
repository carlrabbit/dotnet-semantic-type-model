# Decision: Conditional Constraint Literals Are Typed

## Status

Accepted for M0058.

## Context

`SemanticRequiredWhenAttribute` stores comparison values as strings. This loses the distinction between strings and typed values such as enum members, booleans, numbers, null, dates, and identifiers.

The immediate defect was observed with enum-based `SemanticRequiredWhen` conditions. The general issue is broader than enums.

## Decision

The semantic model will introduce typed literal semantics for conditional constraints.

`SemanticRequiredWhen` remains source-compatible, but its string value is normalized against the resolved source property type.

## Consequences

- Enum condition values become enum-member literals.
- String condition values remain strings only for string source properties.
- Invalid condition literals produce diagnostics.
- JSON Schema can project conditional required semantics deterministically.
- EF Core keeps condition metadata out of entity discovery and storage mapping decisions.

## Rejected Alternatives

### Keep RequiredWhen values as strings

Rejected because it loses semantic identity and makes projections guess.

### Add only enum-specific behavior

Rejected because the same defect class applies to booleans, numbers, null, dates, and identifiers.

### Build a full expression language

Rejected for 2.6.0. This milestone covers simple typed literal conditions only.
