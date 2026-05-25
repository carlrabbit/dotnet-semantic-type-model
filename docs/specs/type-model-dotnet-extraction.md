# Type Model .NET Extraction Specification

## Purpose

Define Roslyn-based .NET type-system extraction into canonical semantic type model contracts.

M0010 extends this baseline with explicit attribute and convention sub-specifications:

- `docs/specs/type-model-dotnet-attributes.md`
- `docs/specs/type-model-dotnet-conventions.md`

## Authority

This specification is authoritative for:

- type discovery rules from .NET symbols;
- baseline attribute vocabulary and mapping;
- symbol-to-model mapping baseline;
- nullability/requiredness mapping behavior;
- generic, inheritance, and dictionary baseline handling;
- extraction diagnostics expectations.

## Package Boundary

- Extraction package: `SemanticTypeModel.DotNet`.
- Extraction logic may depend on Roslyn symbols, but Roslyn dependencies must remain isolated from core abstraction packages.
- Core abstraction contracts remain independent from Roslyn/source-generator APIs.

## Discovery Rules

- Baseline discovery is explicit opt-in via `[SemanticType]`.
- M0010 discovery modes are configured by `DotNetTypeDiscoveryMode` and default to `ExplicitAttributes`.
- Reachable type expansion from discovered roots is enabled for referenced property types.
- Default behavior remains conservative and does not scan full compilations without explicit roots.
- Baseline exclusions:
  - `[SemanticIgnore]` types or members;
  - private/static/indexer/compiler-generated members;
  - methods/events/fields by default.

## Attribute Vocabulary

Baseline extraction attributes:

- `[SemanticType]`
- `[SemanticIgnore]`
- `[SemanticName]`
- `[SemanticDescription]`
- `[SemanticRole]`
- `[SemanticKey]`
- `[SemanticRelationship]`

Required behavior:

- attributes map to canonical semantics/annotations;
- invalid usage is diagnosable;
- attribute data does not bypass model validation.

## Symbol-to-Model Mapping Baseline

### Named types

- class/record class/struct/record struct -> object baseline mapping;
- enum -> enum baseline mapping;
- unsupported shapes -> diagnostics.

### Members

- baseline member extraction is public instance properties;
- `required` and nullable annotations are preserved on property contracts;
- methods/events/indexers/static/non-public/compiler-generated members are excluded.

### Nullability and requiredness

Mapping preserves separation between:

- requiredness (presence),
- nullability (value),
- collection shape.

Defaults:

- C# `required` -> required property;
- nullable references/value types -> nullable property;
- non-nullable references/value types -> non-nullable property.

### Scalar baseline

- bool -> boolean;
- string -> string;
- integer primitives -> integer;
- floating primitives -> number;
- decimal/date/time/datetime/datetimeoffset/duration/guid/binary/json -> deterministic scalar+annotation baseline.

### Enum baseline

- enum names and numeric values are extracted;
- duplicate/ambiguous numeric values are diagnosable.

### Collection/dictionary baseline

Collections:

- arrays;
- `IEnumerable<T>`;
- `IReadOnlyCollection<T>`;
- `IReadOnlyList<T>`;
- `ICollection<T>`;
- `IList<T>`;
- `List<T>`;
- `HashSet<T>`.

Dictionaries:

- `IDictionary<TKey,TValue>`;
- `IReadOnlyDictionary<TKey,TValue>`;
- `Dictionary<TKey,TValue>`.

Unsupported dictionary key types are diagnosable.

### Generics baseline

- closed constructed generics produce stable deterministic type ids;
- open generic roots are diagnosable and not emitted.

### Inheritance/interface baseline

- base type and implemented interfaces are preserved as deterministic metadata annotations;
- ambiguous/unsupported inheritance cases are diagnosable.

## Diagnostics

Extraction diagnostics use `STM5xxx` range and include at least:

- invalid attribute usage;
- unsupported type shape;
- unsupported open generic type;
- unsupported dictionary key type;
- enum numeric ambiguity;
- unresolved/deferred extraction cases.
