# Samples

Samples are executable documentation under `samples/`. Public samples demonstrate consumer package usage and are validated against locally prepared SemanticTypeModel NuGet packages.

## Available Samples

- [Code-first JSON Schema](samples/code-first-json-schema.md) — annotated C# domain model, packaged generator, generated provider, semantic derivation, and JSON Schema export.
- [Code-first EF Core](samples/code-first-ef-core.md) — annotated C# domain model, packaged generator, generated provider, EF Core domain semantic model derivation, and provider-neutral `ModelBuilder` configuration.
- [Code-first Power BI](samples/code-first-powerbi.md) — annotated C# domain model, packaged generator, generated provider, Power BI domain semantic model derivation, and local metadata output.
- [System.Text.Json resolver](samples/system-text-json-resolver.md) — user-authored `JsonSerializerContext` customized by SemanticTypeModel resolver metadata.
- [Runtime DI](samples/runtime-di.md) — dependency-injection registration and projection usage.

Legacy JSON Schema import/roundtrip samples are not the primary 2.0.0 authoring path. Prefer code-first samples for new consumers.

## Run the Samples

Prepare local packages, then run package-based sample validation:

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

`./eng/samples.sh` restores SemanticTypeModel packages from `artifacts/nuget` and keeps public feeds available for third-party dependencies.
