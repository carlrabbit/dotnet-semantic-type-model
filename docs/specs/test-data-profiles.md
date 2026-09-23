# TestData Profiles and Sampling Policies

## Status

Authoritative behavioral specification for TestData Profile configuration, sampling policy, and M0084 coordinated exact-property rules.

This specification composes with `test-data-generation.md`. The canonical model remains the validity boundary.

## Purpose

Provide a first-class TestData-domain configuration for named generation scenarios without adding TestData-only meaning to canonical annotations.

A TestData Profile answers:

```text
Given values that are legal according to TypeSchemaModel,
how should this named TestData scenario sample them?
```

It does not redefine legality.

## Package and Public Boundary

The capability belongs to:

```text
SemanticTypeModel.TestData
```

The package inventory does not change.

The public facade gains the equivalent of:

```csharp
model.TestData().WithProfile(profile)
```

`WithProfile` is additive. Existing generation without a profile remains supported and uses the current M0082 defaults.

A profile is runtime .NET configuration. M0083 adds no profile JSON/YAML format, source-generator input, attribute model, canonical annotation vocabulary, or persistence protocol.

## Model Binding and Immutability

A TestData Profile is built against one `TypeSchemaModel`.

Its successful immutable form carries the model identity needed to reject consumption by a different model.

Equivalent minimum construction shape:

```csharp
TestDataProfile profile = TestDataProfile
    .Create(model, "TypicalCustomer")
    // rules
    .Build();
```

Exact builder/helper type names may follow repository conventions, but the public capability must preserve:

- required non-empty scenario/profile name;
- bound model identity;
- immutable successful profile;
- canonical-ID operation without CLR types.

Programmatic/dynamic models are first-class profile inputs.

`WithProfile` rejects a profile bound to a different model rather than silently rebinding it.

## Profile Name

The profile name is a scenario label for diagnostics, inspection/debugging, and application organization.

The name is not canonical semantic identity and is not a persisted compatibility key in M0083.

## Rule Scopes

M0083 supports four policy scopes:

```text
profile defaults
containing object TypeId
Logical Type
exact property identity
```

Exact property identity is canonical owner `TypeId` plus `PropertyId`.

A containing-object rule applies while generating the effective property set of that object, including inherited properties. An exact property rule remains anchored to the property's canonical identity and therefore outranks the containing-object rule.

A Logical Type rule applies only to property occurrences carrying that exact case-sensitive Logical Type.

### Effective precedence

For each applicable policy field:

```text
exact property
-> Logical Type
-> containing object type
-> profile defaults
-> built-in TestData default
```

Only explicitly configured fields override a lower-precedence field.

The same precedence applies regardless of the order fluent builder calls were made.

## Profile Composition

Profiles may be composed explicitly into a new named profile.

Equivalent shape:

```csharp
TestDataProfile combined =
    TestDataProfile.Compose(
        "TypicalBoundary",
        typical,
        boundaryOverlay);
```

Rules:

- all composed profiles target the same canonical model;
- profiles are applied left-to-right;
- later profiles override fields explicitly set earlier at the same scope;
- fields not set by the later profile remain from earlier profiles;
- a later weighted-value set replaces an earlier weighted-value set at the same scope as one atomic rule;
- composition does not create an inheritance graph;
- no parent traversal, multiple-inheritance resolution, persisted lineage, or profile reset/unset language is introduced in M0083.

Applications that need a materially different rule set may build a separate profile rather than mutating an existing one.

## Public Sampling Vocabulary

M0083 introduces the conceptual value strategies:

```text
Random
Boundary
BoundaryMixed
```

Equivalent public naming may follow package conventions, but these three semantics are required.

Sampling policy is orthogonal. A rule may combine, for example:

```text
BoundaryMixed
+ optional presence 0.8
+ nullable null probability 0.1
```

Do not encode cross-products as a combinatorial strategy enum.

## Built-In Defaults

With no TestData Profile, or where no profile rule overrides a field:

```text
value strategy                  Random
optional-property presence      1.0
nullable-value null probability 0.0
collection count                current size-profile target
```

These defaults preserve M0082 behavior.

## Optional-Property Presence

A profile may set an optional-property presence probability from `0.0` through `1.0`.

The policy is considered only for properties whose canonical cardinality permits omission.

Required properties remain present regardless of profile-default/type/Logical-Type optional-presence policy.

An exact property rule that attempts to configure omission probability for a required property is invalid profile configuration and must fail during profile construction/validation.

For an optional property:

```text
presence decision
-> absent: omit property from ObjectTestValue
-> present: continue with null/value generation
```

Presence decisions use a dedicated deterministic entropy discriminator.

## Nullable Null Probability

