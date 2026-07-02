# Type Model EF Core Projection Specification

## Status

Authoritative behavioral specification for the EF Core domain semantic model and provider-neutral `ModelBuilder` projection.

## Purpose

Define deterministic derivation of code-generated canonical semantic models into an EF Core domain semantic model and provider-neutral EF Core `ModelBuilder` configuration.

This specification is authoritative for:

- EF Core domain semantic model behavior;
- EF Core derivation pipeline behavior;
- EF Core metadata precedence;
- entity/property/key/index/conversion/relationship/inheritance mapping;
- provider-neutral `ModelBuilder` application;
- unsupported EF Core scope boundaries;
- EF Core projection diagnostics.

## Product Role

`SemanticTypeModel.EFCore` projects code-generated canonical semantic models into EF Core metadata.

The package flow is:

```text
Code-generated canonical Semantic Type Model
  -> EF Core derivation transformations
  -> EfCoreSemanticModel
  -> EF Core ModelBuilder configuration
```

The package does not own database lifecycle, migrations, providers, DbContext discovery/generation, runtime database validation, or global query filters.

## Dependency Boundary

- Projection package: `SemanticTypeModel.EFCore`.
- Core abstraction contracts remain independent from EF Core-specific dependencies.
- The package exposes a domain derivation API and a provider-neutral `ModelBuilder` application API.
- Baseline usage must not require a database provider, database server, connection string, migration, or live connection.

## Domain Semantic Model Contract

EF Core behavior is derived through an EF Core domain semantic model before `ModelBuilder` configuration is applied.

The domain semantic model must represent:

```text
model metadata
entity types
table and schema metadata
properties
column metadata
primary keys
alternate keys
indexes
requiredness/nullability
max length
precision/scale
type conversion metadata
relationships
inheritance metadata
owned/value-object metadata
ignored members
diagnostics
```

The `ModelBuilder` projection must operate from the EF Core domain semantic model, not from scattered ad hoc annotation lookups.

## Derivation API

The package must expose a domain derivation API equivalent to:

```csharp
var result = model.DeriveEfCoreModel(options =>
{
    options.UseDefaultTransformations();
});
```

The result must follow the M0028 pattern:

```csharp
SemanticDerivationResult<EfCoreSemanticModel>
```

or provide equivalent behavior:

```text
domain model
diagnostics
transformation trace
```

Users must be able to configure EF Core derivation transformations in code.

## ModelBuilder API Contract

The package exposes an API equivalent to:

```csharp
modelBuilder.ApplyEfCoreSemanticModel(result.Model);
```

A convenience API may derive and apply in one call only if diagnostics and trace remain available.

The `ModelBuilder` entry point applies provider-neutral configuration for projected entities, properties, keys, indexes, relationships, owned/value-object mappings, conversions, and explicitly configured inheritance.

## Annotation Namespaces

Projection may consume:

```text
efCore.*
dotnet.*
schema.*
ui.*
```

Required handled or explicitly diagnosed metadata includes:

```text
efCore.entity
efCore.owned
efCore.tableName
efCore.schemaName
efCore.columnName
efCore.primaryKey
efCore.alternateKey
efCore.index
efCore.valueGenerated
efCore.conversion
efCore.providerClrType
efCore.valueConverterType
efCore.enumStorage
efCore.deleteBehavior
efCore.inheritanceStrategy
efCore.discriminator
dotnet.clrType
dotnet.nullability
schema.*
ui.*
```

## Precedence

### Entity/table names

1. `efCore.tableName`
2. configured naming transformation
3. canonical display name when enabled
4. canonical name

### Column/property names

1. `efCore.columnName`
2. configured naming transformation
3. property display name when enabled
4. property name

### Semantic precedence

- Canonical role, key, relationship, requiredness, and nullability semantics provide defaults.
- Explicit `efCore.*` metadata may refine EF-specific representation details.
- Invalid target-specific metadata is diagnosable and must not silently override canonical semantics.

