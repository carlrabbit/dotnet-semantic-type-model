# Projection Capabilities

## Goal

Decide whether a semantic feature is supported directly, supported with options, degraded with diagnostics, or unsupported for a target projection.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.2.0`.

## Packages

- Projection packages such as `SemanticTypeModel.JsonSchema`, `SemanticTypeModel.EFCore`, `SemanticTypeModel.PowerBI`, `SemanticTypeModel.SystemTextJson`, and `SemanticTypeModel.Configuration`.
- `SemanticTypeModel.Core` for shared capability concepts and diagnostics.

## Minimal path

1. Identify the target projection.
2. Check whether the feature is core semantic meaning or target-specific representation.
3. Read the target guide for supported policy choices.
4. Inspect projection diagnostics after derivation.
5. Change source semantics or target policy when support is partial.

## Full example

```csharp
var json = AppSemanticTypeModel.Create().DeriveJsonSchemaModel();
var ef = AppSemanticTypeModel.Create().DeriveEfCoreModel();
var powerBi = AppSemanticTypeModel.Create().DerivePowerBiModel();
var configuration = AppSemanticTypeModel.Create().DeriveConfigurationModel();

json.Diagnostics.ThrowIfErrors();
ef.Diagnostics.ThrowIfErrors();
powerBi.Diagnostics.ThrowIfErrors();
configuration.Diagnostics.ThrowIfErrors();
```

## How it works

Projection packages consume the same generated semantic model but apply target-specific policies. A value of `supported with policy` means users must select or review a target option; it does not mean every shape is lossless.

## Options and policies

| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
| Capability status vocabulary | No default | `supported`, `supported with policy`, `preserved as metadata`, `ignored by default`, `unsupported with diagnostics`, `planned`, `not applicable` | Keeps cross-guide support statements consistent | Vague prose should be replaced by one of these statuses. |
| Diagnostics review | Required after each projection | Target diagnostics collection | Shows feature loss or unsupported values | Skipping diagnostics can hide lossy output. |
| Target policy | Target default | Projection-specific options | Changes representation per target | Unsupported combinations must be diagnosed. |

## Capability matrix

| Capability | Core semantic? | JSON Schema | EF Core | Power BI | System.Text.Json | Configuration | Default behavior | Diagnostics |
|---|---|---|---|---|---|---|---|---|
| Entity | yes | supported | supported with key policy | supported as table | preserved as metadata | ignored by default | Entity roles inform target defaults | Missing key for EF is diagnostic. |
| ValueObject | yes | supported | supported with policy | supported with policy | preserved as metadata | supported for nested options | EF flattens; Power BI diagnoses by default | Unsupported nested shapes diagnosed. |
| Configuration | yes | preserved as metadata | ignored by default | ignored by default | ignored by default | supported | Requires section metadata for binding | Missing section is diagnostic. |
| Required / Nullable | yes | supported | supported | preserved as metadata | supported with resolver metadata | supported validation | Mapped from CLR/semantic metadata | Contradictions diagnosed by target. |
| Constraint | yes | supported when representable | preserved as metadata | preserved as metadata | not applicable | supported when representable | JSON Schema emits validation keywords | Unsupported constraints diagnosed. |
| RequiredWhen | yes | supported with policy | unsupported with diagnostics | unsupported with diagnostics | not applicable | supported | Equality conditions only | STM1020-STM1024 style diagnostics. |
| Enum | yes | supported | supported with policy | supported with policy | supported by existing contract | supported validation | Names by default for schema/Power BI; strings for EF | Unsupported numeric assumptions diagnosed. |
| Format | yes | supported | preserved as metadata | supported/preserved | not applicable | preserved/validation hint | Emits target metadata where meaningful | Unknown formats may be hints only. |
| DisplayName / Description | yes | supported | supported with naming option | supported | ignored by default | preserved for diagnostics/docs | Labels/descriptions flow to docs/output | Name collisions diagnosed. |
| Ownership | yes | supported as nested schema | supported with policy | supported with policy | preserved by contract shape | supported for nested binding | EF flattens; Power BI diagnoses by default | Cycles/unsupported collections diagnosed. |
| Envelope payload | yes | supported with root/payload policy | supported with storage policy | supported with analytical policy | preserved by contract shape | not applicable | Target-specific policy required for non-default shape | Missing/multiple payload diagnostic. |
| Version / Revision | yes | preserved as metadata | preserved as metadata | preserved as metadata | preserved by contract shape | preserved | No automatic migration/concurrency | Misuse as target behavior is not enforced. |
| Temporal validity | yes | preserved as metadata | preserved as metadata | preserved as metadata | preserved by contract shape | not applicable | No EF temporal tables by default | Invalid endpoints diagnosed in core. |
| Lifecycle state | yes | supported as property/metadata | supported as property/metadata | supported as categorical metadata | preserved by contract shape | preserved | No workflow behavior | Unsupported enum/state shape diagnosed by target. |
| Extension data | yes | supported with additionalProperties policy | unsupported with diagnostics or JSON policy | unsupported with diagnostics | supported when resolver has extension data | supported/planned as unknown-key policy | Dictionary-like members only | Non-dictionary shapes diagnosed. |
| Target-specific metadata | no | supported in JSON Schema namespace | supported in EF namespace | supported in Power BI namespace | supported in STJ namespace | supported in Configuration namespace | Ignored by other targets | Invalid target values diagnosed by target. |

## Diagnostics

| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
| Capability marked unsupported | Target cannot represent the semantic safely | Change source semantics, choose a target policy, or handle manually. |
| Capability marked preserved only | Target keeps metadata but does not enforce behavior | Do not claim runtime behavior; add target-specific code if needed. |
| Lossy projection warning | Target maps a richer semantic to a simpler construct | Accept explicitly or change the model/policy. |
| Name collision | Target naming policy maps two items to one name | Change names or select suffix behavior where available. |

## Common mistakes

- Reading `supported with policy` as “always automatic.”
- Assuming one target’s default is shared by every projection.
- Treating preserved metadata as runtime enforcement.
- Ignoring warnings because export still produced output.

## Limitations

The matrix is a decision aid, not a lossless-shape guarantee. Provider-specific EF Core features, Power BI service behavior, JSON Editor runtime behavior, and host configuration provider behavior remain outside the shared model.

## Related docs

- [JSON Schema guide](json-schema.md)
- [EF Core projection guide](ef-core-projection.md)
- [Power BI projection guide](power-bi-projection.md)
- [Compatibility](../api/compatibility.md)
