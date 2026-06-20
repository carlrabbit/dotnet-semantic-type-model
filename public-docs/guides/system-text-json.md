# System.Text.Json Integration

## Goal

Use semantic metadata to customize System.Text.Json resolver behavior while preserving user-owned serializer contexts and converters.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.3.0`.

## Packages

- `SemanticTypeModel.SystemTextJson` for derivation and resolver helpers.
- `SemanticTypeModel.DotNet` and `SemanticTypeModel.Generators` for code-first model generation.
- `System.Text.Json` for runtime serialization.

## Minimal path

1. Keep or create your own `JsonSerializerContext` when source generation is needed.
2. Generate the semantic model.
3. Wrap an existing resolver.
4. Choose `PropertyNameSource` explicitly.
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
        options => options.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName);
```

## How it works

The package wraps an existing `IJsonTypeInfoResolver`. It customizes metadata that System.Text.Json already exposes for the type. It does not replace user converters, does not generate a `JsonSerializerContext`, and does not create metadata for types absent from the wrapped resolver.

## Options and policies

| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
| `PropertyNameSource` | `ExistingJsonContract` | `ExistingJsonContract`, `SystemTextJsonPropertyNameAnnotation`, `SemanticPropertyName` | Keeps existing names, uses imported `[JsonPropertyName]`, or uses semantic member names | Duplicate final names are diagnostics. |
| Resolver wrapping order | Existing resolver first | Any `IJsonTypeInfoResolver` supplied by the app | Preserves app-owned context and then applies semantic metadata | Missing type metadata cannot be invented. |
| Required marker handling | Preserve existing contract unless semantic metadata is applicable | C# required/STJ required plus semantic requiredness | Can mark required members when safely represented | Unsupported required metadata is diagnostic. |
| Ignored members | Preserve existing ignore behavior | `[JsonIgnore]` and resolver-produced metadata | Ignored members stay outside the final JSON contract | Semantic metadata on ignored members may not affect output. |
| Extension data | Preserve resolver extension-data member | `[JsonExtensionData]` plus semantic extension data | Keeps unknown JSON member handling | Multiple or incompatible extension-data members are diagnostic. |
| Existing `JsonSerializerContext` | Required for source-generated contracts | Any user context/resolver | SemanticTypeModel wraps, not owns, context generation | Generated-context creation is unsupported. |
| Converter boundaries | User converters win | App-supplied converters/resolver metadata | Semantic changes do not emulate arbitrary converter behavior | Converter-controlled members may not be safely customized. |

## Diagnostics

| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
| Duplicate final JSON name | Selected property-name source maps two CLR members to one JSON name | Use `ExistingJsonContract`, change `[JsonPropertyName]`, or change semantic names. |
| Missing type metadata | Wrapped resolver does not know the type | Add the type to your `JsonSerializerContext` or resolver chain. |
| Unsupported resolver customization | Converter or metadata kind prevents safe mutation | Keep existing contract names or customize the converter manually. |
| Required marker not applied | Member is ignored, converter-owned, or unavailable in metadata | Move requiredness to the active contract or remove unsupported customization. |

## Common mistakes

- Expecting SemanticTypeModel to generate `JsonSerializerContext` declarations.
- Switching to `SemanticPropertyName` without checking duplicate JSON names.
- Assuming semantic names replace `[JsonPropertyName]` by default.
- Trying to override behavior hidden inside custom converters.

## Limitations

The package does not generate serializer contexts, emulate arbitrary converters, validate JSON payloads, or make semantic names replace JSON names by default.

## Related docs

- [SemanticTypeModel.SystemTextJson package](../nuget/SemanticTypeModel.SystemTextJson.md)
- [System.Text.Json resolver sample](../samples/system-text-json-resolver.md)
