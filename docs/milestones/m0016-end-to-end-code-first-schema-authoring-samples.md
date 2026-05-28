# M0016: End-to-End Code-First Schema Authoring Samples

## Goal

Create polished, runnable, user-facing samples that prove the intended code-first authoring path from annotated C# types to the canonical semantic type model and the primary projections.

The milestone should demonstrate the expected user journey:

```text
annotated C# domain types
  -> generated or extracted TypeSchemaModel
  -> JSON Schema projection
  -> EF Core ModelBuilder application
  -> JSON Editor/UI-hint projection
```

The samples should be small enough to understand quickly, but realistic enough to expose usability gaps in semantic annotations, source generation, runtime APIs, projection configuration, diagnostics, and public documentation.

## Context

This milestone follows the annotation usability and EF Core projection hardening work. Its purpose is to make the library understandable from the perspective of a package consumer rather than from internal test fixtures.

The sample suite should become the canonical evidence that the current public API can be used as intended.

## Required Reading

- `docs/TERMINOLOGY.md`
- `docs/SPECS.md`
- `docs/MILESTONES.md`
- `docs/ENGINEERING.md`
- `docs/PUBLIC-DOCS.md`
- `docs/GUARDRAILS.md`
- `docs/guardrails/testing.md`
- `docs/guardrails/implementation.md`
- `docs/engineering/samples.md`, create if missing
- `docs/specs/dotnet-type-system-extraction.md`, if present
- `docs/specs/compile-time-generator.md`, if present
- `docs/specs/ef-core-projection.md`, if present
- `docs/specs/json-schema-projection.md`, if present
- `docs/specs/json-editor-projection.md`, if present
- `docs/research/project-setup-guide-v5.md`
- `docs/research/engineering-guide-v4.md`

## Dependencies

- M0014 Semantic Type Annotation Usability should be complete or sufficiently stable.
- M0015 EF Core Projection Hardening should be complete or sufficiently stable.
- Existing JSON Schema projection and generator packages should build.
- Existing release/package work should not be weakened by sample changes.

## Scope

Create and document a small set of end-to-end code-first samples that exercise the supported authoring model.

The samples must cover:

- semantic root type selection;
- projection-neutral semantic annotations;
- value objects;
- entities;
- enum metadata;
- validation constraints;
- nullable versus required semantics;
- simple relationships;
- source generator usage;
- runtime model access;
- JSON Schema export;
- EF Core model application;
- JSON Editor/UI hint output;
- diagnostics for one intentionally invalid scenario, where practical.

## Non-Goals

- Adding new projection capabilities beyond what samples need to show supported usage.
- Building a full demo application.
- Creating a documentation website.
- Adding local README files under `samples/`.
- Replacing unit tests or package smoke tests.
- Treating samples as performance benchmarks.
- Creating exhaustive scenario coverage for every projection.

## Deliverables

### Sample Projects

Add sample projects under `samples/`.

Recommended sample structure:

```text
samples/
  CodeFirst.Basic/
  CodeFirst.EfCore/
  CodeFirst.JsonSchema/
  CodeFirst.JsonEditor/
```

Alternative structure is acceptable if it remains small, documented, and validated.

Do not create `samples/README.md`.

### Sample Content

At minimum, the sample model should include:

- `Customer` or equivalent entity;
- primary key;
- required string property;
- optional nullable property;
- enum property with display metadata;
- value object such as `Address` or `Money`;
- one relationship such as `Customer` to `Order`;
- one constrained property such as max length, range, or format;
- at least one UI/display hint;
- at least one projection-specific override, only where needed.

### Canonical Model Output

Samples must show how to obtain the canonical `TypeSchemaModel` from annotated code.

Expected user-facing shape:

```csharp
var model = AppSemanticTypeModel.Create();
```

or the currently supported generated/provider equivalent.

The sample must avoid direct projection-specific generation from source types.

### JSON Schema Output

Provide a sample that produces Draft 2020-12 JSON Schema from the canonical model.

The sample should write output to an ignored artifact path such as:

```text
artifacts/samples/json-schema/
```

### EF Core Output

Provide a sample that applies the semantic model to EF Core using the configurable API shape from M0015.

Expected conceptual usage:

