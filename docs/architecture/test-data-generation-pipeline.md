# TestData Generation Pipeline

## Status

Authoritative TestData subsystem architecture.

## Purpose

Define stable responsibility boundaries inside `SemanticTypeModel.TestData` as TestData evolves from a single deterministic generator into a separate runtime configuration and generation domain.

Detailed sampling behavior belongs in `docs/specs/test-data-profiles.md`; base value-generation semantics belong in `docs/specs/test-data-generation.md`.

## System Boundary

TestData consumes the canonical model but does not author it:

```text
TypeSchemaModel
      |
      +----------------------------+
      |                            |
      v                            v
TestDataProfile            generation settings
(runtime policy)       seed / size / budgets /
      |                terminology / callbacks
      +-------------+--------------+
                    |
                    v
             Generation Request
                    |
                    v
             Policy Resolution
                    |
                    v
             Generation Engine
           /          |          \
 structural      value-source    deterministic
 traversal       resolution      entropy
                    |
                    v
             SemanticTestValue
               /          \
         inspection    optional CLR
                       materialization
```

The canonical model defines valid semantic space. TestData configuration defines how that space is sampled.

## Configuration Boundary

The public `model.TestData()` facade remains the consumer entry point.

The facade is an immutable configuration/orchestration surface over a cohesive internal settings state. Adding a TestData feature must not require accumulating unrelated feature-specific mutable maps directly in the facade.

Generation settings may include:

- seed;
- size profile;
- safety budgets;
- optional TestData Profile;
- optional Semantic Terminology Profile;
- programmatic property/Logical-Type generators.

Exact internal record/type layout is implementation-owned.

## TestData Profile Domain

`TestDataProfile` is a package-owned immutable runtime configuration model.

It is separate from:

- canonical `TypeSchemaModel`;
- canonical annotations;
- `SemanticTerminologyProfile`;
- CLR materialization;
- target projections.

A profile resolves by canonical identities and is validated against its bound model before generation.

Profile construction/composition logic must not be embedded throughout recursive graph traversal.

## Effective Policy Resolution

Generation resolves one Effective Sampling Policy for the current property/use site before policy-dependent decisions are made.

Rule precedence is:

```text
exact property
-> Logical Type
-> containing object type
-> profile defaults
-> built-in defaults
```

A rule layer overrides only fields it explicitly supplies.

The policy resolver owns profile scope/composition semantics. Structural generation should consume the resolved policy rather than reimplementing precedence at each node kind.

## Structural Generation

The structural engine owns graph shape:

- object property traversal;
- composition/inheritance expansion;
- arrays and dictionaries;
- references;
- recursion termination;
- node/depth budgets;
- canonical path/identity propagation.

It does not own profile composition rules or semantic candidate validation.

Presence/null sampling may decide whether a structurally legal property occurrence is omitted or null before leaf value-source selection, but those decisions are supplied by resolved policy.

## Value-Source Resolution

Scalar/enum value selection is conceptually a pipeline.

With M0083 the precedence is:

```text
programmatic property generator
-> programmatic Logical Type generator
-> property-scoped TestData Profile weighted values
-> Logical-Type TestData Profile weighted values
-> property Semantic Terminology Profile candidates
-> Logical-Type Semantic Terminology Profile candidates
-> built-in generation using the Effective Sampling Policy
```

The implementation need not expose public pipeline interfaces, but this precedence and separation must exist as one coherent internal responsibility rather than duplicated branch chains.

## Candidate Validation

Validation of a supplied scalar/enum candidate against canonical representation, format, pattern, and use-site constraints is TestData generation logic, not terminology-specific logic.

Semantic Terminology Profile candidates and TestData Profile weighted candidates must use one coherent internal validation path so the two sources cannot disagree about whether the same candidate is legal.

An explicit programmatic callback remains fail-closed when it supplies an invalid candidate.

## Deterministic Entropy

The M0082 generation-coordinate contract remains infrastructure for all sampling.

Entropy must be available as independent deterministic substreams/discriminators for policy decisions such as:

- optional-property presence;
- nullable null choice;
- weighted value selection;
- boundary-versus-random strategy choice;
- collection count selection;
- existing scalar/enum generation;
- uniqueness retry attempts.

Adding one policy decision must not perturb another merely because an implementation consumed entropy in a different order.

Exact entropy helper types and algorithms remain implementation-owned, subject to same-version/platform determinism and existing M0082 compatibility rules.

## Semantic Values, Materialization, and Inspection

The public semantic value family remains package-owned output independent of CLR materialization.

CLR materialization is a downstream conversion of an existing successful value graph. It must not own sampling policy or regenerate values.

Inspection is likewise downstream and remains deterministic human-readable output.

The implementation should reflect these boundaries rather than treating materialization as part of facade configuration or recursive sampling logic.

## Architecture Normalization

M0083 is allowed and expected to refactor the current TestData implementation so the subsystem reflects these responsibilities.

The architecture requires logical separation of:

- immutable configuration/settings;
- TestData Profile construction and policy resolution;
- deterministic entropy;
- structural graph traversal;
- value-source selection;
- reusable candidate validation;
- CLR materialization;
- inspection.

Exact files, class names, internal interfaces, namespaces below the existing public surface, and refactoring order are implementation-owned.

Do not introduce a general public middleware/plugin framework merely to express these seams.

## Forward Boundary

M0083 does not add stateful cross-value coordination.

A future coordinated-generation milestone may add:

- cross-property dependencies;
- sequences;
- shared/frozen values;
- scoped uniqueness;
- dependency-cycle detection.

The M0083 architecture must leave room for those concerns without implementing them early.

Cross-root coherent datasets and referential integrity remain a later, separate boundary.
