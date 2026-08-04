# EF Model Shape Test Matrix and Member Placement

## Status

Authoritative for M0057 and `2.5.3`.

## Purpose

Define permanent surgical test coverage for the opinionated EF relational projection.

A large real-life fixture is required but insufficient. The EF package must also pass small, isolated model-shape fixtures that each exercise one failure surface.

## Core Rule

Property declaration and property storage are distinct.

For every projected property, derive:

```text
CLR declaring type
semantic declaring type
CLR storage entity type
semantic storage entity type
column/property name
storage table
```

EF application must configure the property on the storage entity builder, not on whichever CLR type exposes the property through inheritance.

## Placement Matrix

| Shape | Storage |
|---|---|
| Concrete entity property | Concrete entity table |
| Semantic base entity property | Semantic base table |
| Derived entity property | Derived TPT table |
| Non-semantic base property | First semantic storage entity |
| Non-semantic grandbase property | First semantic storage entity |
| ValueKind property inherited from non-semantic base | JSON column on semantic storage entity |
| ExtensionData inherited from non-semantic base | JSON column on semantic storage entity |
| Hidden duplicate member | Diagnostic unless unambiguous |

## Required Test Groups

```text
flat entity
non-semantic base scalar
non-semantic base ExtensionData
non-semantic base owned ValueKind object
non-semantic base owned ValueKind collection
semantic TPT inheritance
TPT with non-semantic grandbase
ValueKind reused by multiple entities
ValueKind with inherited scalar
ValueKind with nested ValueKind
polluted ModelBuilder
hidden property
semantic base plus derived duplicate property
non-semantic base plus semantic base chain
base-owned JSON plus derived-owned JSON
```

## Mandatory Assertions

Every ModelBuilder test must assert:

```text
final EF CLR entity set equals semantic Entity CLR set
```

Every inheritance test must assert:

```text
property appears on expected EF entity type
property does not appear as a duplicate local property on derived entity types
```

Every ValueKind test must assert:

```text
ValueKind is not an EF entity
ValueKind is not keyless
ValueKind has no table
owner property is mapped as JSON-converted property
```

## Diagnostics

Required diagnostics:

```text
EF_MEMBER_DECLARATION_AMBIGUOUS
EF_MEMBER_STORAGE_ENTITY_UNRESOLVED
EF_MEMBER_DECLARING_TYPE_MISMATCH
```

Known inherited-member placement failures must produce diagnostics or correct mappings, not raw EF exceptions.
