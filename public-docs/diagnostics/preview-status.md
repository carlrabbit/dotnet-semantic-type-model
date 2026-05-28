# Diagnostics Preview Status

## Stability

Stable STM-prefixed diagnostic IDs (STM0xxx, STM3xxx, STM5xxx) are documented in [../diagnostics.md](../diagnostics.md) and their reference pages. They should not change within a pre-release milestone set unless documented in release notes.

Projection codes using descriptive prefixes (`EFCORE_*`, `POWERBI_*`, `JSONSCHEMA_*`) are preview/unstable. Their IDs and messages may change across milestones.

## Usage Guidance

When consuming diagnostics:

- treat preview IDs as subject to change across pre-release milestones;
- prefer severity and category handling over exact message text for preview codes;
- validate behavior against current release notes.

## EF Core Projection Diagnostics (preview)

`SemanticTypeModel.EFCore` emits projection diagnostics through `EfCoreModelBuilderProjectionResult.Diagnostics` and `EfModelDefinition.Diagnostics`.

| Code | Description |
|------|-------------|
| `EFCORE_ENTITY_PRIMARY_KEY_MISSING` | Entity has no primary key |
| `EFCORE_DUPLICATE_PROJECTED_NAME` | Two types resolve to the same projected name |
| `EFCORE_NAME_COLLISION_SUFFIX_APPLIED` | A suffix was applied to resolve a name collision |
| `EFCORE_OBJECT_NOT_PROJECTED` | Object type was not projected to an entity |
| `EFCORE_PROPERTY_TYPE_NOT_FOUND` | Property type could not be resolved during projection |
| `EFCORE_KEY_PROPERTY_NOT_PROJECTED` | A key property was not projected |
| `EFCORE_RELATIONSHIP_ENDPOINT_TYPE_NOT_PROJECTED` | A relationship endpoint type was not projected |
| `EFCORE_RELATIONSHIP_ENDPOINT_PROPERTY_MISSING` | A relationship endpoint property is missing |
| `EFCORE_RELATIONSHIP_ENDPOINT_PROPERTY_NOT_PROJECTED` | A relationship endpoint property was not projected |
| `EFCORE_MANY_TO_MANY_UNSUPPORTED` | Many-to-many relationship is not supported |
| `EFCORE_VALUE_OBJECT_REQUIRES_MODE` | Value object requires a flattening or JSON mode |
| `EFCORE_VALUE_OBJECT_FLATTEN_EMPTY` | Value object flatten produced no columns |
| `EFCORE_VALUE_OBJECT_FLATTEN_UNSUPPORTED` | Value object cannot be flattened |
| `EFCORE_VALUE_OBJECT_SERIALIZED_JSON` | Value object is stored as a JSON column |
| `EFCORE_ENUM_STORAGE_INVALID` | Enum storage configuration is invalid |
| `EFCORE_INVALID_ANNOTATION_VALUE` | An annotation value was invalid |
| `EFCORE_NUMERIC_CONSTRAINTS_PRESERVED_AS_ANNOTATION` | Numeric constraints are preserved as annotations, not enforced |
| `EFCORE_STRING_PATTERN_PRESERVED_AS_ANNOTATION` | String pattern constraint is preserved as annotation |
| `EFCORE_OPTIONALITY_PRESERVED_AS_ANNOTATION` | Optionality is preserved as annotation |
| `EFCORE_{SHAPE}_UNSUPPORTED` | Shape kind `{SHAPE}` is not supported by EF Core projection |
| `EFCORE_{SHAPE}_IGNORED` | Shape kind `{SHAPE}` was ignored |
| `EFCORE_{SHAPE}_SERIALIZED_JSON` | Shape kind `{SHAPE}` is stored as JSON |

## Power BI Projection Diagnostics (preview)

`SemanticTypeModel.PowerBI` emits projection diagnostics through its result type.

| Code | Description |
|------|-------------|
| `POWERBI_DUPLICATE_PROJECTED_NAME` | Two types resolve to the same projected name |
| `POWERBI_NAME_COLLISION_SUFFIX_APPLIED` | A suffix was applied to resolve a name collision |
| `POWERBI_OBJECT_NOT_PROJECTED` | Object type was not projected to a table |
| `POWERBI_PROPERTY_TYPE_NOT_FOUND` | Property type could not be resolved during projection |
| `POWERBI_RELATIONSHIP_MISSING_KEY` | Relationship is missing a required key |
| `POWERBI_RELATIONSHIP_ENDPOINT_TABLE_NOT_PROJECTED` | A relationship endpoint table was not projected |
| `POWERBI_RELATIONSHIP_ENDPOINT_COLUMN_NOT_PROJECTED` | A relationship endpoint column was not projected |
| `POWERBI_RELATIONSHIP_ENDPOINT_PROPERTY_MISSING` | A relationship endpoint property is missing |
| `POWERBI_MANY_TO_MANY_RELATIONSHIP_UNSUPPORTED` | Many-to-many relationships are not supported |
| `POWERBI_VALUE_OBJECT_UNSUPPORTED` | Value object type is not supported |
| `POWERBI_VALUE_OBJECT_FLATTEN_EMPTY` | Value object flatten produced no columns |
| `POWERBI_VALUE_OBJECT_FLATTEN_RECURSIVE_UNSUPPORTED` | Recursive value object flattening is not supported |
| `POWERBI_VALUE_OBJECT_FLATTEN_CYCLE_UNSUPPORTED` | Cyclic value object flattening is not supported |
| `POWERBI_VALUE_OBJECT_SERIALIZED_JSON` | Value object is stored as JSON |
| `POWERBI_UNSUPPORTED_SHAPE` | Shape kind is not supported |
| `POWERBI_UNSUPPORTED_SHAPE_IGNORED` | Shape kind was ignored |
| `POWERBI_UNSUPPORTED_SHAPE_SERIALIZED_JSON` | Shape kind is stored as JSON |
| `POWERBI_UNSUPPORTED_MEASURE_EXPRESSION_LANGUAGE` | Measure expression language is not supported |
| `POWERBI_INVALID_ANNOTATION_VALUE` | An annotation value was invalid |

