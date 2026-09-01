# System.Text.Json

## Use

The primary runtime path uses ordinary `JsonSerializerOptions`; no `JsonSerializerContext` is required:

```csharp
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
options.AddSemanticTypeModelJson(AppSemanticTypeModel.Create());
```

Semantic Entity inheritance receives
automatic `$type` polymorphism using the canonical derived type name.

Native System.Text.Json lexical behavior is the baseline for scalar fidelity: temporal values, GUIDs, URIs,
Base64 binary values, and raw JSON DOM values are serialized by the framework; STM does not add representation-
changing converters merely to match a schema annotation.

Applications may also compose an existing `IJsonTypeInfoResolver` or `JsonSerializerContext`.

```csharp
[JsonSerializable(typeof(Customer))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

IJsonTypeInfoResolver resolver =
    AppJsonContext.Default.WithSemanticTypeModelJson(
        AppSemanticTypeModel.Create(),
        options => options.PropertyNameSource =
            SemanticJsonPropertyNameSource.SemanticPropertyName);
```

## Configure

The important policy is where final JSON property names come from:

- existing JSON contract;
- imported System.Text.Json property-name annotation;
- semantic property name.

The integration preserves application-owned resolver/context/converter boundaries. If the base resolver already
defines `JsonTypeInfo.PolymorphismOptions`, that explicit application contract is preserved unchanged. Register
all semantic models before first serialization; System.Text.Json freezes options after use.

Multiple independent models can be registered on one options instance. The same API is used by Minimal APIs:

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.AddSemanticTypeModelJson(semanticModel);
});
```

This configures Minimal API request deserialization and response serialization.

During semantic-model extraction, System.Text.Json attributes can be imported as target-specific annotations;
`JsonPropertyName` is not promoted to a semantic name unless explicitly configured. See the
[generator configuration reference](../configuration.md). STM does not own application configuration binding
or Options registration.

## Diagnose

| Symptom | Likely cause | Fix |
|---|---|---|
| Duplicate final JSON name | Selected property-name source maps multiple members to one name | Change JSON/semantic names or select another property-name source. |
| Missing type metadata | Wrapped resolver/context does not know the CLR type | Add the type to the application resolver/context chain. |
| Customization has no effect | Converter/metadata kind owns behavior | Keep existing contract behavior or customize the converter manually. |
| Required marker not applied | Member is ignored/converter-owned/unavailable | Fix the active JSON contract rather than forcing semantic metadata. |
| Expected semantic names but existing names remain | `PropertyNameSource` kept existing contract | Select the intended source explicitly. |
| `STJ009` or polymorphism failure | Invalid/ambiguous semantic Entity hierarchy | Fix CLR inheritance/model ownership or preserve an explicit application contract. |

## Reference

SemanticTypeModel does not generate `JsonSerializerContext`, replace arbitrary converters, or make semantic
names replace serialization names by default. Supported STM-configured output is the one-way fidelity contract
with the derived schema; schema-to-serializer equivalence and representation-changing custom contracts are not
guaranteed.

Automatic polymorphism/discriminator output is outside the JSON Schema/STJ fidelity baseline. See
[Troubleshooting](../troubleshooting.md) and `samples/system-text-json-resolver/`.
