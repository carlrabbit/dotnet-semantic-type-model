# M0008 - EF Core DbModel Projection Prototype

## Purpose

Deliver the first EF Core-like DbModel projection prototype over the hardened canonical `TypeSchemaModel`, without introducing provider, database, or runtime `DbContext` dependencies.

## Delivered Runtime Surface

- New projection package: `SemanticTypeModel.EFCore`.
- EF-like intermediate metadata model:
  - `EfModelDefinition`
  - `EfEntityTypeDefinition`
  - `EfPropertyDefinition`
  - `EfKeyDefinition`
  - `EfRelationshipDefinition`
- Projection entrypoint:
  - `EfCoreModelProjection : ISchemaProjection<EfModelDefinition>`
- Deterministic projection options:
  - entity candidacy for unannotated objects
  - keyless-entity allowance
  - value-object modes (diagnose, owned, flatten, serialize)
  - unsupported-shape behavior (diagnose, ignore-with-warning, serialize)
  - enum storage modes
  - alternate-key representation modes
  - display-name participation for table/column naming
  - name-collision behavior (diagnose or suffix)

## Supported Baseline Mapping

- Object roles (`Entity`) and aggregate-root/entity annotations map objects to entity definitions.
- Scalar and enum properties map to EF-like property metadata with CLR type, requiredness, nullability, max length, precision, conversion, and generation metadata.
- Primary, alternate, natural, surrogate, and external keys are represented in EF-like key metadata.
- Canonical relationships map one-to-one, one-to-many, and many-to-one relationships into EF-like relationship metadata.
- Value objects can be diagnosed, flattened, serialized, or marked as owned-navigation metadata according to options.
- Schema-originated required/optional/nullability distinctions are preserved through property flags and generated annotations, with diagnostics when the prototype cannot claim exact EF runtime fidelity.

## Diagnostics Coverage

M0008 emits projection diagnostics for:

- object not projected due to missing role/annotation;
- entity candidates without projected primary keys when keyless projection is disabled;
- unresolved property type, key property, relationship endpoint type, and relationship endpoint property references;
- unsupported many-to-many relationships;
- value-object mode requirements and unsupported flattening cases;
- unsupported array, dictionary, union, and nested object shapes;
- preserved-but-not-directly-applied pattern and numeric constraint metadata;
- invalid `efCore.*` and `dotnet.*` annotation values;
- duplicate projected entity, property, key, and relationship names;
- enum storage misconfiguration and lossy JSON scalar mapping.

## Fixture/Test Coverage

Short-running unit tests cover:

1. simple entity projection (generated key, requiredness, nullability, max length, table/column annotations);
2. alternate-key and relationship projection;
3. value-object behavior across diagnose/flatten/serialize modes;
4. enum storage behavior and invalid configuration diagnostics;
5. unsupported shape diagnostics and configured serialization behavior;
6. name collision diagnostics and deterministic suffix behavior;
7. JSON Schema-originated optional-vs-nullable property preservation.

## Non-goals Preserved

- no database server dependency;
- no migrations generation;
- no provider-specific relational mapping completeness;
- no runtime `DbContext` factory integration;
- no query generation;
- no change-tracking customization beyond metadata projection;
- no Power BI, TOM, OpenAPI, browser, JavaScript, TypeScript, or Playwright dependency additions.
