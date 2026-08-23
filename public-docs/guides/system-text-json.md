# System.Text.Json

## Use

SemanticTypeModel customizes metadata produced by an application-owned `IJsonTypeInfoResolver` or
`JsonSerializerContext`. It does not generate a serializer context.

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

The integration preserves application-owned resolver/context/converter boundaries. Semantic metadata cannot
safely replace behavior hidden inside arbitrary converters. For the bounded JSON representation-fidelity
contract, STM-configured `JsonSerializerOptions.AddSemanticTypeModelJson(...)` also establishes supported
Strong Scalar converters; resolver-only customization satisfies that wire guarantee only when equivalent
converters are already present.

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

## Reference

SemanticTypeModel does not generate `JsonSerializerContext`, replace arbitrary converters, or make semantic
names replace serialization names by default. Supported STM-configured output is the one-way fidelity contract
with the derived schema; schema-to-serializer equivalence and representation-changing custom contracts are not
guaranteed.

See [Troubleshooting](../troubleshooting.md) and `samples/system-text-json-resolver/`.
