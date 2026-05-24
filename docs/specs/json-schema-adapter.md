# JSON Schema Adapter Specification

## Purpose

Define the supported JSON Schema Draft 2020-12 behavior for import into and export from the canonical semantic type model.

## Authority

This specification is authoritative for:
- supported JSON Schema features;
- import normalization behavior;
- export projection behavior;
- adapter failure expectations within the baseline feature set.

## Supported Input and Output Features

The baseline adapter must support:
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

## Import Rules

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

- Import may throw JSON parsing exceptions for invalid JSON.
- Import may throw `InvalidOperationException` when canonical model construction detects unresolved references.
- Export requires a non-null model instance.

## Related Documents

- [type-schema-model.md](type-schema-model.md)
- ../decisions/json-schema-as-primary-dialect.md
