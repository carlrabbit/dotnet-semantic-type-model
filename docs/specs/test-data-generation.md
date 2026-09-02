# Constraint-Aware Test-Data Generation Specification

## Status

Authoritative behavioral specification for the initial SemanticTypeModel test-data synthesis capability.

## Purpose

Define how SemanticTypeModel derives deterministic, valid synthetic values from a canonical `TypeSchemaModel` without introducing a second authoring source, domain-specific faker semantics, invalid-data generation, or CLR materialization policy.

The initial capability answers one question:

```text
Given a valid canonical semantic model, can STM synthesize a finite value graph that satisfies the supported semantic constraints?
```

## Package Boundary

The capability is owned by a new aligned suite package:

```text
SemanticTypeModel.TestData
```

The package consumes the current canonical semantic model and depends inward on canonical/core contracts. It must not require EF Core, JSON Schema, Power BI, System.Text.Json integration, the .NET source generator, or another target package at runtime.

Canonical packages must not depend on `SemanticTypeModel.TestData`.

`SemanticTypeModel.TestData` is a runtime capability, not a canonical model authoring source and not a target projection. Generated values never mutate or enrich `TypeSchemaModel`.

The package joins the aligned `SemanticTypeModel.*` suite and therefore uses the same exact package version as the rest of the suite whenever packed or consumed together.

## Development-Line Compatibility

This capability begins on the `5.1.0` development line after the completed `5.0.1` maintenance work.

M0075 uses prerelease validation version:

```text
5.1.0-m0075
```

M0075 does not authorize publication or claim a stable 5.1.0 API freeze. The semantic contract in this specification is authoritative; final convenience API and CLR materialization ergonomics remain reserved for the later developer-experience milestone.

## Generation Result Boundary

The package owns a semantic test-data value representation sufficient to preserve:

- the canonical type identity of each generated value;
- object property identity;
- scalar value/kind;
- array/collection element values;
- dictionary key/value entries;
- explicit null where null is used as a legal recursion terminator.

The initial generated representation is a finite, acyclic value graph. CLR object construction is not part of this specification.

Concrete public type names and builder/extension-method ergonomics are implementation mechanics for M0075 and may be refined before stable 5.1.0; the package must nevertheless expose a small usable runtime entry point that accepts a canonical model, a root type, a size profile, and an optional seed and returns a generation result with diagnostics.

A generation result with any error diagnostic is not successful generated test data. The implementation must not silently return a partially invalid value as success.

## Core Validity Invariant

For every feature declared supported by this specification:

```text
generated value
MUST satisfy
all applicable canonical semantic constraints
```

The generator must return an error when it cannot establish that invariant. Guessing, approximate validity, silent constraint dropping, or fallback to arbitrary stringification is forbidden.

The generator operates on the canonical model as supplied. It does not reinterpret CLR attributes or target-specific annotations to recover missing semantics.

## Determinism and Seeds

Generation is deterministic by default.

The default seed is `0`.

For the same SemanticTypeModel suite version and the same:

```text
canonical model content
root type
size profile
seed
```

the generated semantic value graph must be structurally equivalent across repeated runs and supported operating systems.

A different seed may select different valid scalar/enum/choice values, but different seeds are not required to produce distinct output.

Seed variation must never change whether a constraint is enforced or whether an unsupported semantic is diagnosed.

## Size Profiles

The initial named profiles are exactly:

```text
Simple
Moderate
Extreme
```

They are **size profiles**, not validity profiles, realism profiles, boundary-testing profiles, or numeric-magnitude profiles.

They control only target sizes for variable-length generated values:

| Value category | Simple | Moderate | Extreme |
|---|---:|---:|---:|
| String length | 8 | 32 | 1024 |
| Binary length | 8 | 32 | 1024 |
| Array/collection item count | 1 | 8 | 100 |
| Dictionary entry count | 1 | 8 | 100 |

For each generated value, the profile target is clamped into the effective legal interval formed by all applicable canonical minimum/maximum constraints.

Examples:

```text
MinLength=1, MaxLength=20, Extreme -> 20
MinLength=50, MaxLength=100, Moderate -> 50
MinItems=0, MaxItems=0, Simple -> 0
MinItems=3, no MaxItems, Simple -> 3
```

Where no semantic minimum/maximum exists, the profile target is used directly.

The three profiles do not control:

- numeric magnitude;
- date/time distance from an epoch;
- optional-property presence probability;
- null probability;
- enum frequency;
- business realism;
- invalid or adversarial data.

## Built-In Safety Budgets

Built-in generation is intentionally bounded even when the semantic model has no upper bound or declares a very large upper bound.

M0075 defaults are:

| Budget | Ceiling |
|---|---:|
| One generated string | 65,536 characters |
| One generated binary value | 65,536 bytes |
| One generated array/collection | 10,000 items |
| One generated dictionary | 10,000 entries |
| Nested generation depth | 32 |
| Total generated value nodes for one root generation | 100,000 |

Profile targets remain below these ceilings. A declared maximum above a ceiling does not force the generator to approach that maximum.

