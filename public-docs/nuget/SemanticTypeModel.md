# SemanticTypeModel Package Suite

This README is shared by all `SemanticTypeModel.*` NuGet packages because the packages are designed, versioned,
and tested as one tightly coupled suite.

## Version alignment

**Use the same exact version for every `SemanticTypeModel.*` package in your application. Mixing SemanticTypeModel
package versions is unsupported.**

Generator/analyzer packages are part of that rule too. The compile-time semantic manifest requires exact
producer/consumer suite-version alignment.

## Choose packages by scenario

| Scenario | Packages |
|---|---|
| Define semantic annotations | `SemanticTypeModel.DotNet` |
| Generate the canonical model at compile time | `SemanticTypeModel.Generators` |
| JSON Schema | `SemanticTypeModel.JsonSchema` |
| EF Core | `SemanticTypeModel.EFCore`, `SemanticTypeModel.EFCore.Generators` |
| System.Text.Json | `SemanticTypeModel.SystemTextJson` |
| Microsoft.Extensions.Options | `SemanticTypeModel.Configuration` |
| Power BI local metadata | `SemanticTypeModel.PowerBI` |
| Runtime DI composition | `SemanticTypeModel.DependencyInjection` |

`SemanticTypeModel.Configuration.Generators` is not part of the current package suite.

`SemanticTypeModel.Abstractions` and `SemanticTypeModel.Core` provide shared model/runtime contracts used by the
suite. Applications normally start from the scenario packages rather than selecting internal package layers
first.

## Minimal model

```csharp
using SemanticTypeModel.DotNet;

[SemanticType(SemanticTypeRole.Entity)]
[SemanticMutable]
public sealed class Customer
{
    [SemanticKey]
    [SemanticImmutable]
    public required string Id { get; init; }

    public required string Name { get; init; }
}
```

With `SemanticTypeModel.Generators`, build the project and consume the generated provider:

```csharp
using SemanticTypeModel.Generated;

TypeSchemaModel model = AppSemanticTypeModel.Create();
```

Lifecycle mutability is optional. No mutability attribute means STM makes no lifecycle-mutability claim.

## Configure

Common generator settings include:

- generated namespace/provider name;
- discovery mode and namespace filters;
- internal type/member inclusion;
- naming policy;
- key inference;
- technical-description requirements;
- System.Text.Json metadata import.

General relationship inference is not a current generator capability.

See the complete [configuration reference](../configuration.md).

## JSON Schema semantic annotations

`SemanticTypeModel.JsonSchema` exports Draft 2020-12 and can preserve selected STM-only semantics under one
optional `x-stm` object:

```text
role
aggregateRoot
mutability
technicalDescription
keys
unit
ui
```

JSON Schema import and JSON Editor compatibility modes are not supported current APIs.

## EF Core application

`SemanticTypeModel.EFCore.Generators` emits composable `IEntityTypeConfiguration<TEntity>` implementations for
explicitly selected semantic models. Applications own `DbContext` composition and unrelated/manual entities.

The model assembly's semantic manifest is ephemeral compile-time transport. Model and EF generator packages must
use the same exact SemanticTypeModel suite version.

## Diagnose

If generation or projection fails:

- [Troubleshooting](../troubleshooting.md) — symptom-oriented fixes;
- [Diagnostics](../diagnostics.md) — diagnostic IDs and fixes;
- target guides below — target-specific limitations and policies.

## Target guides

- [Using SemanticTypeModel](../usage.md)
- [Core semantics](../guides/core-semantics.md)
- [JSON Schema](../guides/json-schema.md)
- [EF Core](../guides/ef-core.md)
- [System.Text.Json](../guides/system-text-json.md)
- [Configuration / Options](../guides/configuration-options.md)
- [Power BI](../guides/power-bi.md)
- [Projection capabilities](../guides/projection-capabilities.md)

## Package roles

| Package | Responsibility |
|---|---|
| `SemanticTypeModel.Abstractions` | Shared canonical model/runtime contracts |
| `SemanticTypeModel.Core` | Core semantics, transformations, diagnostics, inspection |
| `SemanticTypeModel.DotNet` | Attributes and Roslyn extraction contracts |
| `SemanticTypeModel.Generators` | Compile-time canonical model provider and semantic manifest generation |
| `SemanticTypeModel.JsonSchema` | JSON Schema derivation and Draft 2020-12 export |
| `SemanticTypeModel.EFCore` | EF relational inspection, selection contract, converters/comparers/helpers |
| `SemanticTypeModel.EFCore.Generators` | Generated composable `IEntityTypeConfiguration<TEntity>` application |
| `SemanticTypeModel.SystemTextJson` | Resolver metadata derivation/customization |
| `SemanticTypeModel.Configuration` | Configuration semantic model and explicit Options registration |
| `SemanticTypeModel.PowerBI` | Deterministic local analytical metadata projection |
| `SemanticTypeModel.DependencyInjection` | Runtime provider/projection service registration |

## Important boundaries

SemanticTypeModel defines semantic meaning and target projection defaults; applications own global target
infrastructure composition.

For example:

- EF Core remains responsible for providers/migrations/database lifecycle; applications own their `DbContext`,
  manual EF configuration, and target-specific relationships;
- System.Text.Json serializer contexts remain application-owned;
- Power BI service publishing/authentication remains outside local metadata projection;
- JSON Schema is an export target, not a canonical authoring source.

See [Compatibility](../api/compatibility.md) for current breaking boundaries.
