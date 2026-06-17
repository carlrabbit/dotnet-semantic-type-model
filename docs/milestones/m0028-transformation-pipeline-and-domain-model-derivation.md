# M0028: Transformation Pipeline and Domain Model Derivation

## Status

Planned.

## Maturity Mode

Core architecture implementation for a public package set.

The repository has public packages, public samples, package README sources, public API compatibility documentation, and package validation. This milestone introduces the shared transformation and domain-derivation contract that future JSON Schema, EF Core, Power BI, and System.Text.Json domain packages must reuse.

## Task Mode

Milestone implementation routing.

This milestone implements the M0026 transformation and domain semantic model architecture. It does not introduce a new durable architectural decision; M0026 already established that domain packages derive domain-specific semantic models before domain functionality.

Do not introduce TBPs, issue templates, workflow YAML, non-root README files, generated code, or broad public-documentation rewrites in this planning package.

## Goal

After M0028, SemanticTypeModel has a configurable transformation pipeline that:

```text
takes a code-generated canonical Semantic Type Model;
runs deterministic ordered transformations;
derives additional core semantic meaning;
emits diagnostics instead of silently guessing;
can be configured or replaced in code;
produces inspectable transformation results;
defines the contract used by domain packages to create domain semantic models.
```

The core architecture remains:

```text
Canonical semantic model
  -> configured transformations
  -> transformed canonical model or domain semantic model
  -> diagnostics and transformation trace
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
docs/specs/type-model-core.md
docs/specs/type-model-query-and-inspection.md
```

Read these only when the selected focus area touches the relevant component:

```text
docs/specs/type-schema-model.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-annotations.md
docs/specs/diagnostics.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-contract-integration.md
docs/PUBLIC-DOCS.md
public-docs/samples.md
public-docs/nuget/*.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Define and implement the shared transformation abstraction.
- Define and implement a deterministic transformation pipeline.
- Define and implement transformation options for add, remove, replace, and ordered insertion.
- Define transformation context and diagnostic emission rules.
- Define transformation result and trace contracts.
- Provide a minimal core default pipeline.
- Implement at least alias normalization and key derivation when model support exists.
- Add relationship validation or derivation only where the current model shape supports it safely.
- Define the domain semantic model derivation contract.
- Define shared result shape for domain model derivation.
- Provide deterministic text inspection for transformation results/traces, reusing M0027 inspection conventions.
- Add short-running tests for ordering, replacement, diagnostics, deterministic traces, and generated-model usage.
- Keep the API usable directly from tests and console programs.

### Out of Scope

- Full EF Core semantic model implementation.
- Full Power BI semantic model implementation.
- Full JSON Schema semantic model implementation.
- Full System.Text.Json domain model implementation.
- Visual transformation graph.
- Full model diff engine.
- Parallel transformation execution.
- External plugin discovery.
- Reflection-based transformation auto-loading.
- File-based transformation configuration.
- DI-only transformation configuration.
- Runtime model editing.
- Broad public documentation rewrite.
- Release publication.

## Package Boundary

The preferred package boundary is:

| Package | Responsibility |
|---|---|
| `SemanticTypeModel.Abstractions` | Shared transformation and derivation interfaces only if required across packages. |
| `SemanticTypeModel.Core` | Transformation pipeline, core transformations, result/trace types, diagnostics helpers, and inspection integration. |
| `SemanticTypeModel.DotNet` | Source metadata and attribute extraction used by core transformations. |
| `SemanticTypeModel.Generators` | Generated models must contain metadata needed by transformations. |
| Domain packages | Later milestones provide domain-specific transformations and domain semantic models using the shared derivation contract. |

Avoid forcing domain packages to depend on each other. Core must remain projection-neutral.

## Focus Areas

### Focus Area 1 — Transformation Contract

#### Intent

Create the minimal stable abstraction for deterministic model transformations.

#### Required Authority

```text
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-core.md
docs/specs/code-first-semantic-model-architecture.md
```

#### Implementation Requirements

- A transformation has a stable identifier.
- A transformation has a deterministic display name.
- A transformation receives an immutable input model and context.
- A transformation returns a new or unchanged model through a result object.
- A transformation emits diagnostics through a diagnostic sink or result.
- A transformation does not mutate the input model.
- A transformation is deterministic for the same input and options.

#### Candidate API Shape

The exact API may differ, but equivalent behavior must exist:

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

#### Validation

- Tier 1:
  - transformation contract tests;
  - no-input-mutation tests;
  - deterministic output tests.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-transformation-and-domain-derivation.md
```

