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
- guardrails;
- repository naming conventions;
- initial package/project structure.

## Required Naming

The following names are mandatory:

- Solution: `SemanticTypeModel.slnx`
- Root namespace: `SemanticTypeModel`
- Package prefix: `SemanticTypeModel.*`

All projects, namespaces, and package identifiers must follow these conventions.

## Required Reading

- `docs/research/project-setup-guide-v4.md`
- `docs/research/engineering-guide-v3.md`

## Deliverables

### Repository structure

Create the repository structure defined by Project Setup Guide V4 and Engineering Guide V3.

At minimum:

```text
/docs
/docs/milestones
/docs/research
/docs/engineering
/docs/guardrails
/docs/workflows
/docs/specs
/docs/architecture
/docs/decisions
/docs/tbps
/src
/tests/unit
/tests/integration
/benchmarks
/eng
/.github/workflows
/.github/ISSUE_TEMPLATE
```

### Guide relocation

Move:

```text
/project-setup-guide-v4.md
/engineering-guide-v3.md
```

into:

```text
/docs/research/project-setup-guide-v4.md
/docs/research/engineering-guide-v3.md
```

Create:

- `docs/RESEARCH.md`
- `docs/ENGINEERING.md`
- `docs/GUARDRAILS.md`
- `docs/WORKFLOWS.md`
- `docs/SPECS.md`
- `docs/ARCHITECTURE.md`
- `docs/DECISIONS.md`
- `docs/TBPS.md`
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

- `docs/ENGINEERING.md`;
- `docs/GUARDRAILS.md`;
- `docs/TBPS.md`;
- `docs/MILESTONES.md`.

### Issue templates

Create:

```text
.github/ISSUE_TEMPLATE/bug.md
.github/ISSUE_TEMPLATE/documentation.md
.github/ISSUE_TEMPLATE/milestone-implementation.md
.github/ISSUE_TEMPLATE/release.md
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
- the guides were moved into `docs/research/`;
- the solution builds successfully;
- tests run successfully;
- benchmark project builds successfully;
- CI workflow is operational;
- no README files exist outside the repository root;
- all projects follow the required naming conventions.
