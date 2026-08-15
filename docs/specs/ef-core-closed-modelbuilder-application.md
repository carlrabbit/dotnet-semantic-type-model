# EF Core Closed ModelBuilder Application
> **Superseded for static CLR application in 3.0.0 by `ef-core-generated-configuration-contract.md` and generated ordinary EF configuration.**


## Status

Authoritative behavioral specification for M0052.

## Purpose

Define the EF Core application architecture for SemanticTypeModel.

SemanticTypeModel owns a closed semantic domain. EF Core is a projection target. EF Core conventions must not decide or extend domain shape outside the derived `EfCoreSemanticModel`.

## Core Invariant

No EF Core convention-discovered member may affect the final EF model unless it is present in, or explicitly permitted by, `EfCoreSemanticModel`.

## Architecture

```text
TypeSchemaModel
  -> EfCoreSemanticModel
  -> ModelBuilder
```

`EfCoreSemanticModel` is the complete EF application contract.

`ApplySemanticTypeModel(...)` is a convenience wrapper:

```text
TypeSchemaModel
  -> derive EfCoreSemanticModel
  -> ApplyEfCoreSemanticModel(...)
```

`ApplyEfCoreSemanticModel(...)` is the lower-level application API and must be able to perform closed application when required source lineage is present.

## EfCoreSemanticModel Source Lineage

The EF-specific semantic model must preserve enough source lineage to apply itself without asking EF conventions to rediscover semantic shape.

Required lineage includes:

```text
source semantic type id
source CLR type name
source property id
source member name
source declaring CLR type name
semantic role
root/value-object/owned classification
storage kind
semantic-only member suppression
owned mapping source/target identity
```

## Application Modes

Preferred modes:

```text
ClosedClrModel
SharedTypeModel
```

### ClosedClrModel

STM owns final EF model shape.

EF conventions may be used only as EF infrastructure and must be constrained.

Required behavior:

```text
configure only EF semantic model members
suppress semantic-only members
diagnose or reject unexpected convention-created model shape
preserve value-object boundaries
```

### SharedTypeModel

STM applies provider-neutral shared-type EF metadata.

This mode is explicit and secondary.

It may not be the implicit fallback for CLR-backed applications.

## Legacy Convention Augmentation

The concept of CLR convention augmentation is deprecated as an authority model.

If retained for binary/source compatibility, it must route through closed model application where possible or be documented as legacy compatibility behavior.

## Semantic-Only Suppression

`EfCoreSemanticModel` must explicitly represent members suppressed from EF mapping.

Initial required semantic-only member:

```text
ExtensionData
```

Suppressed members must not become scalar properties, navigations, relationships, owned navigations, or entity types.

## ValueObject Boundary

Semantic `ValueObject` types are not root EF entities, are not promoted to root entities by reachability, are not valid as DbSet roots unless a future explicit policy allows it, and are mapped only through owner storage policy.

## Missing Lineage

Closed CLR application without required lineage must fail explicitly.

Diagnostic code:

```text
EFCORE_SOURCE_LINEAGE_REQUIRED
```

The message must direct users to derive `EfCoreSemanticModel` with source lineage or call `ApplySemanticTypeModel(...)`.

## Documentation Requirements

Docs must state:

- STM owns the EF semantic model.
- EF conventions are not domain authority.
- `ApplySemanticTypeModel(...)` is a wrapper over `EfCoreSemanticModel` derivation and application.
- `ApplyEfCoreSemanticModel(...)` is the lower-level closed application path.
- Shared-type projection is explicit.
- Broad CLR convention augmentation is not the primary model.

## Non-Goals

- Provider-specific JSON storage.
- DbContext generation.
- Migrations.
- Arbitrary EF convention mapping.
- Extension-data persistence in EF.
- Value objects as root EF entities.

## 2.4.5 derivation policy

`EfCoreDerivationOptions.ApplicationMode` selects and records the application policy before lineage construction. Closed CLR lineage failures are blocking diagnostics. Shared-type derivation permits absent CLR lineage, but does not downgrade contradictory semantic ownership shapes.


## 2.4.6 regression qualification

Source lineage is restricted to EF projection/application scope. Compatibility changes must be exercised through unit projection/lineage tests, real CLR `DbContext` model construction, and SQLite in-memory provider tests; canonical types outside that scope do not produce CLR-lineage diagnostics.
