# M0004 - JSON Schema Runtime Import and Export Baseline

## Purpose

Deliver the first runtime end-to-end JSON Schema Draft 2020-12 import/export slice over the canonical `TypeSchemaModel`.

## Delivered Runtime Surface

- `JsonSchemaImporter.Import(...)` runtime entry points for:
  - `string`
  - `Stream`
  - `JsonDocument`
  - `JsonElement`
- `JsonSchemaExporter.Export(...)` runtime export entry point.
- Result models:
  - `JsonSchemaImportResult` (`Model`, `Diagnostics`)
  - `JsonSchemaExportResult` (`Document`, `Diagnostics`)
- Options models:
  - `JsonSchemaImportOptions`
  - `JsonSchemaExportOptions`
  - `UnsupportedKeywordBehavior`
- Default and only dialect:
  - JSON Schema Draft 2020-12 (`https://json-schema.org/draft/2020-12/schema`)

## Baseline Support

- Document and references: `$schema`, `$id`, `$defs`, `$ref`.
- Scalar/object/array baseline type mapping including nullable unions from `type` arrays and `oneOf`.
- Object keywords: `properties`, `required`, `additionalProperties`, `minProperties`, `maxProperties`.
- Array keywords: `items`, `minItems`, `maxItems`, `uniqueItems`.
- String and numeric constraints: `minLength`, `maxLength`, `pattern`, `format`, `minimum`, `maximum`, `exclusiveMinimum`, `exclusiveMaximum`, `multipleOf`.
- Enum and const:
  - `enum` mapped to `EnumShape`.
  - `const` modeled as a canonical constraint entry and re-exported.
- Composition:
  - `oneOf` and `anyOf` mapped to `UnionShape` semantics (`jsonSchema.unionSemantics` annotation).
  - `allOf` currently preserved as annotation with diagnostics (deferred intersection reduction).

## Unsupported and Deferred Handling

- Unsupported keywords follow `UnsupportedKeywordBehavior`:
  - preserve as namespaced annotation + informational diagnostic;
  - ignore + warning diagnostic;
  - reject + error diagnostic.
- `prefixItems`, unresolved local `$ref`, remote `$ref`, unsupported composition reduction, and invalid/ambiguous type shapes emit diagnostics.

## Roundtrip Expectations

- Supported baseline constructs roundtrip semantically through:
  - JSON Schema -> `TypeSchemaModel` -> JSON Schema.
- Unsupported/deferred constructs are preserved where configured and accompanied by diagnostics.
- Byte-for-byte JSON equality is not required.

## Test Fixtures Added

- Fixture 1: simple object (requiredness vs nullability).
- Fixture 2: form/editor-friendly metadata (`ui.*`, `format`).
- Fixture 3: `$defs` + `$ref` reuse/recursion.
- Fixture 4: composition (`oneOf`, `anyOf`, deferred `allOf`).
- Fixture 5: constraints (string, numeric, array, object).
- Fixture 6: unsupported keyword behavior (preserve/ignore/reject).

## Deferred Follow-up

- Full intersection semantics for `allOf`.
- Remote reference loading.
- Projection-specific adapters (JSON editor runtime adapter, EF Core, TOM/Power BI, OpenAPI).

## How This Enables Later Milestones

This runtime normalization baseline provides stable import/export behavior and diagnostics that downstream projection milestones can consume without coupling core abstractions to projection-specific dependencies.
