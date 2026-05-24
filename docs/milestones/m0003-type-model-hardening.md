# M0003 - Type Model Hardening

## Purpose

Harden and finalize the canonical semantic type model so projection-heavy work can build on stable contracts.

## Scope

This milestone defines the hardened canonical contracts and supporting specs for:

- canonical model layers (shape, member, constraint, semantic, projection annotation);
- projection-aware but projection-independent contracts;
- JSON Schema Draft 2020-12 mapping baseline;
- projection capability expectations and diagnostics strategy.

## Deliverables

- `docs/specs/type-model-core.md`
- `docs/specs/type-model-annotations.md`
- `docs/specs/type-model-json-schema-mapping.md`
- `docs/specs/type-model-projection-capabilities.md`
- compile-only hardening contracts under `SemanticTypeModel.Abstractions.Hardening.*`
- short-running tests for required example coverage and contract-level invariants.

## Invariants

- The canonical model remains projection-independent.
- Projection-specific metadata is carried through namespaced annotations.
- Requiredness, nullability, and cardinality remain distinct.
- Keys and relationships are first-class semantic model concepts.
- Unsupported/lossy projection cases produce diagnostics.

## Non-goals

- full EF Core model generation;
- full Power BI/TOM model generation;
- full DAX validation;
- runtime CLR type generation;
- production-ready JSON editor adapter.

## Exit Criteria

- Required specs exist and are indexed.
- Hardened contracts compile and are publicly available.
- Short-running tests prove representation of the four required examples.
- Diagnostic strategy is defined for unsupported/lossy projections.