## JSON Schema Import/Export Diagnostics (preview)

`SemanticTypeModel.JsonSchema` emits diagnostics for import and export operations.

| Code | Description |
|------|-------------|
| `JSONSCHEMA_IMPORT_BUILD_FAILED` | JSON Schema import failed to build the model |
| `JSONSCHEMA_INVALID_OR_AMBIGUOUS_TYPE` | Type is invalid or ambiguous |
| `JSONSCHEMA_INVALID_REQUIRED` | Required keyword is invalid |
| `JSONSCHEMA_UNRESOLVED_LOCAL_REF` | A local $ref could not be resolved |
| `JSONSCHEMA_REMOTE_REF_UNSUPPORTED` | Remote $ref is not supported |
| `JSONSCHEMA_UNSUPPORTED_ALLOF` | allOf composition is not supported in this context |
| `JSONSCHEMA_UNSUPPORTED_DIALECT` | JSON Schema dialect is not supported |
| `JSONSCHEMA_UNSUPPORTED_KEYWORD_IGNORED` | Unsupported keyword was ignored |
| `JSONSCHEMA_UNSUPPORTED_KEYWORD_PRESERVED` | Unsupported keyword was preserved as annotation |
| `JSONSCHEMA_UNSUPPORTED_KEYWORD_REJECTED` | Unsupported keyword caused rejection |
| `JSONSCHEMA_UNSUPPORTED_PREFIX_ITEMS` | prefixItems is not supported |
| `JSONSCHEMA_TYPE_ARRAY_MAPPED_TO_UNION` | JSON type array was mapped to a union type |
| `JSONSCHEMA_ADDITIONAL_PROPERTIES_SCHEMA_PRESERVED` | additionalProperties schema was preserved as annotation |
| `JSONSCHEMA_CONST_MODELED_AS_CONSTRAINT` | const keyword was modeled as a constraint |
| `JSONSCHEMA_EXPORT_ALLOF_PRESERVED` | allOf was preserved during export |
| `JSONSCHEMA_EXPORT_ANNOTATION_SKIPPED` | Annotation was skipped during export |
| `JSONSCHEMA_EXPORT_REMOTE_REF_REEMITTED` | Remote $ref was re-emitted |
| `JSONSCHEMA_EXPORT_UNSUPPORTED_DIALECT` | Export dialect is not supported |
| `JSONSCHEMA_EXPORT_UNSUPPORTED_UNION_SEMANTICS` | Union semantics are not supported for export |
| `JSONSCHEMA_UI_DOWNSTREAM_MODE_REQUIRED` | Downstream UI mode is required |
| `JSONSCHEMA_UI_ENUM_LABEL_MISMATCH` | Enum label count does not match enum values |
| `JSONSCHEMA_UI_HINT_IMPORTED` | UI hint was imported from schema |
| `JSONSCHEMA_UI_HINT_NOT_REPRESENTABLE` | UI hint cannot be represented in the output |
| `JSONSCHEMA_UI_INVALID_BOOLEAN` | Invalid boolean UI annotation |
| `JSONSCHEMA_UI_INVALID_ENUM_LABELS` | Invalid enum labels annotation |
| `JSONSCHEMA_UI_INVALID_KEY` | Invalid UI annotation key |
| `JSONSCHEMA_UI_INVALID_ORDER` | Invalid order annotation |
| `JSONSCHEMA_UI_INVALID_PROPERTY_ORDER` | Invalid property order annotation |
| `JSONSCHEMA_UI_INVALID_WIDGET` | Invalid widget annotation |
| `JSONSCHEMA_UI_PROPERTY_ORDER_CONFLICT` | Property order annotation conflicts with another |
| `JSONSCHEMA_UI_UNSUPPORTED_JSONEDITOR_KEY` | JSON Editor key is not supported |
| `JSONSCHEMA_UI_UNSUPPORTED_WIDGET` | Widget type is not supported |

## Related Docs

- [../diagnostics.md](../diagnostics.md)
- [../api/compatibility.md](../api/compatibility.md)
- [../release-notes.md](../release-notes.md)

