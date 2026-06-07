# Getting Started

## Goal

Build your first code-first SemanticTypeModel flow from annotated .NET code to a derived domain model.

## Prerequisites

- .NET 10 SDK

## First Flow

1. Annotate .NET types with core semantics such as entity, key, display name, constraints, or envelope metadata.
2. Let the packaged generator produce a canonical semantic model provider.
3. Inspect the canonical model and diagnostics.
4. Derive a target domain model such as JSON Schema, EF Core, or Power BI.
5. Inspect target diagnostics before using the generated output.

Start with the code-first JSON Schema sample:

```text
samples/code-first-json-schema
public-docs/samples/code-first-json-schema.md
```

Then try the target-specific samples:

```text
samples/code-first-ef-core
samples/code-first-powerbi
```

Prepare local packages before running the package-based sample set:

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

## Core Semantics

Core semantics are projection-neutral. Use them for domain meaning that should be available to all targets. Use target-specific metadata only for representation choices that belong to JSON Schema, EF Core, Power BI, System.Text.Json, or another target.

See [guides/core-semantics.md](guides/core-semantics.md).

## Release Status

`2.0.0` is the code-first semantic model release. Documented public APIs follow the compatibility policy unless a page explicitly marks a feature as preview.
