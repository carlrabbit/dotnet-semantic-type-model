# Executable Samples

Samples are executable consumer examples under `samples/`. The source project is the detailed documentation;
this page is only an index.

| Scenario | Project | Demonstrates |
|---|---|---|
| Code-first JSON Schema + TestData | `samples/code-first-json-schema/` | Generated model -> deterministic Random/Profile-guided TestData -> typed CLR value, plus JSON Schema derivation/export |
| Code-first EF Core | `samples/code-first-ef-core/` | Selected semantic manifest -> generated EF configurations |
| Code-first Power BI | `samples/code-first-powerbi/` | Generated model -> local analytical metadata |
| System.Text.Json resolver | `samples/system-text-json-resolver/` | Application-owned resolver/context customized with semantic metadata |
| Runtime DI | `samples/runtime-di/` | Runtime provider/projection composition |
| Programmatic model catalog | `samples/programmatic-model-catalog/` | Dynamic canonical authoring -> occurrence-diverse Random, TestData sampling profiles, and Semantic Terminology Profile-guided semantic TestData -> deterministic inspection, including `random-diversity`, `sampling-profile`, and unsupported diagnostics |
| Shared model | `samples/OrderFulfillment.Domain/` | Shared annotated domain consumed by multiple target samples |

## Run

Prepare local packages first, then run sample validation:

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

All `SemanticTypeModel.*` package references used together must use the same exact version.

The programmatic catalog is individually navigable. Use `list` for stable scenario IDs, one ID for a focused
example, or `all` for the complete executable catalog:

```sh
dotnet run --project samples/programmatic-model-catalog -- list
dotnet run --project samples/programmatic-model-catalog -- scalar-string
dotnet run --project samples/programmatic-model-catalog -- random-diversity
dotnet run --project samples/programmatic-model-catalog -- sampling-profile
dotnet run --project samples/programmatic-model-catalog -- all
```

Its “Example-guided” sections use validated Semantic Terminology Profile candidates. They are distinct from Random
TestData and from programmatic generator callbacks; the source code is the detailed documentation.

For explanation and options, use the corresponding [public guides](usage.md), including the
[TestData guide](guides/test-data.md), rather than adding per-sample Markdown pages.
