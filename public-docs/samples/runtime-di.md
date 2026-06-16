# Runtime DI Sample

## Scenario Goal

Register SemanticTypeModel runtime services in a consumer dependency-injection container and project a model to JSON Schema.

## Packages Used

- `Microsoft.Extensions.DependencyInjection`
- `SemanticTypeModel.DependencyInjection`
- `SemanticTypeModel.JsonSchema`

## Run Command

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

The sample project path is `samples/runtime-di/runtime-di.csproj`.

## Expected Output

The sample prints runtime and projection diagnostic counts, then prints the projected JSON Schema document.

## Consumer Pattern Demonstrated

A consumer registers model creation, validation transformation, and JSON Schema projection services through normal package APIs.

## Non-Goals

This sample does not use a source generator, external service, database, or network dependency; the model factory is intentionally local to keep the DI composition example focused.
