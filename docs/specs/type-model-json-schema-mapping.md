# Type Model JSON Schema Mapping (Draft 2020-12)

## Purpose

Define baseline projection mapping from the hardened canonical type model to JSON Schema Draft 2020-12.

## Authority

This spec is authoritative for canonical-to-JSON-Schema projection intent for the hardened model.

This spec is not authority for JSON-Schema-to-canonical authoring. JSON Schema import is unsupported as canonical model creation under [code-first-semantic-model-architecture.md](code-first-semantic-model-architecture.md). Any retained JSON-Schema-to-canonical mapping is legacy/internal compatibility behavior only.

## Domain Semantic Model Contract

JSON Schema behavior is derived through a JSON Schema domain semantic model before a Draft 2020-12 document is exported.

The domain semantic model must carry projected keyword intent, `$defs`/reference structure, unsupported-shape diagnostics, and any explicitly configured extension keyword behavior.

## Baseline Keyword Mapping

| JSON Schema | Canonical model |
|---|---|
| `$id` | `TypeSchemaModel.Id` and/or `schema.*` annotation |
| `$defs` | named `TypeDefinition` entries keyed by `TypeId` |
| `$ref` | `TypeRef` |
| `type` | `TypeKind` + scalar/object/array/dictionary contracts |
| `properties` | `ObjectTypeDefinition.Properties` |
| `required` | `PropertyDefinition.Cardinality.IsRequired` |
| `additionalProperties` | `ObjectConstraints.AdditionalProperties` and/or `DictionaryTypeDefinition` |
| `patternProperties` | dictionary/pattern policy via typed constraints + `jsonSchema.*` annotation |
| `items` | `ArrayTypeDefinition.ItemType` |
| `minItems`/`maxItems` | array/cardinality constraints |
| `uniqueItems` | `ArrayTypeDefinition.UniqueItems` / `ArrayConstraints.UniqueItems` |
| string constraints | `StringConstraints` |
| numeric constraints | `NumericConstraints` |
| `enum` | `EnumTypeDefinition` |
| `const` | canonical constraint entry (`const`) and re-export as JSON literal |
| `oneOf` | `UnionTypeDefinition` with `UnionSemantics.OneOf` |
| `anyOf` | `UnionTypeDefinition` with `UnionSemantics.AnyOf` |
| `allOf` | `IntersectionTypeDefinition` and/or `ObjectComposition.AllOf` |
| `format` | `ScalarTypeDefinition.Format` or `jsonSchema.format` annotation |
| `title` | `DisplayName` and/or `ui.*` annotation |
| `description` | `Description` |
| `default` | annotation (`schema.default` / `jsonSchema.default`) |

## Nullability and Requiredness

- Absent property is represented by `Cardinality.IsRequired = false`.
- Present `null` is represented by `Cardinality.AllowsNull = true` and/or nullable type semantics.
- `type: ["string","null"]` maps to nullable scalar or a union including null semantics.
- Required nullable property is represented by `IsRequired = true` and `AllowsNull = true`.
- Cardinality (`MinItems`/`MaxItems`) remains separate from requiredness/nullability.

## Unsupported Keywords

Unsupported or not-yet-modeled keywords must follow one of these strategies:

- preserved as namespaced annotations;
- diagnosed as unsupported (warning/error);
- diagnosed as ignored;
- explicitly deferred by this specification.

Legacy/internal import, when retained, may support configurable behavior:

- preserve as annotation + informational diagnostic;
- ignore + warning diagnostic;
- reject + error diagnostic.

Public model authoring must not rely on this path.

When preserved, unsupported keyword annotations use reserved namespaces such as `jsonSchema.*` and `ui.*`.

## UI and JSON-Editor Hint Mapping (M0006)

- `ui.*` annotation keys may be emitted as JSON Schema extension keywords using `ui:*` names.
- known JSON-editor keywords (`propertyOrder`, `options`, `watch`, `template`) map to `jsonEditor.*` annotations only in retained legacy/internal import paths.
- downstream-specific keywords are emitted only when explicit JSON-editor compatibility mode is enabled.
- export defaults to standard JSON Schema semantics without downstream keyword emission.
- display text export precedence is `ui.title` -> `schema.title`/`title` and `ui.description` -> `schema.description`/`description`.
- UI hints are non-semantic projection hints and must not alter canonical requiredness/nullability semantics.

## Composition and Reference Behavior

- `$defs`/`$ref` must preserve stable identifier-based references.
- Recursive references are represented through `TypeRef(TypeId)`.
- `allOf` object composition may be represented as composition metadata even when full intersection reduction is deferred.

For the M0004 runtime baseline:

- `oneOf` and `anyOf` map to canonical union shape semantics.
- `allOf` intersection reduction is deferred and is preserved as annotation with diagnostics unless reduced by a future transform.
