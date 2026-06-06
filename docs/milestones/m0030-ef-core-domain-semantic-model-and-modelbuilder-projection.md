# M0030: EF Core Domain Semantic Model and ModelBuilder Projection

## Status

Planned.

## Maturity Mode

Domain package architecture implementation for a public package set.

The repository has public packages, package README sources, public samples, public API baselines, and package validation. This milestone implements `SemanticTypeModel.EFCore` as a substantial domain semantic model projection package. It is intended to establish the durable EF Core boundary, not merely a minimal prototype.

## Task Mode

Milestone implementation routing and architecture-boundary implementation.

This milestone implements the M0026/M0028 domain semantic model architecture for EF Core and adds one architectural decision that permanently limits EF Core integration to semantic model derivation and provider-neutral `ModelBuilder` configuration.

Do not introduce TBPs, issue templates, workflow YAML, non-root README files, generated code files, or broad public-documentation rewrites in this planning package.

## Goal

After M0030, a consumer can:

```text
annotate C# entity/value-object types;
generate or extract a canonical Semantic Type Model;
run core transformations;
derive an EfCoreSemanticModel through configurable EF Core transformations;
inspect diagnostics and transformation trace;
apply the EfCoreSemanticModel to EF Core ModelBuilder;
configure basic-to-intermediate EF Core mapping without database lifecycle ownership.
```

The architectural pipeline is:

```text
Code-generated canonical semantic model
  -> EF Core derivation transformations
  -> EfCoreSemanticModel
  -> ModelBuilder configuration
```

## Required Authority

Read these documents before implementing any focus area:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/SPECS.md
docs/specs/code-first-semantic-model-architecture.md
docs/architecture/code-first-domain-projection-pipeline.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-ef-core-projection.md
docs/decisions/ef-core-integration-stops-at-modelbuilder-configuration.md
```

Read these only when the selected focus area touches the relevant component:

```text
docs/specs/type-model-core.md
docs/specs/type-schema-model.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-annotations.md
docs/specs/diagnostics.md
docs/PUBLIC-DOCS.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/guides/ef-core-projection.md
public-docs/release-notes.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Define and implement `EfCoreSemanticModel` or equivalent package-owned EF Core domain semantic model.
- Define EF Core derivation transformations using the M0028 domain derivation contract.
- Apply `EfCoreSemanticModel` to EF Core `ModelBuilder`.
- Support provider-neutral EF Core mapping for:
  - entities;
  - properties;
  - primary keys;
  - alternate keys;
  - indexes;
  - requiredness/nullability;
  - table names;
  - column names;
  - max length;
  - precision/scale;
  - explicit type conversions/value converters;
  - simple explicit relationships;
  - simple explicit inheritance using TPH, TPT, or TPC as selected by the user;
  - explicit owned/value-object mapping where current model support is sufficient.
- Support EF-specific metadata as overrides over core semantics.
- Emit diagnostics for unsupported or ambiguous mappings.
- Integrate with M0027 query/inspection and M0028 trace behavior.
- Add short-running tests and at least one code-first EF Core sample.
- Keep all behavior provider-neutral and independent from database lifecycle.

### Out of Scope

These are intentionally outside the EF Core package boundary and should not remain vague future backlog:

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

These advanced EF mapping features are also out of M0030 unless explicitly implemented later by new authority:

