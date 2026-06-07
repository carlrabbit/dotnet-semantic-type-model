# M0030: EF Core Domain Semantic Model and ModelBuilder Projection

## Status

Implemented for 2.0.0.

## Completed Outcomes

- EF Core integration is bounded to `EfCoreSemanticModel` derivation and provider-neutral `ModelBuilder` configuration.
- The durable EF Core boundary decision is recorded in `docs/decisions/ef-core-integration-stops-at-modelbuilder-configuration.md`.
- `docs/specs/type-model-ef-core-projection.md` is the authoritative behavior spec for entities, properties, keys, indexes, conversions, explicit relationships, explicit inheritance, diagnostics, inspection, and `ModelBuilder` application.
- Public EF Core docs and package README source describe supported 2.0.0 behavior without implying database creation, migration generation, provider-specific behavior, DbContext discovery/generation, runtime database validation, or global query filters.

## Documentation Synchronization

Direct documentation impact for this milestone has been synchronized into the relevant index, public guide, package README source, and release-note files for 2.0.0. Any future behavior changes must update the authoritative specs first and then synchronize the public documentation surfaces listed in `docs/PUBLIC-DOCS.md`.
