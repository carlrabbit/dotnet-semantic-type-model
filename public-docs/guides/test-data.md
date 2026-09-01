# Constraint-aware test data

`SemanticTypeModel.TestData` generates a finite semantic value graph from a canonical `TypeSchemaModel`.
It does not construct CLR objects and does not change the canonical model.

```csharp
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Generated;
using SemanticTypeModel.TestData;

TypeSchemaModel model = AppSemanticTypeModel.Create();
TestDataGenerationResult result = SemanticTestDataGenerator.Generate(
    model,
    new TypeId("global::MyApp.Order"),
    TestDataSizeProfile.Moderate,
    seed: 42);

if (result.HasErrors)
    throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(d => d.Message)));

SemanticTestValue value = result.Value!;
```

The default seed is `0`. `Simple`, `Moderate`, and `Extreme` target string/binary lengths of 8/32/1024 and
collection/dictionary sizes of 1/8/100, clamped by modeled constraints and fixed safety ceilings. Profiles do
not change numeric magnitude, optional-property probability, enum frequency, or business realism.

Supported generation includes canonical scalars, predefined formats, enums, objects and
composition, arrays, dictionaries, references, `Any`, nullability, and supported constraints. Generated
values are deterministic for the same model, root, profile, and seed.

Built-in generation fails closed for regex patterns, custom or unknown formats, custom constraints, `Unknown`,
`Never`, unions, intersections, unsatisfiable constraints, unresolved references, recursion without a legal
finite terminator, and exhausted uniqueness or safety budgets. These conditions return error diagnostics with
the `TESTDATA_*` prefix and a canonical model path when available. The initial capability does not provide CLR materialization,
invalid-data generation, or custom generator registration.