```text
complex relationship inference
many-to-many skip navigations
shadow foreign-key discovery
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

## Package Boundary

| Package | Responsibility |
|---|---|
| `SemanticTypeModel.EFCore` | EF Core domain semantic model, EF Core derivation transformations, diagnostics, and provider-neutral `ModelBuilder` application. |
| `SemanticTypeModel.Core` | Query, inspection, transformation, and derivation result infrastructure. |
| `SemanticTypeModel.DotNet` | Code extraction metadata consumed by code-first samples and generated models. |
| `SemanticTypeModel.Generators` | Generated providers consumed by EF Core derivation samples. |

`SemanticTypeModel.EFCore` must not become an EF infrastructure or database lifecycle framework.

## Focus Areas

### Focus Area 1 — Architectural Boundary and Public Scope

#### Intent

Make the EF Core boundary explicit and enforceable.

#### Required Authority

```text
docs/decisions/ef-core-integration-stops-at-modelbuilder-configuration.md
docs/specs/type-model-ef-core-projection.md
docs/specs/code-first-semantic-model-architecture.md
```

#### Implementation Requirements

- Preserve the boundary: semantic model derivation and provider-neutral `ModelBuilder` configuration only.
- Do not add database creation, migration generation, DbContext discovery/generation, provider-specific behavior, runtime database validation, or global query filters.
- Ensure public API names and docs do not imply database lifecycle ownership.
- Ensure package behavior can be tested without a database provider, database server, or live connection.

#### Validation

- Tier 1: EF Core package tests without provider/live database; public API tests if API names change; sample tests if public sample changes.
- Tier 2 before completion if code changes.
- Tier 3 only if package layout or package consumption behavior changes.

#### Direct Documentation Impact

```text
docs/decisions/ef-core-integration-stops-at-modelbuilder-configuration.md
docs/specs/type-model-ef-core-projection.md
```

#### Deferred Documentation Impact

```text
README.md
public-docs/guides/ef-core-projection.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/release-notes.md
```

### Focus Area 2 — EF Core Domain Semantic Model

#### Intent

Create the EF Core package-owned domain semantic model.

#### Required Authority

```text
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/architecture/code-first-domain-projection-pipeline.md
```

#### Implementation Requirements

- Define `EfCoreSemanticModel` or equivalent.
- Represent entity types, table/schema metadata, properties, columns, primary keys, alternate keys, indexes, relationships, inheritance, owned/value-object metadata, conversion metadata, and diagnostics.
- Keep the domain model explicit enough that `ModelBuilder` application does not rely on scattered ad hoc annotation lookups.
- Provide inspection integration or package-specific inspection methods.

#### Validation

- Tier 1: domain model construction tests; domain model inspection tests; deterministic ordering tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-ef-core-projection.md
```

#### Deferred Documentation Impact

```text
public-docs/nuget/SemanticTypeModel.EFCore.md
```

### Focus Area 3 — EF Core Derivation Pipeline

#### Intent

Derive `EfCoreSemanticModel` from code-generated canonical models using M0028.

#### Required Authority

```text
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
```

#### Implementation Requirements

- Expose a domain derivation API equivalent to `DeriveEfCoreModel`.
- Return a `SemanticDerivationResult<EfCoreSemanticModel>` or equivalent.
- Reuse core default transformations where appropriate.
- Add EF Core-specific derivation transformations.
- Allow users to configure, add, remove, replace, and order EF Core transformations in code.
- Accumulate diagnostics and trace entries.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
var result = model.DeriveEfCoreModel(options =>
{
    options.UseDefaultTransformations();
    options.Transformations.Replace(new MyEfCoreTableNamingTransformation());
});

result.Diagnostics.ThrowIfErrors();
modelBuilder.ApplyEfCoreSemanticModel(result.Model);
```

#### Validation

- Tier 1: derivation pipeline tests; transformation replacement tests; diagnostic accumulation tests; generated model derivation tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-ef-core-projection.md
```

#### Deferred Documentation Impact

```text
consumer sample docs if derivation customization is demonstrated
```

### Focus Area 4 — Entity, Property, Key, Index, and Conversion Mapping

#### Intent

Support useful EF Core configuration on par with the JSON Schema projection milestone, while remaining provider-neutral.

#### Required Authority

```text
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-core.md
docs/specs/type-model-dotnet-attributes.md
```

#### Required Mapping Support

```text
entity derivation from core Entity semantics or explicit efCore metadata
table name and schema metadata
property mapping
column name metadata
primary keys
alternate keys
single-column indexes
composite indexes
unique indexes
requiredness/nullability
max length
precision/scale
explicit value converter type
explicit provider CLR type
enum storage where modeled
ignored member metadata when explicit
```

#### Rules

- Core semantics provide defaults.
- EF-specific metadata overrides EF-specific derivation.
- Invalid combinations emit diagnostics.
- No provider-specific fluent extensions are used.
- Converters are explicit; the package must not invent custom converter logic.

