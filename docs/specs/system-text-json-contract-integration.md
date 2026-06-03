# System.Text.Json Contract Integration Spec

## Status

Authoritative behavioral specification.

## Purpose

`SemanticTypeModel.SystemTextJson` preserves and applies supported `System.Text.Json` serializer-contract metadata without turning the canonical semantic model into a serializer model.

This spec is authoritative for:

- `System.Text.Json` annotation keys;
- semantic-name and serialization-name separation;
- `System.Text.Json` attribute import behavior;
- runtime resolver customization behavior;
- unsupported generated-context behavior;
- System.Text.Json integration diagnostics.

## Package Boundary

- `SemanticTypeModel.Abstractions` remains projection-neutral and has no `System.Text.Json` dependency.
- `SemanticTypeModel.DotNet` may import `System.Text.Json` attribute metadata into canonical annotations when configured.
- `SemanticTypeModel.SystemTextJson` owns public annotation constants, projection options, extraction option helpers, and runtime resolver helper APIs.
- `SemanticTypeModel.Generators` must not generate `JsonSerializerContext` declarations.

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

Additional internal matching annotations may be introduced when needed to map CLR members to semantic properties reliably, for example an annotation representing the original CLR member name. Such annotations must use the existing annotation namespace policy and must not replace semantic names. The .NET extractor records `dotnet.memberName` on extracted properties for resolver matching.

## Name Boundary

Semantic property names and JSON serialization names are distinct by default.

Definitions:

| Concept | Meaning |
|---|---|
| CLR property name | The source-level .NET member name. |
| Semantic property name | `PropertyShape.Name`, the canonical model property identity. |
| Semantic display/title metadata | Human-facing metadata such as semantic name/title/display name annotations. |
| System.Text.Json property name | The JSON property name used by `System.Text.Json` for serialization/deserialization. |
| `JsonPropertyNameAttribute` value | Explicit STJ serialization-name metadata imported as `systemTextJson.propertyName`. |
| Existing JSON contract name | The name already present on `JsonPropertyInfo.Name` before SemanticTypeModel customization. |

`JsonPropertyNameAttribute` is imported into `systemTextJson.propertyName` and does not replace `PropertyShape.Name` unless `UseJsonPropertyNameAsSemanticName` or an equivalent explicit extraction option is enabled.

Using semantic property names as JSON serialization names is a runtime resolver projection choice and must be explicitly configured.

## Generated Contexts Are Unsupported

`SemanticTypeModel` must not generate `JsonSerializerContext` declarations.

Unsupported behavior:

```text
SemanticTypeModel generator emits a class deriving from JsonSerializerContext.
System.Text.Json source generator is expected to process that emitted class.
```

Reason:

```text
Source generators are not an ordered transformation pipeline and generated output from one generator cannot be relied on as input to another generator in the same compilation.
```

Required behavior:

- no generated source file may declare a type deriving from `JsonSerializerContext`;
- no option may promise generated context output;
- `GenerateJsonSerializerContext` and `GeneratedContextName` are removed from `SemanticTypeModel.SystemTextJson` options;
- `GenerateSystemTextJsonContext` and `SystemTextJsonContextName` are removed from `SemanticTypeModelGeneratorOptionsAttribute`;
- `SemanticTypeModelGenerateSystemTextJsonContext=true` and `SemanticTypeModelSystemTextJsonContextName` MSBuild properties are rejected with explicit generated-context removal guidance;
- samples must use user-authored `JsonSerializerContext` declarations when demonstrating source generation.

Compatibility behavior:

```text
Generated JsonSerializerContext support is removed in 1.1.0 because it depended on unsupported source-generator chaining and did not produce a reliable consumer feature.
```

## User-Owned Source-Generated Contexts

Consumers who want `System.Text.Json` source generation own the context declaration:

```csharp
[JsonSerializable(typeof(Customer))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
```

`SemanticTypeModel.SystemTextJson` may customize metadata from a user-owned context by wrapping or composing the context as an `IJsonTypeInfoResolver`.

The supported pattern is resolver composition, not context generation.

## Runtime Resolver Customization

`SemanticTypeModel.SystemTextJson` may apply supported semantic metadata to `JsonTypeInfo` through `IJsonTypeInfoResolver`.

The resolver contract must support these scenarios:

