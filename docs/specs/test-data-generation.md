# Constraint-Aware Test-Data Generation Specification

## Status

Authoritative behavioral specification for SemanticTypeModel test-data synthesis, terminology enrichment, and CLR materialization.

M0082 refines the built-in Random generation contract on the 6.1.0 development line. It replaces constant placeholder generation and traversal-order-sensitive pseudo-random selection with deterministic occurrence-derived diversity.

## Purpose

Define how SemanticTypeModel derives deterministic, valid, useful synthetic values from a canonical `TypeSchemaModel` without introducing a second authoring source, domain faker library, invalid-data generation, or cross-root dataset semantics.

The capability answers:

```text
Given a valid canonical semantic model,
can STM synthesize a finite value graph that:
- satisfies supported semantic constraints;
- is deterministic for the same generation inputs;
- varies meaningfully across different semantic occurrences;
- remains useful even without a terminology profile?
```

Terminology and programmatic generators add semantic meaning when available. The built-in Random fallback is nevertheless required to produce test-friendly structural diversity rather than repeated placeholder constants.

## Package Boundary

The capability is owned by:

```text
SemanticTypeModel.TestData
```

The package consumes the canonical semantic model and depends inward on canonical/core contracts. It must not require EF Core, JSON Schema, Power BI, System.Text.Json integration, the .NET source generator, another target package, or a third-party faker/random-data package.

Canonical packages must not depend on `SemanticTypeModel.TestData`.

Generated values never mutate or enrich `TypeSchemaModel`.

The package remains part of the exact-version-aligned `SemanticTypeModel.*` suite.

## Development-Line Compatibility

Stable baseline:

```text
6.0.0
```

Current TestData development line:

```text
6.1.0
```

M0082 validation package version:

```text
6.1.0-m0082
```

M0082 deliberately changes built-in Random values relative to 6.0/M0081. Exact generated scalar values are not a cross-version compatibility contract.

For the same aligned suite version and same generation inputs, determinism remains required.

Stable 6.1.0 publication, tagging, and GitHub Release creation are outside M0082 unless separately authorized.

## Generation Result Boundary

The package-owned semantic value representation preserves:

- canonical type identity;
- object property identity;
- scalar value/kind;
- enum identity/value;
- collection item values;
- dictionary key/value entries;
- explicit null when semantically legal.

A generated value graph is finite and acyclic.

A result containing an error diagnostic is not successful generated test data. The implementation must not return a known-invalid partial graph as success.

## Core Validity Invariant

For every supported feature:

```text
generated value
MUST satisfy
all applicable canonical semantic constraints
```

Diversity is subordinate to validity.

If a diverse value cannot be produced while satisfying the supported semantic contract, the generator must either produce another valid value or report the existing appropriate TestData diagnostic. It must not weaken constraints, truncate into invalid formats, or silently ignore semantics.

## Random Generation Quality Invariant

Random generation means deterministic pseudo-random synthetic generation, not business realism.

The baseline built-in Random generator must provide:

```text
same generation coordinate
    -> same value

different ordinary semantic occurrence
    -> occurrence-specific value

high-cardinality legal domain
    -> values vary by default

small finite domain
    -> repetition is natural and allowed

terminology/custom value source
    -> still takes precedence over Random
```

The built-in generator must not use one shared constant such as `"test"`, a padded `"testxxxx"` template, one fixed Guid, one fixed date/time, repeated fixed binary bytes, or one fixed formatted-string example for every occurrence.

Random generation is intentionally opinionated but synthetic. It does not attempt names, postal addresses, company names, countries, product descriptions, or other domain-specific faker semantics without terminology/custom values.

## Generation Coordinate

Built-in pseudo-random selection is derived from a stable semantic occurrence coordinate rather than accidental consumption order from one mutable global `Random` stream.

The coordinate includes, as applicable:

- configured base seed;
- root ordinal for bulk generation;
- canonical root/type/property identity for the current use site;
- structural ordinals such as array item index or dictionary entry/key/value position;
- retry/attempt ordinal when uniqueness or constraint satisfaction requires another candidate;
- a stable value-role discriminator when two values at the same structural index serve different roles.

The implementation may choose the internal deterministic hash/PRNG algorithm, but it must be stable across supported Windows/Linux runtimes for one aligned suite version and must not use randomized runtime hash codes, process/machine state, clock time, or environment-specific entropy.

### Stability under unrelated model edits

When an existing property's canonical identity, constraints, seed, root ordinal, and structural position are unchanged, adding or reordering an unrelated sibling property must not change that property's built-in Random value merely because traversal order changed.

