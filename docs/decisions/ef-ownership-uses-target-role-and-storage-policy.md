# EF Ownership Uses Target Role and Storage Policy

## Status

Accepted and current.

## Context

Semantic ownership describes lifecycle containment. It is projection-neutral and does not itself mean EF `OwnsOne`, `OwnsMany`, flattening, a separate table, or any other relational representation.

Conflating semantic ownership with one EF mechanism makes the canonical model target-specific and creates incorrect behavior for structural values, entities, and collections.

## Decision

EF representation is selected from semantic ownership **together with target role/shape and the current EF storage policy**.

The current provider-neutral EF policy treats owned structural value shapes as JSON-converted properties/collections. Semantic Entities are configured as entities. Semantic ownership does not cause EF owned-entity graph inference.

Detailed mapping rules belong to `../specs/ef-core.md`.

## Consequences

- `[SemanticOwned]` is a lifecycle/containment statement, not an EF mapping command.
- EF does not infer `OwnsOne` or `OwnsMany` from semantic ownership.
- Structural value objects/collections can be represented without turning them into independently discovered EF entity graphs.
- Owned entity-role targets and unsupported ambiguous shapes require explicit supported semantics or diagnostics rather than target guessing.
- Future provider-specific storage capabilities may add target policy, but they must not redefine canonical ownership meaning.
