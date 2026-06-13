# SemanticTypeModel.SystemTextJson

System.Text.Json contract integration for SemanticTypeModel.

```sh
dotnet add package SemanticTypeModel.SystemTextJson --version 2.0.0
```

## What it does

- Publishes `systemTextJson.*` annotation constants.
- Provides `SystemTextJsonProjectionOptions` and `SemanticJsonPropertyNameSource`.
- Enables `.NET` extraction options through `UseSystemTextJson()`.
- Provides conservative `JsonSerializerOptions` and `IJsonTypeInfoResolver` helpers that preserve an existing resolver or user-authored `JsonSerializerContext`.
- Can apply imported `systemTextJson.propertyName` values or semantic property names as JSON serialization names when explicitly configured.

Semantic names and JSON property names remain separate by default. The default resolver behavior preserves the existing System.Text.Json contract.

## Generated contexts

`SemanticTypeModel.SystemTextJson` does not generate `JsonSerializerContext` declarations. Consumers own the context when source generation is needed, then wrap it with `WithSemanticTypeModelJson(...)`.

## Projection alignment

The repository architecture treats System.Text.Json as a domain projection target. M0035 aligns the implementation internals with that model. Public package documentation should not require consumer-facing domain-model APIs until they are implemented and shipped.

More details: `public-docs/guides/system-text-json.md`.