## Entity Mapping

Object types project to EF Core entity definitions when one of these is true:

```text
canonical role is Entity
aggregate-root semantics indicate entity
explicit efCore.entity = true
options enable projection of unannotated objects
```

Value objects do not become root entities by default.

Missing primary keys on entity candidates are diagnosable unless keyless entity projection is explicitly enabled.

## Property and Column Mapping

Properties project when they are compatible with EF Core property mapping or explicitly configured.

Required support:

```text
CLR type metadata
requiredness
nullability
column name
max length
precision/scale
value generation metadata when explicit
ignored member metadata when explicit
conversion metadata when explicit
```

Unsupported property shapes must be diagnosed rather than silently ignored.

## Scalar and Enum Mapping

Provider-neutral baseline scalar mapping must be deterministic.

Lossy or ambiguous mappings are diagnosable.

Enum support:

- default behavior is configurable;
- string storage and numeric storage must be represented when enough metadata exists;
- invalid enum storage configuration is diagnosable.

## Type Conversion Mapping

Explicit type conversion mapping is supported.

Required support:

```text
explicit value converter type
explicit provider CLR type
explicit conversion mode when modeled
diagnostics when converter metadata is missing or ambiguous
```

Rules:

- the package must not invent custom converter logic;
- custom date/value-object classes require explicit converter or provider type metadata;
- invalid converter types emit diagnostics;
- provider-specific conversions are out of scope.

## Keys

Projection supports:

```text
primary keys
alternate keys
composite keys
surrogate keys when explicit
external identity markers when explicitly modeled
```

Rules:

- primary keys are derived from canonical key semantics by default;
- EF-specific key metadata can refine EF-specific behavior;
- multiple primary keys without composite-key semantics emit diagnostics;
- missing key properties emit diagnostics.

## Indexes

Projection supports provider-neutral indexes:

```text
single-column index
composite index
unique index
named index
deterministic property order
```

Out of scope:

```text
filtered indexes
included columns
full-text indexes
spatial indexes
provider-specific index methods
```

Invalid or unresolved index property references emit diagnostics.

## Relationships

Projection supports explicit simple relationships:

```text
one-to-one
one-to-many
many-to-one
explicit foreign-key property
explicit navigation metadata
required/optional relationship
explicit delete behavior where provider-neutral
```

Rules:

- both endpoint types must resolve to EF Core entities;
- endpoint properties must resolve when required;
- ambiguous navigation pairing emits diagnostics;
- unsupported many-to-many skip navigations emit diagnostics;
- shadow FK discovery is out of scope;
- complex relationship inference is out of scope.

## Owned and Value-Object Mapping

Explicit owned/value-object mapping is supported where EF Core can represent it provider-neutrally.

Supported:

```text
owned reference mapping
value-object property mapping when explicit
diagnostics for ambiguous ownership
diagnostics for unsupported owned collections
```

Out of scope:

```text
owned collections
complex owned graph inference
provider-specific owned mapping behavior
```

## Inheritance Mapping

Simple explicit inheritance mapping is supported.

Supported strategies:

```text
TPH
TPT
TPC
```

Rules:

- user must choose inheritance strategy through options or `efCore.*` metadata;
- arbitrary inheritance must not automatically imply a strategy;
- derived types must resolve to EF Core entity definitions;
- discriminator metadata is supported for TPH when explicitly configured;
- ambiguous or unsupported inheritance emits diagnostics.

Candidate configuration behavior:

```csharp
var result = model.DeriveEfCoreModel(options =>
{
    options.Projection = options.Projection with
    {
        DefaultInheritanceStrategy = EfCoreInheritanceStrategy.Tph
    };
});
```

Per-type inheritance strategy may also be selected with `efCore.inheritanceStrategy` metadata when a type needs to override the default projection option.

## ModelBuilder Application

Provider-neutral `ModelBuilder` application should apply where domain metadata exists:

