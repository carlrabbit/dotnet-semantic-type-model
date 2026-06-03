# Samples Engineering

## Status

Authoritative engineering policy for public samples.

## Purpose

Define constraints and validation for runnable samples used as executable public documentation.

Samples must show how a consumer uses SemanticTypeModel packages. They are not a place for repository-internal generator harnesses, source-tree integration experiments, or unit-test substitutes.

## Validation Command

Primary sample validation is performed through:

```sh
./eng/samples.sh
```

When samples consume locally prepared packages, sample validation must be run after the required packages have been packed into the configured local package source.

If a versioned sample command is introduced, the command contract must document it and `./eng/samples.sh` must route to or explain the versioned command.

## Public Sample Rules

Public samples live under `samples/` and must satisfy these rules:

- Samples are standalone consumer projects.
- Samples are runnable and deterministic.
- Samples consume SemanticTypeModel through `PackageReference`, not `ProjectReference` to `src/*`.
- Samples restore SemanticTypeModel packages from locally prepared package artifacts during repository validation.
- Samples may use public package feeds for third-party dependencies.
- Samples must not manually invoke Roslyn source-generator APIs.
- Samples must not compile C# source strings to demonstrate normal source-generator usage.
- Samples that use `SemanticTypeModel.Generators` must let the generator run through normal MSBuild/NuGet package behavior.
- Samples must not require secrets, network services, cloud accounts, external databases, Power BI service access, or timing-dependent behavior.
- Samples must not introduce local README files.
- Sample documentation lives under `public-docs/samples/`.
- Samples do not replace tests; tests remain responsible for exhaustive behavior and regression coverage.

## Internal Harnesses

Internal development harnesses must not be presented as public samples.

Examples of internal harness behavior:

```text
manual CSharpCompilation construction
manual CSharpGeneratorDriver execution
source strings used as the primary sample model
in-memory assembly emission
reflection used to invoke generated providers
direct references to source projects for normal package scenarios
```

Such code belongs in tests or tooling, not in public samples.

## Required Public Sample Set

The public sample set should cover the main consumer workflows:

| Sample scenario | Purpose |
|---|---|
| JSON Schema roundtrip | Import and export JSON Schema through supported public APIs. |
| Code-first JSON Schema | Annotated C# model, packaged generator, generated provider, JSON Schema export. |
| Code-first EF Core | Annotated C# model, packaged generator, generated provider, EF Core projection. |
| Code-first Power BI | Annotated C# model, packaged generator, generated provider, Power BI projection metadata. |
| System.Text.Json resolver | User-authored `JsonSerializerContext` and SemanticTypeModel resolver customization. |
| Runtime DI | Consumer-style dependency-injection registration and projection usage. |

Sample directory names may differ, but public documentation must clearly map scenario names to project paths.


## Current Public Sample Set and Classification

All projects currently under `samples/` are public consumer samples. The M0025 classification is:

| Project path | Classification | Consumer scenario |
|---|---|---|
| `samples/json-schema-roundtrip` | Keep public sample | JSON Schema import, transformation, validation, and export. |
| `samples/code-first-json-schema` | Rewrite public sample | Normal packaged generator usage with JSON Schema export. |
| `samples/code-first-ef-core` | Rewrite public sample | Normal packaged generator usage with EF Core projection metadata. |
| `samples/code-first-powerbi` | Rewrite public sample | Normal packaged generator usage with Power BI projection metadata. |
| `samples/system-text-json-resolver` | Rewrite public sample | User-authored `JsonSerializerContext` with resolver customization. |
| `samples/runtime-di` | Rewrite public sample | Consumer-style dependency-injection registration and projection usage. |

The former generator-driver/source-string harnesses and source-project projection samples are removed from the public sample set rather than retained as internal examples under `samples/`.

## Source Generator Sample Contract

A source-generator sample must use the normal consumer path:

```text
PackageReference to SemanticTypeModel.Generators.
Domain model files included in the project.
Generator options configured through supported project properties or assembly attributes.
Generated provider consumed directly from generated source.
No manual generator driver.
No source-string compilation.
No reflection over generated providers.
```

Example consumer shape:

```csharp
TypeSchemaModel model = AppSemanticTypeModel.Create();
```

The exact generated namespace and provider name depend on sample configuration.

## Package-Based Restore Contract

Repository sample validation must exercise packaged artifacts.

The sample validation setup must ensure:

- SemanticTypeModel packages come from locally prepared `artifacts/nuget` or an equivalent configured local package source.
- Public package feeds remain available for third-party dependencies.
- Missing local SemanticTypeModel packages cause sample validation to fail.
- Analyzer/source-generator package assets are exercised through package restore and build.
- Sample projects remain runnable as consumer projects outside the repository when pointed at an appropriate package source.

## Code Comment Guidance

Samples should include comments that teach consumer usage.

Use comments to explain:

- package or project configuration that enables a feature;
- why a semantic annotation is present;
- where generated provider code comes from;
- what the projection output represents;
- how local artifacts are produced;
- why a `JsonSerializerContext` is user-authored;
- where sample output is written.

Do not use comments that merely restate the syntax.

## Documentation Contract

When sample policy or sample projects change, review and update:

```text
public-docs/samples.md
public-docs/samples/*.md
eng/samples.sh
docs/engineering/command-contract.md when commands change
```

Public sample documentation must state:

- scenario goal;
- package references used;
- run command;
- expected output;
- consumer pattern demonstrated;
- non-goals;
- sample project path.

## Validation Tiers

Use the repository validation tiers.

| Tier | Use for samples |
|---|---|
| Tier 0 | Sample documentation changes and sample policy text. |
| Tier 1 | Affected sample build/run, package-based restore validation, affected sample command changes. |
| Tier 2 | Completion gate for sample implementation changes. |
| Tier 3 | Package preparation and package-smoke validation when package artifacts or package-based sample validation are involved. |
| Tier 4 | Not used for sample implementation unless explicit publish work is in scope. |
