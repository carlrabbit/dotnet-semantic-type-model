# Samples

Samples are executable documentation under `samples/`. Public samples demonstrate consumer package usage and are validated against locally prepared SemanticTypeModel NuGet packages.

## Available Samples

- [Code-first JSON Schema](samples/code-first-json-schema.md) — annotated C# domain model, packaged generator, generated provider, semantic derivation, and JSON Schema export.
- [Code-first EF Core](samples/code-first-ef-core.md) — annotated C# domain model, packaged generator, generated provider, EF Core domain semantic model derivation, and provider-neutral `ModelBuilder` configuration.
- [Code-first Power BI](samples/code-first-powerbi.md) — annotated C# domain model, packaged generator, generated provider, Power BI domain semantic model derivation, and local metadata output.
- [System.Text.Json resolver](samples/system-text-json-resolver.md) — user-authored `JsonSerializerContext` customized by SemanticTypeModel resolver metadata.
- [Runtime DI](samples/runtime-di.md) — dependency-injection registration and projection usage.

New consumers should start with the code-first samples. JSON Schema import/roundtrip flows are not the supported canonical authoring path.

## Run the Samples

Prepare local packages, then run package-based sample validation:

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

`./eng/samples.sh` restores SemanticTypeModel packages from `artifacts/nuget` and keeps public feeds available for third-party dependencies.

### Shared Order Fulfillment sample model

The code-first projection samples now share `samples/OrderFulfillment.Domain`. Each executable creates `OrderFulfillmentSemanticModel.Create()` and then applies only its own projection. This demonstrates that a complete semantic model can contain EF Core entities, JSON Schema editing contracts, Power BI analytical tables, System.Text.Json envelope serialization types, runtime DI registration, and Configuration option types without requiring every consumer to consume every type.

## 2.4.1 Sample Canary

Package-based samples should continue to validate generated canonical models when shared contracts include dictionary-backed extension data. The 2.4.1 patch release uses the Order Fulfillment sample suite as a canary for the former `STM0002` dictionary-key extraction regression.

## 2.4.2 URI and EF owned-value-object scenario

The shared Order Fulfillment model includes URI scalars and owned ValueKinds. The EF Core sample verifies the fixed 2.5.0 relational model and CLR-backed application path.

## 2.4.4 closed EF scenario

The package-based EF sample derives `EfRelationalModel` and applies the fixed CLR-backed table, scalar, TPT, and JSON-column contract through `ApplySemanticRelationalModel`.