This is a required semantic benefit of occurrence-derived entropy.

Changing the relevant property identity, constraints, seed, structural index, or aligned suite version may change the generated value.

### Different seeds

For a representative model containing high-cardinality built-in values, changing the base seed must change at least one such generated value.

A different seed is not required to change Boolean/enum values or every individual scalar occurrence.

## Distinct-by-Default versus Semantic Uniqueness

M0082 distinguishes useful diversity from correctness-level uniqueness.

### Distinct-by-default

For ordinary unconstrained or broadly constrained high-cardinality built-in domains, different generation coordinates should map to different values until the practical domain forces reuse.

This applies to the built-in generation of:

- String;
- Integer/Number/Decimal when the effective supported range is sufficiently broad;
- Date/Time/DateTime/DateTimeOffset;
- Duration;
- Guid/uuid;
- Binary;
- Json;
- variable predefined-format strings.

Representative `GenerateMany` acceptance populations must not collapse these kinds to repeated constants.

### Natural repetition

Repetition is valid and expected for low-cardinality domains such as:

- Boolean;
- Enum;
- a very small constrained integer/range domain;
- very short strings whose legal domain is smaller than the requested occurrence population;
- a terminology candidate set smaller than the number of generated occurrences.

Distinct-by-default is not a hidden semantic uniqueness constraint.

### Hard uniqueness

Only explicit semantic requirements such as `UniqueItems` or dictionary-key uniqueness are correctness guarantees in M0082.

Cross-root key uniqueness and dataset referential integrity remain outside this specification.

If hard uniqueness is required and the finite supported domain is exhausted, generation fails with the existing uniqueness diagnostic rather than silently duplicating values.

## Size Profiles

The named profiles remain exactly:

```text
Simple
Moderate
Extreme
```

They are size profiles, not realism, validity, boundary, or numeric-magnitude profiles.

The authoritative targets are:

| Value category | Simple | Moderate | Extreme |
|---|---:|---:|---:|
| String length | 8 | 32 | 1024 |
| Binary length | 8 | 32 | 1024 |
| Array/collection item count | 1 | 8 | 100 |
| Dictionary entry count | 1 | 8 | 100 |

The implementation must use category-appropriate targets. A single shared `1/8/100` target is not valid for string/binary generation.

For each generated value, the target is clamped into the effective legal minimum/maximum interval.

Examples:

```text
String MinLength=1, MaxLength=20, Extreme -> 20 characters
String MinLength=50, MaxLength=100, Moderate -> 50 characters
Array MinItems=0, MaxItems=0, Simple -> 0 items
Array MinItems=3, no MaxItems, Simple -> 3 items
```

The profiles do not control:

- numeric magnitude;
- temporal distance from an epoch;
- optional-property presence probability;
- null probability;
- enum frequency;
- semantic realism;
- invalid/adversarial data.

## Built-In Safety Budgets

Default ceilings remain:

| Budget | Ceiling |
|---|---:|
| One generated string | 65,536 characters |
| One generated binary value | 65,536 bytes |
| One generated array/collection | 10,000 items |
| One generated dictionary | 10,000 entries |
| Nested generation depth | 32 |
| Total generated value nodes for one root | 100,000 |

If a semantic minimum exceeds the relevant ceiling, generation fails. The generator must not truncate below the semantic minimum.

Configured budget overrides remain part of the facade.

## Property Presence and Nullability

Random generation remains complete-object oriented:

- modeled properties are generated when a finite legal value can be produced;
- optional properties are normally included;
- nullable properties are normally non-null;
- unmodeled properties are never invented.

Optionality/nullability are not probability knobs.

The recursion terminator policy remains:

1. null when nullable;
2. omit when optional;
3. empty collection/dictionary when zero is legal;
4. otherwise fail.

## Constraint Composition

Type-level and property/use-site constraints compose conjunctively.

Effective numeric/string/collection ranges are the intersection of applicable constraints.

Unsatisfiable constraints produce an error at the affected model path. Random generation must not relax one constraint to satisfy another.

## Built-In Scalar Generation

Built-in Random generation supports:

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

All built-in scalar generators use occurrence-derived deterministic entropy where the legal domain admits variation.

### Boolean

Generate `true` or `false` deterministically from the generation coordinate.

Repetition is expected.

### String

For an ordinary string without a predefined format or external candidate:

