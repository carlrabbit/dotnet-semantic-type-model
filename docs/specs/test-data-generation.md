# Constraint-Aware Test-Data Generation Specification

## Status

Authoritative behavioral specification for SemanticTypeModel test-data synthesis, terminology enrichment, TestData Profile integration, and CLR materialization.

M0082 established deterministic occurrence-derived Random generation. M0083 adds model-bound runtime sampling policy through the separate `TestDataProfile` contract in `test-data-profiles.md`.

## Purpose

Define how SemanticTypeModel derives deterministic, valid, useful synthetic values from a canonical `TypeSchemaModel` without introducing a second canonical authoring source, domain faker library, invalid-data generation, or cross-root dataset semantics.

The canonical model defines the legal semantic space.

Optional TestData Profiles control how that legal space is sampled. Semantic Terminology Profiles and programmatic generators may provide candidate values without mutating the canonical model.

## Package Boundary

The capability is owned by:

```text
SemanticTypeModel.TestData
```

The package consumes canonical/Core contracts and must not require EF Core, JSON Schema, Power BI, System.Text.Json integration, the .NET source generator, another target package, or a third-party faker/random-data package.

Canonical packages do not depend on `SemanticTypeModel.TestData`.

Generated values and TestData Profiles never mutate or enrich `TypeSchemaModel`.

The package remains part of the exact-version-aligned `SemanticTypeModel.*` suite.

## Development-Line Compatibility

Stable baseline:

```text
6.0.0
```

Current development line:

```text
6.1.0
```

M0083 validation package version:

```text
6.1.0-m0083
```

Exact built-in Random scalar values are not a cross-version compatibility contract.

For one aligned suite version, same generation inputs remain deterministic across supported Windows/Linux runtimes.

Stable 6.1.0 publication, tagging, and GitHub Release creation are outside M0083 unless separately authorized.

## Generation Result Boundary

The package-owned semantic value representation preserves:

- canonical type identity;
- object property identity;
- scalar value/kind;
- enum identity/value;
- collection item values;
- dictionary key/value entries;
- explicit null where semantically legal.

A generated value graph is finite and acyclic.

A result containing an error diagnostic is not successful generated TestData. The implementation must not return a known-invalid partial graph as success.

## Core Validity Invariant

For every supported feature:

```text
generated value
MUST satisfy
all applicable canonical semantic constraints
```

Sampling policy is subordinate to validity.

A TestData Profile cannot weaken requiredness, nullability, format, pattern, numeric/string/collection constraints, custom-constraint fail-closed behavior, safety budgets, or unsupported-type boundaries.

## Generation Coordinate

Pseudo-random and sampling choices are derived from a stable semantic occurrence coordinate rather than accidental consumption order from one mutable global RNG.

The coordinate includes, as applicable:

- configured base seed;
- root ordinal;
- canonical type/property/use-site identity;
- collection/dictionary structural ordinal;
- retry attempt;
- stable value-role/policy discriminator.

The implementation must not use randomized runtime hash codes, process/machine state, clock time, local timezone, or environment-specific entropy.

Adding/reordering an unrelated sibling property must not change an unchanged property's generated value or custom-generator context seed merely because traversal order changed.

M0083 policy choices such as presence, null, weighting, boundary mixing, and collection count use independent deterministic discriminators so introducing one policy does not perturb unrelated entropy streams.

## Random Generation Quality

Built-in Random generation remains deterministic, occurrence-diverse, and synthetic rather than business-realistic.

Different ordinary high-cardinality occurrences should vary by default.

Natural repetition remains valid for low-cardinality domains such as Boolean, Enum, tiny constrained numeric domains, short finite string domains, and small candidate sets.

Distinct-by-default is not semantic uniqueness.

Hard uniqueness remains limited to current explicit guarantees such as `UniqueItems` and dictionary keys.

Cross-root key uniqueness and referential integrity remain outside this specification.

## Size Profiles

Named size profiles remain:

```text
Simple
Moderate
Extreme
```

Authoritative default targets:

| Value category | Simple | Moderate | Extreme |
|---|---:|---:|---:|
| String length | 8 | 32 | 1024 |
| Binary length | 8 | 32 | 1024 |
| Array/collection item count | 1 | 8 | 100 |
| Dictionary entry count | 1 | 8 | 100 |

Size profiles are not validity, boundary, nullability, enum-frequency, or realism profiles.

A TestData Profile may override collection/dictionary count sampling according to `test-data-profiles.md`; string/binary size targets otherwise remain these profile targets subject to canonical constraints and boundary strategy.

## Safety Budgets

Default ceilings remain:

| Budget | Ceiling |
|---|---:|
| One generated string | 65,536 characters |
| One generated binary value | 65,536 bytes |
| One generated array/collection | 10,000 items |
| One generated dictionary | 10,000 entries |
| Nested generation depth | 32 |
| Total generated value nodes for one root | 100,000 |

Semantic minimums above the relevant ceiling fail rather than truncate below validity.