#### Deferred Documentation Impact

```text
public-docs/nuget/SemanticTypeModel.Core.md
```

### Focus Area 2 — Transformation Pipeline Configuration

#### Intent

Allow users and domain packages to configure transformation sequences directly in code.

#### Required Authority

```text
docs/specs/type-model-transformation-and-domain-derivation.md
docs/architecture/code-first-domain-projection-pipeline.md
```

#### Implementation Requirements

- Pipeline order is explicit and deterministic.
- Users can add transformations.
- Users can remove transformations.
- Users can replace transformations by type or identifier.
- Users can insert transformations before or after a known transformation.
- Missing insertion targets fail deterministically.
- Duplicate transformation identifiers are rejected unless replacement is explicit.
- Pipeline execution supports stop-on-error and continue-on-error modes.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
var result = model.Transform(pipeline =>
{
    pipeline.UseCoreDefaults();
    pipeline.Replace<InferEntityKeysTransformation>(new MyKeyInference());
    pipeline.Remove<InferRelationshipsTransformation>();
    pipeline.AddAfter<NormalizeSemanticAliasesTransformation>(new MyTransformation());
});
```

#### Validation

- Tier 1:
  - add/remove/replace/insert tests;
  - duplicate ID tests;
  - missing target tests;
  - stop-on-error tests;
  - continue-on-error tests.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-transformation-and-domain-derivation.md
```

#### Deferred Documentation Impact

```text
package README configuration listing
consumer sample if transformation customization is demonstrated
```

### Focus Area 3 — Core Default Transformations

#### Intent

Provide the first small set of core transformations that derive semantic meaning from code-generated facts.

#### Required Authority

```text
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-annotations.md
docs/specs/code-first-semantic-model-architecture.md
```

#### Implementation Requirements

Implement a minimal core default pipeline where current model support allows:

```text
Normalize semantic alias attributes.
Derive core semantic type roles.
Derive keys from explicit SemanticKey metadata.
Normalize display metadata.
Validate required core semantic primitives.
Validate relationship metadata if current model support is sufficient.
```

Derivations must be explicit and diagnostic-producing. Do not infer domain-specific semantics prematurely.

#### Validation

- Tier 1:
  - alias normalization tests;
  - key derivation tests;
  - display metadata normalization tests;
  - relationship validation tests if implemented;
  - diagnostic tests for ambiguous or invalid derivation.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-transformation-and-domain-derivation.md
```

#### Deferred Documentation Impact

```text
public-docs/nuget/SemanticTypeModel.Core.md
sample docs if default transformations are demonstrated
```

### Focus Area 4 — Transformation Result and Trace Inspection

#### Intent

Make transformations inspectable for the M0027 development loop.

#### Required Authority

```text
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
```

#### Implementation Requirements

- Transformation results include the resulting model.
- Transformation results include accumulated diagnostics.
- Transformation results include an ordered trace.
- Trace entries include transformation id, display name, emitted diagnostics, and optional deterministic change summary.
- Text inspection is deterministic and suitable for snapshot tests.
- Trace output does not require a full model diff engine.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
SemanticModelTransformationResult result = model.Transform(p => p.UseCoreDefaults());

Console.WriteLine(result.ToTransformationText());
Console.WriteLine(result.Diagnostics.ToDiagnosticText());
```

#### Validation

- Tier 1:
  - trace ordering tests;
  - transformation text snapshot tests;
  - diagnostic accumulation tests;
  - no nondeterministic data tests.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-transformation-and-domain-derivation.md
```

#### Deferred Documentation Impact

```text
package README troubleshooting or development-loop section
public-docs/samples.md if shown in samples
```

### Focus Area 5 — Domain Model Derivation Contract

#### Intent

Define the shared contract that JSON Schema, EF Core, Power BI, and System.Text.Json packages will use to derive domain semantic models.

#### Required Authority

```text
docs/specs/type-model-transformation-and-domain-derivation.md
docs/architecture/code-first-domain-projection-pipeline.md
docs/specs/code-first-semantic-model-architecture.md
```

#### Implementation Requirements

- Define a reusable derivation result shape for `TDomainModel`.
- Domain derivation returns a domain semantic model, diagnostics, and trace.
- Domain derivation can configure transformation sequences in code.
- Domain derivation may reuse the core default pipeline.
- Domain derivation must not directly emit external artifacts before creating the domain semantic model.
- Domain packages own their domain model types and domain transformation defaults.
- Core owns shared contracts and base behavior.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
public sealed class SemanticDerivationResult<TDomainModel>
{
    public TDomainModel Model { get; }
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; }
    public SemanticTransformationTrace Trace { get; }
}
```

