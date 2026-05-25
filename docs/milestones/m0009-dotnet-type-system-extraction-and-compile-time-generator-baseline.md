# M0009 - .NET Type System Extraction and Compile-Time Generator Baseline

## Purpose

Deliver the first compile-time path from C# type symbols to the canonical semantic model used by existing projections.

## Delivered Package Boundaries

- `SemanticTypeModel.DotNet`
  - attribute vocabulary for opt-in discovery and metadata;
  - Roslyn-based discovery and extraction rules;
  - scalar/object/enum/collection/dictionary/generic mapping baseline;
  - deterministic extraction diagnostics (`STM5xxx`) for unsupported or ambiguous shapes.
- `SemanticTypeModel.Generators`
  - incremental source generator baseline;
  - deterministic generated provider API:
    - `SemanticTypeModel.Generated.AppSemanticTypeModel.Create()`;
  - generator diagnostics surfaced from extraction diagnostics;
  - generated output that produces canonical `TypeSchemaModel` instances for existing transformation and projection pipelines.

## Discovery Baseline

- Explicit opt-in root discovery through `[SemanticType]`.
- Reachable graph expansion from discovered roots for referenced property types.
- Conservative default behavior:
  - no implicit full-compilation scanning without explicit roots.
- Exclusions:
  - `[SemanticIgnore]` types and members;
  - private/static/indexer/compiler-generated members;
  - methods, events, and fields by default.

## Attribute Vocabulary Baseline

- `[SemanticType]`
- `[SemanticIgnore]`
- `[SemanticName]`
- `[SemanticDescription]`
- `[SemanticRole]`
- `[SemanticKey]`
- `[SemanticRelationship]`
- `[assembly: SemanticTypeModelGeneratorOptions(...)]` for generator namespace/provider/options baseline.

## Mapping Baseline

- Named CLR object-like types map to object shapes.
- Enums map to enum shapes with value names and numeric metadata annotations.
- Public instance properties map to property shapes.
- Requiredness and nullability extraction baseline:
  - C# `required` -> required property;
  - nullable reference/value -> nullable property;
  - non-nullable -> non-nullable property.
- Scalar mapping baseline includes:
  - bool/string/integer/number;
  - decimal/date/time/datetime/datetimeoffset/duration/guid/binary/json via canonical scalar+annotation mapping.
- Collection baseline:
  - arrays, `IEnumerable<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `ICollection<T>`, `IList<T>`, `List<T>`, `HashSet<T>`.
- Dictionary baseline:
  - `IDictionary<TKey,TValue>`, `IReadOnlyDictionary<TKey,TValue>`, `Dictionary<TKey,TValue>`;
  - unsupported key types emit diagnostics.
- Generic baseline:
  - stable ids for closed constructed generics;
  - open generics diagnosed.
- Inheritance/interface baseline:
  - preserved as deterministic annotations (`dotnet.baseType`, `dotnet.interfaces`).

## Generator Output Baseline

- Deterministic generated source.
- Generated code depends on canonical model libraries only.
- No direct projection-specific generation.
- Generated model can be consumed by:
  - transformation pipeline;
  - JSON Schema export path.

## Diagnostics Baseline

`STM5xxx` baseline includes:

- invalid attribute usage;
- unsupported type shapes;
- unsupported open generic types;
- unsupported dictionary key types;
- duplicate/ambiguous enum numeric values;
- deferred/unsupported extraction cases.

## Fixture/Test Coverage

Short-running generator tests cover:

1. simple annotated object;
2. scalar mappings;
3. collections and dictionaries + unsupported key diagnostics;
4. enum extraction;
5. nested object reachability;
6. generics (closed identity + open generic diagnostics);
7. inheritance/interface metadata baseline;
8. generated-model-to-JSON-Schema export composition.

## Non-goals Preserved

- no direct Roslyn-to-JSON-Schema generation;
- no direct EF Core/Power BI/OpenAPI/UI generator outputs;
- no runtime reflection extractor baseline;
- no analyzer/fixer package;
- no long-running test introduction.
