# M0027: Query and Inspection API for Code-First Semantic Models

## Status

Planned.

## Maturity Mode

Core product surface implementation for a public package set.

The repository has public packages, public samples, package README sources, and public API compatibility documentation. This milestone introduces a new public development-loop surface for querying and inspecting generated semantic models. The feature is foundational for later domain-specific semantic models and must be specified before implementation.

## Task Mode

Milestone implementation routing.

This milestone implements the M0026 query and inspection product surfaces. It does not change the architecture direction and does not require a new decision record.

Do not introduce TBPs, issue templates, workflow YAML, non-root README files, generated code, or broad public-documentation rewrites in this planning package.

## Goal

After M0027, a consumer can:

```text
Generate a semantic model from annotated code.
Query the model by CLR type or canonical string identifier.
Query types, properties, semantic primitives, annotations, relationships, and diagnostics.
Inspect deterministic text output for models and diagnostics.
Use the query and inspection APIs in tests and console development loops.
```

The core consumer loop is:

```text
Annotate types.
Run test or console program.
Inspect diagnostics and semantic text.
Assert expected model content.
Adjust annotations/configuration.
Repeat.
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
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-core.md
```

Read these only when the selected focus area touches the relevant component:

```text
docs/specs/type-schema-model.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-runtime-api.md
docs/specs/type-model-di-integration.md
docs/specs/diagnostics.md
docs/architecture/code-first-domain-projection-pipeline.md
docs/PUBLIC-DOCS.md
public-docs/samples.md
public-docs/nuget/*.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Add a core query API for canonical semantic models.
- Support type lookup by CLR type when CLR metadata is available.
- Support type lookup by canonical string identifier.
- Support property lookup by property expression and by string name.
- Support filters for semantic primitives, annotations, constraints, and model paths.
- Support diagnostic query helpers by severity, code, type, property, and model path.
- Add assertive query APIs suitable for tests.
- Add safe query APIs suitable for libraries.
- Add deterministic text inspection for models and diagnostics.
- Add inspection options for summary, normal, and detailed output.
- Ensure APIs work for generated models and loaded snapshots.
- Add short-running tests and snapshot-style deterministic output tests.
- Add or update consumer sample code only where needed to demonstrate the development loop.

### Out of Scope

- Full LINQ provider.
- Query language parser.
- Graph query language.
- Interactive debugger or visualizer.
- JSON/YAML model dump as a serialization format.
- Runtime model editing.
- Full transformation trace system.
- Domain-specific query DSLs for EF Core, Power BI, JSON Schema, or System.Text.Json.
- Domain-specific inspection formatters beyond shared extension hooks.
- Broad public documentation rewrite.
- Release publication.

## Package Boundary

The preferred package boundary is:

| Package | Responsibility |
|---|---|
| `SemanticTypeModel.Abstractions` | Shared query/inspection result types only if they are required by all packages. |
| `SemanticTypeModel.Core` | Query implementation, inspection text formatters, and diagnostic query helpers. |
| Domain packages | Optional domain-specific query and inspection extensions in later milestones. |
| `SemanticTypeModel.DotNet` | CLR metadata annotations used by typed queries. |
| `SemanticTypeModel.Generators` | Generated models must include enough metadata to support typed queries. |

Avoid placing the common query/inspection surface in `SemanticTypeModel.DotNet`, because loaded snapshots without the original codebase must still support string-based query and text inspection.

## Focus Areas

### Focus Area 1 — Query API Shape

#### Intent

Create a small, stable, test-friendly query API without inventing a broad query framework.

#### Required Authority

```text
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-core.md
docs/specs/code-first-semantic-model-architecture.md
```

#### Implementation Requirements

- Provide typed-first query helpers when CLR metadata is available.
- Provide string fallback query helpers for snapshots and low-level usage.
- Provide safe and assertive variants:
  - `Try...` / nullable / result-based APIs for library use;
  - `Require...` APIs for tests and console development.
- Preserve deterministic behavior.
- Return useful exception messages from assertive query APIs.

#### Candidate API Shape

The exact API may differ, but implementation must support equivalent behavior:

```csharp
var type = model.RequireType<Customer>();
var maybeType = model.TryGetType<Customer>(out var customerType);

