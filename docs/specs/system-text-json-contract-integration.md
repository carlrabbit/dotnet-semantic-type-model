# System.Text.Json Contract Integration Spec

## Status

Authoritative behavioral specification.

## Purpose

`SemanticTypeModel.SystemTextJson` applies supported runtime JSON representation derived from the canonical semantic model while keeping System.Text.Json-specific behavior out of projection-neutral core semantics.

This spec is authoritative for:

- System.Text.Json annotation keys;
- semantic-name and serialization-name separation;
- System.Text.Json attribute import behavior;
- runtime `JsonSerializerOptions` / resolver customization behavior;
- automatic semantic-Entity polymorphism;
- runtime multi-model composition;
- unsupported generated-context generation behavior;
- System.Text.Json integration diagnostics.

The package-owned domain model is specified in `docs/specs/system-text-json-domain-model-and-resolver-projection.md`.

## Package Boundary

- `SemanticTypeModel.Abstractions` remains projection-neutral and has no System.Text.Json dependency.
- `SemanticTypeModel.DotNet` may import System.Text.Json attribute metadata into canonical annotations when configured.
- `SemanticTypeModel.SystemTextJson` owns annotation constants, projection options, domain-model derivation, runtime resolver/options helpers, and automatic Entity-polymorphism projection.
- `SemanticTypeModel.Generators` does not generate `JsonSerializerContext` declarations.
- M0071 adds no ASP.NET Core dependency to the package; Minimal API usage configures the application's existing `JsonSerializerOptions`.

## Annotation Keys

Current imported keys include:

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
| `systemTextJson.polymorphism` | Existing application STJ polymorphism metadata marker when preserved/imported |

The .NET extractor may continue recording internal CLR matching annotations such as `dotnet.memberName`.

Automatic semantic-Entity polymorphism is derived from canonical Entity inheritance plus CLR identity; it is not authored as a new canonical polymorphism/discriminator annotation.

## Name Boundary

Semantic property/type names and JSON representation names are distinct concepts.

`JsonPropertyNameAttribute` remains target-specific metadata unless an explicit extraction option promotes it to semantic naming.

Semantic property names are used as JSON property names only under the configured property-name source policy.

For automatic Entity polymorphism, canonical semantic **type Name** is the default derived discriminator value. This is a target projection policy and does not create canonical discriminator semantics.

## Runtime Options Are the Primary Surface

Normal complete runtime configuration is:

```csharp
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
options.AddSemanticTypeModelJson(model);
```

If no resolver exists, use `DefaultJsonTypeInfoResolver` as the base resolver.

If an existing resolver exists, preserve and compose over it.

The options helper configures supported runtime metadata behavior, including automatic semantic-Entity polymorphism.

The domain-model and canonical-model options overloads are behaviorally equivalent for an equivalent derived model.

## Resolver Composition and Immutability

STM must not require consumers to add package callbacks to `DefaultJsonTypeInfoResolver.Modifiers`.

Package behavior composes at the `IJsonTypeInfoResolver` boundary and customizes `JsonTypeInfo` returned by the base resolver before returning that metadata to System.Text.Json.

Multiple STM models may be registered on one `JsonSerializerOptions` instance before first serializer use.

After serialization/deserialization begins, System.Text.Json freezes options/metadata. STM does not promise post-freeze model registration.

## Property Name Source

Supported policies:

| Name source | Meaning |
|---|---|
| `ExistingJsonContract` | Preserve the base resolver/context property name. |
| `SystemTextJsonPropertyNameAnnotation` | Use imported `systemTextJson.propertyName` when present. |
| `SemanticPropertyName` | Use canonical semantic property name. |

Default behavior preserves the existing JSON contract.

## Matching JsonPropertyInfo to Semantic Properties

Do not rely only on `JsonPropertyInfo.Name` when naming policy, attributes, or previous resolver behavior may have changed it.

Use stable CLR/member identity where possible. Unsafe matching must not silently mutate unrelated properties.

## Automatic Semantic-Entity Polymorphism

If a semantic Entity base has modeled concrete semantic Entity descendants matching CLR inheritance, STM automatically configures polymorphism when the effective base resolver has no explicit polymorphism contract.

Default contract:

