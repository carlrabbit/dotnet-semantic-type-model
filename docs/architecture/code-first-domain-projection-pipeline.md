# Code-First Domain Projection Pipeline

## Status

Authoritative architecture document.

## Purpose

Define the structural pipeline for code-first SemanticTypeModel usage, the canonical semantic boundary, target-specific domain derivation, and compile-time/runtime composition.

## System Mental Model

```text
Annotated .NET code
        |
        +-- runtime extraction (SemanticTypeModel.DotNet)
        |
        +-- compile-time generation (SemanticTypeModel.Generators)
                 |
                 v
          canonical TypeSchemaModel
                 |
        query / inspect / validate / transform
                 |
        +--------+-------------+-------------+----------------+---------------+
        |                      |             |                |               |
        v                      v             v                v               v
   JSON Schema              EF Core       Power BI    System.Text.Json  Configuration
   domain model             domain/       domain       domain model     domain model
                            relational      model
                            model
        |                      |             |                |               |
        v                      v             v                v               v
   document export        generated EF   local          resolver/options   Options
                          configuration   metadata       behavior           registration
```

The canonical model owns semantic meaning. Target packages own representation and integration choices.

## Authoring Source

Annotated .NET code is the supported authoring source for canonical semantic models.

Inputs include:

- C# types;
- SemanticTypeModel attributes;
- supported aliases/conventions;
- target-specific annotations where a target package explicitly owns them;
- extraction/generator configuration.

A persisted semantic snapshot may preserve a generated model for later access, but it is not a second authoring language.

External formats such as JSON Schema are projection/integration targets unless a future accepted architecture explicitly adds another source path.

## Canonical Model Boundary

`TypeSchemaModel` is the projection-neutral semantic source of truth.

It represents meaning such as:

- stable type/property identity;
- semantic roles and shapes;
- keys and requiredness/nullability;
- ownership/containment;
- scalar and enum semantics;
- constraints and typed conditional literals;
- envelopes;
- lifecycle/evolution metadata;
- extension data;
- audience-specific descriptions;
- diagnostics and transformation/query metadata.

It does not own target representation choices such as EF relationship inference, JSON Schema document structure, Power BI service publishing, serializer implementation, or configuration source loading.

## Acquisition and Generation

### Runtime extraction

`SemanticTypeModel.DotNet` extracts supported annotated CLR types into the canonical model when runtime construction is required.

### Compile-time generation

`SemanticTypeModel.Generators` generates canonical semantic-model providers for code-first projects.

It also emits the deterministic compile-time semantic manifest used for cross-project generator composition. The manifest is generated metadata derived from the canonical semantics and CLR lineage; it is not a user-authored schema.

Manifest compatibility/version-evolution policy is a separate architectural topic and is not defined by this document.

## Transformation and Domain Derivation

Transformations operate on canonical meaning and may normalize, derive, or validate semantic information.

Target packages derive package-owned domain models before target functionality is applied. Domain packages must not redefine canonical meaning merely to fit the target representation.

## Target Pipelines

### JSON Schema

```text
canonical TypeSchemaModel
  -> JSON Schema derivation
  -> JSON Schema domain model
  -> Draft 2020-12 export
```

JSON Schema is not a supported canonical-model authoring source.

### EF Core

EF Core has two related paths: provider-neutral relational derivation/inspection and compile-time CLR application.

```text
semantic model assembly
  -> compile-time semantic manifest
  -> persistence project explicitly selects model(s)
  -> SemanticTypeModel.EFCore.Generators
  -> generated IEntityTypeConfiguration<TEntity> per semantic Entity
  -> generated Apply<Model>SemanticModel()
  -> application-owned DbContext composition
```

The generated model configures only the CLR entities it owns. It does not police the global EF model. Multiple semantic models and application-owned EF entities can therefore compose in one `DbContext`.

`SemanticTypeModel.EFCore` retains provider-neutral relational derivation/inspection contracts and runtime helper primitives used by generated configuration.

Migrations, database lifecycle, provider setup, and final `DbContext` composition remain application-owned.

### Power BI

```text
canonical TypeSchemaModel
  -> Power BI derivation
  -> Power BI domain model
  -> local metadata output
```

Service publishing, workspace management, refresh scheduling, PBIX generation, and full TOM parity are outside the library boundary.

### System.Text.Json

```text
canonical TypeSchemaModel
  -> System.Text.Json derivation
  -> package-owned domain model
  -> resolver / JsonSerializerOptions behavior
```

SemanticTypeModel does not generate a custom `JsonSerializerContext` or serializer implementation.

### Configuration

```text
canonical TypeSchemaModel
  -> Configuration derivation
  -> Configuration domain model
  -> Microsoft.Extensions.Options registration / validation
```

SemanticTypeModel does not become a configuration provider or own source data loading.

## Package and Dependency Boundaries

Core direction remains inward toward abstractions/canonical semantics; target integrations depend on canonical contracts rather than the reverse.

Conceptually:

```text
SemanticTypeModel.Abstractions
        ^
        |
SemanticTypeModel.Core
        ^
        +-- SemanticTypeModel.DotNet
        +-- SemanticTypeModel.Generators
        +-- target/domain packages
        +-- integration/generator packages
```

Target packages may share internal helper code where appropriate, but canonical packages must not depend on target-specific packages.

For EF generation, the intended project layering is:

```text
Domain/model project
  -> DotNet + semantic generator
  -> no EF dependency required

Persistence project
  -> domain/model project
  -> SemanticTypeModel.EFCore
  -> SemanticTypeModel.EFCore.Generators
  -> EF Core/provider packages
```

## Architectural Constraints

- Code-first annotated .NET remains the supported authoring source.
- The canonical model remains projection-neutral.
- Target/domain models are explicit and package-owned.
- Target packages own representation choices, not canonical meaning.
- Diagnostics are preferred over ambiguous target guessing.
- Compile-time output and target behavior must be deterministic.
- EF generated configuration owns only selected semantic entities; the application owns the global `DbContext`.
- Historical adapter/application architectures do not remain equal authority in the working tree after they are superseded.