var typeById = model.RequireType("global::MyApp.Customer");
var property = model.RequireProperty<Customer>(x => x.Email);
var propertyByName = model.RequireProperty("global::MyApp.Customer", "Email");
```

#### Validation

- Tier 1:
  - focused Core query tests;
  - generated model query tests;
  - snapshot/string-only query tests if snapshot loading exists.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-query-and-inspection.md
```

#### Deferred Documentation Impact

```text
package README quickstart
public-docs/samples.md if samples are updated
```

### Focus Area 2 — Semantic and Annotation Queries

#### Intent

Support queries over semantic primitives and annotation metadata while preserving string fallback.

#### Required Authority

```text
docs/specs/type-model-query-and-inspection.md
docs/specs/code-first-semantic-model-architecture.md
docs/specs/type-model-annotations.md
```

#### Implementation Requirements

- Query types and properties by semantic primitive.
- Query types and properties by annotation key and optional value.
- Support typed semantic concepts when available through public constants or semantic wrappers.
- Support raw string keys as fallback.
- Avoid hard dependency on domain packages from Core.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
var entities = model.Types().WithSemanticType("Entity");
var keys = model.PropertiesOf<Customer>().WithSemantic("Key");
var annotated = model.Types().WithAnnotation("semantic.type", "Entity");
var efAnnotated = model.Properties().WithAnnotation("efCore.primaryKey");
```

#### Validation

- Tier 1:
  - annotation query tests;
  - semantic primitive query tests;
  - deterministic ordering tests.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-query-and-inspection.md
```

#### Deferred Documentation Impact

```text
package README configuration/reference sections
public-docs/nuget/SemanticTypeModel.Core.md
```

### Focus Area 3 — Diagnostics Query Helpers

#### Intent

Make diagnostics easy to inspect and assert in tests.

#### Required Authority

```text
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-core.md
docs/specs/diagnostics.md
```

#### Implementation Requirements

- Query diagnostics by severity, code, stage, model path, type, and property.
- Provide helpers to detect and assert errors.
- Provide deterministic ordering.
- Keep diagnostics machine-queryable and human-readable.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
diagnostics.HasErrors();
diagnostics.Errors();
diagnostics.WithCode("STM5008");
diagnostics.ForType<Customer>();
diagnostics.ForProperty<Customer>(x => x.Email);
diagnostics.ThrowIfErrors();
```

#### Validation

- Tier 1:
  - diagnostic query helper tests;
  - diagnostic assertion message tests;
  - deterministic diagnostic text tests.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-query-and-inspection.md
```

#### Deferred Documentation Impact

```text
public-docs/diagnostics.md if new public helpers are documented
```

### Focus Area 4 — Model Text Inspection

#### Intent

Add deterministic human-readable text output for models.

#### Required Authority

```text
docs/specs/type-model-query-and-inspection.md
docs/specs/code-first-semantic-model-architecture.md
```

#### Implementation Requirements

- Provide model text output suitable for console debugging and snapshot tests.
- Support summary, normal, and detailed inspection levels.
- Include deterministic ordering for types, properties, relationships, constraints, annotations, and diagnostics.
- Make inspection output human-readable but not a serialization format.
- Avoid embedding timestamps, machine-specific paths, nondeterministic ordering, or culture-sensitive formatting.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
string text = model.ToSemanticText();

string detailed = model.ToSemanticText(new SemanticTextOptions
{
    Detail = SemanticTextDetail.Detailed,
    IncludeDiagnostics = true,
    IncludeAnnotations = true,
    IncludeConstraints = true,
    IncludeSource = false
});
```

#### Validation

- Tier 1:
  - model text snapshot tests;
  - deterministic output tests;
  - options coverage tests.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-query-and-inspection.md
```

#### Deferred Documentation Impact

```text
sample docs if a sample demonstrates inspection output
package README quickstart
```

### Focus Area 5 — Diagnostic Text Inspection

#### Intent

Add deterministic human-readable text output for diagnostics.

#### Required Authority

