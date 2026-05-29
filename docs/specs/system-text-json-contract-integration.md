# System.Text.Json Contract Integration Spec

## Purpose

`SemanticTypeModel.SystemTextJson` preserves `System.Text.Json` serializer-contract metadata without turning the canonical semantic model into a serializer model.

## Package Boundary

- `SemanticTypeModel.Abstractions` remains projection-neutral and has no `System.Text.Json` dependency.
- `SemanticTypeModel.DotNet` exposes neutral extraction/generator options that can be enabled directly or through `SemanticTypeModel.SystemTextJson` extension APIs.
- `SemanticTypeModel.SystemTextJson` owns public annotation constants, projection options, extraction option helpers, and conservative runtime helper APIs.

## Annotation Keys

| Key | Meaning |
|---|---|
| `systemTextJson.propertyName` | `JsonPropertyNameAttribute` serialization name |
| `systemTextJson.ignore` | `JsonIgnoreAttribute` marker |
| `systemTextJson.ignoreCondition` | `JsonIgnoreCondition` value when available |
| `systemTextJson.include` | `JsonIncludeAttribute` marker |
| `systemTextJson.converter` | `JsonConverterAttribute` converter type metadata |
| `systemTextJson.numberHandling` | `JsonNumberHandlingAttribute` value |
| `systemTextJson.required` | `JsonRequiredAttribute` marker |
| `systemTextJson.extensionData` | `JsonExtensionDataAttribute` marker |
| `systemTextJson.objectCreationHandling` | `JsonObjectCreationHandlingAttribute` value when available |
| `systemTextJson.unmappedMemberHandling` | `JsonUnmappedMemberHandlingAttribute` value when available |
| `systemTextJson.polymorphism` | Polymorphism metadata marker when metadata is preserved but not modeled canonically |

## Name Boundary

Semantic property names and JSON serialization names are distinct by default. `JsonPropertyNameAttribute` is imported into `systemTextJson.propertyName` and does not replace `PropertyShape.Name` unless `UseJsonPropertyNameAsSemanticName` is explicitly enabled.

## Generator Contexts

The source generator emits no `JsonSerializerContext` by default. Context generation is enabled with `SemanticTypeModelGenerateSystemTextJsonContext=true` or equivalent assembly options. The generated context includes extracted object and enum types that are safe to reference in `typeof(...)`; unsupported object-typed members produce `STJ005` guidance.

## Diagnostics

`STJ001` through `STJ008` are stable System.Text.Json integration diagnostics covering name-policy conflicts, unsupported converters, ignore/required conflicts, generated-context root problems, object/polymorphic surfaces, `JsonRequired` conflicts, and unsupported extension-data member types.
