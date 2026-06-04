# JSON Schema Adapter Specification

## Status

Legacy baseline superseded for public canonical model creation by [code-first-semantic-model-architecture.md](code-first-semantic-model-architecture.md) and [../decisions/code-first-only-model-source.md](../decisions/code-first-only-model-source.md).

## Purpose

Define the JSON Schema Draft 2020-12 export baseline and document the legacy import behavior that existed before the code-first-only architecture.

## Authority

This specification is authoritative for:
- supported JSON Schema export features;
- legacy/internal import normalization behavior only when retained for compatibility or tests;
- adapter failure expectations within retained legacy behavior;
- JSON Schema projection behavior.

This specification is not authority for canonical model authoring sources. JSON Schema import is unsupported as canonical model creation.

## Supported Output Features

The baseline exporter must support:
- object schemas;
- scalar schemas;
- arrays;
- enums;
- required properties;
- nullable semantics;
- `$defs`;
- `$ref`;
- `oneOf` baseline support;
- annotations;
- common validation constraints stored in the canonical model.

## Legacy/Internal Import Rules

JSON Schema import is not a supported public source for canonical semantic models.

If import components remain for compatibility, tests, or internal migration, they are legacy/internal behavior and must not be presented as an authoring path.

Legacy/import-compatible behavior is:

- `$defs` entries are imported as named shapes.
- `$ref` values that target `#/$defs/{name}` resolve to the corresponding named shape identifier.
- `$ref` value `#` resolves to the root shape identifier.
- Object properties become `PropertyShape` entries.
- Object `required` members set `PropertyShape.IsRequired`.
- Object `additionalProperties: false` sets `ObjectShape.AdditionalPropertiesAllowed` to `false`.
- An object with schema-valued `additionalProperties` and no declared properties is imported as `DictionaryShape`.
- `oneOf` with a null branch and one non-null branch is normalized as nullable semantics for the non-null branch when the canonical shape can express that meaning.
- Other `oneOf` forms are imported as `UnionShape`.
- `title`, `description`, `default`, and `examples` are imported as annotations.
- Supported validation keywords are imported into `ConstraintSet` as named entries.

## Export Rules

- Export emits JSON Schema Draft 2020-12.
- The exported document includes `$schema` with the draft 2020-12 meta-schema URI.
- The root canonical shape is emitted at the document root.
- Non-root named shapes are emitted under `$defs`.
- Named `ShapeRef` values export as `$ref`.
- Nullable scalar and property semantics export using `oneOf` with a `null` branch.
- `ObjectShape` properties export under `properties`.
- Required object properties export under `required`.
- `DictionaryShape` exports as an object schema with `additionalProperties`.
- `UnionShape` exports as `oneOf` unless it represents a single `$ref` wrapper.
- Canonical annotations and supported constraints export back to their JSON Schema keywords.

## Failure Semantics

- Legacy/internal import may throw JSON parsing exceptions for invalid JSON.
- Legacy/internal import may throw `InvalidOperationException` when canonical model construction detects unresolved references.
- Export requires a non-null code-generated or snapshot-loaded model instance.

## Related Documents

- [type-schema-model.md](type-schema-model.md)
- [code-first-semantic-model-architecture.md](code-first-semantic-model-architecture.md)
- ../decisions/code-first-only-model-source.md
- ../decisions/json-schema-as-primary-dialect.md (superseded for model source authority)
