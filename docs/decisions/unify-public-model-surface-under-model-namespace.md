# Decision: Unify Public Model Surface Under `SemanticTypeModel.Abstractions.Model`

## Status

Accepted for M0038 planning.

## Context

The repository previously contained two active public model surfaces:

```text
SemanticTypeModel.Abstractions.Model
SemanticTypeModel.Abstractions.Canonical
```

The source generator emits the old `SemanticTypeModel.Abstractions.Model.TypeSchemaModel` shape graph, while current domain projections consume `SemanticTypeModel.Abstractions.Canonical.TypeSchemaModel`.

This split prevents generated code-first models from being passed directly to JSON Schema, EF Core, Power BI, System.Text.Json, dependency-injection, transformation, query, and inspection APIs.

It also causes public samples to hand-build canonical models instead of demonstrating the supported generated-provider workflow.

## Decision

Move the current canonical semantic model contracts to `SemanticTypeModel.Abstractions.Model` and make that namespace the sole public model namespace.

Remove the old shape-graph model contracts rather than preserving them as a compatibility layer.

Remove `SemanticTypeModel.Abstractions.Canonical` from shipped source and public API compatibility documentation after migration.

Update the source generator to emit unified `Model` contracts so generated provider output is directly consumable by every projection package.

## Rationale

`Model` is the clearer permanent public namespace for the semantic type model contract.

`Canonical` was useful as a transition namespace, but keeping both surfaces makes the repository harder to use and easier to drift.

The generated provider must produce the same model type that the rest of the package set consumes. Otherwise code-first samples cannot demonstrate the real consumer path.

Removing the old shape graph is preferable to retaining adapters because the old model family is less expressive and no longer matches the current transformation and projection architecture.

## Alternatives Considered

### Keep `Canonical` as the final namespace

Rejected. It would leave the public API with an awkward permanent namespace and keep `Model` available for old contracts unless separately removed or renamed.

### Make the generator emit `Canonical` types

Rejected. This would fix samples temporarily but preserve a non-final public namespace and still require a later public model rename.

### Keep both model families and add adapters

Rejected. This keeps the source of drift alive and encourages projection packages and samples to choose different surfaces.

### Add obsolete type forwards or compatibility wrappers

Rejected by default. A temporary shim may be approved by human review only if public API validation proves the break is otherwise too disruptive for the 2.2.0 line.

## Consequences

- M0038 is a public API breaking cleanup for the 2.2.0 line.
- Public API compatibility documentations must be updated intentionally.
- Public docs must explain the unified generated-provider workflow.
- Samples must use generated model providers instead of hand-built model instances.
- Projection packages become simpler because they can accept one model type.
- Old `Model` shape graph consumers must migrate to the unified model contracts.

## Validation

The implementation must prove that:

```text
source-generated models compile
source-generated models are accepted directly by all projection packages
old shape graph contracts are absent from shipped source
Canonical namespace is absent from shipped source and public API compatibility documentation
sample and package validation pass
public API compatibility documentation reflect intentional changes
```
