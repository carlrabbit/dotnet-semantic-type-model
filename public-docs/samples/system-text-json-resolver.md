# System.Text.Json Resolver Sample

## Scenario Goal

Show a user-authored `JsonSerializerContext` wrapped by SemanticTypeModel System.Text.Json resolver customization.

## Packages Used

- `SemanticTypeModel.DotNet`
- `SemanticTypeModel.Generators`
- `SemanticTypeModel.SystemTextJson`

## Run Command

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

The sample project path is `samples/system-text-json-resolver/system-text-json-resolver.csproj`.

## Expected Output

The sample prints serialized JSON and the deserialized display name.

## Consumer Pattern Demonstrated

A consumer owns the `JsonSerializerContext` and composes a SemanticTypeModel resolver to apply supported metadata, including semantic-property-name-as-JSON-name behavior when explicitly configured.

## Current Implementation Note

The current sample demonstrates resolver wrapping. M0035 replaces manual model-shape setup with the same code-first extraction/generation pattern used by the other public samples. Until that implementation work is complete, this sample page should not claim that a public System.Text.Json domain model API is shipped.

## Non-Goals

This sample does not claim that SemanticTypeModel generates `JsonSerializerContext` classes and does not demonstrate unsupported generator chaining.
