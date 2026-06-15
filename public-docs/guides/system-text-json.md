# System.Text.Json Integration

`SemanticTypeModel.SystemTextJson` imports serializer-contract metadata into namespaced annotations while keeping the canonical semantic model projection-neutral.

## Install

```sh
dotnet add package SemanticTypeModel.SystemTextJson --version 2.1.0
```

## Attribute Import

Enable import during compile-time extraction with MSBuild:

```xml
<PropertyGroup>
  <SemanticTypeModelImportSystemTextJsonAttributes>true</SemanticTypeModelImportSystemTextJsonAttributes>
</PropertyGroup>
```

`[JsonPropertyName("customer_id")]` becomes `systemTextJson.propertyName=customer_id`. It does not replace the semantic member name unless `SemanticTypeModelUseJsonPropertyNameAsSemanticName=true` is explicitly configured.

`[JsonExtensionData]` can be imported as System.Text.Json metadata and, where supported by the current core semantic vocabulary, normalized to extension-data semantics by explicit extraction or derivation behavior.

## Source Generation

`SemanticTypeModel` does not generate `JsonSerializerContext` declarations. Consumers who want `System.Text.Json` source generation own the context declaration:

```csharp
[JsonSerializable(typeof(Customer))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
```

Generated context output from SemanticTypeModel remains unsupported because source generators are not an ordered transformation pipeline.

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

## Domain Projection Alignment

The supported consumer concept is resolver-centered integration: canonical semantic model metadata is used to produce deterministic resolver customization behavior. M0035 aligns the implementation internals with the repository's domain-projection pipeline so System.Text.Json follows the same canonical-model-to-domain-model pattern as JSON Schema, EF Core, and Power BI.

Until that implementation cleanup is complete, public documentation should not require consumers to use implementation-specific domain-model types that are not yet available in shipped packages.