Configured budget overrides remain part of the facade.

## Property Presence and Nullability

Without a TestData Profile, the complete-object baseline remains:

- optional properties are normally present;
- nullable properties are normally non-null;
- modeled properties are generated whenever a finite legal value exists;
- unmodeled properties are not invented.

With a TestData Profile, legal optional omission and legal null values may be sampled according to `test-data-profiles.md`.

Sampling never omits a required property or nulls a non-nullable property.

Recursion may still use a legal finite terminator in this order:

1. null where nullable;
2. omission where optional;
3. empty collection/dictionary where zero is legal;
4. otherwise fail.

## Constraint Composition

Type-level and use-site constraints compose conjunctively.

Effective numeric/string/collection ranges are intersections of all applicable canonical constraints.

Unsatisfiable constraints produce an error at the affected model path. Sampling policy never relaxes them.

## Built-In Scalar Kinds

Built-in generation supports:

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

`Unknown` remains unsupported.

### Boolean

Choose `true`/`false` deterministically. Repetition is expected.

### String

Ordinary strings use an invariant lowercase-ASCII/digit alphabet, occurrence-derived content, and the effective legal/profile length.

No shared `"test"` prefix/template is used.

### Integer / Number / Decimal

Size profile does not control numeric magnitude.

For unbounded/broad ranges, Random generation prefers:

```text
-10_000 .. 10_000
```

when this intersects the legal domain.

Canonical minimum/maximum, exclusive bounds, `multipleOf`, integer integrality, and represented precision/scale remain authoritative.

### Date

Unconstrained Date generation uses the fixed synthetic window:

```text
2000-01-01 .. 2039-12-31
```

### Time

Unconstrained Time generation uses a deterministic valid time over the 24-hour day at second-level resolution.

### DateTime / DateTimeOffset

Use the fixed synthetic date window plus deterministic time-of-day; DateTimeOffset does not depend on local timezone.

### Duration

Use a deterministic non-negative duration from:

```text
0 seconds .. 30 days
```

unless canonical constraints narrow the legal domain.

### Guid

Use a deterministic occurrence-derived RFC-compatible Guid rather than a shared constant.

### Binary

Use deterministic occurrence-derived bytes and string/binary size targets `8/32/1024`, subject to constraints/budgets.

### Json

Use deterministic valid non-null JSON with occurrence-derived content; do not infer application-specific JSON structure.

## Predefined String Formats

Built-in generation recognizes:

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

Random formatted values vary by occurrence and remain valid.

Synthetic domains remain:

- email local part varies under `example.test`;
- absolute URI uses `https://example.test/`;
- hostname uses a varying label under `example.test`;
- IPv4 uses RFC documentation ranges;
- IPv6 uses `2001:db8::/32`;
- temporal/uuid formats use the corresponding deterministic scalar policies.

Never blindly truncate/pad formatted values into invalid syntax.

Unknown/custom formats remain fail-closed unless an explicit supplied candidate can be validated.

## Regular-Expression Pattern Policy

Built-in regex synthesis remains unsupported.

Patterned strings require an eligible supplied candidate from a higher value-source tier.

If none exists, Random/boundary generation returns:

```text
TESTDATA_PATTERN_UNSUPPORTED
```

A supplied candidate succeeds only after validation against the pattern and all other constraints.

## Enums

Enum generation selects one declared value deterministically.

A TestData Profile may provide weighted legal enum values according to `test-data-profiles.md`.

An enum with no usable declared value is an error.

## Objects and Composition

Object generation emits the effective modeled property set, including supported inherited/composed properties, subject only to legal profile-driven omission/null behavior.

Generation does not infer relationships, faker semantics, target behavior, or hidden uniqueness from metadata.

Current `RequiredWhen` remains satisfied by generated presence when its condition applies; profile omission policy may not cause a canonically required conditional property to be absent when the modeled condition requires it.

Object property-count constraints remain authoritative.

## Arrays and Dictionaries

Without an explicit TestData Profile collection-size rule, counts use size-profile targets clamped to canonical bounds and budgets.

A TestData Profile may choose fixed/ranged counts or modeled boundary counts according to `test-data-profiles.md`.

Array item and dictionary entry/key/value coordinates remain distinct.

`UniqueItems` and dictionary-key uniqueness remain hard guarantees. Exhaustion produces the existing uniqueness diagnostic.

## References

A `ReferenceTypeDefinition` resolves its canonical target while preserving the use-site generation coordinate.

Unresolved references are errors.

## Any, Never, Union, Intersection

`Any` uses one deterministic valid JSON-like value family.

`Never` has no valid instance and errors.

Union/intersection synthesis remains unsupported.

## Custom Constraints and Unknown Semantic Rules

Unknown/custom canonical validity rules are never ignored.

A non-empty custom constraint remains fail-closed unless an explicit custom/value-source capability can prove validity according to supported semantics.

Unknown annotations do not become generation rules.

