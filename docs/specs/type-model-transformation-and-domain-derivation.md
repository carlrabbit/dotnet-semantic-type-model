# Type Model Transformation and Domain Derivation Specification

## Status

Authoritative behavioral specification.

## Purpose

Define the shared transformation pipeline and domain semantic model derivation contracts for code-first SemanticTypeModel.

This specification is authoritative for:

- transformation abstraction;
- transformation ordering and configuration;
- transformation result and trace behavior;
- core default transformation requirements;
- domain semantic model derivation result shape;
- transformation diagnostic conventions;
- inspection integration for transformation results.

## Product Role

Transformations are the mechanism that turns extracted code facts into semantic meaning and domain-specific semantic models.

The canonical flow is:

```text
Code-generated canonical model
  -> core transformations
  -> transformed canonical model
  -> domain derivation transformations
  -> domain semantic model
  -> domain functionality
```

Transformations are not an implementation detail. They are a public, code-configurable development-loop surface.

## Package Boundary

| Package | Responsibility |
|---|---|
| `SemanticTypeModel.Abstractions` | Shared interfaces/result contracts only when cross-package usage requires them. |
| `SemanticTypeModel.Core` | Transformation pipeline, core transformations, trace/result types, diagnostic conventions, inspection helpers. |
| `SemanticTypeModel.DotNet` | Extraction metadata consumed by core transformations. |
| `SemanticTypeModel.Generators` | Generated model metadata needed by transformations. |
| Domain packages | Domain transformations, domain semantic models, and domain functionality. |

Core must not depend on domain packages.

## Transformation Contract

A transformation is a deterministic operation over a semantic model.

Required transformation properties:

- stable identifier;
- deterministic display name;
- deterministic behavior for the same input and options;
- no input model mutation;
- explicit diagnostics for unsafe derivation;
- no silent lossy transformation.

Candidate shape:

```csharp
public interface ISemanticModelTransformation
{
    string Id { get; }
    string DisplayName { get; }

    SemanticModelTransformationResult Transform(
        TypeSchemaModel model,
        SemanticModelTransformationContext context);
}
```

The final API may differ, but equivalent behavior is required.

## Transformation Context

A transformation context carries execution metadata and services needed by a transformation.

Required context capabilities:

- transformation id;
- pipeline run id or deterministic run context;
- diagnostic sink;
- execution options;
- cancellation token if the repository keeps async transformation support;
- access to query helpers;
- access to deterministic metadata policies such as annotation policy.

Context must not be used to hide global mutable state.

## Transformation Result

A transformation result contains:

- resulting model;
- diagnostics emitted by the transformation;
- trace entry or trace contribution;
- indication of whether execution should continue when supported.

Rules:

- returning the original model is valid when no changes are needed;
- returning a new model is required when model content changes;
- diagnostics accumulate at pipeline level;
- errors may stop later transformations according to pipeline execution policy.

## Pipeline Configuration

The pipeline is configured directly in code.

Required operations:

| Operation | Behavior |
|---|---|
| Add | Append or insert a transformation. |
| Remove | Remove a default or previously configured transformation by type or id. |
| Replace | Replace a known transformation by type or id. |
| AddBefore | Insert before a known transformation. |
| AddAfter | Insert after a known transformation. |
| UseCoreDefaults | Add the default core transformation sequence. |
| Clear | Remove all configured transformations when explicit. |

Failure rules:

- duplicate identifiers are rejected unless replacement is explicit;
- missing insertion targets fail deterministically;
- removing a missing optional transformation may be configurable, but default behavior must be explicit;
- final pipeline order must be inspectable.

Candidate shape:

```csharp
var result = model.Transform(pipeline =>
{
    pipeline.UseCoreDefaults();
    pipeline.Replace<InferEntityKeysTransformation>(new MyKeyInference());
    pipeline.Remove<InferRelationshipsTransformation>();
    pipeline.AddAfter<NormalizeSemanticAliasesTransformation>(new MyTransformation());
});
```

