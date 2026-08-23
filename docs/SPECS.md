# Specifications

## Purpose

Specifications define **exact current required behavior**. They are implementation authority, not implementation logs or historical release records.

Prefer extending an existing subsystem specification over creating a feature-, bug-, release-, or milestone-specific spec.

## Current Reading Map

Read only the subsystem contracts relevant to the task.

### Canonical model and semantic vocabulary

- [Core semantic vocabulary](specs/core-semantic-vocabulary.md)
- [Current canonical model surface](specs/current-canonical-model-surface.md)
- [Strong Scalar semantics](specs/strong-scalar-semantics.md)
- [Conditional constraint semantics](specs/core-conditional-constraint-semantics.md)
- [Audience-specific description semantics](specs/audience-specific-description-semantics.md)
- [Diagnostics](specs/diagnostics.md)

### Code-first .NET acquisition and generation

- [Code-first semantic model architecture](specs/code-first-semantic-model-architecture.md)
- [Type-model .NET extraction](specs/type-model-dotnet-extraction.md)
- [Type-model .NET attributes](specs/type-model-dotnet-attributes.md)
- [Type-model .NET conventions](specs/type-model-dotnet-conventions.md)
- [Compile-time generator](specs/type-model-compile-time-generator.md)
- [Query and inspection](specs/type-model-query-and-inspection.md)
- [Transformation and domain derivation](specs/type-model-transformation-and-domain-derivation.md)
- [Runtime API](specs/type-model-runtime-api.md)
- [Dependency injection](specs/type-model-di-integration.md)

### Projection and integration contracts

- [JSON representation fidelity](specs/json-representation-fidelity.md) — cross-target contract between supported System.Text.Json wire output and JSON Schema representation/validation.
- [EF Core](specs/ef-core.md) — current EF relational inspection, manifest/generation, mapping, composition, and validation contract.
- [JSON Schema domain model and export](specs/json-schema-domain-model-and-export.md)
- [System.Text.Json domain model and resolver projection](specs/system-text-json-domain-model-and-resolver-projection.md)
- [System.Text.Json contract integration](specs/system-text-json-contract-integration.md)
- [Power BI/TOM projection](specs/type-model-powerbi-tom-projection.md)
- [Projection capability and compatibility contract](specs/type-model-projection-capabilities.md)

## Supporting/Overlap Files

Some non-EF specification families predate the living-subsystem-contract policy and still contain overlapping documents. They remain in the working tree until their detailed requirements are audited and safely consolidated.

Do not treat the existence of a second overlapping file as permission to create a third. Prefer the primary reading-map contract above and reconcile supporting material when changing that subsystem.

The documentation reset intentionally consolidates EF Core first because the supersession boundary is explicit and the old runtime application architecture is known to be retired.

## Specification Lifecycle

When replacing a specification:

1. move all still-current behavior into the replacement contract;
2. preserve historical architectural significance in `HISTORY.md` only when useful;
3. move migration/version detail to compatibility/release documentation when consumer-relevant;
4. delete the superseded spec from the working tree.

Git history is the detailed historical record.
