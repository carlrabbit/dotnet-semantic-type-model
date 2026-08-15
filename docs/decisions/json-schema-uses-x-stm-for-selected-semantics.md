# JSON Schema Uses `x-stm` for Selected Semantic Preservation

## Status

Accepted for M0062.

## Decision

`SemanticTypeModel.JsonSchema` uses one optional JSON Schema extension object named `x-stm` to preserve selected canonical STM semantics that standard JSON Schema cannot represent faithfully.

`x-stm` is not a serialized canonical model and is not an alternative authoring format.

The initial extension vocabulary is intentionally bounded to:

```text
role
aggregateRoot
mutability
technicalDescription
keys
unit
ui
```

Standard JSON Schema keywords remain authoritative for concepts JSON Schema already represents.

## UI Metadata

UI metadata is transported under:

```json
{
  "x-stm": {
    "ui": {}
  }
}
```

Canonical `ui.*` annotations map into the `ui` object by removing the leading `ui.` prefix.

Existing explicit semantics such as `ui.title`, `ui.category`, and `ui.order` remain meaningful. Other JSON-compatible `ui.*` annotations are pass-through metadata and are not required to belong to a closed vocabulary.

Do not add JSON Editor-specific behavior, widget inference, strict widget vocabularies, or editor-specific keyword translation to the core JSON Schema projection.

## Description Semantics

JSON Schema `description` continues to use canonical `UserDescription`.

Canonical `TechnicalDescription` is independent and, when semantic annotations are enabled, is preserved as:

```json
{
  "x-stm": {
    "technicalDescription": "..."
  }
}
```

The JSON Schema projection must not substitute technical description for missing user description.

## Extension Boundary

Do not duplicate native JSON Schema semantics in `x-stm`, including:

```text
requiredness
nullability
string/numeric/collection constraints
format
enum values
oneOf/anyOf/allOf
title
user description
additionalProperties
```

Do not put source/compiler or projection-specific implementation details in `x-stm`, including:

```text
CLR setter/init shape
Roslyn identities
generator/manifest versions
EF mappings
database names
Power BI target metadata
```

## Compatibility and Versioning

The exporter must allow callers to disable STM semantic annotations and obtain plain JSON Schema.

Do not add an `x-stm` protocol version, compatibility negotiation, reader ranges, or adapters in M0062.

Unknown future `x-stm` members should be treated as extension data by tolerant consumers.

If the extension later becomes a separately versioned interoperability contract, that requires an explicit future decision.
