# Type Model JSON Schema Mapping (Draft 2020-12)

## Status

Authoritative mapping companion for code-first JSON Schema export.

## Purpose

Define baseline mapping between the code-generated canonical semantic model, the JSON Schema domain semantic model, and JSON Schema Draft 2020-12 export.

## Authority

This specification is authoritative for canonical-to-JSON-Schema projection intent for the hardened model.

The detailed domain model, derivation, export, and composition behavior is defined by [json-schema-domain-model-and-export.md](json-schema-domain-model-and-export.md).

This spec is not authority for JSON-Schema-to-canonical authoring. JSON Schema import is unsupported as canonical model creation under [code-first-semantic-model-architecture.md](code-first-semantic-model-architecture.md). Any retained JSON-Schema-to-canonical behavior is legacy/internal compatibility behavior only.

## Mapping Pipeline

```text
Code-generated canonical Semantic Type Model
  -> JsonSchemaSemanticModel
  -> JSON Schema Draft 2020-12 document
```

## Baseline Keyword Mapping

| JSON Schema | Canonical/domain source |
|---|---|
| `$id` | model id and/or configured JSON Schema document metadata |
| `$schema` | export option; defaults to Draft 2020-12 for full document export |
| `$defs` | named domain schema definitions derived from canonical type identifiers |
| `$ref` | domain schema references derived from stable canonical references |
| `type` | domain schema kind derived from canonical type kind/scalar kind |
| `properties` | object domain schema properties |
| `required` | property requiredness |
| `additionalProperties` | dictionary/object additional-properties metadata where modeled |
| `items` | array item schema reference or inline schema |
| `minItems`/`maxItems` | array/cardinality constraints when modeled |
| `uniqueItems` | array uniqueness constraint when modeled |
| string constraints | string constraint metadata |
| numeric constraints | numeric constraint metadata |
| `enum` | enum domain schema values |
| `const` | const constraint when modeled |
| `oneOf` | simple exclusive alternatives over named branch schemas |
| `anyOf` | simple non-exclusive alternatives over named branch schemas |
| `allOf` | deferred except for explicitly modeled future support |
| `format` | scalar format or configured JSON Schema format annotation |
| `title` | display name/title metadata |
| `description` | description metadata |
| `default` | annotation/constraint only when export option allows it |
| extension keywords | namespaced annotations only when configured for export |

## Nullability and Requiredness

Requiredness and nullability are separate.

Rules:

- absent property is represented by optional property semantics;
- present `null` is represented by nullable semantics;
- required nullable property is both required and nullable;
- nullability export strategy is configured by JSON Schema export options;
- supported strategies are type-array nullability and `oneOf` with a `null` branch when feasible.

## Simple Composition Mapping

M0029 supports simple composition export.

### `oneOf`

`oneOf` maps from exclusive code-derived alternatives.

Rules:

- alternatives should be named domain schema definitions;
- branches should be emitted as `$ref` to `$defs` when possible;
- branch order is deterministic;
- unsupported inline or nested alternatives emit diagnostics.

### `anyOf`

`anyOf` maps from non-exclusive code-derived alternatives.

Rules:

- alternatives should be named domain schema definitions;
- branches should be emitted as `$ref` to `$defs` when possible;
- branch order is deterministic;
- unsupported inline or nested alternatives emit diagnostics.

### Unsupported Composition

Unsupported unless a later milestone adds support:

```text
not
if/then/else
dependentSchemas
unevaluatedProperties
dynamicRef/dynamicAnchor
full allOf reduction
full discriminator semantics
arbitrary nested boolean-schema composition
```

## Unsupported Keywords

Unsupported or not-yet-modeled keywords must follow one of these strategies:

- diagnosed as unsupported;
- ignored with warning diagnostics;
- preserved as namespaced annotation only when an explicit export option supports it;
- deferred by this specification.

When preserved, unsupported keyword annotations use reserved namespaces such as `jsonSchema.*` and `ui.*`.

## UI and JSON-Editor Hint Mapping

- `ui.*` annotation keys may be emitted as JSON Schema extension keywords using `ui:*` names.
- downstream-specific keywords are emitted only when explicit JSON-editor compatibility mode is enabled.
- export defaults to standard JSON Schema semantics without downstream keyword emission.
- display text export precedence is `ui.title` -> `schema.title`/`title` and `ui.description` -> `schema.description`/`description`.
- UI hints are non-semantic projection hints and must not alter canonical requiredness/nullability semantics.

## Legacy/Internal Import Boundary

JSON Schema import is not a supported public source for canonical semantic models.

If import components remain for compatibility, tests, or internal migration, they are legacy/internal behavior and must not be presented as an authoring path.
