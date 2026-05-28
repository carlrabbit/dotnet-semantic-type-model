# Diagnostics

Diagnostics describe conditions detected during model extraction, validation, and projection. This page is the public reference for all stable STM-prefixed diagnostic codes.

Preview projection codes (EF Core, Power BI, JSON Schema import/export) are documented separately in [diagnostics/preview-status.md](diagnostics/preview-status.md).

API compatibility expectations: [api/compatibility.md](api/compatibility.md)

## Diagnostic ID Scheme

All stable diagnostic IDs use the prefix `STM` followed by a four-digit number:

| Range       | Description                              | Package                          |
|-------------|------------------------------------------|----------------------------------|
| STM0xxx     | Semantic model validation                | SemanticTypeModel.Core           |
| STM3xxx     | JSON Schema runtime projection           | SemanticTypeModel.JsonSchema     |
| STM5xxx     | .NET type extraction / source generator  | SemanticTypeModel.DotNet / SemanticTypeModel.Generators |

## Severity Levels

| Severity  | Meaning                                                                 |
|-----------|-------------------------------------------------------------------------|
| Error     | Output cannot be produced; model or compilation is structurally invalid |
| Warning   | Output is produced with degraded semantics or a fallback was applied    |
| Info      | Informational; does not affect output                                   |

## Suppression

Runtime diagnostics (`SchemaDiagnostic`) are returned in result collections; they do not throw. Filter by `Code` or `Severity` to suppress specific conditions.

Compile-time diagnostics (source generator) can be suppressed with standard `#pragma warning disable STMXXXX` or via `<NoWarn>` in your project file.

## Quick Reference

### Model Validation (STM0xxx)

| Code    | Severity | Title                          |
|---------|----------|--------------------------------|
| STM0001 | Error    | Duplicate type ID              |
| STM0002 | Error    | Unresolved type reference      |
| STM0003 | Error    | Duplicate property name        |
| STM0004 | Error    | Duplicate key name             |
| STM0005 | Error    | Key property reference missing |
| STM0006 | Error    | Relationship type missing      |
| STM0007 | Error    | Relationship property reference missing |
| STM0008 | Error    | Invalid cardinality bounds     |
| STM0009 | Error    | Invalid string constraint bounds |
| STM0010 | Error    | Invalid numeric constraint bounds |
| STM0011 | Warning  | Malformed annotation key       |
| STM0012 | Error    | Duplicate enum value name      |
| STM0013 | Error    | Duplicate enum value payload   |

Full reference: [diagnostics/stm0xxx.md](diagnostics/stm0xxx.md)

### JSON Schema Runtime Projection (STM3xxx)

| Code    | Severity | Title                                      |
|---------|----------|--------------------------------------------|
| STM3201 | Warning  | Root ID fallback applied                   |
| STM3202 | Warning  | Semantic members skipped                   |
| STM3203 | Warning  | Dictionary key metadata lost               |
| STM3204 | Warning  | Union type approximated as oneOf           |
| STM3205 | Warning  | Intersection type approximated as union    |
| STM3206 | Warning  | Unsupported type kind                      |
| STM3207 | Warning  | Non-string enum value serialized as string |

Full reference: [diagnostics/stm3xxx.md](diagnostics/stm3xxx.md)

### .NET Type Extraction and Source Generator (STM5xxx)

| Code    | Severity | Title                              |
|---------|----------|------------------------------------|
| STM5001 | Warning  | Type resolution failed             |
| STM5002 | Warning  | Member resolution failed           |
| STM5003 | Warning  | Generic argument resolution failed |
| STM5004 | Warning  | Type argument binding skipped      |
| STM5006 | Warning  | Annotation invalid                 |
| STM5007 | Warning  | Type included without attribute    |
| STM5008 | Warning  | Unsupported discovery mode         |
| STM5009 | Warning  | Root type not found                |
| STM5010 | Warning  | Type excluded                      |
| STM5011 | Warning  | Property type mapping failed       |
| STM5012 | Warning  | Type skipped (abstract or empty)   |
| STM5013 | Warning  | Key definition invalid             |
| STM5014 | Warning  | Relationship definition invalid    |
| STM5015 | Warning  | Relationship endpoint unresolved   |
| STM5016 | Warning  | Enum member unsupported value      |
| STM5017 | Warning  | Attribute argument invalid         |
| STM5018 | Warning  | Unsupported naming policy          |
| STM5019 | Warning  | Generated provider name collision  |
| STM5020 | Warning  | XML documentation missing          |
| STM5021 | Warning  | Constraint value invalid           |
| STM5022 | Warning  | Constraint range invalid           |
| STM5023 | Warning  | Semantic name duplicate            |
| STM5024 | Warning  | Annotation conflict                |
| STM5025 | Warning  | Member shape unsupported           |

Full reference: [diagnostics/stm5xxx.md](diagnostics/stm5xxx.md)