Domain example intent:

```csharp
var result = model.DeriveEfCoreModel(options =>
{
    options.UseDefaultTransformations();
    options.Transformations.Replace(new MyEfCoreTableNamingTransformation());
});
```

#### Validation

- Tier 1:
  - generic derivation result tests;
  - fake domain model derivation tests in Core tests;
  - configuration/replacement tests using a fake domain pipeline.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-transformation-and-domain-derivation.md
```

#### Deferred Documentation Impact

```text
domain package specs in later milestones
public-docs/guides/*.md
```

### Focus Area 6 — Diagnostic Conventions for Transformations

#### Intent

Ensure transformations emit useful, machine-queryable diagnostics.

#### Required Authority

```text
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/diagnostics.md
docs/specs/type-model-core.md
```

#### Implementation Requirements

Every transformation diagnostic must include where available:

```text
code
severity
message
model path
transformation id
pipeline stage or equivalent stage metadata
related model paths
```

Rules:

- Core transformations own core diagnostic IDs.
- Domain packages own domain diagnostic IDs.
- Do not reuse retired IDs.
- Do not emit silent lossy derivations.
- Diagnostics must be queryable through M0027 diagnostic helpers.
- Diagnostic text inspection must include transformation context when detailed output is enabled.

#### Validation

- Tier 1:
  - diagnostic metadata tests;
  - diagnostic ID stability tests when new IDs are added;
  - diagnostic text integration tests.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-transformation-and-domain-derivation.md
public-docs/diagnostics/*.md only if new public diagnostics are added
```

#### Deferred Documentation Impact

```text
public-docs/diagnostics.md summary if helper usage is documented later
```

## Required Acceptance Criteria

M0028 is complete when:

- A shared transformation abstraction exists.
- Transformations are deterministic and do not mutate input models.
- A configurable transformation pipeline exists.
- Pipeline configuration supports add, remove, replace, and ordered insertion.
- Pipeline execution supports stop-on-error and continue-on-error modes.
- A transformation result includes resulting model, diagnostics, and trace.
- Transformation trace inspection is deterministic.
- A minimal core default pipeline exists.
- Core default transformations include alias normalization and key derivation where model support allows.
- Invalid or ambiguous derivations emit diagnostics.
- A generic domain derivation result contract exists.
- A fake/test domain derivation pipeline proves the shared domain-derivation contract.
- Diagnostics emitted by transformations are queryable and inspectable through M0027 helpers.
- Tests cover ordering, replacement, diagnostics, deterministic trace text, and generated model usage.
- Tier 2 validation passes, or any inability to run it is explicitly reported with the exact lower-tier validation performed.
- No TBPs, issue templates, non-root README files, implementation source patches, generated code files, workflow YAML, or broad public-doc rewrites are introduced by the planning package itself.

## Validation Plan

Use the smallest validation tier that can catch the expected regression.

### Tier 1

Use focused validation for:

```text
Core transformation tests
pipeline configuration tests
diagnostic metadata tests
trace inspection tests
fake domain derivation tests
generated model transformation tests
```

Expected command shape:

```sh
./eng/test-project.sh <core-test-project>
./eng/test-filter.sh <transformation-or-derivation-filter>
./eng/check-affected.sh src/SemanticTypeModel.Core tests
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
```

This is not a release publication milestone; do not run publish validation.

## Direct Documentation Impact

The implementation should directly update:

```text
docs/specs/type-model-transformation-and-domain-derivation.md
```

Update related existing specs only when implementation changes contradict current authority:

```text
docs/specs/type-model-core.md
docs/specs/code-first-semantic-model-architecture.md
docs/specs/type-model-query-and-inspection.md
docs/architecture/code-first-domain-projection-pipeline.md
```

## Deferred Documentation Impact

Leave explicit notes for a later documentation synchronization pass covering:

```text
docs/SPECS.md index entry for the new spec
docs/MILESTONES.md index entry for M0028
README.md if the quickstart should show transformation diagnostics
public-docs/getting-started.md
public-docs/samples.md
public-docs/samples/*.md when samples demonstrate transformation customization
public-docs/nuget/SemanticTypeModel.Core.md
public-docs/diagnostics.md and public-docs/diagnostics/*.md when new diagnostics are public
public-docs/release-notes.md
```

Do not perform broad public documentation synchronization as part of this implementation milestone unless a consumer-facing behavior change directly requires it.
