# System.Text.Json Integration

`SemanticTypeModel.SystemTextJson` imports serializer-contract metadata into namespaced annotations while keeping the canonical model projection-neutral.

## Install

```sh
dotnet add package SemanticTypeModel.SystemTextJson --version 1.0.0
```

## Attribute Import

Enable import during compile-time extraction with MSBuild:

```xml
<PropertyGroup>
  <SemanticTypeModelImportSystemTextJsonAttributes>true</SemanticTypeModelImportSystemTextJsonAttributes>
</PropertyGroup>
```

`[JsonPropertyName("customer_id")]` becomes `systemTextJson.propertyName=customer_id`. It does not replace the semantic member name unless `SemanticTypeModelUseJsonPropertyNameAsSemanticName=true` is explicitly configured.

## Generated Context

Context generation is opt-in:

```xml
<PropertyGroup>
  <SemanticTypeModelGenerateSystemTextJsonContext>true</SemanticTypeModelGenerateSystemTextJsonContext>
  <SemanticTypeModelSystemTextJsonContextName>AppSemanticJsonContext</SemanticTypeModelSystemTextJsonContextName>
</PropertyGroup>
```

The generator includes safe extracted object and enum roots. Object-typed or polymorphic surfaces may require hand-authored `JsonSerializable` roots.

## Runtime Helpers

`JsonSerializerOptions.AddSemanticTypeModelJson(model)` installs a conservative resolver that applies supported property-name annotations when CLR metadata can be matched safely. It does not attempt to emulate arbitrary custom converter behavior.
