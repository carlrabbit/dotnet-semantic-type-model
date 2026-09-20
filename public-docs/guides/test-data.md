# Constraint-aware test data

`SemanticTypeModel.TestData` generates a finite semantic value graph from a canonical `TypeSchemaModel` and can
materialize a successful graph into a public CLR object. It does not change the canonical model.

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
the `TESTDATA_*` prefix and a canonical model path when available.

## Typed generation and materialization

Programmatically authored models use the semantic-value facade without CLR materialization:

```csharp
SemanticTestValue value = model.TestData().Generate(new TypeId("Order"));
IReadOnlyList<SemanticTestValue> values = model.TestData().WithSeed(42).GenerateMany(new TypeId("Order"), 10);
```

Canonical-ID property generators work without a CLR type and are checked against the same constraints:

```csharp
model.TestData()
    .WithPropertyGenerator(new TypeId("Order"), new PropertyId("Number"), _ => "ORD-001")
    .Generate(new TypeId("Order"));
```

`GenerateMany` returns an empty sequence for zero and rejects negative counts. These operations return
`SemanticTestValue` graphs; they do not synthesize CLR types. Invalid generation throws
`TestDataGenerationException` with `TESTDATA_*` diagnostics.

The convenience facade preserves canonical semantics while providing typed generation:

```csharp
TestDataScenario scenario = model.TestData()
    .WithTerminology(profile)
    .WithSizeProfile(TestDataSizeProfile.Moderate)
    .WithSeed(42)
    .Generate<TestDataScenario>();

IReadOnlyList<TestDataScenario> scenarios = model.TestData().WithSeed(42).GenerateMany<TestDataScenario>(10);
```

`Generate<T>()` uses public constructors and writable public members. Use `Materialize<T>(value)` to materialize
an existing semantic graph without regenerating it. Private construction, private-member mutation, and inference
of CLR single-value wrappers are unsupported and produce `TestDataGenerationException` diagnostics.

Register property or Logical Type generators for domain values. Property callbacks take precedence over Logical
Type callbacks, profile candidates, and built-in generation. Callback results are checked against canonical
constraints. `WithBudgets` sets explicit generation ceilings; exhausted budgets remain deterministic `TESTDATA_*`
failures.

## Profile-guided terminology

`SemanticTerminologyProfileJson.Export(model)` creates a deterministic version-1 JSON sidecar containing
AI-facing instructions and read-only canonical context. Enrich only the `values` arrays, then import with
`SemanticTerminologyProfileJson.Import(model, json)`. The live canonical model remains authoritative; profile
instructions and context are informational. A profile is bound to the exact model ID and invalid or stale
entries produce deterministic `TESTDATA_PROFILE_*` diagnostics.

Use the imported profile with `WithTerminology` or the low-level overload of `SemanticTestDataGenerator.Generate`. Profile-guided mode
selects valid property-specific candidates first, then reusable Logical Type candidates, and falls back to the
deterministic random generator. Random mode remains available without a profile. Candidates are validated
against scalar formats and constraints, including patterns; STM does not synthesize regex values, bypass custom
constraints, or infer terminology from names or CLR wrapper shapes.

## Inspecting semantic values

Programmatic models and other consumers that do not have CLR types can inspect a generated semantic graph directly:

```csharp
using SemanticTypeModel.TestData.Inspection;

SemanticTestValue value = model.TestData().WithSeed(42).Generate(new TypeId("Order"));
string text = value.ToSemanticText(model);
```

Inspection is deterministic, invariant-culture, newline-normalized human-readable development/test output. It
resolves object property names from the canonical model and preserves object, array, dictionary, enum, scalar, and
null structure without materializing CLR objects. It is not JSON serialization, a wire format, a parser contract,
or a persisted fixture format.

The executable [programmatic model catalog](../samples.md) labels Random and Example-guided output separately.
Example-guided means candidates supplied through a validated Semantic Terminology Profile. Programmatic property or
Logical Type callbacks are a separate customization capability and are not terminology examples.
