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

The current sample demonstrates resolver wrapping with a local model factory. This remains a release-review item for M0039 because code-first samples should normally use generated providers; do not treat the hand-built model factory as the preferred authoring path.

## Non-Goals

This sample does not claim that SemanticTypeModel generates `JsonSerializerContext` classes and does not demonstrate unsupported generator chaining.
