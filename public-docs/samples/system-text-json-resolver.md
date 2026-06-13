# System.Text.Json Resolver Sample

## Scenario Goal

Show a user-authored `JsonSerializerContext` wrapped by `SemanticTypeModel.SystemTextJson` resolver customization.

## Packages Used

- `SemanticTypeModel.Abstractions`
- `SemanticTypeModel.Core`
- `SemanticTypeModel.DotNet`
- `SemanticTypeModel.SystemTextJson`

## Run Command

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

The sample project path is `samples/system-text-json-resolver/system-text-json-resolver.csproj`.

## Expected Output

The sample prints serialized JSON, the deserialized display name, and the semantic annotation key used for System.Text.Json property names.

## Consumer Pattern Demonstrated

A consumer obtains a runtime canonical semantic model, derives a System.Text.Json domain semantic model, owns the `JsonSerializerContext`, and composes a SemanticTypeModel resolver to apply semantic property-name metadata, including semantic-property-name-as-JSON-name behavior.

## Non-Goals

This sample does not claim that SemanticTypeModel generates `JsonSerializerContext` classes, does not demonstrate unsupported generator chaining, and does not use JSON Schema import as a model source.
