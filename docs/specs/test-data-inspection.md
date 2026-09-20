# TestData Inspection Specification

## Status

Authoritative behavioral specification for deterministic human-readable inspection of
`SemanticTypeModel.TestData` semantic value graphs.

Introduced for M0081 on the 6.1.0 development line.

## Purpose

Provide a small inspection surface for a generated `SemanticTestValue` when no CLR type exists or CLR
materialization is not desired.

The capability answers:

```text
Given a canonical TypeSchemaModel and a SemanticTestValue graph,
can a consumer render the semantic value deterministically for console output,
tests, executable samples, and debugging without turning that output into a persistence format?
```

## Package Boundary

The capability belongs to:

```text
SemanticTypeModel.TestData
```

No new package is introduced.

The formatter consumes the canonical model and TestData's existing semantic value types. It must not require
`SemanticTypeModel.DotNet`, source generation, CLR materialization, JSON Schema, Power BI, EF Core, or
System.Text.Json integration.

The package may use its existing inward dependencies. It must not add a third-party formatting or serialization
dependency for this capability.

## Public Surface

The required public namespace is:

```text
SemanticTypeModel.TestData.Inspection
```

The required minimum API is equivalent to:

```csharp
public static class SemanticTestDataTextExtensions
{
    public static string ToSemanticText(
        this SemanticTestValue value,
        TypeSchemaModel model);
}
```

The exact static class implementation details are local mechanics. The extension method name, receiver/arguments,
namespace, and behavior defined by this specification are the public contract for M0081.

No options object or alternate machine-readable output format is required by M0081.

## Input Contract

Inspection requires both:

- the semantic value graph;
- the canonical model that defines the referenced type and property identities.

Rules:

- no CLR type is required;
- the model is never mutated;
- the semantic value graph is never mutated;
- inspection does not regenerate data;
- inspection does not validate or repair candidate/generation semantics already decided by TestData;
- a value/model mismatch must fail explicitly rather than inventing missing type/property names.

Ordinary null/misuse failures may use standard .NET argument/invalid-operation exceptions. M0081 does not create a
new `TESTDATA_*` diagnostic family for formatter misuse.

## Output Role

Inspection text is human-readable development/test output.

It is suitable for:

- executable sample output;
- console tools;
- diagnostics/debugging;
- focused snapshot-style tests.

It is not:

- a serialization format;
- a round-trip persistence format;
- a stable wire protocol;
- canonical JSON;
- a model snapshot;
- a replacement for Semantic Terminology Profile JSON;
- a contract that requires a parser.

Exact punctuation and indentation may evolve between aligned suite versions. Within one suite version, equivalent
input must produce deterministic output.

## Determinism

For the same aligned SemanticTypeModel version, canonical model, and semantic value graph, inspection text must be
deterministic across repeated runs and supported Windows/Linux environments.

Requirements:

- use invariant scalar formatting;
- normalize line endings to `\n`;
- do not include timestamps, process/machine data, random values, hashes, or culture-sensitive text;
- do not rely on `IReadOnlyDictionary` iteration order for object properties;
- preserve array item order;
- preserve the explicit ordered entry sequence represented by `DictionaryTestValue.Entries`.

Object properties must use a stable order based on canonical identity rather than runtime dictionary enumeration.
Ordinal `PropertyId` ordering is the baseline deterministic rule unless another existing canonical order contract
is explicitly applicable.

## Structural Rendering

Inspection must preserve the semantic graph shape.

### Scalar

Render the scalar value using a deterministic invariant textual representation.

Representative requirements:

- Boolean: culture-independent boolean text;
- String: quoted/escaped so whitespace and control characters are unambiguous;
- Integer/Number/Decimal: invariant numeric text;
- Date/Time/DateTime/DateTimeOffset: invariant unambiguous lexical forms;
- Duration: invariant duration text;
- Guid: canonical deterministic textual form;
- Binary: deterministic Base64 text rather than platform/debug byte formatting;
- Json: deterministic valid JSON text for the represented JSON scalar payload.

The renderer does not reinterpret scalar constraints or formats.

### Enum

Render the enum value together with enough semantic context to distinguish it from an untyped scalar.

### Object

Render:

- the root/object type identity;
- each generated property;
- the canonical property name resolved from the supplied model;
- the recursively rendered property value.

The formatter must not require `dotnet.memberName` or other CLR-lineage annotations.

If a property identity in the value graph cannot be resolved against the supplied canonical object model,
inspection fails explicitly.

### Array

Render items recursively in semantic item order.

### Dictionary

Render each entry as an explicit key/value pair.

Arbitrary semantic dictionary keys must not be coerced into JSON object-property names merely to make output look
like JSON.

### Null

Render null explicitly and preserve the nullable semantic value's type identity where needed for an unambiguous
human-readable representation.

## Relationship to Core Inspection

Core model inspection and TestData value inspection are sibling development-loop capabilities.

Typical dynamic-model sample flow is:

```csharp
string modelText = model.ToSemanticText();
SemanticTestValue value = model.TestData().Generate(rootTypeId);
string valueText = value.ToSemanticText(model);
```

The TestData formatter may follow established Core inspection conventions for normalization and readability, but
it remains owned by `SemanticTypeModel.TestData` because it understands TestData's package-owned value graph.

The capability must not move `SemanticTestValue` types into Core or make Core depend on TestData.

## Example-Guided Data

Inspection is value-source neutral.

A Random value and a Profile-guided value use the same inspection API. The renderer must not infer or annotate
whether a value came from Random generation, terminology, or a callback generator.

Samples and calling applications own section labels such as:

```text
Random
Example-guided
```

## Compatibility

M0081 is additive on the 6.1.0 line.

It does not change:

- `SemanticTestValue` semantic meaning;
- generation precedence;
- terminology profile format/version;
- generation diagnostics;
- CLR materialization behavior;
- canonical model contracts.

Inspection text itself is not a persisted compatibility protocol. Consumers requiring durable structured data
must not treat exact inspection text as such.

The M0081 validation package version is:

```text
6.1.0-m0081
```

Stable 6.1.0 publication is outside M0081 unless separately authorized.

## Tests

Short-running tests must cover at least:

- deterministic repeated output;
- Windows/Linux-neutral line endings and invariant formatting assumptions;
- scalar representative values including binary and temporal forms;
- enum;
- object property-name resolution;
- object-property ordering independent of dictionary insertion/enumeration order;
- nested object;
- array;
- dictionary with a non-string semantic key representation;
- explicit null;
- JSON scalar rendering;
- value/model mismatch failure.

Snapshot-style assertions are appropriate for representative formatter output because this is an inspection
surface, but tests must not reclassify the text as a persisted wire format.

## Public Documentation

The TestData guide must explain:

- how to call the inspection extension;
- why it is useful for dynamic/programmatic models;
- that it requires no CLR materialization;
- that it is deterministic human-readable inspection, not serialization/persistence.

The shared NuGet README may show the minimal call.

The executable programmatic-model catalog is the detailed code-as-documentation surface for the capability.

## Non-goals

This specification does not define:

- JSON/YAML/XML export of `SemanticTestValue`;
- a round-trip parser;
- persisted fixtures;
- CLR materialization;
- object-to-dictionary conversion as a public contract;
- target-specific serialization;
- changing TestData generation behavior;
- formatter options beyond the minimum M0081 surface;
- a general pretty-printing framework.
