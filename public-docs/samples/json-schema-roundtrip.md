# JSON Schema Roundtrip Sample

## Scenario Goal

Exercise the retained JSON Schema compatibility import path and export JSON Schema again. New code-first consumers should use generated providers instead of JSON Schema import as a model authoring path.

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

A consumer can recognize the retained compatibility import path, but should use annotated .NET code plus the packaged generator for supported model authoring.

## Non-Goals

This sample does not use the source generator, invoke Roslyn APIs, call network services, or validate every JSON Schema keyword.