## Value-Source Precedence

For a present non-null scalar/enum property:

```text
programmatic property generator
-> programmatic Logical Type generator
-> exact-property TestData Profile weighted values
-> Logical-Type TestData Profile weighted values
-> property Semantic Terminology Profile candidates
-> Logical-Type Semantic Terminology Profile candidates
-> built-in generation using the Effective Sampling Policy
```

An explicit invalid programmatic candidate fails closed.

TestData Profile and terminology candidates are validated through one coherent canonical candidate-validation path.

An ineligible Logical-Type profile/terminology tier falls through without weakening constraints.

Boundary strategy affects only built-in generation and does not transform higher-precedence supplied values.

## Semantic Terminology Profiles

`SemanticTerminologyProfile` remains the existing version-1 model-bound external JSON sidecar for candidate vocabulary.

Its export/import, exact model binding, normalization, property-over-Logical-Type precedence, stale-entry handling, and candidate validation remain unchanged.

The existing `TESTDATA_PROFILE_*` diagnostics remain terminology-sidecar diagnostics.

TestData Profile policy is a separate runtime concept and uses separate policy terminology/diagnostics.

## TestData Profiles

`TestDataProfile` is the model-bound immutable runtime sampling domain defined in `test-data-profiles.md`.

It may control:

- legal optional presence;
- legal null sampling;
- weighted legal property/Logical-Type values;
- Random/Boundary/BoundaryMixed built-in strategy;
- collection/dictionary count policy.

It is not a canonical annotation or persisted schema.

## Custom Generator Context

`TestDataGeneratorContext.RootOrdinal` remains the zero-based bulk root ordinal.

`TestDataGeneratorContext.Seed` remains an occurrence-local deterministic callback seed.

Same-version/same-coordinate callback seeds remain stable across supported platforms and unrelated sibling insertion/reordering.

## Bulk Generation

`GenerateMany<T>(count)` and `GenerateMany(TypeId,count)` retain:

- zero -> empty sequence;
- negative count -> argument error;
- root ordinals `0..count-1`.

The configured seed remains a base seed; root ordinal is a separate coordinate component.

Profile presence/null/weight/boundary/count policy is evaluated per occurrence/root coordinate.

This does not create cross-root semantic uniqueness.

## Typed and Dynamic Facade

The supported consumer surface remains:

```text
model.TestData()
```

Current configuration remains:

```text
WithSizeProfile
WithSeed
WithTerminology
WithBudgets
WithPropertyGenerator
WithLogicalTypeGenerator
```

M0083 adds:

```text
WithProfile
```

Current generation remains:

```text
Generate(TypeId)
GenerateMany(TypeId,count)
Generate<T>()
GenerateMany<T>(count)
Materialize<T>(value)
```

Programmatic/dynamic canonical models remain first-class through canonical `TypeId`.

CLR materialization is downstream conversion only and never re-samples values.

## Inspection

`SemanticTestValue.ToSemanticText(model)` remains deterministic human-readable development/test output.

It does not generate values and is not a persisted/wire format.

## Diagnostics

Package diagnostics retain:

```text
TESTDATA_*
```

Existing `TESTDATA_PROFILE_*` remains reserved for Semantic Terminology Profile import/binding/candidate concerns.

M0083 profile-policy validation may use a distinct descriptive `TESTDATA_POLICY_*` category.

Exact diagnostic messages are not API contracts.

## Dependency Policy

TestData must not add:

- third-party faker libraries;
- regex-generation libraries;
- random-data services;
- AI SDK/runtime dependencies.

Semantic realism remains the responsibility of terminology/custom generators.

## Compatibility Boundary

Stable behavioral contracts include:

- canonical validity;
- same-version determinism;
- occurrence-derived diversity;
- M0083 sampling-policy semantics;
- source precedence;
- size targets;
- documented synthetic domains;
- failure categories.

Existing generation with no TestData Profile preserves M0082 behavior during M0083 implementation, including current fixed deterministic test vectors.

Exact Random values remain non-contractual across aligned suite versions.

## Non-Goals

The current TestData capability does not add:

- canonical TestData annotations;
- domain faker datasets;
- invalid/adversarial generation;
- Unicode/control-character stress profiles;
- regex synthesis;
- dependent/cross-property value generation;
- sequences/counters;
- shared/frozen generated values;
- TestData-scoped uniqueness beyond existing canonical collection/dictionary rules;
- cross-root datasets/key uniqueness;
- referential integrity;
- database seeding;
- new union/intersection synthesis;
- arbitrary custom-constraint interpretation;
- target-specific generation;
- stable 6.1.0 publication.

## Validation Expectations

Short-running tests must retain M0082 coverage and add the M0083 obligations from `test-data-profiles.md`.

Package-based sample validation must exercise the current packed `SemanticTypeModel.TestData` package and the programmatic-model catalog profile scenario.

Release-candidate validation uses:

```text
6.1.0-m0083
```

without publication.
