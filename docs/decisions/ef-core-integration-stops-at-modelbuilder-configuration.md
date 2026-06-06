# Decision: EF Core Integration Stops at Semantic Model Derivation and ModelBuilder Configuration

## Status

Accepted for M0030.

## Context

`SemanticTypeModel.EFCore` is a domain package in the code-first SemanticTypeModel architecture.

The package should be useful enough to remove most future EF Core backlog while avoiding a scope expansion into database/application infrastructure.

EF Core has a large surface area that includes model configuration, provider-specific behavior, migrations, database lifecycle, DbContext construction, runtime validation, query filters, and operational database concerns. SemanticTypeModel should only own the semantic mapping portion.

## Decision

`SemanticTypeModel.EFCore` derives an `EfCoreSemanticModel` from the canonical semantic model and applies that domain model to EF Core `ModelBuilder`.

The EF Core package owns:

```text
EF Core domain semantic model derivation
provider-neutral ModelBuilder configuration
entities/properties/keys/indexes
explicit type conversions
explicit simple relationships
explicit simple inheritance mapping
explicit owned/value-object mapping
diagnostics and inspection for supported mapping
```

The EF Core package does not own:

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

## Rationale

- SemanticTypeModel is a semantic metadata framework, not an EF infrastructure framework.
- `ModelBuilder` is the correct EF Core boundary because it is provider-neutral and testable without a live database.
- Users know their database lifecycle, provider choices, migrations, DbContext structure, query filters, and deployment model best.
- Supporting provider-specific and database-lifecycle behavior would make the package responsible for operational concerns outside the semantic model.
- Including indexes, conversions, explicit relationships, and explicit inheritance makes the EF Core projection useful without crossing into infrastructure ownership.

## Implementation Notes

M0030 implementation keeps the package boundary at `DeriveEfCoreModel(...)` and `ApplyEfCoreSemanticModel(...)`; convenience APIs that derive directly from a canonical model must still return diagnostics and trace rather than hiding derivation state.

## Consequences

- EF Core support should be broad enough for common semantic mapping, including indexes, conversions, explicit relationships, and explicit inheritance.
- EF-specific metadata may override EF-specific derivation, but it must not silently override canonical semantics.
- Unsupported or ambiguous mapping must emit diagnostics.
- Tests must not require a database server, database provider-specific behavior, or live connection.
- Public docs and package README content must not imply database creation, migration generation, provider-specific behavior, DbContext discovery, or runtime database validation.
- Future EF Core work must stay within semantic model derivation and `ModelBuilder` configuration unless a later accepted decision changes the boundary.

## Alternatives Considered

### Minimal EF Core Mapping Only

Rejected because entities/properties/keys alone would leave important semantic mapping features as vague future backlog.

### Full EF Core Infrastructure Support

Rejected because migrations, provider behavior, DbContext generation, and runtime validation belong to the application and EF Core tooling, not SemanticTypeModel.

### Provider-Specific Extensions

Rejected for this package boundary. Provider-specific mapping can be handled by user code after SemanticTypeModel applies provider-neutral configuration.

### Automatic Relationship and Inheritance Inference

Rejected for M0030. The package supports explicit simple relationships and explicit user-selected inheritance strategies, but ambiguous inference must produce diagnostics.
