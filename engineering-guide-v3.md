# Engineering Guide V3

## Status

Authoritative engineering guide for the default .NET repository profile.

## Purpose

This guide defines an opinionated, AI-agent-friendly engineering setup for a professional repository.

The default stack is:

- .NET 10
- Microsoft Testing Platform (MTP)
- TUnit
- BenchmarkDotNet
- Bun
- Biome

Optional modules cover:

- Blazor
- Playwright
- TypeScript runtime/browser tooling
- NuGet packaging
- samples
- GitHub Pages

This guide defines the concrete engineering substrate:

- repository command contract;
- build, test, format, benchmark, package, release, and site commands;
- toolchain pinning;
- project layout;
- engineering building blocks;
- test classification;
- optional modules;
- agent validation expectations.

This guide is referenced by Project Setup Guide V4 through:

- `docs/ENGINEERING.md`
- `docs/engineering/dotnet.md`
- `docs/guardrails/testing.md`
- `docs/guardrails/implementation.md`
- language-specific guardrails.

## Relationship to Project Setup Guide V4

Project Setup Guide V4 defines the repository knowledge model.

This guide defines the concrete engineering implementation profile.

In short:

```text
Project Setup Guide V4 tells the repository how to organize knowledge.
Engineering Guide V3 tells the repository how to build, test, validate, and package.
```

## README rule

Only the root `README.md` is allowed.

Do not create local README files in:

```text
eng/
samples/
site/
tools/
docs/**/
```

Use named documents under `docs/engineering/` instead:

```text
docs/ENGINEERING.md
docs/engineering/dotnet.md
docs/engineering/command-contract.md
docs/engineering/building-blocks.md
docs/engineering/samples.md
docs/engineering/site.md
docs/engineering/typescript-tools.md
docs/engineering/packaging.md
```

---

# 1. Core principles

## 1.1 Agent-executable over descriptive

Instructions must be executable or directly checkable.

Prefer:

```text
Run ./eng/check.sh and ensure it exits with code 0.
```

Avoid:

```text
Make sure the project looks clean.
```

## 1.2 One canonical command per workflow

Agents must not guess which command to run.

Each repository should expose these canonical commands:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh
./eng/check.sh
./eng/benchmark.sh
```

Optional modules may add:

```text
./eng/e2e.sh
./eng/frontend-check.sh
./eng/frontend-format.sh
./eng/package.sh
./eng/publish.sh
./eng/site-build.sh
./eng/samples.sh
```

## 1.3 Building blocks, not one giant template

Repositories start small and add capabilities by applying building blocks.

A block must define:

- block ID;
- purpose;
- when to apply;
- files to create or modify;
- packages or tools to add;
- commands to expose;
- validation command;
- done criteria.

## 1.4 Tooling must be pinned or explicit

The repository must pin or explicitly define:

- .NET SDK version through `global.json`;
- package versions through central package management;
- JavaScript/TypeScript tooling through `package.json`, `bun.lock`, and `biome.json` when the frontend/tooling module is used.

## 1.5 Optional means absent by default

Blazor, Playwright, TypeScript, NuGet packaging, samples, and GitHub Pages are optional modules.

Do not add them unless the repository needs them.

---

# 2. Required repository layout

A repository generated from the base blocks should use this layout:

```text
/
├─ .config/
│  └─ dotnet-tools.json
├─ .github/
│  ├─ workflows/
│  ├─ instructions/
│  └─ copilot-instructions.md
├─ artifacts/
│  └─ .gitkeep
├─ docs/
│  ├─ ENGINEERING.md
│  ├─ GUARDRAILS.md
│  ├─ WORKFLOWS.md
│  ├─ engineering/
│  │  ├─ dotnet.md
│  │  ├─ command-contract.md
│  │  ├─ building-blocks.md
│  │  ├─ optional-modules.md
│  │  ├─ packaging.md
│  │  ├─ samples.md
│  │  ├─ site.md
│  │  └─ typescript-tools.md
│  ├─ guardrails/
│  │  ├─ testing.md
│  │  ├─ implementation.md
│  │  └─ languages/
│  │     ├─ dotnet.md
│  │     └─ typescript.md
│  └─ workflows/
├─ eng/
│  ├─ restore.sh
│  ├─ build.sh
│  ├─ test.sh
│  ├─ format.sh
│  ├─ check.sh
│  ├─ benchmark.sh
│  ├─ common.sh
│  ├─ ci/
│  ├─ local/
│  └─ templates/
├─ src/
├─ tests/
│  ├─ unit/
│  └─ integration/
├─ benchmarks/
├─ samples/
├─ site/
├─ packages/
├─ tools/
├─ .editorconfig
├─ .gitignore
├─ AGENTS.md
├─ Directory.Build.props
├─ Directory.Packages.props
├─ NuGet.config
├─ global.json
└─ README.md
```

Optional modules may add:

```text
tests/e2e/
web/
package.json
bun.lock
biome.json
tsconfig.json
playwright.config.ts
```

## Folder ownership

| Path | Purpose |
|---|---|
| `src/` | Production source projects. |
| `tests/unit/` | Fast unit tests. No network, no database, no browser. |
| `tests/integration/` | Integration tests. May use databases, containers, test hosts, or real infrastructure substitutes. |
| `tests/e2e/` | Optional browser/system tests. Requires Playwright block. |
| `benchmarks/` | BenchmarkDotNet projects only. Not part of normal test execution. |
| `eng/` | Canonical repository commands and reusable engineering scripts. Agents must use these. |
| `eng/ci/` | CI-only helper scripts or workflow fragments. |
| `eng/local/` | Local developer utilities not required in CI. |
| `eng/templates/` | Reusable file templates for generators or agents. |
| `packages/` | Local NuGet packages or packaging output when package publishing is enabled. |
| `samples/` | Small runnable usage examples. No local README. Document in `docs/engineering/samples.md`. |
| `site/` | Optional static project website source for GitHub Pages. No local README. Document in `docs/engineering/site.md`. |
| `tools/` | Repository-local helper tools, generators, scripts, and development utilities. No local README. |
| `docs/` | Human- and agent-readable engineering documentation. |
| `artifacts/` | Local/generated outputs. Usually ignored except for `.gitkeep`. |

---

# 3. `eng/` folder design

The `eng/` folder is the canonical engineering entry point for both humans and AI agents.

The goal is:

- one stable location for engineering operations;
- minimal command ambiguity;
- reusable script composition;
- deterministic CI behavior;
- easy discoverability for agents.

## 3.1 Script layering

Use the following model:

```text
eng/
  common.sh         shared helpers
  restore.sh        canonical entry point
  build.sh          canonical entry point
  test.sh           canonical entry point
  format.sh         canonical entry point
  check.sh          canonical entry point
  benchmark.sh      canonical entry point

  ci/
    *.sh            CI-only helpers

  local/
    *.sh            optional local utilities

  templates/
    *               reusable templates
