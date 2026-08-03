# Decision: EF Source Lineage Is Diagnostic-First and Application-Policy-Aware

## Status

Accepted for M0053.

## Context

The 2.4.4 implementation introduced closed EF semantic model application, but `EfCoreSourceLineage.Create(...)` can still throw raw runtime exceptions when creating owned mappings. It also constructs lineage without direct access to the derivation application mode.

Additionally, `ApplicationMode` can be set when applying to `ModelBuilder`, but not when deriving an `EfCoreSemanticModel` through `DeriveEfCoreModel(...)`.

That leaves manual derivation weaker than the convenience application path.

## Decision

EF source-lineage construction must be diagnostic-first.

`DeriveEfCoreModel(...)` must expose application mode/policy and store it in the derived `EfCoreSemanticModel`.

`ApplySemanticTypeModel(...)` must converge with manual derivation and application.

Owned target resolution must be guarded and shape-aware.

## Consequences

- Model-authoring issues surface as STM diagnostics, not LINQ exceptions.
- Closed CLR application can classify lineage problems as blocking diagnostics.
- Shared-type application can avoid unnecessary CLR-lineage requirements.
- Manual `DeriveEfCoreModel(...)` becomes a first-class path.
- Public docs must describe application mode on derivation, not only on ModelBuilder application.

## Rejected Alternatives

### Keep `.Single(...)` and rely on upstream validation

Rejected because the released package surfaced raw runtime exceptions and diagnostics were missing.

### Keep ApplicationMode only on ModelBuilder options

Rejected because `EfCoreSemanticModel` is the EF application contract and must carry its intended application policy.

### Treat all owned annotations as owned single-object lineage

Rejected because owned collections, dictionaries, arrays, unions, and unsupported shapes require distinct diagnostics or policies.
