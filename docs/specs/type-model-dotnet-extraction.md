# Type Model .NET Extraction Specification

## Purpose

Define Roslyn-based .NET type-system extraction into canonical semantic type model contracts.

Runtime .NET extraction is one of the supported code-first canonical model creation paths.

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
  - methods/events and unannotated fields by default; public fields with an explicit lifecycle-mutability declaration are extracted as canonical properties.

## Attribute Vocabulary

Baseline extraction attributes:

- `[SemanticType]`
- `[SemanticIgnore]`
- `[SemanticName]`
- `[SemanticUserDescription]`
- `[SemanticTechnicalDescription]`
- `[SemanticRole]`
- `[SemanticKey]`
- `[SemanticMutable]`
- `[SemanticImmutable]`

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
- decimal/date/time/datetime/datetimeoffset/duration/guid/binary/json -> deterministic scalar+annotation baseline;
- `char` is String, `ReadOnlyMemory<byte>` is Binary, and `JsonDocument`/`JsonElement`/`JsonNode` are Json;
- `System.Uri` is String with inferred `schema.format=uri-reference`; explicit format metadata may request `uri`.

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

## M0049 Dictionary Type Extraction Invariant

Dictionary extraction MUST normalize and extract both key and value type arguments before creating the dictionary type definition. The dictionary `KeyType` and `ValueType` references MUST resolve in the canonical model for supported dictionary key types, including extension-data dictionaries.

## M0050 URI Scalar Compatibility

`System.Uri` and nullable `System.Uri` members MUST extract as string-compatible scalar definitions. A `Uri` member implies `schema.format=uri-reference` because relative references are valid. An explicit supported format annotation may request the stronger `uri` constraint. `STM5025` remains required for formats on unsupported member shapes.

## M0051 inherited semantic members

Extraction includes inherited public properties on semantic derived types. A non-semantic or abstract base type may therefore contribute `[SemanticExtensionData]` to a derived semantic value object without itself becoming a semantic root. Hidden derived properties take precedence by CLR member name.