```

Top-level scripts are the public engineering API.

Agents and CI should prefer only these scripts:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/check.sh
./eng/format.sh
./eng/benchmark.sh
```

Nested scripts are implementation details.

## 3.2 Canonical script rules

Top-level scripts should:

- be short;
- compose lower-level helpers;
- avoid duplicated logic;
- avoid hidden side effects;
- fail fast;
- use deterministic command ordering.

Prefer:

```sh
./eng/restore.sh
./eng/build.sh
./eng/test.sh
```

Avoid duplicated restore/build/test logic in CI YAML or issue instructions.

## 3.3 Shared helper example

`eng/common.sh`:

```sh
#!/usr/bin/env sh
set -eu

require_command() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "Required command not found: $1" >&2
    exit 1
  }
}
```

## 3.4 Script extension rules

When adding a new capability:

- prefer extending an existing canonical script first;
- add a new top-level script only if the workflow is conceptually separate;
- avoid creating many overlapping commands.

Good examples:

```text
eng/e2e.sh
eng/package.sh
eng/publish.sh
eng/site-build.sh
eng/samples.sh
```

Bad examples:

```text
eng/test-all.sh
eng/test-fast.sh
eng/test-fast-no-db.sh
eng/test-local.sh
```

## 3.5 CI behavior

CI workflows should call `eng/` scripts instead of embedding repository logic directly.

Prefer:

```yaml
run: ./eng/check.sh
```

Avoid:

```yaml
run: |
  dotnet restore
  dotnet build
  dotnet test
```

## 3.6 Portability rules

Scripts should:

- use POSIX shell where practical;
- avoid unnecessary Bash-specific features;
- avoid machine-local assumptions;
- work in Linux containers, GitHub Actions, and ChromeOS Linux environments.

If PowerShell support is required, add parallel `.ps1` wrappers while preserving the same command contract.

---

# 4. Required command contract

## `eng/restore.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet restore

if [ -f package.json ]; then
  bun install --frozen-lockfile
fi
```

## `eng/build.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet build --no-restore
```

## `eng/test.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet test --no-build --configuration Debug --filter "TestCategory!=Slow&TestCategory!=E2E"
```

If the selected test framework or adapter does not use `TestCategory`, the repository must document and implement the equivalent filter.

## `eng/format.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet format

if [ -f biome.json ]; then
  bun run format
fi
```

## `eng/check.sh`

```sh
#!/usr/bin/env sh
set -eu

./eng/restore.sh
./eng/build.sh
./eng/test.sh

dotnet format --verify-no-changes

if [ -f biome.json ]; then
  bun run check
fi
```

## `eng/benchmark.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet run --configuration Release --project benchmarks/PROJECT_NAME.Benchmarks
```

Replace `PROJECT_NAME.Benchmarks` with the actual benchmark project name when the benchmark block is applied.

---

# 5. Building block overview

| Block | Name | Required | Purpose |
|---|---|---:|---|
| BB00 | Repository Base | Yes | Common repository skeleton and command contract. |
| BB01 | .NET Solution | Yes | Solution, source project, test project structure. |
| BB02 | Shared Build Configuration | Yes | `global.json`, `Directory.Build.props`, central package management. |
| BB03 | EditorConfig and C# Style | Yes | Opinionated formatting, analyzers, and style rules. |
| BB04 | MTP + TUnit Unit Tests | Yes | Fast unit testing foundation. |
| BB05 | Test Guardrails | Yes | Fast/slow/integration/e2e separation. |
| BB06 | BenchmarkDotNet | Recommended | Dedicated benchmark project. |
| BB07 | GitHub Actions CI | Recommended | Build/test/check automation. |
| BB08 | Agent Instructions | Yes | Repository-local operating instructions for AI agents. |
| BB09 | Bun + Biome | Optional | TypeScript/JavaScript tooling. |
| BB10 | Blazor Module | Optional | Blazor application project. |
| BB11 | Playwright E2E Module | Optional | Browser automation tests. |
| BB12 | TypeScript Runtime Tools | Optional | Self-authored TypeScript scripts/runtime utilities. |
| BB13 | Documentation Skeleton | Yes | Minimal docs required for maintainability. |
| BB14 | NuGet Packaging | Optional | NuGet package generation and publishing conventions. |
| BB15 | Samples | Optional | Runnable examples that demonstrate supported usage patterns. |
| BB16 | GitHub Copilot | Optional | Repository instructions for Copilot Chat, coding agent, and code review. |
| BB17 | OpenAI Codex | Optional | Repository instructions and command contracts optimized for Codex. |
| BB18 | GitHub Pages Website | Optional | Static project website deployed through GitHub Pages. |

---

# 6. BB00 — Repository Base

## Purpose

Create the repository skeleton and canonical engineering scripts.

## Apply when

Always.

## Files to create

```text
.gitignore
README.md
AGENTS.md
eng/restore.sh
eng/build.sh
eng/test.sh
eng/format.sh
eng/check.sh
eng/benchmark.sh
artifacts/.gitkeep
docs/ENGINEERING.md
docs/engineering/dotnet.md
docs/engineering/command-contract.md
docs/guardrails/testing.md
docs/guardrails/implementation.md
```

Do not create local README files outside the root repository `README.md`.

## Required conventions

- Shell scripts in `eng/` are executable.
- Agents must use `eng/check.sh` before declaring work complete.
- `artifacts/` is used for generated local output and is ignored except for `.gitkeep`.
- `README.md` lists canonical commands and links to `docs/ENGINEERING.md`.

## Example `.gitignore`

```gitignore
# .NET
bin/
obj/
TestResults/
*.user
*.suo
*.rsuser

