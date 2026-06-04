# Getting Started

## Goal

Build your first semantic model flow from JSON Schema import to JSON Schema export.

## Prerequisites

- .NET 10 SDK

## First Flow

1. Import Draft 2020-12 JSON Schema with `JsonSchemaImporter`.
2. Work with the canonical `TypeSchemaModel` result.
3. Export JSON Schema with `JsonSchemaExporter`.

See runnable sample: `samples/json-schema-roundtrip` and [public-docs/samples/json-schema-roundtrip.md](samples/json-schema-roundtrip.md).

For a code-first JSON Schema flow, run `samples/code-first-json-schema` and see [public-docs/samples/code-first-json-schema.md](samples/code-first-json-schema.md).

Prepare local packages before running the package-based sample set:

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

> `1.0.0` is the first stable release. Documented public APIs follow the compatibility policy. `1.1.0` corrects the System.Text.Json contract and sample validation model.
