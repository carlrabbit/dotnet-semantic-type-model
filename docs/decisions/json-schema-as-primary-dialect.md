# Decision: JSON Schema as the Primary Initial Dialect

## Status

Accepted

## Context

Milestone 2 needs one external schema dialect to prove the canonical semantic type model can support import, normalization, traversal, and export without making the dialect the runtime storage format.

## Decision

JSON Schema Draft 2020-12 is the primary initial adapter dialect for milestone 2.

## Rationale

- JSON Schema provides enough shape variety to exercise the canonical model.
- JSON Schema is a common interchange format for schema-driven systems.
- The milestone requires a roundtrip path that can demonstrate canonical-model-first architecture.
- Using a single initial dialect keeps the baseline implementation focused while preserving future projection-oriented expansion.

## Consequences

- Milestone 2 implements JSON Schema import and export before other dialect adapters.
- The canonical model must remain independent from JSON Schema document structure.
- Future adapters must integrate through the same source, transformation, and projection boundaries.

## Alternatives Considered

- OpenAPI-first adapter: rejected because the milestone does not require API semantics.
- CLR-type-first runtime model: rejected because the milestone requires a canonical schema model rather than runtime type emission.