# BenchmarkDotNet
BenchmarkDotNet.Artifacts/

# Local artifacts
artifacts/*
!artifacts/.gitkeep

# Packages
packages/*
!packages/.gitkeep

# Bun / JS / TS
node_modules/
bun.lockb

# IDE
.vs/
.vscode/.ropeproject
.idea/

# OS
.DS_Store
Thumbs.db
```

If Bun creates `bun.lock`, commit it. If Bun creates `bun.lockb`, commit it only if this is the configured lockfile format for the selected Bun version.

## Validation

```sh
./eng/check.sh
```

## Done criteria

- Required files exist.
- Scripts are executable.
- Root `README.md` lists commands.
- No non-root README files exist.
- `eng/check.sh` exists, even if later blocks fill in its full behavior.

---

# 7. BB01 — .NET Solution

## Purpose

Create the .NET solution and project structure.

## Apply when

Always.

## Files/projects to create

Example for repository name `Example.Project`:

```text
Example.Project.slnx
src/Example.Project/Example.Project.csproj
tests/unit/Example.Project.Tests.Unit/Example.Project.Tests.Unit.csproj
tests/integration/Example.Project.Tests.Integration/Example.Project.Tests.Integration.csproj
```

Use `.slnx` when supported by the installed .NET SDK and tooling. Use `.sln` only when required by external tooling.

## Example commands

```sh
dotnet new sln --name Example.Project
mkdir -p src tests/unit tests/integration benchmarks

dotnet new classlib --name Example.Project --output src/Example.Project

dotnet sln add src/Example.Project/Example.Project.csproj
```

If the project is an application instead of a library, replace `classlib` with the appropriate template.

## Required conventions

- Production projects live under `src/`.
- Unit test projects live under `tests/unit/`.
- Integration test projects live under `tests/integration/`.
- Project names include their role.
- Test projects reference the production projects they test.

## Validation

```sh
dotnet build
```

## Done criteria

- Solution exists.
- At least one production project exists.
- At least one unit test project exists.
- Solution builds.

---

# 8. BB02 — Shared Build Configuration

## Purpose

Centralize .NET SDK, build, analyzer, and package configuration.

## Apply when

Always.

## Files to create

```text
global.json
Directory.Build.props
Directory.Packages.props
.config/dotnet-tools.json
```

## Example `global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

Update the SDK version to the exact .NET 10 SDK used by the repository.

## Example `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>Recommended</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
    <Deterministic>true</Deterministic>
  </PropertyGroup>

  <PropertyGroup Condition="$(MSBuildProjectName.Contains('.Tests.'))">
    <IsTestProject>true</IsTestProject>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

## Example `Directory.Packages.props`

```xml
<Project>
  <ItemGroup>
    <PackageVersion Include="TUnit" Version="0.0.0" />
    <PackageVersion Include="TUnit.Assertions" Version="0.0.0" />
    <PackageVersion Include="Microsoft.Testing.Platform" Version="0.0.0" />
    <PackageVersion Include="BenchmarkDotNet" Version="0.0.0" />
  </ItemGroup>
</Project>
```

Replace `0.0.0` with current approved versions during repository creation.

## Required conventions

- Package versions must be defined centrally.
- Project files must not contain inline package versions unless justified.
- SDK version must be pinned.
- Production code treats warnings as errors.

## Validation

```sh
dotnet restore
dotnet build
```

## Done criteria

- SDK is pinned.
- Central package management is enabled.
- Build properties apply to all projects.
- Restore and build succeed.

---

# 9. BB03 — EditorConfig and C# Style

## Purpose

Provide concrete formatting and analyzer rules so agents do not infer style from examples.

## Apply when

Always.

## File to create

```text
.editorconfig
```

## Example `.editorconfig`

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space

[*.cs]
indent_size = 4

# C# language style
csharp_style_namespace_declarations = file_scoped:warning
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion
csharp_style_expression_bodied_methods = false:suggestion
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_accessors = true:suggestion
csharp_style_prefer_null_check_over_type_check = true:suggestion
csharp_prefer_braces = true:warning
csharp_style_prefer_primary_constructors = true:suggestion

# .NET style
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_null_propagation = true:suggestion

# Analyzer severity baseline
dotnet_analyzer_diagnostic.category-Style.severity = warning
dotnet_analyzer_diagnostic.category-Performance.severity = warning
dotnet_analyzer_diagnostic.category-Reliability.severity = warning
dotnet_analyzer_diagnostic.category-Security.severity = warning

[*.{json,yml,yaml,md,ts,tsx,js,jsx,css,html}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

## Required conventions

- `.editorconfig` is authoritative for C# style.
- Agents must run `dotnet format --verify-no-changes` before completion as part of `eng/check.sh`.
- Do not rely on IDE defaults.

## Validation

```sh
dotnet format --verify-no-changes
```

## Done criteria

- `.editorconfig` exists.
- `dotnet format --verify-no-changes` passes.

---

# 10. BB04 — MTP + TUnit Unit Tests

## Purpose

Create the default test foundation using Microsoft Testing Platform and TUnit.

## Apply when

Always.

## Files/projects to create or modify

```text
tests/unit/Example.Project.Tests.Unit/Example.Project.Tests.Unit.csproj
```

## Example test project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="TUnit" />
    <PackageReference Include="TUnit.Assertions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../src/Example.Project/Example.Project.csproj" />
  </ItemGroup>
</Project>
```

## Example test

```csharp
using TUnit.Assertions;
using TUnit.Core;

namespace Example.Project.Tests.Unit;

public sealed class ExampleTests
{
    [Test]
    public async Task Example_should_be_true()
    {
        var value = true;

        await Assert.That(value).IsTrue();
    }
}
```

## Required conventions

- Unit tests must be fast.
- Unit tests must not use network, real database, browser automation, or sleeps.
- Test names should describe observable behavior.
- Tests must be deterministic.
- Avoid broad generated tests that assert implementation details.

## Validation

```sh
dotnet test tests/unit/Example.Project.Tests.Unit/Example.Project.Tests.Unit.csproj
```

## Done criteria

- At least one unit test exists.
- Unit tests run through `dotnet test`.
- Unit test project participates in `eng/test.sh`.

---

# 11. BB05 — Test Guardrails

## Purpose

Prevent agents from creating slow, broad, or operationally expensive tests by default.

## Apply when

Always.

## Test categories

| Category | Default run | Description |
|---|---:|---|
| Unit | Yes | Fast, isolated tests. |
| Integration | Optional in local loop | Uses database, filesystem, test server, or containers. |
| Slow | No | Expensive tests not suitable for normal agent iterations. |
| E2E | No | Browser/system tests. |
| Benchmark | Never via test command | BenchmarkDotNet only. |

## Required rules

- `eng/test.sh` runs fast tests only.
- Integration tests must have their own command or documented filter.
- E2E tests must not run as part of normal `dotnet test` unless explicitly requested.
- Benchmarks must never be represented as tests.
- Agents must not add sleeps to tests unless unavoidable and documented.
- Agents must not create tests that depend on test execution order.

## Required documentation

Update:

```text
docs/guardrails/testing.md
docs/engineering/command-contract.md
docs/workflows/test-short.md
docs/workflows/test-long.md
```

## Validation

```sh
./eng/test.sh
```

## Done criteria

- Test categories are documented.
- Default test command excludes slow/e2e work.
- Benchmark policy is documented.
- Long-running tests are not part of normal agent validation.

---

# 12. BB06 — BenchmarkDotNet

## Purpose

Add performance measurement without polluting the test suite.

## Apply when

Recommended for libraries, algorithms, serialization, parsers, graph processing, data structures, or performance-sensitive services.

## Files/projects to create

```text
benchmarks/Example.Project.Benchmarks/Example.Project.Benchmarks.csproj
benchmarks/Example.Project.Benchmarks/Program.cs
```

## Example project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Example.Project/Example.Project.csproj" />
  </ItemGroup>
</Project>
```

## Example `Program.cs`

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<ExampleBenchmark>();

[MemoryDiagnoser]
public class ExampleBenchmark
{
    [Benchmark]
    public int Baseline()
    {
        return 42;
    }
}
```

## Required conventions

- Benchmarks run in Release configuration.
- Benchmarks are not part of `eng/test.sh`.
- Benchmark output is written to ignored artifacts.
- Benchmark projects must not contain correctness assertions as their primary purpose.

## Validation

```sh
./eng/benchmark.sh
```

## Done criteria

- Benchmark project exists.
- Benchmark command is documented.
- Normal test command does not execute benchmarks.

---

# 13. BB07 — GitHub Actions CI

## Purpose

Provide hosted validation for build, test, formatting, and optional frontend checks.

## Apply when

Recommended for every repository hosted on GitHub.

## Files to create

```text
.github/workflows/ci.yml
docs/workflows/build.md
docs/workflows/test-short.md
```

## Example workflow

```yaml
name: ci

on:
  pull_request:
  push:
    branches:
      - main

jobs:
  check:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Setup Bun
        if: hashFiles('package.json') != ''
        uses: oven-sh/setup-bun@v2

      - name: Check
        run: ./eng/check.sh
```

## Required conventions

- CI must use the same commands as local development.
- CI must not invent separate build logic.
- Optional module setup must be conditional.
- Workflow intent must be documented in `docs/workflows/`.

## Validation

CI passes on a clean checkout.

## Done criteria

- Workflow exists.
- Workflow uses `./eng/check.sh`.
- Workflow supports repositories with and without Bun tooling.
- Workflow documentation exists.

---

# 14. BB08 — Agent Instructions

## Purpose

Provide local operating rules for AI agents.

## Apply when

Always.

## Files to create or modify

```text
AGENTS.md
.github/copilot-instructions.md
```

## Required `AGENTS.md` rules

```md
# Agent Instructions

## Required Reading

- docs/TERMINOLOGY.md
- docs/GUARDRAILS.md
- docs/ENGINEERING.md
- docs/engineering/dotnet.md
- docs/TBPS.md
- relevant specs
- relevant guardrails
- relevant workflows

## Required Workflow

Before completing implementation work, run:

```sh
./eng/check.sh
```

If this command fails, fix the failure or document exactly why it could not be fixed.

## Repository Rules

- Do not add README files outside the root README.md.
- Use eng/ scripts instead of inventing commands.
- Do not add new root-level folders without updating documentation.
- Do not add package versions directly to project files. Use Directory.Packages.props.
- Do not use npm. Use Bun for JavaScript/TypeScript tooling.
- Do not add ESLint or Prettier. Use Biome unless explicitly instructed otherwise.
- Do not add slow tests to the default test path.
- Do not run benchmarks during normal validation.
- Do not introduce Playwright unless the Playwright building block is applied.
- Prefer small, vertical changes over broad rewrites.
- Preserve the command contract under eng/.
```

## Validation

Manual review.

## Done criteria

- `AGENTS.md` exists.
- Rules match installed building blocks.
- No command contradicts `README.md`, `docs/ENGINEERING.md`, or guardrails.

---

# 15. BB09 — Bun + Biome

## Purpose

Add JavaScript/TypeScript tooling with minimal moving parts.

## Apply when

Apply when the repository needs:

- TypeScript runtime scripts;
- browser TypeScript assets;
- Blazor JavaScript interop source files;
- Playwright tests;
- frontend linting/formatting.

Do not apply for pure .NET repositories.

## Files to create

```text
package.json
biome.json
tsconfig.json
docs/guardrails/languages/typescript.md
```

## Example `package.json`

```json
{
  "private": true,
  "type": "module",
  "scripts": {
    "check": "biome check . && bun run typecheck",
    "format": "biome check --write .",
    "typecheck": "tsc --noEmit",
    "test": "bun test"
  },
  "devDependencies": {
    "@biomejs/biome": "0.0.0",
    "typescript": "0.0.0"
  }
}
```

Use approved pinned versions instead of `0.0.0` when creating a production repository.

## Example `biome.json`

```json
{
  "$schema": "https://biomejs.dev/schemas/2.0.0/schema.json",
  "files": {
    "includes": ["**", "!artifacts", "!bin", "!obj", "!BenchmarkDotNet.Artifacts"]
  },
  "formatter": {
    "enabled": true,
    "indentStyle": "space",
    "indentWidth": 2,
    "lineWidth": 100
  },
  "linter": {
    "enabled": true,
    "rules": {
      "recommended": true
    }
  },
  "assist": {
    "enabled": true,
    "actions": {
      "source": {
        "organizeImports": "on"
      }
    }
  }
}
```

## Example `tsconfig.json`

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true,
    "verbatimModuleSyntax": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true
  },
  "include": ["tools/**/*.ts", "web/**/*.ts", "tests/e2e/**/*.ts"]
}
```

## Required conventions

- Use Bun, not npm.
- Use Biome, not ESLint/Prettier.
- Commit the Bun lockfile.
- Keep TypeScript optional and scoped.
- Prefer self-authored TypeScript over framework-heavy build chains.

## Validation

```sh
bun install --frozen-lockfile
bun run check
```

## Done criteria

- Bun install succeeds.
- Biome check passes.
- TypeScript typecheck passes.
- `eng/check.sh` invokes `bun run check` when `biome.json` exists.

---

# 16. BB10 — Blazor Module

## Purpose

Add a Blazor application while keeping frontend tooling optional.

## Apply when

Apply when the repository needs a web UI implemented primarily in .NET/Blazor.

## Files/projects to create

```text
src/Example.Project.Web/Example.Project.Web.csproj
src/Example.Project.Web/Program.cs
src/Example.Project.Web/Components/
```

## Example command

```sh
dotnet new blazor --name Example.Project.Web --output src/Example.Project.Web
```

## Required conventions

- Blazor is the primary UI framework when this block is applied.
- Do not add a separate SPA framework unless explicitly required.
- JavaScript interop code should be small, typed, and isolated.
- If TypeScript is used for browser interop, also apply BB09.
- If browser tests are needed, also apply BB11.
- Document runtime usage in `docs/engineering/dotnet.md` or a named engineering document, not in a local README.

## Validation

```sh
dotnet build src/Example.Project.Web/Example.Project.Web.csproj
```

## Done criteria

- Blazor project builds.
- Root README links to the engineering documentation.
- Optional TypeScript usage is documented if present.

---

# 17. BB11 — Playwright E2E Module

## Purpose

Add explicit browser/system testing.

## Apply when

Apply only when the repository contains a UI or browser-observable workflow that requires real browser testing.

## Files/projects to create

Choose one of the following approaches and document it.

Option A — .NET Playwright tests:

```text
tests/e2e/Example.Project.Tests.E2E/Example.Project.Tests.E2E.csproj
```

Option B — TypeScript Playwright tests:

```text
tests/e2e/playwright.config.ts
tests/e2e/*.spec.ts
```

For this repository style, prefer Option B only if TypeScript browser tooling is already present.

## Required conventions

- E2E tests are opt-in.
- E2E tests must not run in `eng/test.sh`.
- E2E tests run through `eng/e2e.sh`.
- E2E tests should be few and high-value.
- Avoid testing every UI detail through Playwright.

## Example `eng/e2e.sh`

```sh
#!/usr/bin/env sh
set -eu

bunx playwright test
```

## Validation

```sh
./eng/e2e.sh
```

## Done criteria

- E2E command exists.
- E2E tests are excluded from default test command.
- Browser installation/setup is documented in `docs/engineering/`.

---

# 18. BB12 — TypeScript Runtime Tools

## Purpose

Support self-authored TypeScript scripts or runtime utilities without adopting a full frontend stack.

## Apply when

Apply when the repository needs TypeScript for:

- graph layout processing;
- code generation;
- JSON/schema transformations;
- browser-adjacent asset generation;
- local development utilities.

## Files to create

```text
tools/ts/
docs/engineering/typescript-tools.md
```

Also apply BB09.

Do not create `tools/ts/README.md`.

## Example script

`tools/ts/layout.ts`:

```ts
export interface LayoutInput {
  readonly nodes: readonly { readonly id: string }[];
  readonly edges: readonly { readonly source: string; readonly target: string }[];
}

export interface LayoutOutput {
  readonly nodes: readonly { readonly id: string; readonly x: number; readonly y: number }[];
}

export function createTrivialLayout(input: LayoutInput): LayoutOutput {
  return {
    nodes: input.nodes.map((node, index) => ({
      id: node.id,
      x: index * 100,
      y: 0
    }))
  };
}
```

## Example package script

```json
{
  "scripts": {
    "tools:layout": "bun run tools/ts/layout.ts"
  }
}
```

## Required conventions

- TypeScript tools must have explicit inputs and outputs.
- Prefer pure functions and file-based boundaries.
- Avoid hidden global state.
- Avoid long-running watchers unless explicitly needed.
- Heavy libraries such as graph layout engines must be isolated behind small adapter modules.

## Validation

```sh
bun run check
bun run tools:layout
```

## Done criteria

- TypeScript tool code is typechecked.
- Tool scripts are documented in `docs/engineering/typescript-tools.md`.
- Heavy dependencies are isolated behind adapters.

---

# 19. BB13 — Documentation Skeleton

## Purpose

Provide the minimum documentation needed for maintainable human and agent work.

## Apply when

Always.

## Files to create

```text
README.md
docs/ENGINEERING.md
docs/GUARDRAILS.md
docs/guardrails/testing.md
docs/guardrails/implementation.md
docs/engineering/dotnet.md
docs/engineering/command-contract.md
AGENTS.md
.github/copilot-instructions.md
```

## Root README required content

```md
# PROJECT_NAME

## Requirements

- .NET 10 SDK
- Bun, only if TypeScript tooling is enabled

## Restore

```sh
./eng/restore.sh
```

## Build

```sh
./eng/build.sh
```

## Test

```sh
./eng/test.sh
```

## Check before committing

```sh
./eng/check.sh
```

## Benchmarks

```sh
./eng/benchmark.sh
```

## Documentation

- docs/ENGINEERING.md
- docs/GUARDRAILS.md
- docs/TERMINOLOGY.md
```

## `docs/ENGINEERING.md` required content

- repository structure;
- command contract;
- package management policy;
- formatting policy;
- optional module policy;
- links to engineering subdocuments.

## Done criteria

- Required docs exist.
- Docs do not contradict scripts.
- Docs mention optional modules only when installed or clearly marked optional.
- No non-root README files exist.

---

# 20. BB14 — NuGet Packaging

## Purpose

Provide a standardized NuGet packaging and publishing setup.

## Apply when

Apply when the repository produces reusable libraries distributed as NuGet packages.

Do not apply for application-only repositories unless internal package publishing is explicitly required.

## Files to create or modify

```text
NuGet.config
docs/engineering/packaging.md
eng/package.sh
eng/publish.sh
packages/.gitkeep
```

## Example `NuGet.config`

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

## Example packaging properties

Add to `Directory.Build.props` or package projects:

```xml
<PropertyGroup>
  <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
  <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  <RepositoryType>git</RepositoryType>
  <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

## Example `eng/package.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet pack --configuration Release --no-build --output ./packages
```

## Example `eng/publish.sh`

```sh
#!/usr/bin/env sh
set -eu

: "${NUGET_API_KEY:?NUGET_API_KEY must be set}"

for package in ./packages/*.nupkg; do
  dotnet nuget push "$package" \
    --api-key "$NUGET_API_KEY" \
    --source "https://api.nuget.org/v3/index.json" \
    --skip-duplicate
done
```

## Required conventions

- Packages are generated only through `eng/package.sh`.
- Publishing is explicit and never part of normal CI validation.
- Package output goes to `packages/`.
- Package metadata should be centralized where practical.
- Public packages should include source link and symbol packages.
- Repositories producing multiple packages should document package ownership.

## Example package metadata

```xml
<PropertyGroup>
  <PackageId>Example.Project</PackageId>
  <Authors>Example</Authors>
  <Description>Example package.</Description>
  <PackageTags>example dotnet</PackageTags>
  <RepositoryUrl>https://github.com/example/example-project</RepositoryUrl>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
</PropertyGroup>
```

## Validation

```sh
./eng/package.sh
```

## Done criteria

- Package generation succeeds.
- Package output exists under `packages/`.
- Packaging commands are documented.
- Publishing requires explicit credentials.

---

# 21. BB15 — Samples

## Purpose

Add a `samples/` area for small, runnable examples that demonstrate how the repository is intended to be used.

Samples are not tests, benchmarks, or production applications. They are executable documentation.

## Apply when

Apply when the repository exposes:

- a reusable library;
- a NuGet package;
- public APIs;
- a Blazor component;
- a tool or framework extension;
- a non-trivial integration pattern.

Do not apply for very small internal-only applications unless a sample would clarify intended usage.

## Files/projects to create

Example layout:

```text
samples/
  BasicUsage/
    BasicUsage.csproj
    Program.cs
  BlazorUsage/
    BlazorUsage.csproj
    Program.cs

docs/engineering/samples.md
```

Do not create `samples/README.md`.

Sample names should describe the usage scenario, not the implementation technique.

Prefer:

```text
samples/BasicUsage
samples/BlazorUsage
samples/PackageConsumer
```

Avoid:

```text
samples/TestApp
samples/Scratch
samples/NewFolder
```

## Required conventions

- Samples must be small.
- Samples must compile.
- Samples must reference source projects or published packages intentionally.
- Samples must not contain hidden test assertions.
- Samples must not become a second application architecture.
- Samples must not be required for normal production builds unless explicitly documented.
- Samples should prefer clarity over completeness.

## Solution membership

Samples may be added to the solution when they are lightweight and expected to remain healthy.

If samples require unusual infrastructure, they may stay outside the main solution but must document their own restore/build command in `docs/engineering/samples.md`.

## Optional command

If samples are part of the supported surface, add:

```text
eng/samples.sh
```

Example:

```sh
#!/usr/bin/env sh
set -eu

dotnet build samples/BasicUsage/BasicUsage.csproj --configuration Debug
```

For multiple samples, prefer an explicit list over recursive discovery so agents know which samples are intended to be maintained.

## Validation

```sh
./eng/samples.sh
```

or:

```sh
dotnet build samples/BasicUsage/BasicUsage.csproj
```

## Done criteria

- `docs/engineering/samples.md` exists.
- Each sample has a documented purpose.
- Each sample builds or has documented prerequisites.
- Samples do not replace tests.
- Samples do not contain production-only secrets or local machine assumptions.

---

# 22. BB16 — GitHub Copilot

## Purpose

Add repository-specific instructions for GitHub Copilot so Copilot uses the repository command contract, building blocks, and engineering rules consistently.

## Apply when

Apply when GitHub Copilot Chat, Copilot code review, Copilot coding agent, or Copilot-assisted IDE workflows are expected for the repository.

## Files to create

```text
.github/copilot-instructions.md
.github/instructions/
```

Optional path-specific instruction files:

```text
.github/instructions/dotnet.instructions.md
.github/instructions/typescript.instructions.md
.github/instructions/tests.instructions.md
.github/instructions/docs.instructions.md
```

## Required conventions

- Repository-wide Copilot instructions live in `.github/copilot-instructions.md`.
- Path-specific instructions live under `.github/instructions/` when needed.
- Copilot instructions must point to `AGENTS.md` for shared agent rules.
- Copilot instructions must not duplicate the whole setup guide.
- Copilot instructions should be short, operational, and command-oriented.

## Example `.github/copilot-instructions.md`

```md
# GitHub Copilot Instructions

This repository follows:

- AGENTS.md
- docs/ENGINEERING.md
- docs/GUARDRAILS.md
- docs/engineering/dotnet.md

Before proposing a completed change, prefer the canonical validation command:

```sh
./eng/check.sh
```

Repository rules:

- Use eng/ scripts instead of inventing commands.
- Do not create README files outside the root README.md.
- Use central package management in Directory.Packages.props for NuGet versions.
- Use Bun, not npm, when JavaScript/TypeScript tooling is enabled.
- Use Biome, not ESLint or Prettier.
- Keep tests fast by default.
- Do not add Playwright, Blazor, samples, packaging, or a website unless the corresponding building block is selected.
```

## Example path-specific instruction

```md
---
applyTo: "tests/**/*.cs"
---