If a semantic minimum itself exceeds the relevant ceiling, or a finite valid graph cannot be produced inside the depth/node budgets, generation fails with an explicit diagnostic. The generator must not truncate below a semantic minimum.

Configurable budget overrides and higher-level stress-generation policy are reserved for later developer-experience work.

## Property Presence and Nullability

The baseline generator produces a complete representative object rather than probabilistically sparse data:

- every modeled object property is generated when a non-null finite value can be produced;
- optional properties are normally included;
- nullable properties are normally generated as non-null;
- additional/unmodeled object properties are not invented.

This policy makes optionality/nullability independent from the size profiles.

The only baseline exception is recursion termination. When recursive re-entry would otherwise make the value graph infinite, the generator may, in this order, use a semantically legal finite terminator such as:

1. null at a nullable use site;
2. omission of an optional property;
3. a zero-length collection/dictionary when zero is legal.

If no legal finite terminator exists, generation fails rather than producing an invalid graph.

## Constraint Composition

Type-level and property/use-site constraints are conjunctive. When more than one applicable constraint supplies a bound, the effective legal range is the intersection of those constraints.

If the effective constraint set is unsatisfiable, generation fails with an error identifying the affected model path.

The generator does not weaken one canonical constraint to satisfy another.

## Supported Scalar Kinds

Built-in generation supports these canonical scalar kinds:

```text
Boolean
String
Integer
Number
Decimal
Date
Time
DateTime
DateTimeOffset
Duration
Guid
Binary
Json
```

`Unknown` is unsupported and produces an error.

`Json` generation produces a deterministic valid non-null JSON value; the generator does not infer application-specific JSON structure.

Scalar generation must respect applicable numeric bounds, exclusive bounds, `multipleOf`, and precision/scale semantics where represented by the canonical model.

Size profiles do not deliberately choose small/moderate/extreme numeric values.

## String Formats

Built-in generation recognizes the current predefined semantic format family and the current URI-reference representation used by the canonical code-first surface:

```text
email
uri
uri-reference
hostname
ipv4
ipv6
date
time
date-time
duration
uuid
```

When such a format is applicable, generated values must satisfy both the format and all other applicable supported constraints.

A custom/unknown format is not guessed. Built-in generation returns an error unless a future explicit external value source supplies a candidate that can be validated against the declared contract.

## Regular-Expression Pattern Policy

Built-in regular-expression synthesis is explicitly out of scope.

If an applicable string constraint contains a non-empty `Pattern`, M0075 built-in generation returns an error for that value instead of attempting regex generation.

The durable rule is:

```text
Pattern does not authorize built-in regex synthesis.
```

A later terminology/value-source or custom-generation capability may supply a candidate value. When such a source exists, STM may validate the supplied candidate against the pattern and other constraints; that does not make regex synthesis part of the built-in generator.

M0075 itself does not add terminology documents or custom generator registration, so pattern-constrained values are an explicit built-in-generation failure in this milestone.

## Enums

Enum generation selects one declared enum value and preserves the enum type identity and declared value.

An enum with no usable declared value is a generation error.

Size profiles do not affect enum selection.

## Objects and Composition

Object generation produces the effective modeled property set, including supported inherited/composed object properties represented by the canonical model.

The generator must preserve canonical property identity and must not infer relationships, ownership storage, UI semantics, or target-specific behavior from object metadata.

Current `RequiredWhen` semantics are satisfied by the baseline complete-object policy because the target property is generated whenever possible. M0075 does not attempt to manufacture separate scenario families in which a condition is deliberately triggered or not triggered.

Object property-count constraints are supported only when a valid object can be formed from modeled properties under the no-additional-properties generation policy. If satisfying `MinProperties` would require inventing unmodeled properties, or `MaxProperties` conflicts with required/effectively emitted properties, generation fails.

## Arrays and Collections

Array/collection generation uses the size-profile target clamped to the effective `MinItems`/`MaxItems` range and safety budget.

`UniqueItems` must be honored. If the requested legal count cannot be produced uniquely from the finite item domain, generation fails instead of duplicating values.

## Dictionaries

Dictionary generation uses the size-profile target clamped to applicable bounds and safety budget.

Generated dictionary keys must be unique. If the supported key domain cannot supply enough unique keys for the required count, generation fails.

The package-owned semantic value representation is not required to force dictionary keys through JSON string-key rules.

## References

A `ReferenceTypeDefinition` resolves its canonical target and generates the target semantics while preserving the reference use-site context needed for constraints and recursion handling.

An unresolved reference is a generation error.

## Any, Never, Union, and Intersection

`Any` is supported through one deterministic built-in valid value choice.

`Never` cannot have a valid instance and therefore always produces a generation error.

`Union` and `Intersection` synthesis are out of scope for M0075. They produce explicit unsupported-generation errors rather than choosing an option/merge strategy whose validity could be ambiguous, especially for `oneOf` semantics.

A later milestone may add them only with a separate accepted semantic contract.

