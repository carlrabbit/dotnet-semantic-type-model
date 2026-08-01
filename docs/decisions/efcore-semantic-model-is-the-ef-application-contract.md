# Decision: EfCoreSemanticModel Is the EF Application Contract

## Status

Accepted for M0052.

## Context

The 2.4.3 CLR convention augmentation framing treated EF Core conventions as the initial model source and STM as a repair/suppression layer.

That is too permissive for SemanticTypeModel. The semantic model is a closed domain. EF Core is a projection target and must not rediscover or reinterpret semantic shape through conventions.

## Decision

`EfCoreSemanticModel` is the complete EF application contract.

`ApplySemanticTypeModel(...)` must derive `EfCoreSemanticModel` and then apply it.

`ApplyEfCoreSemanticModel(...)` must apply the same closed EF semantic model when required source lineage exists.

EF Core conventions are constrained, suppressed, overridden, diagnosed, or rejected. They are not model authority.

Shared-type projection remains explicit and secondary.

CLR convention augmentation is removed, renamed, constrained, or made non-default.

## Consequences

- EF application behavior is deterministic.
- Semantic-only members such as `ExtensionData` are represented as EF suppressions in the EF semantic model.
- Value-object boundaries are enforced by the EF semantic model.
- Both public ModelBuilder paths converge.
- `EfCoreSemanticModel` must carry more source lineage.
- Some previously tolerated mixed EF convention usage may produce diagnostics or exceptions.

## Rejected Alternatives

### Keep CLR convention augmentation as default

Rejected because it communicates EF conventions first, STM second.

### Require NotMapped on semantic-only members

Rejected as the primary model because it leaks EF concerns into semantic authoring.

### Keep ApplyEfCoreSemanticModel as shared-type only

Rejected because it would make `EfCoreSemanticModel` a lossy DTO rather than a real EF semantic model.

### Let EF conventions infer relationships absent from the semantic model

Rejected because it violates closed domain semantics.
