# Decision: Canonical Model Authoring Supports Code-First and Programmatic Construction

## Status

Accepted for M0080.

This decision supersedes the earlier form of this document that made annotated .NET code the only supported canonical authoring source. The file path is retained to avoid a documentation move whose only purpose would be renaming historical authority.

## Context

SemanticTypeModel deliberately removed JSON Schema import and other external-schema authoring paths so the project would not become a general schema interchange framework.

That decision also classified runtime model editing as unsupported and left annotated .NET code as the only supported authoring source.

The unified canonical model and downstream runtime APIs have since established a different useful boundary:

```text
TypeSchemaModel
```

is already the projection-neutral contract consumed by validation, transformations, TestData, JSON Schema, Power BI, runtime services, and other domain derivations.

There is a legitimate runtime use case where the semantic model is not known as a compile-time CLR type graph. Examples include metadata-driven test-data generation and analytical models assembled from runtime configuration or discovered metadata.

Requiring such callers to invent CLR types merely to obtain a canonical semantic model adds representation work without adding semantic authority.

## Decision

SemanticTypeModel supports two canonical model authoring paths:

1. **Code-first authoring** from annotated .NET types through runtime extraction or compile-time generation.
2. **Programmatic Model Authoring** through an explicit Core-owned authoring/finalization API that produces the same canonical `TypeSchemaModel` surface.

Both paths converge on one canonical model:

```text
annotated .NET code --------------------+
                                        |
programmatic canonical declarations ----+--> TypeSchemaModel
                                             |
                                  validate / transform / query
                                             |
                               target/runtime consumers
```

There is no separate dynamic model hierarchy, model kind, provenance flag, or target-specific canonical representation.

Programmatic Model Authoring is construction of a canonical snapshot, not live editing of a finalized model. Mutable construction state exists only inside the authoring builder. A successful build returns a fresh canonical model snapshot whose later behavior is independent from subsequent builder changes.

The programmatic authoring API is owned by `SemanticTypeModel.Core`. `SemanticTypeModel.Abstractions` continues to own the canonical contracts and does not gain validation or authoring-policy dependencies.

## Identity and Determinism

Programmatic authors must provide stable `SchemaModelId`, `TypeId`, and `PropertyId` values.

The authoring API must not create random, timestamp-based, machine-specific, or declaration-order-only canonical identities.

Forward references and recursive graphs are allowed through ordinary `TypeRef(TypeId)` references. Resolution is checked when the model is finalized.

Given the same declared canonical content, the resulting model ordering, lookup index, validation diagnostics, and downstream behavior must be deterministic.

## Validation Boundary

Programmatic construction is successful only when the produced canonical model satisfies the same invariants as any other canonical model.

The Core authoring API therefore finalizes through the canonical validator and returns structured diagnostics. It must not silently repair semantic errors, drop invalid declarations, or return an apparently successful model whose canonical invariants are known to be broken.

## Target Boundaries

A consumer that requires only canonical semantics must not reject a valid model merely because it was programmatically authored.

This includes, where the target's existing semantic contract otherwise supports the model shape:

- canonical validation, query, inspection, and transformation;
- TestData semantic value generation;
- JSON Schema derivation/export;
- Power BI domain derivation/local metadata;
- runtime model-provider/projection composition.

Programmatic authoring does not manufacture CLR lineage.

Therefore it does not by itself make these operations available:

- TestData CLR materialization for a type that does not exist;
- System.Text.Json CLR resolver customization without corresponding CLR types/members;
- generated EF Core application configuration that requires compile-time CLR/model manifests;
- any other target behavior whose established contract explicitly depends on CLR identity or generated lineage.

Provider-neutral target behavior that genuinely consumes only canonical semantics may continue to work.

## External Formats

Supporting programmatic construction does not restore external schema formats as canonical authoring sources.

Still unsupported as direct canonical authoring inputs unless a future accepted decision says otherwise:

- JSON Schema import;
- OpenAPI import;
- TypeScript import;
- arbitrary database/schema import;
- YAML/JSON model DSLs treated as canonical interchange contracts.

Applications may parse their own external/runtime inputs and translate them explicitly into the programmatic authoring API. The translation policy remains application-owned unless STM later adopts a specific integration.

Persisted model snapshots remain snapshots, not a second hand-authored schema language.

## Compatibility

This is an additive capability for the 6.1.0 line.

- Existing code-first APIs and generated providers remain supported.
- Existing `TypeSchemaModel` contracts remain the canonical representation.
- No new NuGet package is introduced.
- Exact suite-version alignment remains unchanged.
- The compile-time manifest contract is not broadened into a runtime dynamic-model interchange format.

## Rationale

The prior code-only rule successfully removed competing schema-import languages, but programmatic construction is materially different from schema import: callers express the existing canonical contracts directly and receive the same validation and downstream semantics.

This keeps one semantic model while enabling runtime-defined domains without introducing generated CLR types, a second schema graph, or target-specific authoring models.

## Consequences

- Core gains a small public authoring/finalization surface over existing canonical definitions.
- Public documentation must distinguish code-first authoring from programmatic authoring without presenting external formats as peers.
- TestData gains first-class untyped facade operations keyed by canonical IDs and a canonical-ID property-generator registration path.
- Power BI and JSON Schema require acceptance evidence from a programmatically authored model but do not require new authoring-specific projection APIs.
- Documentation that says code-first is the *only* canonical authoring path must be updated; code-first remains the primary CLR-oriented workflow.
- Dynamic Model may be used informally for a programmatically authored canonical snapshot, but it is not a separate canonical type or mutable runtime object.
