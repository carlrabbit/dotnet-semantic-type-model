# JSON Schema Roundtrip Sample

## Scenario Goal

Import a JSON Schema document, convert it to the canonical semantic model, run deterministic transformations, and export JSON Schema again.

## Packages Used

- `SemanticTypeModel.Core`
- `SemanticTypeModel.JsonSchema`

## Run Command

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

The sample project path is `samples/json-schema-roundtrip/json-schema-roundtrip.csproj`.

## Expected Output

The sample prints import, adapter, transformation, and projection diagnostic counts, then writes `artifacts/samples/json-schema-roundtrip/customer.roundtrip.schema.json`.

## Consumer Pattern Demonstrated

A consumer can use package APIs to import a schema, normalize and validate the semantic model, and project the model back to JSON Schema.

## Non-Goals

This sample does not use the source generator, invoke Roslyn APIs, call network services, or validate every JSON Schema keyword.