#### Validation

- Tier 1: entity/property/key mapping tests; alternate-key tests; index tests; type conversion tests; requiredness/nullability tests; deterministic `ModelBuilder` application tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-ef-core-projection.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/ef-core-projection.md
public-docs/nuget/SemanticTypeModel.EFCore.md
```

### Focus Area 5 — Simple Explicit Relationships and Owned/Value-Object Mapping

#### Intent

Support important relationship/value-object scenarios without complex inference.

#### Required Authority

```text
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-transformation-and-domain-derivation.md
```

#### Required Support

Support explicit simple relationships when metadata is sufficient:

```text
one-to-one
one-to-many
many-to-one
explicit foreign-key property
explicit navigation metadata
required/optional relationship
delete behavior when explicitly configured
```

Support explicit owned/value-object mapping when metadata is sufficient:

```text
owned/value object property
owned reference where EF Core can represent it provider-neutrally
diagnostics for unsupported owned collections
diagnostics for ambiguous ownership
```

#### Out of Scope

```text
complex relationship inference
many-to-many skip navigations
shadow FK discovery
owned collections
automatic relationship pairing
provider-specific delete behavior extensions
```

#### Validation

- Tier 1: explicit one-to-one tests; explicit one-to-many tests; explicit many-to-one tests; owned/value-object tests; unsupported relationship diagnostics tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-ef-core-projection.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/ef-core-projection.md
sample docs if relationships are demonstrated
```

### Focus Area 6 — Simple Explicit Inheritance Mapping

#### Intent

Support simple inheritance scenarios and user-selected EF Core inheritance strategy.

#### Required Authority

```text
docs/specs/type-model-ef-core-projection.md
docs/specs/code-first-semantic-model-architecture.md
```

#### Required Support

Support inheritance mapping only when explicit metadata or configuration identifies strategy:

```text
TPH
TPT
TPC
```

Rules:

- User chooses the inheritance strategy through options or EF-specific metadata.
- The package must not infer TPH/TPT/TPC from arbitrary inheritance without explicit configuration.
- Discriminator metadata is supported for TPH when explicitly configured.
- Derived entities must be resolvable as EF Core entities.
- Ambiguous or unsupported inheritance emits diagnostics.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
var result = model.DeriveEfCoreModel(options =>
{
    options.Inheritance.DefaultStrategy = EfCoreInheritanceStrategy.Tph;

    options.Inheritance.For<BasePaymentMethod>()
        .UseTpc();
});
```

#### Validation

- Tier 1: TPH mapping tests; TPT mapping tests; TPC mapping tests; explicit discriminator tests where supported; ambiguous inheritance diagnostic tests.
- Tier 2 before completion if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-ef-core-projection.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/ef-core-projection.md
sample docs if inheritance is demonstrated
```

### Focus Area 7 — ModelBuilder Application

#### Intent

Apply the EF Core domain semantic model to `ModelBuilder` without owning DbContext or database lifecycle.

#### Required Authority

```text
docs/specs/type-model-ef-core-projection.md
docs/decisions/ef-core-integration-stops-at-modelbuilder-configuration.md
```

#### Required Support

Provider-neutral `ModelBuilder` application should apply:

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
HasConversion(...) when explicit converter metadata exists
HasOne/WithMany/WithOne where explicit relationship metadata exists
OwnsOne where explicit owned metadata exists
UseTph/Tpt/Tpc equivalent EF Core API where supported and explicit
```

Use actual EF Core APIs supported by the referenced EF Core package version.

#### Validation

- Tier 1: ModelBuilder application tests; metadata assertion tests over EF Core model; no provider/live database tests.
- Tier 2 before completion if code changes.
- Tier 3 only if package consumption behavior changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-ef-core-projection.md
```

#### Deferred Documentation Impact

```text
public-docs/guides/ef-core-projection.md
public-docs/nuget/SemanticTypeModel.EFCore.md
```

### Focus Area 8 — Diagnostics, Inspection, and Sample

#### Intent

Make EF Core derivation usable in the same code-first test/console loop as JSON Schema.

