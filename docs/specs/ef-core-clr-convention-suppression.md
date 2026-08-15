# EF Core CLR Convention Suppression
> **Superseded for static CLR application in 3.0.0 by `ef-core-generated-configuration-contract.md` and generated ordinary EF configuration.**


## Status

Authoritative behavioral specification for M0051.

## Purpose

Define how SemanticTypeModel EF integration behaves when consumer applications use CLR-backed EF Core conventions, such as `DbSet<TEntity>` or `modelBuilder.Entity<TEntity>()`, together with SemanticTypeModel annotations.

## Core Problem

EF Core conventions do not understand SemanticTypeModel annotations.

A semantic member such as:

```csharp
[SemanticExtensionData]
public Dictionary<string, JsonElement>? ExtensionData { get; init; }
```

is semantic-only for EF projection, but EF conventions may see it as a property, navigation, or relationship candidate unless it is explicitly suppressed.

## Integration Modes

### STM-Owned Shared-Type Projection

STM owns EF shape.

```text
TypeSchemaModel -> EfModelDefinition -> shared-type EF metadata
```

The consumer should not simultaneously expose the same CLR semantic types as root `DbSet<T>` entities unless closed CLR application is configured.

### CLR-Backed Convention Augmentation

EF discovers CLR types first. STM augments the EF model.

```text
DbSet<T> / modelBuilder.Entity<T>
  -> EF convention model
  -> STM closed semantic application
```

In this mode, STM must suppress semantic-only members from EF conventions and apply semantic projection behavior where source CLR metadata is available.

## Semantic-Only Member Suppression

Required initially:

```text
SemanticExtensionData
```

Suppression must apply to:

- directly declared properties;
- inherited properties;
- properties declared on abstract non-semantic base classes;
- semantic entities;
- semantic value objects reachable from entities;
- owned/value-object CLR types participating in EF metadata.

The effective EF result must be:

```text
not an EF scalar property
not an EF navigation
not an EF relationship
not an EF owned member
```

## ValueObject Boundary

A type marked:

```csharp
[SemanticType(SemanticTypeRole.ValueObject)]
```

is not a root EF entity in STM projection.

In CLR-backed convention mode, `DbSet<TValueObject>` is unsupported unless a future explicit policy allows it.

Required behavior:

```text
diagnose unsupported DbSet<ValueObject> when detectable
or document precise consumer responsibility when detection is not possible
```

Reachability from an entity property must not make a semantic value object an independent root entity.

## Non-Semantic Base Classes

A non-semantic base class may contribute inherited semantic members to a derived semantic type.

Example:

```text
Money : ExtensibleObject
Money has SemanticType(ValueObject)
ExtensibleObject has no SemanticType
ExtensibleObject.ExtensionData has SemanticExtensionData
```

Required behavior:

```text
Money canonical model includes inherited ExtensionData.
ExtensibleObject is not projected as a root semantic or EF entity merely because it declares the member.
ExtensionData is suppressed from EF mapping.
```

## Diagnostics

Required diagnostic categories:

```text
DbSet<ValueObject> unsupported
semantic-only member suppression unavailable because CLR metadata is missing
conflicting suppression/configuration for the same member
closed CLR application not enabled while CLR convention types are detected, if detectable
```

## Documentation Requirements

Documentation must state:

- EF conventions do not understand STM annotations by themselves.
- Shared-type projection and closed CLR application are distinct.
- `[NotMapped]` or manual `Ignore` is a workaround when CLR-backed suppression is unavailable or disabled.
- `DbSet<T>` is intended for semantic entity / aggregate-root roles, not value-object roles.

## Non-Goals

- Complete provider-specific EF mapping.
- Automatic migration generation.
- General dictionary persistence.
- Extension-data storage in EF.
- Supporting value objects as independent root entities.

## M0052 closed-model correction

`EfCoreSemanticModel` is the authority for CLR application. Closed application suppresses convention-discovered members not represented or explicitly permitted by its lineage contract. `ApplySemanticTypeModel` and `ApplyEfCoreSemanticModel` use the same closed engine; shared-type application is explicit and secondary. The legacy augmentation name is not an authority mode.
