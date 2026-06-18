# System.Text.Json Integration

## Goal

Use semantic metadata to customize System.Text.Json resolver behavior while preserving user-owned serializer contexts and converters.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.2.0`.

## Packages

- `SemanticTypeModel.SystemTextJson` for derivation and resolver helpers.
- `SemanticTypeModel.DotNet` and `SemanticTypeModel.Generators` for code-first model generation.
- `System.Text.Json` for runtime serialization.

## Minimal path

1. Keep or create your own `JsonSerializerContext` when source generation is needed.
2. Generate the semantic model.
3. Derive System.Text.Json metadata or wrap an existing resolver.
4. Choose the property-name source explicitly.
5. Check diagnostics for duplicate final JSON names.

## Full example

```csharp
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SemanticTypeModel.SystemTextJson;

[JsonSerializable(typeof(Customer))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

IJsonTypeInfoResolver resolver =
    AppJsonContext.Default.WithSemanticTypeModelJson(
        AppSemanticTypeModel.Create(),
        options => options.PropertyNameSource = SemanticJsonPropertyNameSource.SystemTextJsonPropertyNameAnnotation);
```

## How it works

Annotated .NET code is extracted by the generator into a `TypeSchemaModel`. Core transformations normalize projection-neutral semantics. The target package derives a domain semantic model and then exports or applies target-specific output when that target supports it.

## Options and policies

`SemanticJsonPropertyNameSource` can preserve the existing JSON contract, use imported `systemTextJson.propertyName` annotations, or use semantic property names. Existing resolvers are wrapped rather than replaced.

## Diagnostics

Diagnostics report duplicate projected property names, missing type metadata, unsupported resolver customization, and metadata that cannot be safely applied to the existing JSON contract.

## Common mistakes

- Treating JSON Schema files as the canonical authoring source for new models.
- Mixing target-specific metadata with projection-neutral semantics.
- Skipping diagnostic inspection before using projected output.
- Using stale pre-2.2 model namespace or shape names in current examples.

## Limitations

The package does not generate `JsonSerializerContext` declarations, emulate arbitrary converters, or make semantic names replace JSON names by default.

## Related docs

- [SemanticTypeModel.SystemTextJson package](../nuget/SemanticTypeModel.SystemTextJson.md)
- [System.Text.Json resolver sample](../samples/system-text-json-resolver.md)
