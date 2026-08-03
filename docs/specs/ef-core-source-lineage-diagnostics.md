# EF Core Source Lineage Diagnostics

## Status

Authoritative behavioral specification for M0053.

## Purpose

Define diagnostic-first EF source-lineage construction and application-policy propagation for EF Core derivation.

## Core Rules

Source-lineage construction is part of EF derivation and application. It must not throw raw LINQ or runtime exceptions for model-authoring errors.

`DeriveEfCoreModel(...)` must know the intended EF application policy so diagnostics can be classified correctly.

## Source-Lineage Construction

Lineage construction must return both:

```text
source type mappings
diagnostics
```

It must not rely on `.Single(...)`, `.First(...)`, or equivalent unchecked assumptions when resolving model-authored references.

## Owned Target Resolution

Owned target resolution must distinguish:

```text
owned object
owned collection
owned dictionary
owned array
owned union
owned scalar
owned enum
missing target
ambiguous target
unsupported target
```

Only a valid single object target may create a single-object `EfCoreOwnedMapping`.

Everything else produces a diagnostic.

## Application Policy in Derivation

`EfCoreDerivationOptions` must expose application mode/policy.

The derived `EfCoreSemanticModel` must carry the selected policy.

Required behavior:

```text
ClosedClrModel
  source CLR lineage required
  missing/invalid lineage is error or blocking diagnostic

SharedTypeModel
  CLR lineage may be optional
  model semantic contradictions remain diagnostics
```

## Path Convergence

`ApplySemanticTypeModel(...)` and manual derivation/application must converge:

```text
ApplySemanticTypeModel(model)
  == DeriveEfCoreModel(model) + ApplyEfCoreSemanticModel(derived.Model)
```

where source compatibility allows.

## Diagnostics

Required diagnostic codes:

```text
EFCORE_OWNED_TARGET_TYPE_NOT_FOUND
EFCORE_OWNED_TARGET_TYPE_AMBIGUOUS
EFCORE_OWNED_TARGET_SHAPE_UNSUPPORTED
EFCORE_OWNED_COLLECTION_LINEAGE_POLICY_REQUIRED
EFCORE_SOURCE_LINEAGE_CLR_TYPE_NOT_RESOLVED
EFCORE_SOURCE_LINEAGE_MEMBER_NOT_FOUND
EFCORE_SOURCE_LINEAGE_REQUIRED
EFCORE_SOURCE_LINEAGE_STORAGE_UNSUPPORTED
```

`EFCORE_SOURCE_LINEAGE_STORAGE_UNSUPPORTED` is a warning when an EF-projected member reaches closed CLR application with a CLR type that requires an explicit converter or storage policy. Closed application suppresses that member instead of allowing EF conventions to fail or silently invent storage.

## Error Surface

Consumers must see diagnostics in:

```text
SemanticDerivationResult<EfCoreSemanticModel>.Diagnostics
EfCoreModelBuilderProjectionResult.Diagnostics
```

Runtime exceptions are acceptable only for blocking application failures after diagnostics have been produced or when an API contract is violated. Exception messages must include stable diagnostic code and remediation.

## Non-Goals

- Provider-specific EF behavior.
- EF persistence for extension data.
- General dictionary persistence.
- Value objects as root EF entities.
- Broad public API redesign beyond application-policy derivation and lineage diagnostics.


## 2.4.6 regression qualification

Source lineage is restricted to EF projection/application scope. Compatibility changes must be exercised through unit projection/lineage tests, real CLR `DbContext` model construction, and SQLite in-memory provider tests; canonical types outside that scope do not produce CLR-lineage diagnostics.
