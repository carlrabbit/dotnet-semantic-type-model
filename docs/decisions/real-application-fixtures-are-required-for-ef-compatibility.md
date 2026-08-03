# Decision: Real Application Fixtures Are Required for EF Compatibility

## Status

Accepted for M0054.

## Context

The EF package has repeatedly passed isolated unit tests while failing realistic application-shaped models involving records, inherited semantic members, extension data, optional owned value objects, generic marker interfaces, record infrastructure, dictionaries, and real EF Core model building.

Synthetic tests alone are insufficient for EF compatibility confidence.

## Decision

The repository will add anonymized real-application regression fixtures and require three EF validation layers:

```text
unit projection/lineage tests
real ModelBuilder tests with CLR DbContext
SQLite in-memory integration tests
```

EF source lineage must be projection-scope driven. It must not treat all canonical object definitions as EF source-lineage candidates.

## Consequences

- EF compatibility tests become more representative.
- Private/business source names are not copied into public repository assets.
- Some fixtures may be larger than ordinary unit-test examples.
- SQLite becomes part of the EF integration validation surface.
- Future EF changes must preserve these real application regressions.

## Rejected Alternatives

### Keep only isolated unit tests

Rejected because they missed repeated production-shaped failures.

### Copy real source models verbatim

Rejected because fixture code must be anonymized and aligned with repository sample terminology.

### Treat every canonical object definition as EF lineage

Rejected because it pulls framework/interface/compiler-adjacent types into EF diagnostics and model application.
