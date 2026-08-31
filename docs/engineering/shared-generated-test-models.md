# Shared Generated Test Models

## Purpose

Define the repository-wide positive integration fixture architecture for SemanticTypeModel.

The repository validates a code-first semantic-model system. Positive cross-package integration tests should therefore exercise real annotated CLR types, the real source generator, generated providers/manifests, and the target package boundary rather than reconstructing equivalent canonical models by hand.

## Canonical Fixture Assemblies

The repository maintains two independent projection-neutral model assemblies:

```text
tests/fixtures/SemanticTypeModel.TestModels.ModelA
tests/fixtures/SemanticTypeModel.TestModels.ModelB
```

Both:

- contain directly annotated CLR source types;
- run the real `SemanticTypeModel.Generators` source generator;
- expose independently generated semantic providers and manifests;
- do not depend on EF Core, JSON Schema, Power BI, System.Text.Json, ASP.NET Core, or another target projection package;
- do not reference each other;
- are ordinary buildable .NET projects rather than source strings embedded in target tests.

## Model A — Coverage Model

Model A is dimension-complete, not combinatorially exhaustive.

It should provide representative valid CLR authoring for the supported semantic dimensions used across the package suite, including:

- Entity and ValueObject roles;
- semantic inheritance;
- required/optional and nullable/non-nullable members;
- supported scalar kinds;
- supported Strong Scalar backing kinds, including Guid;
- enum and nullable enum;
- arrays/collections and dictionaries;
- owned object and owned collection shapes, including nullable/nested variants where supported;
- extension data;
- representative string, numeric, collection, format, and conditional-requiredness constraints;
- keys, Display Identity, and Access Paths;
- lifecycle mutability;
- user and technical descriptions;
- representative `ui.*` metadata;
- current envelope/evolution/lifecycle semantics;
- target-neutral CLR shapes needed by multiple projections.

When a new public semantic primitive or supported scalar/Strong Scalar backing kind is added, Model A should normally gain one representative valid authoring case.

Do not create a Cartesian product of every semantic combination.

## Model B — Independent Composition Model

Model B is smaller and independently generated.

It must contain enough overlapping concepts to prove that packages do not accidentally assume one model, one manifest, one namespace, or globally unique simple CLR names.

Where practical, use simple type names that also exist in Model A but place them in Model B's independent CLR namespace/model identity.

Model B should contain at least:

- its own Entity hierarchy;
- its own Guid-backed Strong Scalar;
- an owned value shape;
- enum and nullable member;
- enough independently generated metadata to participate in multi-model package tests.

## Positive Boundary Rule

For a positive test whose purpose crosses an authoring/generator/provider/manifest/target boundary, prefer Model A or Model B rather than a hand-built `TypeSchemaModel`.

Examples include:

```text
annotated CLR
-> source generator
-> generated provider/manifest
-> JSON Schema / EF Core / Power BI / System.Text.Json / DI / package consumer
```

The test should use the real boundary it claims to validate.

## Synthetic Unit-Test Rule

Hand-built canonical models remain appropriate when the purpose is specifically:

- canonical validation of an invalid state;
- transformation behavior isolated from .NET authoring;
- a pathological graph that valid CLR authoring cannot produce;
- a target-domain unit test whose inputs intentionally bypass generator/provider boundaries;
- very small unit input where authoring/generation is not part of the claim.

Do not force every unit test through shared fixtures.

The rule is to remove duplicate positive integration fixture systems, not to abolish synthetic unit tests.

## Multi-Model Rule

Multiple independently generated models are a first-class compatibility requirement.

Where a target naturally composes models into shared runtime/application state, tests must include both Model A and Model B together.

Examples:

```text
System.Text.Json
  -> one JsonSerializerOptions
  -> Model A + Model B

EF Core
  -> one application DbContext/model composition
  -> generated configuration from Model A + Model B
  -> unrelated manual application entity/configuration remains intact
```

Where the target naturally projects one model at a time, derive/project both models in the same test process and verify model-local identity and absence of global-state leakage.

Cross-model inheritance is not implied.

## Fixture Ownership

Target tests may add target-specific local test infrastructure, expected output, database contexts, assertion helpers, or invalid/synthetic models.

They should not create another durable positive CLR semantic-model fixture assembly when Model A or Model B can represent the scenario.

A dedicated fixture project remains justified only when its separate assembly boundary is itself the behavior under test and cannot be represented by the two canonical model assemblies. Such an exception must be explicit in the affected test documentation.

## Cleanup

After a positive scenario is represented by Model A/Model B and the affected target tests pass, remove redundant fixture types, hand-built positive canonical builders, and obsolete dedicated fixture projects.

Git is the history; do not keep parallel fixture systems solely for historical reference.
