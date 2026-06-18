# Projection Capabilities

## Goal

Decide whether a semantic feature is supported directly, supported with options, degraded with diagnostics, or unsupported for a target projection.

## Prerequisites

- .NET 10 SDK.
- Annotated .NET types are the canonical authoring source.
- A generated semantic model provider such as `AppSemanticTypeModel.Create()` is available.
- The examples assume package version `2.2.0`.

## Packages

- Projection packages such as `SemanticTypeModel.JsonSchema`, `SemanticTypeModel.EFCore`, and `SemanticTypeModel.PowerBI`.
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

json.Diagnostics.ThrowIfErrors();
ef.Diagnostics.ThrowIfErrors();
powerBi.Diagnostics.ThrowIfErrors();
```

## How it works

Annotated .NET code is extracted by the generator into a `TypeSchemaModel`. Core transformations normalize projection-neutral semantics. The target package derives a domain semantic model and then exports or applies target-specific output when that target supports it.

## Options and policies

Capability handling is target-specific. JSON Schema can choose schema shape policies, EF Core can choose mapping policies, and Power BI can choose analytical projection policies. Diagnostics should explain unsupported or lossy cases.

## Diagnostics

Projection diagnostics are the compatibility signal for feature loss. Review IDs, message text, target path, and suggested policy changes before accepting output.

## Common mistakes

- Treating JSON Schema files as the canonical authoring source for new models.
- Mixing target-specific metadata with projection-neutral semantics.
- Skipping diagnostic inspection before using projected output.
- Using stale pre-2.2 model namespace or shape names in current examples.

## Limitations

A capability matrix is not a promise that every source model shape maps losslessly to every target. Provider-specific EF Core features, Power BI service behavior, and JSON Editor runtime behavior remain outside the shared model.

## Related docs

- [JSON Schema guide](json-schema.md)
- [EF Core projection guide](ef-core-projection.md)
- [Power BI projection guide](power-bi-projection.md)
- [Compatibility](../api/compatibility.md)
