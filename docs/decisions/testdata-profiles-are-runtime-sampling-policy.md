# Decision: TestData Profiles Are Runtime Sampling Policy, Not Canonical Semantics

## Status

Accepted for M0083.

## Context

The canonical `TypeSchemaModel` answers what a value means and which values are legal. TestData increasingly needs a different class of information:

- how often an optional property should be present;
- how often a nullable property should be null;
- whether a generated batch should emphasize semantic boundaries;
- how candidate values should be weighted;
- how collection counts should be sampled for one test scenario.

These choices are useful for testing but are not durable facts about the domain represented by the canonical model.

Encoding them as canonical annotations would make the semantic model carry test-scenario policy, couple unrelated consumers to TestData configuration, and encourage target/runtime behavior to be inferred from metadata whose meaning exists only during synthetic generation.

Semantic Terminology Profiles already demonstrate a separate sidecar boundary for synthetic vocabulary. M0083 needs a runtime configuration boundary for sampling policy rather than another canonical semantic vocabulary.

## Decision

`SemanticTypeModel.TestData` owns a separate first-class **TestData Profile** domain.

A TestData Profile:

- is immutable after successful construction;
- is bound to one canonical model for validation and consumption;
- is named so applications can represent scenarios such as `Typical`, `Minimal`, or `Boundary`;
- selects canonical elements through stable `TypeId`, `PropertyId`, and Logical Type identities;
- controls only TestData sampling policy;
- never mutates `TypeSchemaModel`;
- never creates canonical annotations;
- is not consumed by Core or target packages;
- is not a canonical authoring source.

The canonical model remains the validity boundary:

```text
TypeSchemaModel
    = what is modeled and legal

TestDataProfile
    = how this TestData scenario samples that legal space
```

A Logical Type may be used as a TestData Profile selector because it is an existing canonical semantic identity. Doing so does not give the Logical Type TestData behavior in the canonical model.

TestData Profiles are runtime .NET configuration in M0083. They do not define a JSON/YAML interchange format, persistence protocol, attribute model, or source-generator input.

## Composition

Named profiles may be composed explicitly.

Composition is deterministic layering, not inheritance:

- all profiles in one composition must target the same model;
- later profiles override fields explicitly set by earlier profiles at the same rule scope;
- a later weighted-candidate set replaces the earlier set at that scope rather than merging weights;
- M0083 does not introduce parent graphs, multiple-inheritance semantics, or a persisted composition model.

## Relationship to Semantic Terminology Profiles

The two concepts remain distinct:

```text
SemanticTerminologyProfile
    = candidate vocabulary / example values

TestDataProfile
    = runtime scenario and sampling policy
```

They may be used together.

TestData Profile rules may provide weighted explicit values for a property or Logical Type. Those values are scenario-specific sampling input, not terminology persistence and not canonical semantics.

## Consequences

TestData can evolve policies such as boundary emphasis or null frequency without adding canonical vocabulary.

Other projections remain unaffected.

M0084 may build coordinated generation such as dependent values, sequences, shared values, and scoped uniqueness on the TestData-domain architecture created by M0083 without changing this canonical boundary.