# Test Instructions

- Unit tests must be fast and isolated.
- Do not use sleeps unless unavoidable and documented.
- Do not add browser tests outside `tests/e2e/`.
- Do not run benchmarks as tests.
```

## Validation

```sh
./eng/check.sh
```

## Done criteria

- `.github/copilot-instructions.md` exists.
- Instructions reference `AGENTS.md`, `docs/ENGINEERING.md`, and `docs/GUARDRAILS.md`.
- Instructions include the canonical validation command.
- Path-specific instruction files exist only when they reduce ambiguity.

---

# 23. BB17 — OpenAI Codex

## Purpose

Optimize the repository for OpenAI Codex local and cloud workflows.

Codex should be able to enter the repository, read the instructions, understand the command contract, make changes, validate them, and report completion without guessing.

## Apply when

Apply when OpenAI Codex CLI, Codex IDE extension, Codex cloud tasks, or Codex code review are expected for the repository.

## Files to create or modify

```text
AGENTS.md
docs/ENGINEERING.md
docs/GUARDRAILS.md
docs/engineering/dotnet.md
eng/check.sh
```

Optional:

```text
docs/engineering/codex.md
```

## Required conventions

- `AGENTS.md` is the primary Codex instruction file.
- `AGENTS.md` must be concise and operational.
- The first validation command must be `./eng/check.sh`.
- Long-running or expensive commands must be explicitly marked.
- Cloud-safe and local-only workflows must be distinguished.
- Instructions must state what completion means.

## Example `AGENTS.md` Codex section

```md
## Codex Workflow

