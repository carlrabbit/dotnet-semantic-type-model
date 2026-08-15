# SemanticTypeModel

SemanticTypeModel is a .NET 10 package suite for defining semantic meaning on .NET types once and using
that model across targets such as JSON Schema, EF Core, Power BI, System.Text.Json, and
Microsoft.Extensions.Options.

Annotated .NET code is the supported authoring source. The source generator builds a canonical
`TypeSchemaModel`; target packages derive or generate target-specific behavior from it.

## Install

**Use the same exact version for every `SemanticTypeModel.*` package in an application.** The packages are
released and tested as one aligned suite; mixing SemanticTypeModel package versions is unsupported.

Install the packages for the scenario you need:

| Scenario | Add these packages |
|---|---|
| Define and generate a semantic model | `SemanticTypeModel.DotNet`, `SemanticTypeModel.Generators` |
| JSON Schema | + `SemanticTypeModel.JsonSchema` |
| EF Core | + `SemanticTypeModel.EFCore`, `SemanticTypeModel.EFCore.Generators` |
| System.Text.Json | + `SemanticTypeModel.SystemTextJson` |
| Configuration / Options | + `SemanticTypeModel.Configuration` |
| Power BI metadata | + `SemanticTypeModel.PowerBI` |
| Runtime DI composition | + `SemanticTypeModel.DependencyInjection` |

The complete package-role map is in the [shared NuGet README](public-docs/nuget/SemanticTypeModel.md).

## First model

```csharp
using SemanticTypeModel.DotNet;

[SemanticType(SemanticTypeRole.Entity)]
public sealed class Customer
{
    [SemanticKey]
    public required string Id { get; init; }

    [SemanticDisplayName("Customer name")]
    public required string Name { get; init; }
}
```

Build the project with `SemanticTypeModel.Generators` referenced as an analyzer/package. The generated
provider defaults to:

```csharp
namespace SemanticTypeModel.Generated;

public static partial class AppSemanticTypeModel
{
    public static TypeSchemaModel Create();
}
```

Use it from application code:

```csharp
using SemanticTypeModel.Generated;

TypeSchemaModel model = AppSemanticTypeModel.Create();
```

See [Using SemanticTypeModel](public-docs/usage.md) for the complete first flow.

## Configure generation

Common generator settings include the generated namespace, provider name, discovery mode, namespace
filters, internal-type/member inclusion, naming policy, key inference, and technical-description
validation.

For example, change the generated namespace with an assembly option:

```csharp
[assembly: SemanticTypeModelGeneratorOptions(
    generatedNamespace: "MyApplication.SemanticModel")]
```

or with an MSBuild property:

```xml
<PropertyGroup>
  <SemanticTypeModelGeneratedNamespace>MyApplication.SemanticModel</SemanticTypeModelGeneratedNamespace>
</PropertyGroup>
```

See the [configuration reference](public-docs/configuration.md) for every generator setting, defaults,
allowed values, and examples.

## Use the model

- [JSON Schema](public-docs/guides/json-schema.md)
- [EF Core](public-docs/guides/ef-core.md)
- [System.Text.Json](public-docs/guides/system-text-json.md)
- [Configuration / Options](public-docs/guides/configuration-options.md)
- [Power BI](public-docs/guides/power-bi.md)
- [Projection capability matrix](public-docs/guides/projection-capabilities.md)

Runnable examples live directly under [`samples/`](samples/). The compact sample index is
[public-docs/samples.md](public-docs/samples.md).

## Diagnose problems

SemanticTypeModel reports model/projection diagnostics at runtime and `STMxxxx` diagnostics from source
generators at compile time.

Start with:

- [Troubleshooting](public-docs/troubleshooting.md) for symptom-oriented fixes;
- [Diagnostics](public-docs/diagnostics.md) for diagnostic ranges and common fixes;
- the target guide for projection-specific limitations and failure modes.

Do not ignore diagnostics merely because a projection produced output; warnings can indicate lossy target
representation.

## Compatibility and versions

All SemanticTypeModel packages in one application should use the same exact version. See
[Versioning](public-docs/versioning.md), [Compatibility](public-docs/api/compatibility.md), and
[Release notes](public-docs/release-notes.md).

## Contributing

Humans start with [CONTRIBUTING.md](CONTRIBUTING.md). Agents start with [AGENTS.md](AGENTS.md).