- generate exactly the category/profile target length after semantic clamping;
- use a small test-friendly invariant alphabet of lowercase ASCII letters and decimal digits;
- do not prepend `"test"` or another shared semantic-looking label;
- derive content from the generation coordinate;
- vary different ordinary occurrences by default.

String generation must remain safe for logs, snapshots, identifiers, and ordinary text handling. M0082 does not deliberately inject control characters, whitespace edge cases, Unicode stress cases, or adversarial payloads.

Those may belong to a future boundary/adversarial-data capability.

### Integer / Number / Decimal

Size profile does not control numeric magnitude.

For unbounded or very broadly bounded numeric values, Random generation uses a stable preferred synthetic interval centered on zero:

```text
-10_000 .. 10_000
```

This interval is a generation preference, not a semantic constraint.

Rules:

- semantic minimum/maximum/exclusive bounds remain authoritative;
- when the preferred interval intersects the legal interval, choose a coordinate-derived value from that intersection;
- when it does not intersect, choose a coordinate-derived legal value from the representable region implied by the semantic bound(s);
- `multipleOf` remains authoritative;
- Integer results remain integral;
- Number/Decimal should produce fractional values where legal rather than collapsing every unconstrained occurrence to an integer/zero;
- represented precision/scale semantics remain authoritative.

For a small legal finite numeric domain, repetition is allowed.

### Date

For unconstrained built-in Date generation, use a deterministic coordinate-derived date within the fixed synthetic window:

```text
2000-01-01 .. 2039-12-31
```

Do not use the current date or system clock.

### Time

For unconstrained built-in Time generation, use a deterministic valid time over the full 24-hour day at second-level resolution.

### DateTime

Use a deterministic date from the fixed Date window plus a deterministic time-of-day. Preserve the package's documented CLR/semantic DateTime kind contract; M0082 does not introduce timezone inference.

### DateTimeOffset

Use a deterministic instant within the same fixed date window with an explicit deterministic offset representation compatible with current materialization. UTC is an acceptable baseline; the generator must not depend on local-machine timezone.

### Duration

Use a deterministic non-negative duration from:

```text
0 seconds .. 30 days
```

unless semantic constraints narrow the legal domain.

### Guid

Generate a deterministic occurrence-derived Guid rather than a shared constant.

Generated Guid values must have stable canonical text/materialized identity and should use standard RFC-compatible version/variant bits rather than arbitrary malformed bit patterns.

### Binary

Generate deterministic occurrence-derived bytes rather than repeating one byte value.

Length uses the authoritative String/Binary size targets:

```text
8 / 32 / 1024
```

after semantic clamping and budget enforcement.

### Json

Generate a deterministic valid non-null JSON value containing occurrence-derived content rather than the same `"{}"` value for every coordinate.

M0082 does not infer application-specific JSON structure.

The representation must remain valid for current TestData materialization/inspection contracts.

## Predefined String Formats

Built-in generation continues to recognize:

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

Formatted Random values must vary by generation coordinate while remaining valid for the declared format and applicable supported string constraints.

Opinionated synthetic domains:

- `email`: local part varies; reserved `example.test` domain;
- `uri`: absolute HTTPS URI under `https://example.test/` with a varying safe path/token;
- `uri-reference`: deterministic varying URI reference; it need not be absolute;
- `hostname`: varying label under `example.test`;
- `ipv4`: values from RFC documentation address ranges (`192.0.2.0/24`, `198.51.100.0/24`, `203.0.113.0/24`);
- `ipv6`: values under the documentation prefix `2001:db8::/32`;
- `date`: same fixed synthetic Date window;
- `time`: same deterministic Time policy;
- `date-time`: same deterministic DateTime/DateTimeOffset-compatible policy;
- `duration`: same deterministic Duration policy;
- `uuid`: same deterministic Guid policy.

The generator must not use live DNS/network state or generate values that intentionally target real external hosts.

A supported formatted value must satisfy string length constraints. The implementation may adapt the variable token length where the format permits it. It must never truncate/pad a formatted value into invalid syntax.

If the supported format cannot satisfy the effective semantic length/constraint combination, generation fails with the existing appropriate unsatisfiable/budget diagnostic.

Unknown/custom formats remain fail-closed unless an explicit external/custom candidate can be independently validated.

## Regular-Expression Pattern Policy

Built-in regex synthesis remains explicitly unsupported.

A non-empty applicable `Pattern` requires an eligible terminology/custom candidate.

Random mode returns:

```text
TESTDATA_PATTERN_UNSUPPORTED
```

rather than guessing a regex value.

A supplied candidate may succeed only when STM validates it against the regex and all other applicable constraints.