#### Required Authority

```text
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/engineering/samples.md
```

#### Implementation Requirements

- EF Core derivation diagnostics include code, severity, model path, transformation id, projection target, and related model paths where available.
- EF Core domain model can be inspected deterministically.
- Transformation trace is available.
- Add or update a code-first EF Core sample.
- Sample must not require a database provider, server, connection string, migration, or live database validation.

#### Validation

- Tier 1: diagnostic tests; inspection snapshot tests; sample validation if sample changes.
- Tier 2 before completion if code changes.
- Tier 3 only if package/sample consumption behavior changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-ef-core-projection.md
public-docs/diagnostics/*.md only if new public diagnostics are added
```

#### Deferred Documentation Impact

```text
public-docs/samples.md
public-docs/samples/*.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/guides/ef-core-projection.md
```

## Required Acceptance Criteria

M0030 is complete when:

- The EF Core architectural boundary decision is documented.
- `SemanticTypeModel.EFCore` does not own database creation, migrations, provider-specific behavior, DbContext discovery/generation, runtime database validation, or global query filters.
- An EF Core domain semantic model exists.
- EF Core derivation uses the M0028 domain derivation contract.
- Derivation returns model, diagnostics, and transformation trace.
- Users can configure EF Core derivation transformations in code.
- Core semantics provide EF defaults and EF-specific metadata can override EF-specific behavior.
- Entity/property/table/column mapping works.
- Primary key and alternate key mapping work.
- Index mapping works for single-column, composite, and unique indexes.
- Requiredness/nullability mapping works.
- Explicit type conversion mapping works.
- Explicit simple relationship mapping works for one-to-one, one-to-many, and many-to-one where metadata is sufficient.
- Explicit owned/value-object mapping works where metadata is sufficient.
- Explicit inheritance mapping works for TPH, TPT, and TPC where metadata is sufficient and strategy is user-selected.
- `ModelBuilder` application configures provider-neutral EF Core metadata without requiring a database provider or live connection.
- Unsupported or ambiguous EF Core mapping emits diagnostics.
- EF Core domain model and derivation trace inspection are deterministic.
- Code-first generated model tests cover EF Core derivation and `ModelBuilder` application.
- Tier 2 validation passes, or any inability to run it is explicitly reported with the exact lower-tier validation performed.
- No TBPs, issue templates, non-root README files, workflow YAML, broad public-doc rewrites, or generated code files are introduced by the planning package itself.

## Validation Plan

Use the smallest validation tier that can catch the expected regression.

### Tier 1

Use focused validation for:

```text
EfCore domain model tests
EfCore derivation tests
ModelBuilder application tests
inheritance tests
index tests
conversion tests
relationship tests
owned/value-object tests
diagnostic tests
code-first generated model tests
affected sample tests
```

Expected command shape:

```sh
./eng/test-project.sh <ef-core-test-project>
./eng/test-filter.sh <ef-core-domain-or-modelbuilder-filter>
./eng/check-affected.sh src/SemanticTypeModel.EFCore tests samples/code-first-ef-core
```

Use actual repository project names after inspecting the solution.

### Tier 2

Run before completing implementation work:

```sh
./eng/check.sh
```

### Tier 3

Run only if package layout, package README generation, or package consumption behavior changes:

```sh
./eng/package.sh <version>
./eng/package-smoke.sh <version>
./eng/samples.sh
```

This is not a release publication milestone; do not run publish validation.

## Direct Documentation Impact

The implementation should directly update:

```text
docs/specs/type-model-ef-core-projection.md
docs/decisions/ef-core-integration-stops-at-modelbuilder-configuration.md
```

Update related existing specs only when implementation changes contradict current authority:

```text
docs/specs/code-first-semantic-model-architecture.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
```

## Deferred Documentation Impact

Leave explicit notes for a later documentation synchronization pass covering:

```text
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
README.md
public-docs/getting-started.md
public-docs/guides/ef-core-projection.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md if new diagnostics are public
public-docs/release-notes.md
```

Do not perform broad public documentation synchronization as part of this implementation milestone unless a consumer-facing behavior change directly requires it.