```csharp
modelBuilder.ApplySemanticTypeModel(
    AppSemanticTypeModel.Create(),
    options =>
    {
        options.DefaultSchema = "sample";
    });
```

The exact options should reflect the implemented API.

The sample does not need to connect to a real database unless the existing EF Core projection requires it.

### JSON Editor/UI Hint Output

Provide a sample that demonstrates UI-hint metadata emitted or projected from the canonical model.

This may be a JSON artifact, console output, or projection model snapshot.

### Invalid Usage Sample

Where practical, include a small intentionally invalid sample or test fixture that demonstrates diagnostic behavior for attribute misuse or unsupported projection shape.

This should not break normal sample validation.

## Engineering Requirements

### Sample Validation Command

Create or update:

```text
eng/samples.sh
```

The command must build or execute the supported samples explicitly.

It must not recursively run arbitrary sample folders.

The release gate should include `eng/samples.sh` when release readiness applies.

### Documentation

Create or update:

```text
docs/engineering/samples.md
public-docs/samples.md
public-docs/samples/code-first.md
```

The documentation must explain:

- which samples exist;
- what each sample demonstrates;
- how to run them;
- what output to expect;
- which package references are used;
- whether samples use project references or packed packages.

### Public Documentation

Update public docs when sample behavior affects user-facing usage:

```text
public-docs/getting-started.md
public-docs/concepts.md
public-docs/packages.md
public-docs/samples.md
public-docs/nuget/
```

## Implementation Notes

- Prefer samples that look like normal application code.
- Avoid internal test-only APIs in sample code.
- Keep samples short and explicit.
- Keep package references intentional.
- If samples use project references during development, document whether package-smoke tests cover packed-package consumption separately.
- Keep generated artifacts under ignored artifact paths.
- Do not add non-root README files.

## Diagnostics Requirements

If a sample demonstrates invalid usage, diagnostics must include:

- stable diagnostic ID;
- clear message;
- source location if emitted by analyzer/source generator;
- model path where applicable;
- public documentation link if supported.

## Testing Requirements

Add tests or validation coverage to ensure:

- sample projects compile;
- generated model can be created;
- JSON Schema sample emits deterministic output;
- EF Core sample applies model without throwing for supported shapes;
- JSON Editor/UI hint sample emits deterministic output;
- sample validation command fails when a sample breaks.

Avoid broad tests that duplicate projection test suites.

## Public Documentation Impact

This milestone affects public documentation.

Review and update:

- `README.md`;
- `public-docs/getting-started.md`;
- `public-docs/concepts.md`;
- `public-docs/packages.md`;
- `public-docs/samples.md`;
- `public-docs/samples/code-first.md`;
- relevant package README sources under `public-docs/nuget/`;
- `public-docs/release-notes.md`.

## Acceptance Criteria

- Code-first samples exist under `samples/`.
- Samples show annotated C# types flowing through the canonical model.
- JSON Schema, EF Core, and JSON Editor/UI-hint output are demonstrated.
- `eng/samples.sh` validates the supported samples.
- Sample documentation exists in internal and public docs.
- Public docs link to the code-first sample.
- No local README files are created under `samples/`.
- Existing `eng/check.sh` remains valid.
- Release validation includes sample validation when release readiness requires it.
- Sample output is deterministic or intentionally documented as non-deterministic.
- The milestone is indexed from `docs/MILESTONES.md`.

## Authority

This document is authoritative for:

- milestone scope;
- milestone deliverables;
- milestone implementation sequence;
- milestone acceptance criteria.

This document is not authoritative for:

- permanent feature behavior after implementation;
- architecture decisions beyond this milestone;
- projection semantics outside the milestone scope.

## Document Contract

### Related Documents

- `docs/TERMINOLOGY.md`
- `docs/MILESTONES.md`
- `docs/SPECS.md`
- `docs/ENGINEERING.md`
- `docs/PUBLIC-DOCS.md`
- `docs/GUARDRAILS.md`
- `docs/WORKFLOWS.md`
- `docs/research/project-setup-guide-v5.md`
- `docs/research/engineering-guide-v4.md`

### Must Be Updated Together

When this milestone is added or materially changed, review and update:

- `docs/MILESTONES.md`;
- related specification documents under `docs/specs/`;
- related public documentation under `public-docs/`;
- related engineering or workflow documents if validation commands change;
- release notes when public behavior changes.

