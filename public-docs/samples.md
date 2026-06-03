# Samples

Samples are executable documentation under `samples/`. Public samples demonstrate consumer package usage and are validated against locally prepared SemanticTypeModel NuGet packages.

## Available Samples

- [JSON Schema roundtrip](samples/json-schema-roundtrip.md) — import, transform, validate, and export JSON Schema.
- [Code-first JSON Schema](samples/code-first-json-schema.md) — annotated C# domain model, packaged generator, generated provider, and JSON Schema export.
- [Code-first EF Core](samples/code-first-ef-core.md) — annotated C# domain model, packaged generator, generated provider, and EF Core projection metadata.
- [Code-first Power BI](samples/code-first-powerbi.md) — annotated C# domain model, packaged generator, generated provider, and Power BI projection metadata.
- [System.Text.Json resolver](samples/system-text-json-resolver.md) — user-authored `JsonSerializerContext` customized by SemanticTypeModel resolver metadata.
- [Runtime DI](samples/runtime-di.md) — dependency-injection registration and projection usage.

## Run the Samples

Prepare local packages, then run package-based sample validation:

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

`./eng/samples.sh` restores SemanticTypeModel packages from `artifacts/nuget` and keeps public feeds available for third-party dependencies.