```text
Entity(...)
ToTable(...)
HasKey(...)
HasAlternateKey(...)
HasIndex(...)
IsUnique(...)
Property(...)
HasColumnName(...)
IsRequired(...)
HasMaxLength(...)
HasPrecision(...)
HasConversion(...) when explicit converter or provider CLR type metadata exists
HasOne/WithMany/WithOne
OwnsOne
TPH/TPT/TPC mapping APIs supported by the referenced EF Core version
```

The implementation must use actual EF Core APIs supported by the referenced EF Core package version.

## Diagnostics

Projection emits structured diagnostics with:

```text
Stage = Projection
ProjectionTarget = EfCore
model path when available
transformation id when emitted during derivation
related model paths when applicable
```

Required diagnostic classes include:

```text
object not projected
missing primary key for entity candidate
unresolved property type
unresolved key property
unresolved relationship endpoint
unsupported many-to-many relationship
ambiguous relationship navigation
unsupported owned collection
ambiguous ownership
invalid inheritance strategy
missing inheritance strategy
unresolved derived entity
invalid discriminator metadata
invalid index property reference
duplicate index definition
invalid converter metadata
lossy scalar or enum mapping
invalid projection annotation type/value
duplicate projected names
provider-specific behavior requested
```

## Determinism

EF Core derivation and `ModelBuilder` application must be deterministic.

Required deterministic ordering:

```text
entities by canonical type identifier or configured entity name
properties by declaration/order metadata when available, then name
keys by configured/canonical order
indexes by configured/canonical order
relationships by model path
inheritance branches by canonical type identifier unless explicitly ordered
diagnostics by model path/code/order of discovery
```

## Inspection Integration

The EF Core domain model and derivation result must integrate with M0027/M0028 inspection.

Required behavior:

```csharp
derived.Diagnostics.ToDiagnosticText();
derived.Trace.ToTransformationText();
derived.Model.ToSemanticText();
```

or equivalent package-specific inspection methods.

Inspection output must be deterministic and suitable for tests.

## Unsupported Scope

The EF Core package does not define or own:

```text
database creation
migration generation
provider-specific SQL Server/PostgreSQL behavior
DbContext discovery
DbContext source generation
runtime database validation
global query filters
connection string handling
transaction handling
seed data management
repository/unit-of-work abstractions
database deployment
provider-specific fluent extensions
```

## Test Coverage Requirements

Short-running tests must cover:

```text
domain model derivation from generated model
entity mapping
table/schema mapping
property/column mapping
primary key mapping
alternate key mapping
single-column index mapping
composite index mapping
unique index mapping
requiredness/nullability mapping
max length mapping
precision/scale mapping
explicit value converter mapping
explicit provider type mapping
simple one-to-one relationship mapping
simple one-to-many relationship mapping
simple many-to-one relationship mapping
owned/value-object mapping
TPH inheritance mapping
TPT inheritance mapping
TPC inheritance mapping
unsupported mapping diagnostics
duplicate/collision diagnostics
deterministic ModelBuilder metadata
deterministic inspection output
no provider/live database requirement
```

## Non-Goals

M0030 does not define:

```text
database creation
migration generation
provider-specific SQL Server/PostgreSQL behavior
DbContext discovery
DbContext source generation
runtime database validation
global query filters
complex relationship inference
many-to-many skip navigations
shadow FK discovery
owned collections
automatic polymorphism inference from arbitrary inheritance
advanced provider-specific indexes
filtered indexes
included columns
full-text indexes
spatial indexes
complex value-converter generation
query filters
interceptors
compiled models
```

## Nullable value-type CLR metadata

When an EF Core property is nullable and the selected projected CLR type is a non-nullable value type, the EF Core projection must use `Nullable<T>` for `EfPropertyDefinition.ClrType`. Runtime `ModelBuilder` application must use that same CLR type so `EfPropertyDefinition.ClrType` and EF Core `IProperty.ClrType` agree with `IsNullable` metadata.