When working in this repository:

1. Read docs/ENGINEERING.md.
2. Read docs/GUARDRAILS.md.
3. Use eng/ scripts for restore, build, test, format, and check.
4. Prefer small vertical changes.
5. Do not add new tooling unless a building block requires it.
6. Do not create README files outside the root README.md.
7. Run ./eng/check.sh before reporting completion.
8. If validation fails, report the exact failing command and summarize the relevant output.

## Expensive Commands

Do not run these unless explicitly requested:

```sh
./eng/benchmark.sh
./eng/e2e.sh
./eng/publish.sh
```
```

## Cloud task guidance

Codex cloud tasks should be able to run:

```sh
./eng/check.sh
```

without requiring:

- interactive prompts;
- local secrets;
- machine-specific paths;
- GUI access;
- unpublished local packages.

If a command needs credentials, it must fail with a clear error message and must not be part of normal validation.

## Validation

```sh
./eng/check.sh
```

## Done criteria

- `AGENTS.md` contains Codex-safe workflow rules.
- Expensive commands are marked.
- `eng/check.sh` is the canonical completion gate.
- Codex can validate a clean checkout without local secrets.

---

# 24. BB18 — GitHub Pages Website

## Purpose

Add a static project website hosted through GitHub Pages.

The website is for project-facing documentation, examples, API overview pages, benchmark reports, release notes, or generated documentation snapshots.

## Apply when

Apply when the project needs a public or internal static website.

Do not apply merely to store normal repository documentation. Use `docs/` for ordinary documentation.

## Files to create

```text
site/
site/index.html
site/assets/
.github/workflows/pages.yml
eng/site-build.sh
docs/engineering/site.md
docs/workflows/pages.md
```

Optional:

```text
site/package.json
site/tsconfig.json
site/biome.json
```

Do not create `site/README.md`.

Prefer a simple static site first. Add a site generator only when the static approach becomes insufficient.

## Required conventions

- Website source lives under `site/`.
- Website output must be generated into `artifacts/site/` or another ignored build output folder.
- Publishing is performed by GitHub Actions.
- The Pages workflow must not publish on every pull request.
- The site build must not require secrets.
- The site must not be required for normal backend build/test unless explicitly selected.

## Example `eng/site-build.sh`

```sh
#!/usr/bin/env sh
set -eu

