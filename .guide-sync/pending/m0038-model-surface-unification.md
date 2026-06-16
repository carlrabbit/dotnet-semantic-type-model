# Guide Sync Hint — M0038 Model Surface Unification

## Status

Pending until M0038 implementation completes.

## Purpose

Track documentation synchronization work that may remain after collapsing the `Model` / `Canonical` split.

## Trigger

Run this synchronization pass after M0038 implementation changes source, tests, samples, public API baselines, and package documentation.

## Check for stale references

Search for and remove or update active references to:

```text
SemanticTypeModel.Abstractions.Canonical
Abstractions.Canonical
TypeShape
ObjectShape
PropertyShape
ShapeRef
SchemaAnnotation
old shape graph
legacy model
hardened model
hand-built canonical model
```

Historical milestone documents may retain historical names when they are clearly historical records.

## Documentation surfaces to review

```text
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/TERMINOLOGY.md
docs/specs/type-model-core.md
docs/specs/type-model-runtime-api.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
public-docs/api/compatibility.md
public-docs/guides/json-schema.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/system-text-json.md
public-docs/nuget/*.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/release-notes.md
README.md
```

## Public documentation expectations

Public docs should state that:

- annotated .NET code is the supported authoring source;
- the source generator creates a `SemanticTypeModel.Abstractions.Model.TypeSchemaModel`;
- generated models can be passed directly to projections;
- the old shape graph and `Canonical` namespace are removed in the 2.2.0 line;
- public samples use generated providers, not hand-built model instances.

## Validation

Use documentation-safe validation after updates:

```sh
./eng/public-docs.sh
```

Run release-readiness validation only if a separate release milestone requests it.
