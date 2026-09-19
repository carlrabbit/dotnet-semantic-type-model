# Programmatic Model Authoring Specification

## Status

Authoritative behavioral specification for M0080 and the 6.1.0 programmatic canonical-model authoring capability.

## Purpose

Allow applications to construct a canonical `TypeSchemaModel` from runtime information without generating CLR types and without introducing a second semantic model hierarchy or external schema language.

The capability answers:

```text
Given explicit canonical declarations assembled at runtime,
can STM deterministically finalize and validate the same TypeSchemaModel
that code-first consumers already use?
```

## Package Boundary

The authoring capability is owned by:

```text
SemanticTypeModel.Core
```

No new package is introduced.

`SemanticTypeModel.Abstractions` continues to own canonical contracts. Core may depend on those contracts and on Core validation, but Abstractions must not depend outward on authoring or validation policy.

The existing transformation-oriented `SemanticTypeModel.Abstractions.Model.TypeSchemaModelBuilder` is not repurposed as the public authoring surface.

## Public Authoring Surface

The primary public concepts are:

```text
TypeSchemaModelAuthoringBuilder
TypeSchemaModelAuthoringResult
```

They live in the fixed public namespace:

```text
SemanticTypeModel.Core.Authoring
```

The minimum public surface is:

```csharp
namespace SemanticTypeModel.Core.Authoring;

public sealed class TypeSchemaModelAuthoringBuilder
{
    public TypeSchemaModelAuthoringBuilder(
        SchemaModelId id,
        AnnotationBag? annotations = null);

    public TypeSchemaModelAuthoringBuilder AddType(TypeDefinition definition);

    public TypeSchemaModelAuthoringResult Build();
}

public sealed record TypeSchemaModelAuthoringResult
{
    public TypeSchemaModel? Model { get; init; }
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }
    public bool Succeeded { get; }
    public bool HasErrors { get; }
}
```

The exact constructor implementation and additional convenience overloads are implementation-local, but the minimum public contract above is fixed.

Rules:

- the caller supplies the `SchemaModelId`;
- optional root annotations are supplied as canonical `AnnotationBag` content;
- `AddType(TypeDefinition)` accepts any current canonical `TypeDefinition` subtype and is fluent;
- declarations are accumulated without requiring `TypesById` maintenance by the caller;
- `Build()` finalizes one fresh canonical snapshot and validates it;
- the result exposes structured diagnostics and success/error state;
- a successful result exposes the finalized `TypeSchemaModel`;
- an unsuccessful result does not expose a model as safe for downstream consumption.

An `AddTypes(IEnumerable<TypeDefinition>)` convenience is allowed but not required.

A second fluent semantic hierarchy duplicating `ObjectTypeDefinition`, `PropertyDefinition`, constraints, annotations, keys, and related canonical records is explicitly out of scope for 6.1. Callers construct those existing canonical definitions directly and use the authoring builder for root-model assembly, indexing, snapshotting, and validation.

## Result Contract

`TypeSchemaModelAuthoringResult` must carry at least:

- `TypeSchemaModel? Model`;
- `IReadOnlyList<SchemaDiagnostic> Diagnostics`;
- a success/error indicator derived from diagnostics and model availability.

Rules:

- success requires a non-null model and no error diagnostics;
- failure must not masquerade as a usable finalized model;
- warnings may accompany a successful model if canonical validation permits them;
- ordinary argument misuse may use normal .NET argument exceptions;
- semantic invalidity is represented by structured canonical diagnostics.

## Identity

Programmatic authors explicitly provide canonical identity.

Required caller-owned identities are:

- `SchemaModelId` for the model;
- `TypeId` for every type;
- `PropertyId` for every property.

The authoring layer must not create canonical IDs from timestamps, random values, machine paths, object hashes, collection positions, or other unstable state.

The author may intentionally choose IDs derived from stable application metadata or names. STM does not impose a global identity scheme beyond current canonical validation.

## Type Set and Lookup Index

The caller declares types once through the authoring builder.

The builder owns construction of:

