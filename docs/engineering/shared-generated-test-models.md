# Shared Generated Test Models

## Purpose

Define the repository-wide positive integration fixture and projection-matrix architecture.

Positive boundary tests should exercise real annotated CLR types, the real source generator, generated providers/manifests, and the target package boundary rather than manually reconstructing equivalent canonical models.

## Canonical Fixture Assemblies

```text
tests/fixtures/SemanticTypeModel.TestModels.ModelA
tests/fixtures/SemanticTypeModel.TestModels.ModelB
```

Both are projection-neutral generated model assemblies and do not depend on target projection packages.

## Model A — Coverage and Matrix Model

Model A is dimension-complete, not combinatorially exhaustive.

It owns representative valid semantic authoring plus explicit matrix carriers for dimensions that must be tested systematically across targets.

At minimum, actual modeled property uses must cover every currently supported scalar and Strong Scalar backing kind:

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
```

A type declaration with no modeled property use does not count as matrix coverage.

Where nullability changes target behavior, include deliberate required/optional cases without creating a Cartesian product.

Model A also continues to cover representative inheritance, enums, ownership, extension data, constraints, keys, Display Identity, Access Paths, mutability, descriptions, `ui.*`, envelope/evolution/lifecycle semantics, and other projection-neutral dimensions.

## Model B — Composition and Isolation Model

Model B is smaller and independently generated.

Its purpose is model independence/composition, not additional semantic breadth.

It should deliberately overlap simple CLR names with Model A where useful and retain its own Entity hierarchy, Guid Strong Scalar, owned value shape, enum/nullable member, and independent generated provider/manifest.

Do not duplicate the full Model A matrix in Model B.

## Matrix Rule

A shared fixture is not a test matrix merely because it contains many types.

For each target:

1. identify applicable Model A matrix dimensions;
2. define target-specific expected behavior for all applicable cases;
3. exercise those cases through the real generated provider/model boundary;
4. keep target expectations local to that target test project;
5. do not force irrelevant dimensions onto a target.

The fixture owns inputs. The target owns expected outputs/behavior.

Do not create one universal cross-package expected-output table.

## Matrix Discoverability

Matrix cases must be deterministic.

Acceptable mechanisms include explicit projection-neutral fixture inventory metadata/helpers, reflection over dedicated matrix carrier properties, or deterministic generated-model inspection scoped to dedicated matrix carrier types.

Do not infer matrix cases from arbitrary naming conventions such as `*Id`.

## Positive vs Synthetic Tests

Use Model A/B for positive boundary claims that include code-first authoring/generation/provider transport.

Hand-built canonical models remain valid for invalid states, isolated transformations, pathological graphs, small direct unit behavior, and target-domain tests intentionally bypassing authoring/generation.

Inline Roslyn source remains valid for extraction/generator diagnostics.

## Multi-Model Rule

Multiple independently generated models are a first-class compatibility requirement.

Where targets compose shared state, test Model A and Model B together. Where targets project one model at a time, project both in one process and prove no global-state leakage.

Composition/isolation tests should assert complete relevant state when practical, not merely containment of selected expected items.

Cross-model inheritance is not implied.

## Fixture Ownership

Target-specific contexts, expected output, serializer options, DI containers, assertion helpers, and database setup stay in target test projects.

Do not create another durable positive CLR fixture assembly when Model A/B can represent the scenario.

Git retains history; do not keep parallel obsolete fixture systems.