Equivalent behavior is required even if names differ.

## Pipeline Execution

Pipeline rules:

- transformations run sequentially in configured order;
- input model is immutable to consumers;
- transformations must not mutate input models;
- diagnostics accumulate in order;
- default execution stops before the next transformation after an error diagnostic;
- continue-on-error mode allows later transformations to run;
- warning promotion happens through diagnostic policy, not by mutating already emitted diagnostics;
- parallel transformation execution is unsupported.

## Core Default Pipeline

The core default pipeline is intentionally small.

Minimum default transformations where current model support allows:

```text
Normalize semantic alias attributes.
Derive core semantic type roles.
Derive keys from explicit SemanticKey metadata.
Normalize display metadata.
Validate required core semantic primitives.
Validate relationship metadata when model support is sufficient.
```

Core transformations must not derive domain-specific EF Core, Power BI, JSON Schema, or System.Text.Json semantics directly. Domain derivation belongs to domain packages.

## Alias Normalization

Alias normalization maps convenience metadata to core semantic primitives.

Examples:

```text
SemanticEntity
  -> SemanticType("Entity")

SemanticValueObject
  -> SemanticType("ValueObject")
```

The transformation must emit diagnostics for conflicting aliases or incompatible semantic type declarations.

## Key Derivation

Key derivation turns explicit key intent into canonical key semantics.

Rules:

- `SemanticKey` on an entity property derives a key semantic entry.
- A key on a non-entity emits a diagnostic unless configured otherwise.
- Multiple primary keys are allowed only if the model explicitly supports composite keys; otherwise emit a diagnostic.
- Key derivation must not infer EF Core primary keys directly.

## Relationship Derivation and Validation

Relationship derivation/validation may be limited in the first implementation.

Rules:

- explicit relationship metadata is validated when present;
- missing targets emit diagnostics when derivation needs a target;
- ambiguous cardinality emits diagnostics;
- invalid relationship target kind emits diagnostics;
- relationship derivation must not infer domain-specific foreign keys directly.

## Transformation Diagnostics

Every transformation diagnostic must include when available:

```text
code
severity
message
model path
transformation id
pipeline stage or equivalent stage metadata
related model paths
```

Diagnostic rules:

- core transformations own core diagnostic IDs;
- domain packages own domain diagnostic IDs;
- do not reuse retired IDs;
- do not emit silent lossy derivations;
- diagnostics must be queryable through query/inspection helpers;
- detailed diagnostic text must include transformation context when available.

## Transformation Trace

A transformation trace is an ordered record of pipeline execution.

Each trace entry contains:

- sequence index;
- transformation id;
- display name;
- diagnostic count;
- emitted diagnostic codes;
- optional deterministic change summary.

Trace rules:

- trace ordering matches pipeline execution order;
- trace output is deterministic;
- trace output must not include timestamps, machine paths, random IDs, or culture-sensitive formatting;
- trace does not require a full model diff engine.

Candidate text output:

```text
Transformation Pipeline: CoreDefaults

[1] NormalizeSemanticAliases
    Diagnostics: 0

[2] InferKeys
    Derived: /types/Customer/keys/Primary -> Id
    Diagnostics: 0

[3] ValidateRelationships
    warning STM5201 /types/Order/properties/Customer Relationship target is not marked as Entity.
```

## Domain Derivation Contract

A domain derivation creates a domain-specific semantic model before domain functionality runs.

Required result shape:

```csharp
public sealed class SemanticDerivationResult<TDomainModel>
{
    public TDomainModel Model { get; }
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; }
    public SemanticTransformationTrace Trace { get; }
}
```

The final API may differ, but equivalent behavior is required.

Domain derivation rules:

- domain models are package-owned;
- domain derivation may reuse core defaults;
- domain derivation may add domain-specific transformations;
- users can configure domain transformation sequences in code;
- domain derivation emits diagnostics for missing or ambiguous domain metadata;
- domain functionality operates on the domain semantic model, not directly on ad hoc annotations.

