# Diagnostics

Diagnostics for this repository are currently documented as preview/unstable unless explicitly marked otherwise.

- Detailed diagnostics status: [diagnostics/preview-status.md](diagnostics/preview-status.md)
- API compatibility expectations: [api/compatibility.md](api/compatibility.md)

## EF Core projection diagnostics (preview)

`SemanticTypeModel.EFCore` emits projection diagnostics through `EfCoreModelBuilderProjectionResult.Diagnostics` and `EfModelDefinition.Diagnostics`.

Common preview codes include:

- `EFCORE_ENTITY_PRIMARY_KEY_MISSING`
- `EFCORE_DUPLICATE_PROJECTED_NAME`
- `EFCORE_ARRAY_UNSUPPORTED`
- `EFCORE_DICTIONARY_UNSUPPORTED`
- `EFCORE_UNION_UNSUPPORTED`
- `EFCORE_RELATIONSHIP_ENDPOINT_TYPE_NOT_PROJECTED`