rm -rf artifacts/site
mkdir -p artifacts/site
cp -R site/* artifacts/site/
```

## Example minimal `site/index.html`

```html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>PROJECT_NAME</title>
  </head>
  <body>
    <main>
      <h1>PROJECT_NAME</h1>
      <p>Project website.</p>
    </main>
  </body>
</html>
```

## Example GitHub Pages workflow

```yaml
name: pages

on:
  push:
    branches:
      - main
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Build site
        run: ./eng/site-build.sh

      - name: Upload Pages artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: artifacts/site

  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    needs: build
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

## Optional generated content

The website may include generated content, for example:

```text
artifacts/site/api/
artifacts/site/benchmarks/
artifacts/site/releases/
```

Generated content must be reproducible from repository commands.

## Validation

```sh
./eng/site-build.sh
```

## Done criteria

- `site/` exists.
- `eng/site-build.sh` creates static output.
- Pages workflow uploads and deploys static output.
- Website publishing is separate from normal validation.
- Site documentation lives in `docs/engineering/site.md`.

---

# 25. Recommended base setup sequence

For a normal professional .NET library or service repository, apply:

```text
BB00 Repository Base
BB01 .NET Solution
BB02 Shared Build Configuration
BB03 EditorConfig and C# Style
BB04 MTP + TUnit Unit Tests
BB05 Test Guardrails
BB06 BenchmarkDotNet
BB07 GitHub Actions CI
BB08 Agent Instructions
BB13 Documentation Skeleton
```

Additionally apply:

```text
BB14 NuGet Packaging
```

when the repository produces reusable libraries or packages.

For repositories with runnable examples, additionally apply:

```text
BB15 Samples
```

For GitHub Copilot support, additionally apply:

```text
BB16 GitHub Copilot
```

For OpenAI Codex support, additionally apply:

```text
BB17 OpenAI Codex
```

For a GitHub Pages project website, additionally apply:

```text
BB18 GitHub Pages Website
```

For TypeScript tooling, additionally apply:

```text
BB09 Bun + Biome
BB12 TypeScript Runtime Tools
```

For Blazor UI, additionally apply:

```text
BB10 Blazor Module
```

For browser tests, additionally apply:

```text
BB11 Playwright E2E Module
```

---

# 26. Agent repository creation workflow

An AI agent creating a new repository must follow this workflow:

1. Determine required blocks.
2. Create the repository skeleton.
3. Create .NET solution and projects.
4. Add shared build configuration.
5. Add `.editorconfig`.
6. Add TUnit/MTP test projects.
7. Add benchmark project if selected.
8. Add optional Bun/Biome tooling only if selected.
9. Add optional Blazor/Playwright modules only if selected.
10. Add docs and agent instructions.
11. Confirm no non-root README files exist.
12. Run `./eng/check.sh`.
13. Fix all failures.
14. Report what blocks were applied and what validation passed.

Agents must not declare the repository complete until `./eng/check.sh` succeeds or the failure is explicitly documented with the exact failing command and output summary.

---

# 27. Completion checklist

A generated base repository is complete only when all applicable items are true:

- [ ] `global.json` exists and pins .NET 10 SDK.
- [ ] `Directory.Build.props` exists.
- [ ] `Directory.Packages.props` exists.
- [ ] `.editorconfig` exists.
- [ ] `AGENTS.md` exists.
- [ ] Root `README.md` lists canonical commands.
- [ ] No non-root README files exist.
- [ ] `docs/ENGINEERING.md` exists.
- [ ] `docs/GUARDRAILS.md` exists.
- [ ] `docs/guardrails/testing.md` exists.
- [ ] `docs/guardrails/implementation.md` exists.
- [ ] `docs/engineering/dotnet.md` exists.
- [ ] `eng/` scripts exist and are executable.
- [ ] Solution builds.
- [ ] Unit tests run through MTP/TUnit.
- [ ] Default test command excludes slow/e2e tests.
- [ ] Benchmark project exists if BB06 was selected.
- [ ] Bun/Biome files exist only if BB09 was selected.
- [ ] Blazor project exists only if BB10 was selected.
- [ ] Playwright setup exists only if BB11 was selected.
- [ ] Samples exist only if BB15 was selected.
- [ ] GitHub Copilot instructions exist only if BB16 was selected.
- [ ] Codex-specific guidance exists only if BB17 was selected.
- [ ] GitHub Pages website exists only if BB18 was selected.
- [ ] `./eng/check.sh` succeeds.

---

# 28. Upgrade guide from Engineering Guide V2

## 1. Move the guide into docs

Replace:

```text
eng-setup-guide.md
```

with:

```text
docs/engineering/dotnet.md
docs/research/engineering-guide-v2.md
```

Store the old V2 guide as research if its history matters.

## 2. Remove local README files

Convert:

```text
eng/README.md
samples/README.md
tools/ts/README.md
site/README.md
```

to named docs:

```text
docs/engineering/command-contract.md
docs/engineering/samples.md
docs/engineering/typescript-tools.md
docs/engineering/site.md
```

## 3. Replace `docs/TESTING.md`

Move testing policy into:

```text
docs/guardrails/testing.md
docs/workflows/test-short.md
docs/workflows/test-long.md
docs/engineering/command-contract.md
```

## 4. Replace `docs/PACKAGING.md`

Move packaging policy into:

```text
docs/engineering/packaging.md
docs/workflows/package.md
docs/workflows/release.md
```

## 5. Replace `docs/ENGINEERING.md` content with an index

`docs/ENGINEERING.md` should index engineering documents and declare authority.

Detailed engineering profile belongs in:

```text
docs/engineering/dotnet.md
```

## 6. Update AGENTS.md

Add:

```text
Do not create README files outside the root README.md.
Use docs/ENGINEERING.md and docs/engineering/dotnet.md for command contracts.
```

## 7. Update Copilot instructions

Add:

```text
docs/ENGINEERING.md
docs/engineering/dotnet.md
docs/GUARDRAILS.md
```

## 8. Align issue templates

Implementation and release issues should reference:

```text
docs/ENGINEERING.md
docs/GUARDRAILS.md
docs/tbps/feature-implementation.md
docs/tbps/release.md
```

---

# 29. Final model

Engineering Guide V3 keeps the strong parts of the previous engineering guide:

- canonical `eng/` scripts;
- explicit command contracts;
- building blocks;
- .NET 10 setup;
- MTP + TUnit;
- BenchmarkDotNet;
- Bun + Biome;
- optional Blazor and Playwright;
- test guardrails;
- GitHub Actions using `eng/check.sh`;
- agent-friendly validation.

It changes the documentation structure to match Project Setup Guide V4:

- no non-root README files;
- engineering docs under `docs/engineering/`;
- guardrails under `docs/guardrails/`;
- workflow intent under `docs/workflows/`;
- root README only;
- `docs/ENGINEERING.md` as the engineering index.
