# Typed Literals and Conditional Constraint Semantics

## Status

Authoritative for M0058 and `2.6.0`.

## Purpose

Define typed literal handling for semantic constraints and prevent conditional metadata from degrading into untyped strings.

## Core Rule

A literal has meaning only in relation to its source type.

```text
"CsvFile" as string != ImportType.CsvFile as enum member
"true" as string != true as boolean
"42" as string != 42 as integer
```

## Typed Literal Shape

Minimum required fields:

```text
Kind
RawText
NormalizedText
TypeId
ClrTypeName
Value
EnumTypeId
EnumMemberName
IsNull
Diagnostics
```

## Required Kinds

```text
String
Boolean
Integer
Decimal
EnumMember
Guid
Date
Time
DateTime
DateTimeOffset
Duration
Null
Unsupported
```

## Conditional Constraint Shape

Minimum required fields:

```text
TargetPropertyId
SourcePropertyName
SourcePropertyId
SourceTypeId
Operator
Literal
Message
```

Initial operators:

```text
Equals
NotEquals
IsNull
IsNotNull
```

## Normalization

`SemanticRequiredWhen(sourceProperty, value)` must be normalized by resolving `sourceProperty`.

Rules:

```text
enum source -> enum-member literal
bool source -> boolean literal
numeric source -> numeric literal
string source -> string literal
nullable source -> null or underlying-type literal
unsupported source -> diagnostic
```

Date, time, date-time, date-time-offset, duration, and GUID CLR sources use invariant
normalization. Unsupported CLR wrapper sources produce the existing unsupported-source diagnostic;
they are not compared as strings.

The STM5034-STM5036 identifiers are the .NET extraction/import diagnostic equivalents for
malformed conditional metadata. Canonical-model validation continues to use the established
STM1020-STM1023 family, so projection and configuration consumers retain their existing core
diagnostic contract.

## Diagnostics

Required stable diagnostics:

```text
STM_TYPED_LITERAL_SOURCE_NOT_FOUND
STM_TYPED_LITERAL_SOURCE_TYPE_UNSUPPORTED
STM_TYPED_LITERAL_VALUE_INVALID
STM_TYPED_LITERAL_ENUM_MEMBER_NOT_FOUND
STM_TYPED_LITERAL_NUMERIC_FORMAT_INVALID
STM_TYPED_LITERAL_NUMERIC_OVERFLOW
STM_TYPED_LITERAL_BOOLEAN_INVALID
STM_TYPED_LITERAL_NULL_NOT_ALLOWED
STM_CONDITIONAL_CONSTRAINT_TARGET_INVALID
STM_CONDITIONAL_CONSTRAINT_SOURCE_INVALID
STM_CONDITIONAL_CONSTRAINT_LITERAL_TYPE_MISMATCH
```

## Projection Contract

EF Core:

```text
enum properties remain scalar string columns
RequiredWhen does not change EF entity discovery
owned ValueKind properties remain JSON columns
```

JSON Schema:

```text
conditional required constraints are emitted when supported
unsupported conditional constraints emit diagnostics
constraints are not silently dropped
```

Core validation:

```text
source exists
target exists
literal matches source type
operator supports literal type
```
