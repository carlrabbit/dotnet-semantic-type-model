# M0001 - Project and Engineering Setup

## Goal

Establish the initial repository structure, documentation model, engineering substrate, command contract, solution skeleton, and AI-agent workflow for the SemanticTypeModel repository.

The milestone must result in a fully bootstrapped repository that can be cloned, restored, built, formatted, tested, and validated through canonical `eng/` commands.

## Scope

This milestone includes:

- repository documentation structure;
- engineering substrate;
- canonical command contract;
- .NET 10 solution setup;
- central package management;
- MTP + TUnit baseline testing;
- BenchmarkDotNet baseline setup;
- CI workflow baseline;
- agent instructions;
- documentation indexes;
- engineering guidance;
- repository naming conventions;
- initial package/project structure.

## Required Naming

The following names are mandatory:

- Solution: `SemanticTypeModel.slnx`
- Root namespace: `SemanticTypeModel`
- Package prefix: `SemanticTypeModel.*`

All projects, namespaces, and package identifiers must follow these conventions.

## Implementation Router

Read only the authoritative documents needed for the focus area being implemented:

- relevant specs from `docs/specs/`;
- `docs/ENGINEERING.md` and `docs/engineering/command-contract.md` for validation-tier selection;
- `docs/PUBLIC-DOCS.md` and affected `public-docs/` pages only when the change is consumer-facing;
- architecture or decision records only when the change alters structure or rationale.

Historical research guide copies are non-authoritative references and are not required milestone reading.

## Focus Areas

Use the milestone scope to choose one or more focused implementation slices instead of treating the whole milestone as a single work item:

| Focus area | Validation tier | Documentation impact |
|---|---|---|
| Behavior or API implementation | Tier 1 during development, Tier 2 before completion | Direct when behavior is consumer-facing; otherwise update specs only when contracts change. |
| Tests and diagnostics | Tier 1 for the affected test project or diagnostic filter, Tier 2 before completion | Direct for public diagnostics; deferred only when examples require a later feature slice. |
| Public documentation, samples, or release readiness | Tier 0 for documentation checks, Tier 3 for package/release readiness | Direct for changed public docs and package README sources; record deferred docs explicitly. |

## Validation Tier

- Default implementation focus areas: Tier 1 during the inner loop, then Tier 2 before completion.
- Documentation-only focus areas: Tier 0 plus `./eng/public-docs.sh` when public documentation changes.
- Packaging or release focus areas: Tier 3 or Tier 4 as described by the release-readiness documents.

## Deliverables

### Repository structure

Create the repository structure defined by Project Setup Guide V4 and Engineering Guide V3.

At minimum:

```text
/docs
/docs/milestones
/docs/engineering
/docs/workflows
/docs/specs
/docs/architecture
/docs/decisions
/src
/tests/unit
/tests/integration
/benchmarks
/eng
/.github/workflows
```

### Guide relocation

Move:

```text
historical setup and engineering guide copies
```

into:

```text
historical research references
```

Create:

- `docs/RESEARCH.md`
- `docs/ENGINEERING.md`
- `docs/WORKFLOWS.md`
- `docs/SPECS.md`
- `docs/ARCHITECTURE.md`
- `docs/DECISIONS.md`
- `docs/TERMINOLOGY.md`

### Solution and projects

Create:

```text
SemanticTypeModel.slnx
```

Create baseline projects:

```text
src/SemanticTypeModel.Abstractions
src/SemanticTypeModel.Core
src/SemanticTypeModel.JsonSchema
tests/unit/SemanticTypeModel.Core.Tests.Unit
tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit
benchmarks/SemanticTypeModel.Benchmarks
```

### Engineering substrate

Create:

```text
eng/restore.sh
eng/build.sh
eng/test.sh
eng/format.sh
eng/check.sh
eng/benchmark.sh
eng/common.sh
```

Commands must follow Engineering Guide V3.

### Shared build configuration

Create:

```text
global.json
Directory.Build.props
Directory.Packages.props
.editorconfig
NuGet.config
```

Requirements:

- target framework `net10.0`;
- nullable enabled;
- implicit usings enabled;
- warnings as errors enabled;
- deterministic builds enabled;
- central package management enabled.

### Testing baseline

Use:

- Microsoft Testing Platform;
- TUnit;
- short-running tests by default.

`eng/test.sh` must exclude long-running and E2E tests.

### Benchmark baseline

Create a working BenchmarkDotNet project.

Benchmarks must not execute during normal validation.

### CI baseline

Create:

```text
.github/workflows/ci.yml
```

The workflow must:

- restore;
- build;
- run short-running tests;
- verify formatting.

The workflow must invoke `./eng/check.sh` instead of duplicating repository logic.

### Agent instructions

Create:

```text
AGENTS.md
.github/copilot-instructions.md
```

Instructions must reference:

- `docs/ENGINEERING.md`;;;
- `docs/MILESTONES.md`.

### Issue templates

Create:

```text
```

## Non-Goals

This milestone does not implement:

- semantic type model behavior;
- JSON Schema import/export;
- transformation pipelines;
- EF Core integration;
- Power BI integration;
- source generators.

## Validation

The following commands must succeed:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/check.sh
```

## Exit Criteria

The milestone is complete when:

- the repository structure exists;
- all required documentation indexes exist;
- historical guides were retained as non-authoritative research references;
- the solution builds successfully;
- tests run successfully;
- benchmark project builds successfully;
- CI workflow is operational;
- no README files exist outside the repository root;
- all projects follow the required naming conventions.
