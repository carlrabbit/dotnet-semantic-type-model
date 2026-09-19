# Canonical Domain Projection Pipeline

## Status

Authoritative architecture document.

The historical file name is retained for repository continuity; the architecture now includes both code-first and programmatic canonical authoring.

## Purpose

Define the structural pipeline for SemanticTypeModel authoring, the canonical semantic boundary, target-specific domain derivation, and compile-time/runtime composition.

## System Mental Model

```text
Annotated .NET code                         Runtime/application metadata
        |                                             |
        +-- runtime extraction                       |
        |   (SemanticTypeModel.DotNet)               |
        |                                             |
        +-- compile-time generation                   |
            (SemanticTypeModel.Generators)            |
                                                      |
                                                      v
                                  SemanticTypeModel.Core authoring/finalization
                                                      |
                 +------------------------------------+
                 |
                 v
          canonical TypeSchemaModel
                 |
        query / inspect / validate / transform
                 |
        +--------+-------------+-------------+----------------+---------------+
        |                      |             |                |
        v                      v             v                v
   JSON Schema              EF Core       Power BI    System.Text.Json    TestData
   domain model             domain/       domain       domain model       value graph
                            relational      model
                            model
        |                      |             |                |
        v                      v             v                v
 document export        generated EF       local        resolver/options
                        configuration      metadata        behavior
```

The canonical model owns semantic meaning. Target packages own representation and integration choices.

## Authoring Sources

SemanticTypeModel supports two canonical authoring paths.

### Code-first authoring

Annotated .NET code remains the primary CLR-oriented authoring workflow.

Inputs include:

- C# types;
- SemanticTypeModel attributes;
- supported aliases/conventions;
- target-specific annotations where a target package explicitly owns them;
- extraction/generator configuration.

### Programmatic Model Authoring

Applications may explicitly assemble current canonical type definitions at runtime and finalize them through the Core-owned authoring API.

Programmatic authoring is appropriate when the semantic shape itself is runtime-defined and no CLR type graph is the natural source of truth.

It produces the same canonical `TypeSchemaModel` and does not create a separate dynamic model kind.

A persisted semantic snapshot may preserve a previously finalized model for later access, but it is not a second hand-authored language.

External formats such as JSON Schema, OpenAPI, or database schemas are not canonical authoring sources merely because an application can translate external data into programmatic declarations. Such translation remains application-owned unless a future accepted integration says otherwise.

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

Canonical consumers that require only canonical semantics operate independently of supported authoring provenance.

## Acquisition and Generation

### Runtime extraction

`SemanticTypeModel.DotNet` extracts supported annotated CLR types into the canonical model when CLR-authored runtime construction is required.

### Compile-time generation

`SemanticTypeModel.Generators` generates canonical semantic-model providers for code-first projects.

It also emits the deterministic compile-time semantic manifest used for cross-project generator composition. The manifest is generated metadata derived from canonical semantics and CLR lineage; it is not a user-authored schema.

The manifest is ephemeral internal compile-time transport. Producer and consumer generator packages must use the same exact SemanticTypeModel suite version; persisted or cross-version manifest consumption is unsupported.

### Programmatic finalization

`SemanticTypeModel.Core` owns programmatic root-model assembly and finalization over the existing canonical definitions.

It owns construction of the root lookup index and invokes canonical validation before returning a successful model. It does not emit CLR types, compiler artifacts, or compile-time manifests.

## Transformation and Domain Derivation

Transformations operate on canonical meaning and may normalize, derive, or validate semantic information.

Target packages derive package-owned domain models before target functionality is applied. Domain packages must not redefine canonical meaning merely to fit the target representation.

`SemanticTypeModel.TestData` is an inward-dependent runtime capability. It consumes `TypeSchemaModel` and produces a package-owned finite semantic value graph. It is neither a canonical authoring source nor a target package dependency. Its semantic generation path does not require CLR materialization.

## Target Pipelines

### JSON Representation Fidelity

JSON Schema and System.Text.Json remain sibling target projections over the same canonical model.

Their coordinated role is:

```text
canonical TypeSchemaModel
        |
        +--> System.Text.Json domain model --> JSON wire output
        |
        +--> JSON Schema domain model ------> declarative schema / validation contract
```

System.Text.Json owns supported runtime serialization representation. JSON Schema owns declarative representation of structural/validation semantics and selected STM-only semantic preservation.

The repository may define a supported configuration under which successfully serialized System.Text.Json output is required to conform to the JSON Schema derived from the same canonical model. That guarantee is one-way output conformance, not bidirectional serializer/schema equivalence.

This coordination does not introduce a new shared JSON package, a new canonical JSON contract model, or a dependency from `SemanticTypeModel.JsonSchema` to `SemanticTypeModel.SystemTextJson` or vice versa.

### JSON Schema

```text
canonical TypeSchemaModel
  -> JSON Schema derivation
  -> JSON Schema domain model
  -> Draft 2020-12 export
```

JSON Schema is not a supported canonical-model import source. A programmatically authored canonical model may be exported normally.

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

The generated application path depends on CLR lineage and the compile-time manifest; programmatic authoring does not fabricate either.

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

Power BI derivation depends on canonical semantics, not code-first provenance. Programmatically authored models are supported when they provide the semantics required by the Power BI projection.

Service publishing, workspace management, refresh scheduling, PBIX generation, and full TOM parity are outside the library boundary.

### System.Text.Json

```text
canonical TypeSchemaModel + compatible CLR model
  -> System.Text.Json derivation
  -> package-owned domain model
  -> resolver / JsonSerializerOptions behavior
```

SemanticTypeModel does not generate CLR types or a custom `JsonSerializerContext` merely because a model was programmatically authored.

The base resolver/context remains application-owned and is preserved by default. Semantic validation constraints are not reimplemented as serializer validation.

### TestData

```text
canonical TypeSchemaModel
  -> deterministic semantic generation
  -> SemanticTestValue graph
  -> optional CLR materialization when compatible CLR types exist
```

Programmatic canonical models are first-class inputs to semantic generation. CLR materialization remains a separate optional boundary and does not cause runtime CLR type generation.

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
        +-- SemanticTypeModel.TestData
        +-- target/domain packages
        +-- integration/generator packages
```

Programmatic authoring belongs in Core because it combines canonical root assembly with canonical validation. It must not add target-package dependencies to Core.

Target packages may share internal helper code where appropriate, but canonical packages must not depend on target-specific packages.

For EF generation, the intended project layering remains:

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

- Code-first annotated .NET remains the primary CLR-oriented authoring source.
- Explicit programmatic construction is a supported peer authoring path for runtime-defined semantic models.
- Both authoring paths converge on the same canonical `TypeSchemaModel`; no dynamic parallel hierarchy is introduced.
- Programmatic authoring does not imply live editing of finalized models.
- External schema formats remain outside canonical authoring unless separately accepted.
- The canonical model remains projection-neutral.
- Target/domain models are explicit and package-owned.
- Target packages own representation choices, not canonical meaning.
- Targets that require CLR lineage may continue to require the code-first/generated path; programmatic authoring does not fabricate CLR types or manifests.
- Diagnostics are preferred over ambiguous target guessing.
- Compile-time output and runtime finalization must be deterministic.
- JSON Schema and System.Text.Json may coordinate through shared project truth and tests but remain sibling packages without a direct dependency.
- System.Text.Json output/schema fidelity is a bounded one-way guarantee for supported configurations, not a claim of complete JSON contract equivalence.
- EF generated configuration owns only selected semantic entities; the application owns the global `DbContext`.
- Historical adapter/application architectures do not remain equal authority in the working tree after they are superseded.
