# SemanticTypeModel Configuration

This page is the consumer reference for configuring .NET extraction and generated semantic-model providers.
It is separate from the `SemanticTypeModel.Configuration` package, which projects semantic configuration types
into Microsoft.Extensions.Options; see [Configuration / Options](guides/configuration-options.md).

## Quick examples

### Change the generated namespace

Assembly configuration:

```csharp
[assembly: SemanticTypeModelGeneratorOptions(
    generatedNamespace: "MyApplication.SemanticModel")]
```

MSBuild configuration:

```xml
<PropertyGroup>
  <SemanticTypeModelGeneratedNamespace>MyApplication.SemanticModel</SemanticTypeModelGeneratedNamespace>
</PropertyGroup>
```

Default: `SemanticTypeModel.Generated`.

### Change the generated provider name

```csharp
[assembly: SemanticTypeModelGeneratorOptions(
    providerName: "DomainSemanticModel")]
```

or:

```xml
<PropertyGroup>
  <SemanticTypeModelGeneratedProviderName>DomainSemanticModel</SemanticTypeModelGeneratedProviderName>
</PropertyGroup>
```

Default: `AppSemanticTypeModel`.

## Generator option reference

| Purpose | Default | Assembly option | MSBuild/analyzer option |
|---|---|---|---|
| Generated namespace | `SemanticTypeModel.Generated` | constructor `generatedNamespace` | `SemanticTypeModelGeneratedNamespace` |
| Provider type name | `AppSemanticTypeModel` | constructor `providerName` | `SemanticTypeModelGeneratedProviderName` |
| Discovery mode | `ExplicitAttributes` | `DiscoveryMode` | `SemanticTypeModelDiscoveryMode` |
| Included namespace prefixes | empty | `IncludedNamespaces` | `SemanticTypeModelIncludedNamespaces` |
| Excluded namespace prefixes | empty | `ExcludedNamespaces` | `SemanticTypeModelExcludedNamespaces` |
| Include internal types | `false` | `IncludeInternalTypes` | `SemanticTypeModelIncludeInternalTypes` |
| Include internal members | `false` | `IncludeInternalMembers` | `SemanticTypeModelIncludeInternalMembers` |
| Naming policy | `Preserve` | `NamingPolicy` | `SemanticTypeModelNamingPolicy` |
| Infer keys | `false` | `InferKeys` | `SemanticTypeModelInferKeys` |
| Require technical descriptions | `false` | `RequireTechnicalDescription` | `SemanticTypeModelRequireTechnicalDescription` |
| Import System.Text.Json attributes | `false` | `ImportSystemTextJsonAttributes` | `SemanticTypeModelImportSystemTextJsonAttributes` |
| Use `JsonPropertyName` as semantic name | `false` | `UseJsonPropertyNameAsSemanticName` | `SemanticTypeModelUseJsonPropertyNameAsSemanticName` |

When both attribute and build-property configuration are supplied, the generator merges the configured
sources deterministically. Prefer one mechanism for a given setting unless you intentionally rely on that
merge; verify the generated result when combining them.

## Discovery modes

`DotNetTypeDiscoveryMode` supports:

| Value | Behavior |
|---|---|
| `ExplicitAttributes` | Discover explicitly attributed semantic roots only. |
| `Namespace` | Discover roots under configured namespace prefixes. |
| `AssemblyPublicTypes` | Discover public top-level types in the compilation assembly. |
| `ReachableFromRoots` | Discover explicit roots and include reachable graph types. |

Example:

```csharp
[assembly: SemanticTypeModelGeneratorOptions(
    DiscoveryMode = DotNetTypeDiscoveryMode.Namespace,
    IncludedNamespaces = "MyApplication.Domain",
    ExcludedNamespaces = "MyApplication.Domain.Internal")]
```

Use the corresponding MSBuild properties when project-level configuration is preferred.

## Naming policies

`DotNetNamingPolicy` supports:

- `Preserve`
- `CamelCase`
- `SnakeCase`
- `KebabCase`

Naming policy affects convention-derived semantic names. Explicit semantic names remain explicit input.

## Internal types and members

Internal types and members are excluded by default. Enable them independently:

```csharp
[assembly: SemanticTypeModelGeneratorOptions(
    IncludeInternalTypes = true,
    IncludeInternalMembers = true)]
```

If an expected internal type/member is missing, verify both the discovery mode and the relevant inclusion flag.

## Key and relationship inference

Inference is disabled by default:

```csharp
[assembly: SemanticTypeModelGeneratorOptions(
    InferKeys = true,
```

Prefer explicit semantic metadata when inference would be ambiguous or when the model is a durable public
contract.

## Technical descriptions

`RequireTechnicalDescription` requires the extraction path to obtain a technical description for supported
items. Explicit `SemanticTechnicalDescription` metadata and supported XML-summary fallback participate in
technical-description extraction.

## System.Text.Json extraction options

`ImportSystemTextJsonAttributes` imports supported System.Text.Json contract metadata as target-specific
annotations. `UseJsonPropertyNameAsSemanticName` additionally promotes `JsonPropertyName` into semantic naming;
it is disabled by default because serialization names and semantic names are different contracts.

SemanticTypeModel does not generate `JsonSerializerContext`; use an application-owned context/resolver with the
System.Text.Json integration guide.

## Inspect effective output

Write generated source to disk when diagnosing configuration:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Look for `SemanticTypeModel.Generated.g.cs` and, when applicable, the emitted manifest source.

## Common configuration failures

| Symptom | Likely cause | Fix |
|---|---|---|
| Generated provider is in the wrong namespace | `GeneratedNamespace` left at default or configured in the wrong project | Set `generatedNamespace` or `SemanticTypeModelGeneratedNamespace` in the model project. |
| Generated provider has an unexpected name | Provider-name setting not applied | Set `providerName` or `SemanticTypeModelGeneratedProviderName`; rebuild. |
| `STM5019` | Generated provider name collides with an existing type | Choose another generated namespace/provider name. |
| A public type is missing | Discovery mode/filter excludes it | Review `DiscoveryMode`, included/excluded namespaces, and root attributes. |
| An internal type/member is missing | Internal inclusion disabled | Enable the appropriate internal type/member flag. |
| `STM5008` | Unsupported discovery-mode value | Use a supported `DotNetTypeDiscoveryMode` value. |
| `STM5018` | Unsupported naming-policy value | Use a supported `DotNetNamingPolicy` value. |
| `STM5020` | Required technical documentation missing | Add technical description/XML summary or disable the requirement. |

See [Troubleshooting](troubleshooting.md) and [Diagnostics](diagnostics.md) for broader failures.