## Custom Constraints and Unknown Semantic Rules

A non-empty `CustomConstraint` set is not ignored. Because its validity semantics are not generically known, built-in generation returns an error unless a future explicit custom/value-source capability owns that constraint.

Unknown annotations that do not define canonical validation semantics do not become generation rules merely because they exist.

## Error and Diagnostic Policy

`SemanticTypeModel.TestData` owns package-specific runtime diagnostics using the descriptive prefix:

```text
TESTDATA_*
```

M0075 does not allocate a new stable `STMxxxx` numeric range.

Diagnostics must be deterministic, identify the canonical model path when available, and distinguish at least:

- unsupported type kind;
- unsupported scalar kind;
- unsupported custom format;
- pattern requires external/custom value source;
- custom constraint requires custom handling;
- unsatisfiable effective constraints;
- uniqueness domain exhausted;
- unresolved reference;
- recursion/depth budget exhausted;
- total generation budget exhausted.

Expected unsupported/unsatisfiable model conditions are reported through generation diagnostics rather than generic exceptions. Ordinary argument/null programmer errors may still use standard .NET exceptions.

## Dependency Policy

M0075 must not add a third-party faker library or regex-generation library.

The built-in generator uses canonical STM contracts and BCL/runtime functionality. Domain realism is deliberately reserved for external terminology enrichment rather than embedded faker datasets.

## Non-Goals

The baseline specification does not add:

- invalid/faulty-data generation;
- deliberate single-constraint violation generation;
- regex synthesis;
- terminology JSON export/import (added by M0078);
- AI integration or an AI SDK dependency;
- custom generator registration (added by M0079);
- CLR object materialization (added by M0079);
- the final typed developer-experience surface;
- cross-root dataset/key-uniqueness policy;
- probabilistic optional/null generation;
- weighted enum/business distributions;
- database seeding or EF Core integration;
- target-specific JSON Schema/System.Text.Json generation;
- union/intersection synthesis;
- arbitrary custom-constraint interpretation;
- publication or stable 5.1 release readiness.

## Required Cross-Boundary Evidence

At least one positive integration test must exercise the real code-first path:

```text
annotated CLR model
-> SemanticTypeModel.Generators
-> generated canonical TypeSchemaModel
-> SemanticTypeModel.TestData
-> generated semantic value graph
```

That evidence must include representative constraints, a collection, an enum, and ordinary scalar values. Hand-built canonical models remain appropriate for focused invalid/pathological generation tests.

The packed-package consumer smoke path must consume `SemanticTypeModel.TestData` from the current locally packed aligned suite rather than through a project reference.

## M0078 Semantic Terminology Profiles

An optional `SemanticTerminologyProfile` is a version-1 JSON sidecar owned by `SemanticTypeModel.TestData`.
Export it with `SemanticTerminologyProfileJson.Export(model)`, enrich only the candidate `values` fields, and
normalize it against the current model with `SemanticTerminologyProfileJson.Import(model, json)`. The profile
is bound to the exact `SchemaModelId`; instructions and exported context are informational and never replace
the live canonical model.

Import rejects unsupported format/version, model mismatches, duplicate identities, invalid scalar
representations, unsupported constraints, and scalar conflicts. Missing Logical Types or properties are stale
warnings and are ignored. Candidate lists are normalized by removing duplicates and sorting their JSON lexical
representation. Logical Type candidates are reusable across matching scalar properties, while property-specific
values take precedence and are filtered by the current use-site constraints.

Pass an imported profile to the overload of `SemanticTestDataGenerator.Generate` to enable Profile-guided mode.
Without a profile, Random mode remains unchanged. Profile-guided generation uses eligible property values, then
Logical Type values, then the built-in generator; a supplied patterned string is accepted only when STM can
validate it, and terminology never adds regex synthesis or bypasses unknown/custom constraints. Candidate values
are never mutated to meet size targets, and selection remains deterministic for the same seed and normalized
profile.

## M0079 Typed Test-Data Experience

The supported convenience surface is `model.TestData()`. It retains the low-level semantic-value API while
adding `WithSizeProfile`, `WithSeed`, `WithTerminology`, `WithBudgets`, `Generate<T>()`, and
`GenerateMany<T>(count)`. Bulk generation uses the root seed plus ordinal and returns an empty sequence for
zero; negative counts and invalid budgets are argument errors.

`Generate<T>()` materializes a successful semantic value graph into a public CLR object. It supports public
constructors and writable public properties/fields, arrays, declared collection interfaces and concrete types,
dictionaries, nullable values, enums, and documented BCL scalar forms. It never invokes private constructors,
bypasses constructors, mutates private members, or infers single-value wrappers.

`Materialize<T>(value)` materializes an existing successful graph without regenerating it. Materialization
failures are reported through `TestDataGenerationException` with `TESTDATA_MATERIALIZATION_FAILED` diagnostics.
Property and Logical Type generators take precedence in that order over terminology candidates, followed by
built-in generation. Budgets are explicit and default to the baseline safety ceilings.
