# SemanticTypeModel

SemanticTypeModel is a .NET 10 library set for code-first semantic type models. Annotated .NET code is extracted into a canonical semantic model, transformed through deterministic pipelines, and projected into domain semantic models such as JSON Schema, EF Core, and Power BI metadata.

## Install

Add the package or packages your scenario needs:

```sh
dotnet add package SemanticTypeModel.Core --version 2.0.0
dotnet add package SemanticTypeModel.DotNet --version 2.0.0
dotnet add package SemanticTypeModel.Generators --version 2.0.0
dotnet add package SemanticTypeModel.JsonSchema --version 2.0.0
```

Scenario packages:

```sh
dotnet add package SemanticTypeModel.EFCore --version 2.0.0
dotnet add package SemanticTypeModel.PowerBI --version 2.0.0
dotnet add package SemanticTypeModel.SystemTextJson --version 2.0.0
dotnet add package SemanticTypeModel.DependencyInjection --version 2.0.0
```

See full package guidance in [public-docs/packages.md](public-docs/packages.md).

## Quick Start

Use annotated .NET code as the supported authoring source for the canonical model:

```csharp
using SemanticTypeModel;

[SemanticType(SemanticTypeRole.Entity)]
public sealed class Customer
{
    [SemanticKey]
    public required string Id { get; init; }

    [SemanticName("Customer name")]
    public required string Name { get; init; }
}

TypeSchemaModel model = AppSemanticTypeModel.Create();
```

From the generated canonical model, derive target-specific domain models:

```csharp
var jsonSchema = model.DeriveJsonSchemaModel();
var efCore = model.DeriveEfCoreModel();
var powerBi = model.DerivePowerBiModel();
```

Each derivation returns diagnostics and inspection output so unsupported or ambiguous semantics are visible instead of silently dropped.

## Package List

- `SemanticTypeModel.Abstractions`
- `SemanticTypeModel.Core`
- `SemanticTypeModel.JsonSchema`
- `SemanticTypeModel.DotNet`
- `SemanticTypeModel.Generators`
- `SemanticTypeModel.DependencyInjection`
- `SemanticTypeModel.PowerBI`
- `SemanticTypeModel.EFCore`
- `SemanticTypeModel.SystemTextJson`

## Stable Release Notice

`2.0.0` is the code-first semantic model release. It adds the core semantic vocabulary, envelope semantics, JSON Schema domain-model export, EF Core domain-model-to-`ModelBuilder` projection, and Power BI domain-model-to-local-metadata projection. Compatibility rules are documented in [public-docs/api/compatibility.md](public-docs/api/compatibility.md).

## Samples

Runnable samples live under `samples/` and are documented in [public-docs/samples.md](public-docs/samples.md).

Start with:

- `samples/code-first-json-schema`
- `samples/code-first-ef-core`
- `samples/code-first-powerbi`

Prepare local packages, then run all package-based samples:

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

## Public Docs

- [public-docs/getting-started.md](public-docs/getting-started.md)
- [public-docs/installation.md](public-docs/installation.md)
- [public-docs/concepts.md](public-docs/concepts.md)
- [public-docs/packages.md](public-docs/packages.md)
- [public-docs/api/public-api.md](public-docs/api/public-api.md)
- [public-docs/diagnostics.md](public-docs/diagnostics.md)
- [public-docs/guides/core-semantics.md](public-docs/guides/core-semantics.md)
- [public-docs/guides/json-schema.md](public-docs/guides/json-schema.md)
- [public-docs/guides/json-editor-compatibility.md](public-docs/guides/json-editor-compatibility.md)
- [public-docs/guides/ef-core-projection.md](public-docs/guides/ef-core-projection.md)
- [public-docs/guides/power-bi-projection.md](public-docs/guides/power-bi-projection.md)
- [public-docs/guides/projection-capabilities.md](public-docs/guides/projection-capabilities.md)
- [public-docs/guides/system-text-json.md](public-docs/guides/system-text-json.md)
- [public-docs/versioning.md](public-docs/versioning.md)
- [public-docs/release-notes.md](public-docs/release-notes.md)

## Contributor Docs

- [docs/TERMINOLOGY.md](docs/TERMINOLOGY.md)
- [docs/ENGINEERING.md](docs/ENGINEERING.md)
- [docs/MILESTONES.md](docs/MILESTONES.md)
- [docs/PUBLIC-DOCS.md](docs/PUBLIC-DOCS.md)