## Enums

Enum generation selects one declared value using the occurrence-derived deterministic stream.

Different occurrences/seeds may select different enum values, but repetition is natural and permitted.

An enum with no usable declared value is an error.

Size profiles do not affect enum selection.

## Objects and Composition

Object generation produces the effective modeled property set, including supported inherited/composed properties.

Generation preserves canonical property identity and does not infer relationships, target behavior, faker semantics, or hidden uniqueness from metadata.

Current `RequiredWhen` semantics remain satisfied by the complete-object policy rather than scenario-family synthesis.

Object property-count constraints remain supported only when a valid object can be formed from modeled properties without inventing additional properties.

## Arrays and Collections

Collection count uses the collection-specific `1/8/100` size targets clamped by semantic bounds and safety budgets.

Each item receives a distinct structural coordinate containing its item index.

Therefore high-cardinality scalar item kinds naturally vary across array positions.

`UniqueItems` remains a hard semantic guarantee. Retry attempts use a distinct attempt coordinate. Domain exhaustion produces the existing uniqueness error.

## Dictionaries

Dictionary count uses the dictionary-specific `1/8/100` size targets.

Each entry, key, and value receives an occurrence coordinate that distinguishes:

- entry ordinal;
- key versus value role;
- retry attempt.

Dictionary keys remain hard-unique. Domain exhaustion is an error.

The semantic representation does not force keys through JSON string-key rules.

## References

A `ReferenceTypeDefinition` resolves its canonical target while preserving the reference use-site coordinate.

An unresolved reference remains an error.

## Any, Never, Union, and Intersection

`Any` remains supported through one deterministic built-in valid JSON-like value choice. Under M0082 the selected built-in content should be occurrence-derived rather than a shared constant where practical.

`Never` has no valid instance and always errors.

`Union` and `Intersection` synthesis remain unsupported.

## Custom Constraints and Unknown Semantic Rules

Unknown/custom canonical validity rules are never ignored.

A non-empty custom constraint remains fail-closed unless an explicit custom/value-source capability owns and validates it.

Unknown annotations do not become Random-generation rules.

## Value-Source Precedence

M0082 does not change TestData value-source precedence:

```text
programmatic property generator
-> programmatic Logical Type generator
-> property terminology
-> Logical Type terminology
-> built-in Random
```

An explicit invalid custom candidate still fails closed.

Terminology values are never mutated to fit Random size targets.

When multiple eligible terminology candidates remain after the existing length-nearest filtering rules, deterministic candidate selection uses the occurrence-derived stream rather than sibling traversal position.

If a property terminology tier contains no eligible candidate, the generator still tries the Logical Type tier before Random.

## Custom Generator Context

`TestDataGeneratorContext.RootOrdinal` remains the zero-based bulk root ordinal.

`TestDataGeneratorContext.Seed` is an occurrence-local deterministic seed suitable for application callback generation.

Under M0082 it is derived from the base seed and the current semantic generation coordinate. It is not a promise that the implementation consumed or advanced one shared `System.Random` instance.

For the same aligned suite version and same generation coordinate, the callback context seed must be stable across supported platforms.

Adding/reordering an unrelated sibling property must not change the context seed of an unchanged property.

## Bulk Generation

`GenerateMany<T>(count)` and `GenerateMany(TypeId, count)` retain:

- zero -> empty sequence;
- negative count -> argument error;
- root ordinal `0..count-1`.

The configured seed is the base seed. Root ordinal is a separate component of the generation coordinate.

Implementations must not rely on arithmetic `seed + ordinal` as the semantic source of bulk diversity.

For broadly valued built-in scalar properties, bulk roots should normally contain distinct values because the root ordinal differs.

This is still not a cross-root semantic uniqueness guarantee.

## Diagnostics

Package runtime diagnostics retain the descriptive prefix:

```text
TESTDATA_*
```

M0082 should reuse existing diagnostics for constraint, budget, format, unsupported, pattern, uniqueness, recursion, and unresolved-reference failures.

A new diagnostic is justified only if the new Random-generation contract exposes a failure state that cannot be represented accurately by an existing diagnostic.

Exact diagnostic message text is not a compatibility contract.

## Dependency Policy

TestData must not add:

- third-party faker libraries;
- regex-generation libraries;
- random-data services;
- AI SDK/runtime dependencies.

The built-in generator uses STM canonical contracts and BCL/runtime primitives.

Semantic realism remains the responsibility of terminology profiles or application-supplied generators.

## Compatibility Boundary for Random Output