## Domain Package Pattern

Domain packages should expose APIs equivalent to:

```csharp
var result = model.DeriveEfCoreModel(options =>
{
    options.UseDefaultTransformations();
    options.Transformations.Replace(new MyEfCoreTableNamingTransformation());
});
```

or:

```csharp
var result = EfCoreSemanticModelDeriver.Derive(model, options =>
{
    options.Transformations.Add(new MyTransformation());
});
```

Domain package implementations may choose naming appropriate to the package, but must preserve the shared derivation result behavior.

## Inspection Integration

Transformation results and domain derivation results must integrate with M0027 inspection.

Required behavior:

```csharp
result.Diagnostics.ToDiagnosticText();
result.Trace.ToTransformationText();
result.Model.ToSemanticText();
```

Text output must be deterministic and suitable for snapshot tests.

## Configuration Boundary

M0028 supports direct code configuration.

Supported:

- fluent pipeline configuration;
- strongly typed transformation replacement;
- identifier-based transformation replacement;
- options objects;
- explicit default pipeline selection.

Unsupported in M0028:

- file-based transformation configuration;
- plugin discovery;
- assembly scanning;
- reflection-based auto-loading;
- DI-only hidden configuration.

DI integration may wrap explicit transformation configuration in later milestones.

## Test Requirements

Short-running tests must cover:

- deterministic pipeline ordering;
- add/remove/replace/insert behavior;
- duplicate transformation id rejection;
- missing insertion target behavior;
- stop-on-error behavior;
- continue-on-error behavior;
- no input model mutation;
- diagnostics accumulation;
- trace text determinism;
- alias normalization;
- key derivation where supported;
- fake domain derivation result;
- generated model transformation;
- query/inspection integration.

## Non-Goals

M0028 does not define:

- full EF Core domain semantic model;
- full Power BI domain semantic model;
- full JSON Schema domain semantic model;
- full System.Text.Json domain semantic model;
- visual transformation graph;
- full model diff engine;
- parallel transformation execution;
- plugin loading;
- file-based transformation configuration;
- runtime model editing.

## M0028 Core Implementation Contract

The M0028 core implementation exposes the direct-code pipeline from `SemanticTypeModel.Core.Transformation`.

Required public behaviors are:

- `ISemanticModelTransformation` identifies every transformation with a stable `Id` and deterministic `DisplayName`.
- `SchemaTransformationPipeline` supports `Add`, `Remove`, `Replace`, `AddBefore`, `AddAfter`, `Clear`, `UseCoreDefaults`, and deterministic order inspection.
- `SemanticModelTransformationExtensions.Transform` creates a configured pipeline directly from a `TypeSchemaModel`.
- `SemanticModelTransformationResult` contains the transformed model, accumulated diagnostics, and a `SemanticTransformationTrace`.
- `SemanticDerivationResult<TDomainModel>` is the shared generic result shape for package-owned domain semantic models.
- `TransformationTextExtensions.ToTransformationText` provides deterministic trace text and must not include timestamps, machine paths, random identifiers, or culture-sensitive formatting.

The minimal core default pipeline order is:

1. normalize annotation keys;
2. normalize semantic role aliases;
3. derive semantic keys from explicit key metadata;
4. normalize display metadata from schema annotations;
5. validate the resulting model.

The core default pipeline remains projection-neutral. It must not derive EF Core, Power BI, JSON Schema, or System.Text.Json domain semantics.

Core transformation diagnostics added by M0028 use the STM1xxx range:

| Code | Severity | Condition |
|---|---|---|
| `STM1004` | Warning | Semantic role alias metadata is not a supported core semantic role. |
| `STM1005` | Warning | Semantic role alias metadata conflicts with an already declared canonical role. |
| `STM1006` | Warning | Explicit semantic key metadata was declared on a non-entity type. |
| `STM1007` | Warning | Multiple primary semantic keys were declared for one type. |
