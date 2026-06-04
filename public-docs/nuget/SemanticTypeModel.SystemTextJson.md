# SemanticTypeModel.SystemTextJson

System.Text.Json contract integration for SemanticTypeModel.

```sh
dotnet add package SemanticTypeModel.SystemTextJson --version 1.1.0
```

## What it does

- Publishes `systemTextJson.*` annotation constants.
- Provides `SystemTextJsonProjectionOptions` and `SemanticJsonPropertyNameSource`.
- Enables `.NET` extraction options through `UseSystemTextJson()`.
- Provides conservative `JsonSerializerOptions` and `IJsonTypeInfoResolver` helpers that preserve an existing resolver or user-authored `JsonSerializerContext`.
- Can apply imported `systemTextJson.propertyName` values or semantic property names as JSON serialization names when explicitly configured.

Semantic names and JSON property names remain separate by default. The default resolver behavior preserves the existing System.Text.Json contract.

## Generated contexts

`SemanticTypeModel.SystemTextJson` does not generate `JsonSerializerContext` declarations. Generated context support is removed in 1.1.0 because it depended on unsupported source-generator chaining and did not produce a reliable consumer feature. Author the context in consumer code when source generation is needed, then wrap it with `WithSemanticTypeModelJson(...)`.
