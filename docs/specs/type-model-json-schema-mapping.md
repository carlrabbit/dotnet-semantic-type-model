# Type Model JSON Schema Mapping (Draft 2020-12)

## Purpose

Define baseline mapping between the hardened canonical type model and JSON Schema Draft 2020-12.

## Authority

This spec is authoritative for canonical-to-JSON-Schema and JSON-Schema-to-canonical mapping intent for the hardened model.

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

Runtime import must support configurable behavior:

- preserve as annotation + informational diagnostic;
- ignore + warning diagnostic;
- reject + error diagnostic.

When preserved, unsupported keyword annotations use reserved namespaces such as `jsonSchema.*` and `ui.*`.

## Composition and Reference Behavior

- `$defs`/`$ref` must preserve stable identifier-based references.
- Recursive references are represented through `TypeRef(TypeId)`.
- `allOf` object composition may be represented as composition metadata even when full intersection reduction is deferred.

For the M0004 runtime baseline:

- `oneOf` and `anyOf` map to canonical union shape semantics.
- `allOf` intersection reduction is deferred and is preserved as annotation with diagnostics unless reduced by a future transform.
