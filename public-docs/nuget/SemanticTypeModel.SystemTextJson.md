# SemanticTypeModel.SystemTextJson

System.Text.Json contract integration for SemanticTypeModel.

```sh
dotnet add package SemanticTypeModel.SystemTextJson --version 1.0.0
```

## What it does

- Publishes `systemTextJson.*` annotation constants.
- Provides `SystemTextJsonProjectionOptions`.
- Enables `.NET` extraction options through `UseSystemTextJson()`.
- Provides conservative `JsonSerializerOptions` and `IJsonTypeInfoResolver` helpers.

Semantic names and JSON property names remain separate by default.
