# Type Model EF Core Projection Specification

## Purpose

Define deterministic projection of hardened canonical `TypeSchemaModel` contracts into an EF Core-oriented intermediate metadata model and `ModelBuilder` configuration surface.

## Authority

This specification is authoritative for:

- EF Core projection scope in M0008 and M0015;
- projection naming and annotation precedence;
- entity/property/key/relationship/value-object/enum mapping behavior;
- projection options and unsupported-shape behavior;
- required diagnostic expectations for this projection target.

## Dependency Boundary

- Projection package: `SemanticTypeModel.EFCore`.
- Core abstraction contracts remain independent from EF Core-specific dependencies.
- Projection supports two entry points:
  - `EfCoreModelProjection` for non-EF runtime consumers and tests.
  - `ModelBuilder.ApplySemanticTypeModel(...)` for EF Core model configuration.
- Baseline usage does not require a database provider, database server, or live connection.
- The package does not create databases or execute migrations.

## ModelBuilder API Contract

`SemanticTypeModel.EFCore` exposes:

- extension method `ModelBuilder.ApplySemanticTypeModel(...)`;
- canonical `TypeSchemaModel` input;
- optional configuration callback;
- result object with projected model metadata and diagnostics.

The `ModelBuilder` entry point applies provider-neutral baseline configuration for projected entities, scalar properties, keys, and unique indexes while keeping unsupported shape behavior diagnosable.

## Domain Semantic Model Contract

EF Core behavior is derived through an EF Core domain semantic model before `ModelBuilder` configuration is applied.

The projection result is represented by:

- `EfModelDefinition`
- `EfEntityTypeDefinition`
- `EfPropertyDefinition`
- `EfKeyDefinition`
- `EfRelationshipDefinition`

Minimum domain semantic model semantics:

- entity types with provider-neutral table/schema metadata;
- property CLR type, requiredness, nullability, max length, precision, conversion, generation, and carried annotations;
- primary and non-primary keys;
- relationship endpoints, cardinality, and delete behavior;
- accumulated structured projection diagnostics.

## Annotation Namespaces

Projection consumes:

- `efCore.*`
- `dotnet.*`
- `schema.*`
- `ui.*` (shared display metadata only when naming options enable it)

Required handled or explicitly diagnosed keys:

- `efCore.entity`
- `efCore.owned`
- `efCore.tableName`
- `efCore.schemaName`
- `efCore.columnName`
- `efCore.valueGenerated`
- `efCore.conversion`
- `efCore.enumStorage`
- `efCore.deleteBehavior`
- `dotnet.clrType`
- `dotnet.nullability`

`efCore.index` remains preserved as annotation metadata in M0008 and is not applied as a distinct intermediate contract element.

## Precedence

### Entity/table names

1. `efCore.tableName`
2. canonical `DisplayName` when `PreferDisplayNamesForTableAndColumnNames` is enabled
3. canonical `Name`

### Column/property names

1. `efCore.columnName`
2. property `DisplayName` when `PreferDisplayNamesForTableAndColumnNames` is enabled
3. property `Name`

### Semantic precedence

- canonical role, key, relationship, requiredness, and nullability semantics remain authoritative by default;
- explicit `efCore.*` and `dotnet.*` annotations may refine target-specific representation details;
- invalid target-specific annotation values are diagnosable and do not silently override canonical semantics.

## Object-to-Entity Mapping

Object types project to entity definitions when one of these is true:

- role is `Entity`;
- `EntitySemantics.IsAggregateRoot` is true;
- explicit `efCore.entity = true` is present;
- options enable projection of unannotated objects.

Value objects do not become root entities by default.

## Property Mapping

### Scalar mapping baseline

- Boolean -> `bool`
- String -> `string`
- Integer -> `long`
- Number -> `double` or `decimal` when precision metadata exists
- Decimal -> `decimal`
- Date -> `DateOnly`
- Time -> `TimeOnly`
- DateTime -> `DateTime`
- DateTimeOffset -> `DateTimeOffset`
- Duration -> `TimeSpan`
- Guid -> `Guid`
- Binary -> `byte[]`
- Json -> `string` with diagnostic in the baseline prototype

Lossy mappings are diagnosable.

### Requiredness and nullability

The prototype preserves:

- property presence requiredness (`EfPropertyDefinition.IsRequired`);
- value nullability (`EfPropertyDefinition.IsNullable`);
- generated annotations (`schema.isRequired`, `schema.isOptional`, `schema.allowsNull`, `dotnet.nullability`).

When canonical schema presence/nullability semantics are not an exact claim about future runtime EF metadata, the projection emits diagnostics while preserving the distinction in the intermediate model and annotations.

### String and numeric constraints

- `maxLength` maps directly to `EfPropertyDefinition.MaxLength`;
- numeric precision maps directly when canonical precision metadata exists;
- pattern, minimum, maximum, and multipleOf remain annotation-preserved and diagnosable in M0008.

## Keys

Projection supports:

- primary keys;
- alternate and natural keys;
- surrogate keys;
- external identity markers.

Composite keys are preserved in the intermediate model.

Missing primary keys on entity candidates are diagnosable unless keyless entity projection is explicitly enabled.

## Relationships

Projection supports:

- one-to-one;
- one-to-many;
- many-to-one.

Many-to-many remains diagnosable and deferred in M0008.

Relationship projection requires:

- both endpoint types projected as entities;
- endpoint properties projected as properties.

Missing endpoint/entity/property resolution is diagnosable.

## Value Objects, Arrays, Dictionaries, and Unions

### Value objects

Options determine behavior:

- diagnose;
- owned/complex-like metadata;
- flatten deterministic nested names (`parent_child`);
- serialize as JSON/text.

### Arrays/dictionaries/unions/nested unsupported objects

Options determine behavior:

- diagnose;
- ignore with warning;
- serialize as JSON/text.

No silent shape loss is allowed.

## Enums

- default behavior uses string storage;
- numeric storage is configurable when canonical enum metadata supports numeric backing;
- invalid enum storage configuration is diagnosable.

## Projection Options

`EfCoreProjectionOptions` includes behavior controls for:

- unannotated object entity projection;
- keyless-entity allowance;
- value-object mode;
- unsupported-shape behavior;
- enum mode;
- alternate-key mode;
- display-name participation for table/column names;
- name collision behavior.

Behavior must be deterministic.

## Name Collision Handling

Duplicate projected names for entities, properties, keys, and relationships must be handled deterministically:

- diagnose-and-skip (default), or
- suffix-based renaming when configured.

## Diagnostics

Projection emits `SchemaDiagnostic` entries with:

- `Stage = Projection`
- `ProjectionTarget = EfCore`

Required diagnostic classes include:

- object not projected (missing role/annotation);
- missing primary key for entity candidate;
- unresolved property type, key property, or relationship endpoint;
- unsupported many-to-many relationships;
- value-object mode conflicts;
- unsupported arrays, dictionaries, unions, and nested objects;
- preserved-but-not-directly-applied constraint metadata;
- invalid projection annotation type/value;
- duplicate projected names;
- lossy scalar or enum mapping decisions.

## Test Coverage Requirements

Short-running tests must cover at least:

1. simple entity fixture;
2. alternate-key and relationship fixture;
3. value-object mode fixture;
4. enum storage fixture;
5. unsupported shape fixture;
6. name collision fixture;
7. code-generated optional-versus-nullable fixture.