```text
TypeSchemaModel.Types
TypeSchemaModel.TypesById
```

Rules:

- `Types` preserves deterministic declaration order;
- `TypesById` is generated from the finalized type set;
- duplicate `TypeId` declarations are validation errors rather than silent replacement;
- callers are not required or allowed to supply an independent `TypesById` index to the authoring builder;
- a successful model has matching `Types` and `TypesById` content.

## References and Recursive Graphs

References use existing canonical `TypeRef(TypeId)` values.

Forward references are supported: a definition may refer to a `TypeId` declared later in the authoring sequence.

Recursive graphs are supported to the same extent as the canonical model itself.

Resolution occurs at finalization/validation. An unresolved reference prevents successful finalization.

The authoring builder must not require topological declaration order.

## Canonical Semantics

Programmatic authoring can express every semantic concept that the existing canonical contracts can represent, including:

- scalar, object, enum, array, dictionary, union, intersection, reference, `Any`, and `Never` types;
- properties and cardinality;
- keys and entity semantics;
- lifecycle mutability;
- constraints and conditional constraints;
- descriptions;
- annotations, including Logical Type metadata and target-owned annotations when the target contract allows them;
- composition and computed-member metadata.

M0080 does not add new canonical semantics merely to make authoring easier.

The escape hatch for the full current model family is `AddType(TypeDefinition)`; no new parallel type-definition hierarchy is required.

## Annotations and Provenance

Programmatic declarations are explicit application declarations.

The authoring layer must not fabricate CLR-lineage annotations such as `dotnet.memberName` or pretend that programmatically declared types originated from source generation.

When the authoring API itself creates helper annotations in a future extension, they must use the established annotation key/scope/source contracts. M0080 does not require helper methods that duplicate the full semantic attribute vocabulary.

## Finalization and Validation

`Build()` performs deterministic finalization and canonical validation.

At minimum it must:

1. snapshot the declared root model content;
2. construct the `TypesById` lookup index;
3. invoke the current canonical `TypeSchemaModelValidator` contract;
4. combine any authoring/finalization diagnostics with canonical validation diagnostics;
5. return success only when no error remains.

Existing canonical validation IDs are reused where applicable, including duplicate identities, unresolved references, invalid property/key references, invalid cardinality/constraint ranges, annotation problems, Logical Type conflicts, and conditional-constraint errors.

The authoring layer must not silently normalize away a declaration merely to obtain a successful result.

## Snapshot and Builder Lifetime

Programmatic authoring uses mutable working state only inside the builder.

Each successful `Build()` returns a fresh model snapshot. Subsequent additions or changes to the builder must not mutate a model returned by an earlier successful build.

The builder may support multiple builds from evolving construction state, provided each result is independently snapshotted and deterministic.

The authoring layer must not expose live mutable root collections that allow a consumer to bypass the finalization boundary after a successful build.

## Downstream Provenance Neutrality

A canonical consumer whose contract depends only on `TypeSchemaModel` semantics must behave the same for equivalent valid models regardless of supported authoring path.

M0080 requires representative evidence for:

```text
Programmatic Model Authoring
-> TypeSchemaModel
-> TestData semantic generation

Programmatic Model Authoring
-> TypeSchemaModel
-> JSON Schema derivation/export

Programmatic Model Authoring
-> TypeSchemaModel
-> Power BI domain derivation/local metadata
```

Equivalent models need not have byte-identical object instances; their canonical identity, semantics, validation outcome, and target-visible behavior must be equivalent under the relevant target contract.

## CLR-Lineage Boundary

Programmatic authoring does not create CLR runtime types and does not establish CLR member lineage.

Therefore:

- TestData semantic generation is supported for programmatically authored models;
- TestData `Generate<T>()` and CLR materialization remain available only when the canonical model and actual CLR type are compatible under the existing materialization contract;
- System.Text.Json resolver customization still requires the CLR types/members required by its existing contract;
- generated EF Core application configuration still requires the compile-time model/manifest path;
- no `Reflection.Emit`, runtime compilation, source emission, or hidden CLR type generation is introduced.

## TestData Dynamic-Model Experience