```text
$type -> canonical semantic derived type Name
```

with ordinal case-sensitive discriminator identity.

This is intentionally opinionated and aligns the runtime projection with the fact that Entity inheritance is already meaningful in the semantic model and EF projection.

No opt-in switch is required for normal automatic Entity polymorphism.

### Explicit STJ Contract Precedence

If the base resolver already provides `JsonTypeInfo.PolymorphismOptions`, that application-owned contract wins and is left unchanged.

STM does not merge additional semantic descendants into it and does not overwrite it.

### Scope

M0071 automatic polymorphism applies to semantic Entity inheritance only.

It does not automatically configure polymorphism for arbitrary structural CLR inheritance, ValueObject inheritance, or cross-model inheritance.

## Multi-Model Composition

Repeated registration before first use is supported:

```csharp
options.AddSemanticTypeModelJson(modelA);
options.AddSemanticTypeModelJson(modelB);
```

Non-conflicting registrations are order-independent.

Model identity/CLR identity, not simple semantic names, determine ownership. Same simple names in different namespaces/models must not cross-contaminate hierarchy or converter behavior.

Incompatible duplicate ownership of the same CLR runtime contract fails deterministically rather than creating last-registration-wins semantics.

## Attribute Import

When configured, extraction recognizes relevant `System.Text.Json.Serialization` attributes and imports them as target annotations.

At minimum existing supported behavior includes:

```text
JsonPropertyNameAttribute
JsonIgnoreAttribute
JsonIncludeAttribute
JsonConverterAttribute
JsonNumberHandlingAttribute
JsonRequiredAttribute
JsonExtensionDataAttribute
```

Where supported by the target framework, extractor metadata may also preserve:

```text
JsonObjectCreationHandlingAttribute
JsonUnmappedMemberHandlingAttribute
JsonPolymorphicAttribute
JsonDerivedTypeAttribute
```

Existing explicit STJ polymorphism remains application-owned runtime behavior and takes precedence over STM automatic polymorphism.

## Generated Context Boundary

SemanticTypeModel does not generate `JsonSerializerContext` declarations.

M0071 is runtime-configuration focused and does not add, expand, or require source-generated-context testing/coverage.

Existing source-generated-context compatibility is not intentionally removed by M0071, but it is not part of M0071 acceptance evidence.

## Minimal API Consumer Integration

ASP.NET Core Minimal API applications configure the same runtime surface through application DI:

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.AddSemanticTypeModelJson(semanticModel);
});
```

This is consumer documentation/integration only; `SemanticTypeModel.SystemTextJson` does not acquire an ASP.NET Core dependency.

## JSON Schema Fidelity Boundary

Automatic STJ polymorphism does not expand the JSON Schema/System.Text.Json fidelity contract.

Polymorphic/discriminator output—whether application-owned or automatically projected from semantic Entity inheritance—is outside the M0066 one-way fidelity baseline until a future milestone explicitly models that representation in JSON Schema.

M0071 therefore does not add canonical discriminator semantics, JSON Schema `oneOf`, discriminator metadata, or a shared JSON wire model.

## Diagnostics

`STJ001` through `STJ008` remain existing integration diagnostics.

Reserve:

```text
STJ009
```

for invalid automatic semantic-Entity hierarchy projection, including duplicate discriminator values, unresolved/invalid CLR hierarchy identity, or incompatible duplicate hierarchy ownership across registered models.

Public diagnostics documentation must be synchronized in M0071.

## Invariants

- Canonical core remains independent of System.Text.Json.
- System.Text.Json attributes remain target metadata rather than canonical structure.
- Semantic property names and JSON serialization names remain separate.
- `JsonSerializerOptions` is the primary complete runtime integration surface.
- Automatic semantic Entity polymorphism is opinionated default target behavior when no explicit application STJ contract exists.
- Explicit application STJ polymorphism wins unchanged.
- Runtime composition does not depend on mutable `DefaultJsonTypeInfoResolver.Modifiers` registration.
- Multi-model runtime registration is supported before first serializer use.
- Source-generated context generation remains unsupported; M0071 does not expand context-specific behavior.
- Unsupported or ambiguous behavior is explicit; silent lossy behavior is not allowed.
