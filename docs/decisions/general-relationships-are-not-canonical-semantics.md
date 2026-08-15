# General Relationships Are Not Canonical Semantics

## Status

Accepted for M0062.

## Decision

Remove the current general relationship model from the canonical Semantic Type Model.

The canonical model must not expose a general-purpose relationship abstraction based on principal/dependent endpoints, foreign keys, cardinality, delete behavior, or equivalent relational concepts.

Do not replace the removed relationship model with a smaller compatibility abstraction in M0062.

## Rationale

The existing relationship model is too close to persistence-oriented modeling and is not backed by a sufficiently clear projection-neutral semantic contract.

A relationship concept that is genuinely semantic would need to answer questions such as identity, direction, lifecycle, ownership, navigation, multiplicity, and target interpretation without inheriting one projection's vocabulary. The current model does not establish that boundary strongly enough.

Keeping an under-specified relationship abstraction in the canonical model encourages projections to infer behavior that should remain target-owned.

## What Remains

Removing general relationships does not remove:

- object-valued properties;
- collections;
- references/type references needed to describe structural type shape;
- keys and identity semantics;
- ownership/lifecycle containment;
- aggregate-root semantics;
- target-specific relationship configuration.

EF Core applications continue to configure application/domain relationships through ordinary EF Core configuration and the existing generated-configuration extension points.

Power BI or other targets may define target-specific relationship behavior later if a target-owned use case requires it.

## Future Work

A future milestone may introduce a new relationship concept only if it starts from a clear projection-neutral semantic requirement.

Do not restore the removed `RelationshipDefinition`, `RelationshipCardinality`, `DeleteBehaviorSemantics`, `SemanticRelationshipAttribute`, relationship inference, or equivalent types merely for compatibility.

Git history preserves the removed design.
