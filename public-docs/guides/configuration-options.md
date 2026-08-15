# Configuration / Microsoft.Extensions.Options

This guide is for the `SemanticTypeModel.Configuration` projection. For configuring SemanticTypeModel itself
(source discovery, generated namespace, provider name, etc.), use
[SemanticTypeModel Configuration](../configuration.md).

## Use

Mark configuration types in the semantic model, then explicitly register each options type used by the
application:

```csharp
OptionsBuilder<ColdStorageOptions> options =
    builder.Services.AddSemanticOptions<ColdStorageOptions>(
        builder.Configuration,
        AppSemanticTypeModel.Create());
```

A complete semantic model may contain multiple Configuration types. Unselected types are not registered
automatically.

## Configure

Configuration semantics can describe section selection, section presence, DataAnnotations validation,
`ValidateOnStart`, conditional `RequiredWhen` validation, named-options metadata, and generated-helper intent
where supported.

Application code remains responsible for configuration providers, files, secrets, and host setup.

Generated helpers, when emitted by the selected package/build, are convenience wrappers and must delegate to
the same runtime `AddSemanticOptions<TOptions>` behavior rather than implementing another binding/validation
path.

## Diagnose

| Symptom | Likely cause | Fix |
|---|---|---|
| Missing/invalid section | Configuration semantic metadata does not identify a valid binding section | Add/fix section metadata or explicit call-site selection. |
| Startup `OptionsValidationException` | Bound values violate DataAnnotations/RequiredWhen/required-section policy | Fix deployed configuration or intentionally change validation policy. |
| `RequiredWhen` source unresolved | Source property/literal metadata is invalid | Use `nameof`, a supported typed literal, and a valid source property. |
| Generated helper cannot be found | Generator package/build did not emit the optional helper | Use runtime `AddSemanticOptions<TOptions>` unless generated output is actually present. |
| Unrelated options type was expected to register automatically | Registration is intentionally per-type | Call `AddSemanticOptions<TOptions>` for each type the application uses. |

## Reference

The package does not load configuration files, choose providers, own secret management, or replace normal
Microsoft.Extensions.Options composition.

See [Diagnostics](../diagnostics.md), [Troubleshooting](../troubleshooting.md), and
`samples/configuration-options/`.