A profile may set a null probability from `0.0` through `1.0`.

The policy is considered only where canonical semantics permit null.

Non-nullable occurrences remain non-null regardless of profile-default/type/Logical-Type null policy.

An exact property rule that attempts to configure positive null probability on a non-nullable property is invalid profile configuration.

For an optional + nullable property, generation order is:

```text
presence decision
-> if present: null decision
-> if non-null: value-source selection
```

A sampled null is represented by the existing `NullTestValue` and does not invoke scalar value sources.

Null decisions use a dedicated deterministic entropy discriminator.

Recursion termination remains allowed to use null/omission when required by the base generation specification even when ordinary profile probability would not have selected it.

## Weighted Values

M0083 supports scenario-specific weighted explicit values for:

- an exact scalar/enum property;
- a scalar Logical Type.

Weights are positive relative integers and do not need to sum to 100.

Equivalent ergonomic shape:

```csharp
.Property(status)
    .Weighted(
        ("Active", 70),
        ("Pending", 20),
        ("Closed", 10))
```

### Exact property scope

All configured weighted values must be legal for the property's canonical type, format, pattern, and use-site constraints.

Invalid configured values make the profile invalid; they are not silently retained and ignored.

### Logical Type scope

Configured values must be representation-compatible with the Logical Type's canonical scalar type.

At each actual property use site, candidates that violate use-site constraints are filtered.

If no Logical-Type weighted candidate is eligible for that occurrence, generation proceeds to the next value-source tier rather than weakening constraints.

### Enum values

Weighted enum candidates must correspond to declared enum values.

### Candidate validation

Weighted values use the same canonical TestData candidate-validation semantics as terminology values, including supported format/pattern validation and safety budgets.

Weighted values do not add regex synthesis or bypass unknown/custom constraints.

### Deterministic selection

Selection uses relative weights and an occurrence-derived deterministic entropy discriminator.

For a sufficiently large deterministic population, a larger weight must materially increase selection frequency relative to a smaller weight; exact statistical distribution is not a compatibility promise.

## Value-Source Precedence

Presence/null decisions occur before non-null value-source selection.

For a present non-null scalar/enum property, precedence is:

```text
programmatic property generator
-> programmatic Logical Type generator
-> exact-property TestData Profile weighted values
-> Logical-Type TestData Profile weighted values
-> property Semantic Terminology Profile candidates
-> Logical-Type Semantic Terminology Profile candidates
-> built-in generation using the Effective Sampling Policy
```

Invalid explicit programmatic callback values remain fail-closed.

An ineligible profile or terminology tier does not weaken canonical validity.

Profile value strategy does not transform or mutate a value returned by a higher-precedence source.

## Boundary Strategy

Boundary strategies apply only when generation reaches the built-in generator.

They emphasize **modeled legal boundaries**, not synthetic business realism.

### Random

`Random` is the M0082 built-in occurrence-diverse behavior.

### Boundary

`Boundary` deterministically selects among meaningful modeled legal boundary candidates when the current value/use site exposes them.

Supported boundary categories include:

- numeric minimum/maximum with exclusive bounds, `multipleOf`, precision/scale, and integer integrality respected;
- string/binary legal minimum and maximum lengths;
- array/dictionary legal minimum and maximum item counts when no explicit collection-size rule overrides count selection.

For an exclusive bound, the selected boundary candidate is the nearest representable legal value, not the illegal declared bound itself.

If no meaningful modeled boundary exists for the current kind, `Boundary` falls back to ordinary Random generation.

Boundary strategy never uses current clock, environment state, or target-specific semantics.

### BoundaryMixed

`BoundaryMixed` uses an opinionated 20/80 split:

```text
20% boundary attempt
80% ordinary Random
```

The choice is deterministic per occurrence from an independent entropy discriminator.

If no meaningful boundary candidate exists, the occurrence uses Random.

For representative bounded `GenerateMany` populations, Boundary/BoundaryMixed must expose both lower and upper legal boundaries where both exist and the population is large enough to exercise the deterministic policy.

Exact row ordinal at which a boundary appears is not a compatibility contract.

## Collection-Size Policy

M0083 supports explicit collection/dictionary count policy independent of `TestDataSizeProfile`.

Required policy forms:

```text
Fixed(count)
InclusiveRange(min, max)
```

Equivalent public naming may vary.

Rules:

- counts are non-negative;
- range minimum must not exceed maximum;
- policy is intersected with canonical `MinItems`/`MaxItems` and safety budgets;
- a fixed count outside the legal interval is invalid profile configuration for an exact property;
- a range with no legal intersection is invalid profile configuration for every currently targeted occurrence;
- a legal range chooses a count deterministically from its inclusive effective interval;
- an explicit collection-size policy takes precedence over Boundary/BoundaryMixed count selection;
- element/key/value generation still uses the effective value strategy.

