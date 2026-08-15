# Real Application Fixtures Are Required for EF Compatibility

## Status

Accepted and current.

## Context

EF compatibility defects can cross several boundaries at once: CLR/Roslyn extraction, generated semantic metadata, generated EF source, EF conventions/model finalization, provider metadata, persistence behavior, and analyzer/NuGet packaging.

Hand-built `TypeSchemaModel` tests can prove provider-neutral derivation logic but cannot prove behavior that depends on those real boundaries.

## Decision

EF compatibility work must use tests at the boundary where the behavior actually exists.

The permanent validation strategy includes:

```text
semantic / relational unit tests
  + real CLR extraction/generation tests
  + EF generator source/compilation tests
  + real DbContext/provider finalization tests
  + persistence round-trip tests where storage behavior matters
  + packed NuGet analyzer smoke for generator packaging
```

Real-application-shaped fixtures must be anonymized and repository-owned rather than copied from private/business source models.

## Consequences

- A manually constructed semantic model cannot close a bug caused by Roslyn extraction, generated source, EF convention finalization, provider metadata, or packaging.
- Provider regressions require provider-backed tests.
- Generator packaging must be tested from the packed NuGet artifact when package layout/discovery is part of the contract.
- Representative fixtures may be larger than ordinary unit examples, but they should remain deterministic and focused on the regression boundary.
