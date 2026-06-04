# System.Text.Json Integration

`SemanticTypeModel.SystemTextJson` imports serializer-contract metadata into namespaced annotations while keeping the canonical model projection-neutral.

## Install

```sh
dotnet add package SemanticTypeModel.SystemTextJson --version 1.1.0
```

## Attribute Import

Enable import during compile-time extraction with MSBuild:

```xml
<PropertyGroup>
  <SemanticTypeModelImportSystemTextJsonAttributes>true</SemanticTypeModelImportSystemTextJsonAttributes>
</PropertyGroup>
```

`[JsonPropertyName("customer_id")]` becomes `systemTextJson.propertyName=customer_id`. It does not replace the semantic member name unless `SemanticTypeModelUseJsonPropertyNameAsSemanticName=true` is explicitly configured.

## Source Generation

`SemanticTypeModel` does not generate `JsonSerializerContext` declarations. Generated context support is removed in 1.1.0 because it depended on unsupported source-generator chaining and did not produce a reliable consumer feature.

Consumers who want `System.Text.Json` source generation own the context declaration:

```csharp
[JsonSerializable(typeof(Customer))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
```

## Runtime Helpers

`JsonSerializerOptions.AddSemanticTypeModelJson(model)` installs a conservative resolver that wraps the existing `TypeInfoResolver` when one is already configured, or uses the default resolver when none is present. It does not attempt to emulate arbitrary custom converter behavior.

Existing resolvers and user-authored source-generated contexts can be wrapped directly:

```csharp
IJsonTypeInfoResolver resolver =
    AppJsonContext.Default.WithSemanticTypeModelJson(
        AppSemanticTypeModel.Create(),
        options => options.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName);
```

The `PropertyNameSource` option controls whether customization preserves the existing JSON contract, uses the imported `systemTextJson.propertyName` annotation, or uses semantic property names as JSON property names. Duplicate final JSON property names fail deterministically.
