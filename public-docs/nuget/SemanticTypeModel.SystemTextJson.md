# SemanticTypeModel.SystemTextJson

System.Text.Json contract integration for SemanticTypeModel.

## What it does

- Publishes `systemTextJson.*` annotation constants.
- Provides `SystemTextJsonProjectionOptions`.
- Enables `.NET` extraction options through `UseSystemTextJson()`.
- Provides conservative `JsonSerializerOptions` and `IJsonTypeInfoResolver` helpers.

Semantic names and JSON property names remain separate by default.