`SemanticTypeModel.TestData` must expose first-class facade operations that do not require a CLR generic root:

```text
SemanticTestValue Generate(TypeId rootTypeId)
IReadOnlyList<SemanticTestValue> GenerateMany(TypeId rootTypeId, int count)
```

These operations use the facade's current seed, size profile, terminology profile, budgets, and custom generators.

Failure behavior matches the typed facade: unsuccessful semantic generation throws `TestDataGenerationException` carrying the underlying `TESTDATA_*` diagnostics. The existing low-level `SemanticTestDataGenerator.Generate(...)` result-based API remains available for callers that prefer non-throwing generation.

Bulk generation preserves the existing deterministic root-seed-plus-ordinal policy. Zero returns an empty sequence; negative count is an argument error.

The facade must also support property-specific programmatic generators without a CLR expression:

```text
WithPropertyGenerator(TypeId ownerTypeId, PropertyId propertyId, Func<TestDataGeneratorContext, object?> generator)
```

The canonical-ID registration participates at the same property-generator precedence level as the existing CLR-expression overload:

```text
property programmatic generator
-> Logical Type programmatic generator
-> property terminology
-> Logical Type terminology
-> Random generation
```

If both property-generator registration forms target the same canonical property on one facade instance, the later registration wins deterministically.

The existing expression-based overload remains supported. Implementation should converge both forms on canonical property identity where practical so source provenance does not create different generation semantics.

## Power BI Boundary

No new Power BI authoring API is introduced by M0080.

`DerivePowerBiModel(TypeSchemaModel, ...)` already represents the correct boundary. Its public documentation and API comments must no longer imply that the supplied canonical model must have come from code-first generation when that is not a semantic requirement.

M0080 adds representative acceptance evidence that a programmatically authored valid canonical model with Dimension/Fact semantics derives the expected local Power BI metadata.

## JSON Schema Boundary

No JSON Schema import path is restored.

A programmatically authored valid canonical model may be exported through the existing JSON Schema derivation/export APIs because the canonical model remains the source of truth.

## Runtime Provider Composition

An application may return a successfully programmatically authored `TypeSchemaModel` through the existing runtime model-provider/service abstractions.

M0080 does not require a new DI registration primitive or provider type merely for programmatic authoring.

## Compatibility and Versioning

This capability targets the `6.1.0` aligned suite.

It is additive relative to 6.0.0:

- no canonical type is removed or reinterpreted;
- code-first generation remains supported;
- no existing target is required to support CLR-dependent behavior without CLR lineage;
- package count remains unchanged;
- exact aligned suite versions remain required.

The M0080 validation package version is:

```text
6.1.0-m0080
```

Stable 6.1.0 publication is outside milestone completion unless separately and explicitly authorized.

## Non-Goals

M0080 does not add:

- a second dynamic/canonical model hierarchy;
- a general mutable model editor;
- JSON/YAML/XML/OpenAPI/JSON Schema import as canonical authoring;
- database-schema import;
- a persisted programmatic-authoring DSL or compatibility protocol;
- automatic CLR type generation or `Reflection.Emit`;
- runtime source compilation;
- EF Core generated configuration for models without compile-time CLR lineage;
- System.Text.Json CLR resolver behavior for models without CLR types;
- new canonical semantic primitives;
- a comprehensive fluent builder layer duplicating every canonical record;
- stable 6.1.0 publication.

## Required Public Documentation

Implementation must update the directly affected consumer surfaces to make the two authoring paths explicit and prevent the old code-only statement from remaining current public guidance.

At minimum review and update as applicable:

- repository `README.md`;
- `public-docs/usage.md`;
- `public-docs/api/compatibility.md`;
- `public-docs/guides/core-semantics.md` or the smallest appropriate authoring guide surface;
- `public-docs/guides/test-data.md`;
- `public-docs/guides/power-bi.md` where provenance wording changes;
- shared NuGet README;
- `public-docs/release-notes.md` for the 6.1.0 development line.

Public documentation must preserve the external-import non-goal and the CLR-lineage target limitations.