| Scenario | Required behavior |
|---|---|
| No existing resolver | Use `DefaultJsonTypeInfoResolver` as the base resolver. |
| Existing `JsonSerializerOptions.TypeInfoResolver` | Preserve and wrap/compose the existing resolver. |
| User-authored `JsonSerializerContext` | Allow the context to be used as the base resolver. |
| Existing source-generated metadata | Modify supported `JsonTypeInfo` metadata before use. |
| Unsupported metadata | Leave unchanged or report explicit diagnostics/errors according to the public API contract. |

`JsonSerializerOptions` helper methods must not blindly discard an existing resolver.

## Property Name Source

The runtime resolver must expose an explicit way to choose the source of JSON serialization names.

The contract must support at least:

| Name source | Meaning |
|---|---|
| `ExistingJsonContract` | Preserve the name already provided by the base resolver/context. |
| `SystemTextJsonPropertyNameAnnotation` | Use `systemTextJson.propertyName` when present. |
| `SemanticPropertyName` | Use `PropertyShape.Name` as the JSON property name. |

The implementation may expose this as an enum, named options, or equivalent public API. Contradictory boolean flags should be avoided for new API.

Default behavior must preserve the existing JSON contract unless a compatibility decision explicitly keeps prior behavior.

## Matching JsonPropertyInfo to Semantic Properties

Resolver customization must not rely only on `JsonPropertyInfo.Name` when that value may already have been changed by:

- a naming policy;
- `JsonPropertyNameAttribute`;
- a source-generated context;
- a previous resolver modifier.

The implementation must use a stable matching strategy, such as:

- original CLR member-name annotations captured during extraction;
- `JsonPropertyInfo.AttributeProvider` where available;
- explicit metadata linking semantic properties to CLR members;
- conservative fallback only when the current JSON name is known to match the semantic property name.

If a property cannot be matched safely, the resolver must not silently mutate an unrelated property.

## Duplicate Final JSON Names

When SemanticTypeModel customization produces duplicate final JSON property names for the same type, the implementation must fail deterministically.

Acceptable failure mechanisms:

- throw a documented exception from the resolver modifier;
- surface a diagnostic/result if the API returns diagnostics.

The implementation must not silently keep one duplicate and discard or overwrite another.

## Attribute Import

When configured, extraction recognizes relevant `System.Text.Json.Serialization` attributes and imports them as annotations.

At minimum:

```text
JsonPropertyNameAttribute
JsonIgnoreAttribute
JsonIncludeAttribute
JsonConverterAttribute
JsonNumberHandlingAttribute
JsonRequiredAttribute
JsonExtensionDataAttribute
```

Where available for the target framework, extraction may also preserve:

```text
JsonObjectCreationHandlingAttribute
JsonUnmappedMemberHandlingAttribute
JsonPolymorphicAttribute
JsonDerivedTypeAttribute
```

Custom converter behavior is not inferred. Converter metadata may be preserved as an annotation or produce a diagnostic when unsupported for a requested operation.

## Diagnostics

`STJ001` through `STJ008` remain the current System.Text.Json integration diagnostic range unless new diagnostics are required.

Existing meanings:

| Diagnostic | Meaning |
|---|---|
| `STJ001` | Conflicting semantic name and JSON property name policy. |
| `STJ002` | Unsupported `JsonConverter` metadata when preservation is disabled or unavailable. |
| `STJ003` | `JsonIgnore` conflicts with required semantic member. |
| `STJ004` | Retired or repurposed generated-context diagnostic; must not be reused for unrelated behavior. |
| `STJ005` | Generated-context object-member diagnostic is obsolete unless explicitly repurposed for resolver guidance. |
| `STJ006` | `JsonRequired` conflicts with nullable or optional semantic member. |
| `STJ007` | Unsupported `JsonExtensionData` member type. |
| `STJ008` | Polymorphism metadata cannot be represented canonically. |

If diagnostics are added or changed, follow the repository diagnostic rules and update the public diagnostics reference.

## Invariants

- The canonical semantic model remains independent of `System.Text.Json`.
- `System.Text.Json` attributes are imported as metadata, not as canonical model structure.
- Semantic property names and JSON serialization names remain separate concepts.
- Semantic names are used as JSON property names only when explicitly configured.
- Resolver customization preserves existing resolver behavior unless explicitly overridden.
- Generated `JsonSerializerContext` declarations are unsupported.
- Unsupported or ambiguous behavior must be explicit; silent lossy behavior is not allowed.