```text
docs/specs/type-model-query-and-inspection.md
docs/specs/diagnostics.md
```

#### Implementation Requirements

- Provide diagnostic text output suitable for tests and console workflows.
- Include severity, code, model path, and message.
- Include related model paths when present.
- Support normal and detailed output.
- Keep formatting stable across operating systems.

#### Candidate API Shape

Equivalent behavior is required:

```csharp
string text = diagnostics.ToDiagnosticText();

diagnostics.ThrowIfErrors(new DiagnosticTextOptions
{
    IncludeRelatedPaths = true
});
```

#### Validation

- Tier 1:
  - diagnostic text snapshot tests;
  - line ending stability tests;
  - `ThrowIfErrors` message tests.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-query-and-inspection.md
```

#### Deferred Documentation Impact

```text
public-docs/diagnostics.md
package README troubleshooting section
```

### Focus Area 6 — Generated Model and Snapshot Compatibility

#### Intent

Ensure query and inspection work for both generated models and loaded snapshots.

#### Required Authority

```text
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-compile-time-generator.md
docs/specs/code-first-semantic-model-architecture.md
```

#### Implementation Requirements

- Generated models must include stable identifiers and CLR metadata annotations needed for typed queries.
- Loaded snapshots must remain queryable by string identifiers and inspectable without CLR type access.
- Typed queries must fail with explicit guidance when CLR metadata is unavailable.
- String fallback must remain first-class.

#### Validation

- Tier 1:
  - generated model typed query test;
  - string fallback query test;
  - snapshot or synthetic no-CLR-metadata test.
- Tier 2 before completing implementation if code changes.

#### Direct Documentation Impact

```text
docs/specs/type-model-query-and-inspection.md
```

#### Deferred Documentation Impact

```text
snapshot persistence documentation when persistence is formalized
```

## Required Acceptance Criteria

M0027 is complete when:

- A public Core query API exists for canonical semantic models.
- Consumers can query types by CLR type where metadata is available.
- Consumers can query types by canonical string identifier.
- Consumers can query properties by expression where metadata is available.
- Consumers can query properties by string name.
- Consumers can filter by semantic primitive and annotation key/value.
- Consumers can query diagnostics by severity, code, model path, type, and property.
- Safe query APIs and assertive test-friendly query APIs are available.
- Model text inspection produces deterministic human-readable output.
- Diagnostic text inspection produces deterministic human-readable output.
- Summary, normal, and detailed inspection levels are supported.
- Inspection output is documented as non-serialization output.
- Query and inspection APIs work for generated models.
- String fallback query and inspection work without access to CLR types.
- Tests cover deterministic ordering and representative code-first generated models.
- Tier 2 validation passes, or any inability to run it is explicitly reported with the exact lower-tier validation performed.
- No TBPs, issue templates, non-root README files, generated code files, workflow YAML, or broad public-doc rewrites are introduced by the planning package itself.

## Validation Plan

Use the smallest validation tier that can catch the expected regression.

### Tier 1

Use focused validation for:

```text
Core query tests
Core inspection tests
diagnostic helper tests
generated model query tests
snapshot/string fallback tests
```

Expected command shape:

```sh
./eng/test-project.sh <core-test-project>
./eng/test-filter.sh <query-or-inspection-filter>
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
docs/specs/type-model-query-and-inspection.md
```

Update related existing specs only when implementation changes contradict current authority:

```text
docs/specs/type-model-core.md
docs/specs/type-model-compile-time-generator.md
docs/specs/code-first-semantic-model-architecture.md
```

## Deferred Documentation Impact

Leave explicit notes for a later documentation synchronization pass covering:

```text
docs/SPECS.md index entry for the new spec
docs/MILESTONES.md index entry for M0027
README.md quickstart if it should demonstrate query/inspection
public-docs/getting-started.md
public-docs/samples.md
public-docs/samples/*.md when samples demonstrate the new loop
public-docs/nuget/SemanticTypeModel.Core.md
public-docs/diagnostics.md
public-docs/release-notes.md
```

Do not perform broad public documentation synchronization as part of this implementation milestone unless a consumer-facing behavior change directly requires it.