Profile-default and containing-object collection-size rules apply only to collection/dictionary occurrences.

A collection-size rule is not applicable to a Logical Type.

## Configuration Validation

Local syntactic programmer errors such as these use ordinary .NET argument validation:

- blank profile name;
- probability outside `[0,1]`;
- non-positive weight;
- negative count;
- inverted count range.

Model-dependent invalid configuration must fail deterministically before ordinary generation proceeds.

This includes, at minimum:

- unknown scoped TypeId/PropertyId;
- unknown Logical Type;
- exact-property presence rule on a required property;
- exact-property positive null rule on a non-nullable property;
- incompatible weighted candidate;
- invalid enum candidate;
- impossible exact collection-size rule;
- composing profiles bound to different models.

The implementation may expose one profile-validation exception/result shape consistent with package conventions, but it must not silently discard invalid exact rules.

Profile validation diagnostics, if exposed, use a distinct `TESTDATA_POLICY_*` category so they are not confused with the existing `TESTDATA_PROFILE_*` terminology-sidecar diagnostics.

Exact diagnostic message text is not a compatibility contract.

## Determinism and Entropy Independence

All profile sampling is deterministic for the same aligned suite version and same:

```text
canonical model
TestData Profile
Semantic Terminology Profile
custom generator registration
root type
size profile
seed
root ordinal
generation coordinate
```

Sampling decisions use stable independent entropy discriminators.

Adding a null/presence policy must not change the non-null Random value of an occurrence merely because entropy-consumption order changed.

Likewise, adding a weighted rule for one unrelated property must not perturb another property's Random value.

M0082 sibling insertion/reordering stability remains required.

## Coordinated exact-property rules

M0084 extends TestData Profiles with coordinated exact-property rules defined in:

```text
docs/specs/test-data-coordination.md
```

The profile remains immutable, model-bound runtime configuration.

Coordinated rules are valid only on exact scalar/enum property rules and do not participate in the M0083 default/object/Logical-Type policy-precedence chain.

Supported coordinated rule families are:

```text
Derived/From
Sequence
Shared
Unique
```

Profile composition overlays coordinated fields at exact-property scope using the same left-to-right composition model. The final composed profile must be revalidated for contradictory combinations and dependency cycles.

M0084 coordinated rules are still TestData policy. They do not create canonical semantics, annotations, relationships, or persisted profile state.

## Facade Composition

`WithProfile`, `WithTerminology`, `WithSeed`, `WithSizeProfile`, and `WithBudgets` are immutable facade configuration operations.

For distinct settings, call order does not change semantic meaning.

Calling `WithProfile` again replaces the previously attached TestData Profile; profile composition is explicit through the TestData Profile composition API.

Existing property/Logical-Type custom-generator registration precedence remains unchanged.

## Compatibility

M0083 is additive on the 6.1.0 development line.

Existing public TestData APIs remain source-compatible.

Generation without a TestData Profile preserves M0082 behavior and existing M0082 focused tests, including the fixed same-version entropy vector, unless a separately identified correctness defect requires planning.

A TestData Profile is not a persisted compatibility artifact.

Exact generated Random values remain non-contractual across aligned suite versions according to `test-data-generation.md`.

## Non-Goals

M0083 does not add:

- canonical TestData annotations;
- source-generator TestData rules;
- JSON/YAML profile serialization;
- profile persistence/version negotiation;
- profile inheritance graphs;
- automatic rule inference from CLR/property names;
- business/domain faker datasets;
- provider/plugin packages;
- cross-object/cross-root dependency paths;
- application-global or persistent sequences;
- shared object/collection graphs;
- structural uniqueness for objects/collections;
- cross-root coherent datasets;
- referential integrity;
- invalid/adversarial generation;
- weighted relationship/business-process modeling;
- regex synthesis;
- new canonical semantic primitives.

Invocation-scoped exact-property coordination is defined by `test-data-coordination.md`; the remaining dataset/relationship/persistence concerns stay outside TestData Profiles.

## Required Consumer Evidence

The existing package-based programmatic model catalog remains the TestData consumer-documentation vehicle.

M0083 adds a stable scenario, equivalent to:

```text
sampling-profile
```

that demonstrates in readable source:

- a named TestData Profile;
- default plus exact-property/Logical-Type sampling rules;
- presence/null policy;
- weighted values;
- BoundaryMixed;
- collection-size policy;
- deterministic profile composition;
- coexistence with a Semantic Terminology Profile;
- deterministic `SemanticTestValue` inspection.

No separate sample project or per-scenario Markdown file is added.