The following are stable behavioral contracts:

- validity against supported canonical constraints;
- deterministic repeatability within one aligned suite version;
- occurrence-derived diversity policy;
- value-source precedence;
- size-profile targets;
- documented synthetic numeric/temporal/format domains;
- diagnostics/failure categories.

The exact built-in Random scalar values for a given seed are **not** a cross-version compatibility contract.

A suite upgrade may change the pseudo-random mapping while preserving the documented policy.

Consumers that require specific semantic values must use terminology/custom generators or assert semantic properties rather than hard-code old Random outputs.

## Non-Goals

M0082 does not add:

- business/domain faker datasets;
- person/company/address/country/product inference;
- invalid/faulty/adversarial data generation;
- Unicode/control-character stress generation;
- probabilistic optional/null generation;
- weighted enum/business distributions;
- regex synthesis;
- cross-root dataset generation;
- canonical key uniqueness across `GenerateMany`;
- referential integrity;
- foreign-key/relationship inference;
- database seeding;
- new union/intersection synthesis;
- arbitrary custom-constraint interpretation;
- target-specific Power BI/EF/JSON generation;
- new canonical semantics;
- stable 6.1.0 publication.

## Required Cross-Boundary Evidence

Existing real code-first and programmatic-authoring TestData integration evidence remains required.

M0082 additionally requires representative evidence that:

```text
programmatic model
-> GenerateMany
-> high-cardinality built-in properties differ across roots
-> terminology still overrides Random
-> TestData inspection renders the diverse values
```

The package-based programmatic-model catalog must expose at least one concise `random-diversity` scenario demonstrating this behavior without CLR materialization.

Packed-package consumer validation must use current locally packed aligned packages rather than source project references.

## Semantic Terminology Profiles

`SemanticTerminologyProfile` remains a version-1 JSON sidecar owned by `SemanticTypeModel.TestData`.

Profile import/export, model binding, stale-entry warnings, normalization, candidate validation, property-over-Logical-Type precedence, use-site filtering, pattern validation, and fail-closed custom/unknown-constraint behavior remain unchanged.

Profile-guided generation uses eligible property candidates, then Logical Type candidates, then built-in Random.

Candidate lists remain normalized independent of input ordering.

Candidate selection is deterministic for the same aligned suite version, seed, root ordinal, semantic occurrence, and normalized profile.

## Typed and Dynamic TestData Facades

The supported facade remains:

```text
model.TestData()
```

with current configuration methods such as:

```text
WithSizeProfile
WithSeed
WithTerminology
WithBudgets
WithPropertyGenerator
WithLogicalTypeGenerator
```

and current semantic/typed generation:

```text
Generate(TypeId)
GenerateMany(TypeId, count)
Generate<T>()
GenerateMany<T>(count)
Materialize<T>(value)
```

M0082 changes built-in Random quality, not the facade shape.

CLR materialization remains a conversion of an already generated semantic graph. It does not regenerate or randomize values.

Programmatic/callback scalar candidates remain validated against the canonical scalar/use-site contract before success.

## TestData Inspection

`SemanticTestValue.ToSemanticText(model)` remains deterministic human-readable inspection owned by `SemanticTypeModel.TestData`.

Inspection does not generate or randomize values and does not become a persistence/wire contract.

M0082 sample and acceptance evidence should use inspection to make Random diversity visible.

## Validation Expectations

Short-running tests must cover at least:

- repeatability for identical inputs;
- different-seed diversity for a representative high-cardinality model;
- sibling insertion/reordering stability for unchanged properties;
- occurrence-local callback `Seed` stability;
- exact string/binary profile target lengths `8/32/1024`;
- exact collection/dictionary profile counts `1/8/100`;
- unconstrained ordinary strings differ across sibling properties and representative bulk roots;
- Guid, Binary, Date, Time, DateTime, DateTimeOffset, Duration, Json, and broadly ranged numeric values vary across representative occurrences;
- Boolean/Enum/small-domain repetition remains legal;
- formatted values vary and validate for every supported predefined format;
- formatted values use reserved synthetic network/domain ranges where specified;
- terminology/custom precedence is unchanged;
- ineligible terminology fallback remains property -> Logical Type -> Random;
- array item/dictionary key diversity;
- `UniqueItems` and dictionary-key hard uniqueness remain enforced;
- existing pattern/custom/unsupported diagnostics remain unchanged in meaning;
- no use of current clock/local timezone affects deterministic output.

The programmatic-model catalog must provide package-consumer evidence for the visible Random improvement.
