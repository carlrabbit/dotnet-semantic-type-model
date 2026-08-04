# M0058: Typed Literals, Conditional Constraints, and Scalar Semantics for 2.6.0

## Status

Implemented; awaiting human review and publication approval.

## Goal

Introduce a coherent typed-literal and constraint model for code-first semantic extraction and downstream projections.

Prepare a non-publishing `2.6.0` release.

This milestone generalizes the observed enum/`SemanticRequiredWhen` defect into a broader semantic-model correction:

```text
literals must be typed
constraints must reference resolved source properties
enum values must be enum-member literals, not opaque strings
scalar types must stay scalar across extraction, validation, JSON Schema, EF Core, and documentation
```

## Repository Profile

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Profile | `dotnet-library` |
| Role | Product repository and capability provider |
| Released baseline | `2.5.3` |
| Target release | `2.6.0` |
| Milestone type | Semantic model expansion and projection hardening |
| Execution mode | `ai-executed-human-reviewed` |
| Publication | Separate human-approved follow-up |

## Problem Statement

`SemanticRequiredWhenAttribute` currently carries a source property and a comparison literal as strings.

Example:

```csharp
[SemanticRequiredWhen(nameof(ImportType), nameof(ImportType.CsvFile))]
public CsvSourceSpecification? CsvSource { get; init; }
```

At runtime, `nameof(ImportType.CsvFile)` evaluates to:

```text
CsvFile
```

The semantic model therefore receives a string literal without preserving:

```text
source property type
literal kind
enum type
enum member identity
provider representation
projection representation
validation semantics
```

That is acceptable for display but insufficient for semantic modeling.

The same defect class can affect:

```text
enum literals
boolean literals
numeric literals
string literals
date/time literals
duration literals
Guid literals
strong identifier literals
nullable literals
collection item literals
conditional constraints
range constraints
allowed-values constraints
projection-specific conversions
```

## Design Principle

A semantic condition must not be represented as an untyped string comparison unless the source property is itself a string.

Required canonical form:

```text
condition target property
source property reference
source property semantic type
operator
typed literal
diagnostics
projection hints
```

## Core Concepts

### Typed Literal

Add a normalized literal representation.

Minimum model:

```text
SemanticLiteral
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

Equivalent names are acceptable, but the model must distinguish:

```text
"CsvFile" as string
CsvFile as enum member
true as boolean
"true" as string
42 as number
"42" as string
2026-08-04 as date
"2026-08-04" as string
```

### Literal Kind

Required kinds:

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
StrongIdentifier
Unsupported
```

### Conditional Constraint

Add or normalize a model concept for conditional required semantics.

Minimum model:

```text
ConditionalConstraint
  TargetPropertyId
  SourcePropertyName
  SourcePropertyId
  SourceTypeId
  Operator
  Literal
  Message
```

Initial operator set:

```text
Equals
NotEquals
IsNull
IsNotNull
```

Only `Equals` is required for parity with `SemanticRequiredWhen`.

### Enum Literal

Enum literals must preserve:

```text
enum semantic type id
enum CLR type name
enum member name
enum member numeric value
optional display name
optional description
```

Do not model enum members as object types.

Do not let enum members become EF entities, ValueKinds, or semantic roots.

## Attribute Surface

Keep the existing attribute for compatibility:

```csharp
public sealed class SemanticRequiredWhenAttribute(string sourceProperty, string value) : Attribute
```

Normalize the string value against the resolved source property type.

This means:

```csharp
[SemanticRequiredWhen(nameof(ImportType), nameof(ImportType.CsvFile))]
```

becomes:

```text
sourceProperty = ImportType
sourcePropertyType = enum ImportType
literalKind = EnumMember
literalValue = CsvFile
```

Do not require the user to pass fully-qualified enum values.

## Extraction Requirements

The .NET extractor and source generator must resolve:

```text
source property exists
source property semantic property id
source property CLR type
source property semantic type
literal kind from source type
literal validity
```

For enum source properties:

```text
literal must match an enum member name
enum member must be preserved as typed literal
invalid enum member emits diagnostic
```

For boolean source properties:

```text
true/false are boolean literals
case-insensitive input accepted only if documented
invalid values emit diagnostic
```

For numeric source properties:

```text
numeric literal is parsed using invariant culture
overflow emits diagnostic
invalid format emits diagnostic
```

For string source properties:

```text
literal remains string
no enum/number coercion
```

For nullable source properties:

```text
null literal is supported
non-null literals resolve against underlying type
```

For unsupported source types:

```text
emit diagnostic
do not guess
```

## Diagnostics

Add diagnostics:

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

Existing diagnostic naming style may use repository conventions, but the codes must be stable and documented.

## Projection Requirements

### EF Core

EF Core must continue to treat enum properties as scalar columns.

Required:

```text
enum property -> string column
nullable enum property -> nullable string column
RequiredWhen metadata does not affect EF entity discovery
RequiredWhen metadata does not turn enum type into entity or ValueKind
owned ValueKind properties guarded by enum RequiredWhen stay JSON columns
```

EF does not need to enforce conditional required semantics in relational schema for this milestone.

It must preserve metadata where the EF model exposes semantic annotations.

### JSON Schema

JSON Schema projection must express conditional required constraints where supported.

For:

```text
CsvSource required when ImportType == CsvFile
```

the JSON Schema projection should emit a deterministic conditional schema, such as:

```text
if ImportType const CsvFile
then required CsvSource
```

Use the repository's existing JSON Schema projection style. If conditional schema generation is deferred, emit a documented unsupported diagnostic rather than dropping the constraint silently.

Enum source values must align with the enum projection representation.

### Core Validation

Core validation must verify:

```text
condition target exists
condition source exists
literal matches source type
enum member exists
literal type is compatible with operator
```

### Documentation / Public Metadata

Public docs must describe:

```text
SemanticRequiredWhen is type-normalized
nameof(Enum.Member) is valid and resolves to an enum-member literal
string literals remain strings only when source property is string
invalid literals produce diagnostics
projection support matrix
```

## Required Test Matrix

Add a dedicated typed-literal and conditional-constraint test suite.

Suggested location:

```text
tests/fixtures/SemanticTypeModel.ConstraintFixtures/
tests/unit/SemanticTypeModel.DotNet.Tests.Unit/
tests/unit/SemanticTypeModel.Core.Tests.Unit/
tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
```

### Enum RequiredWhen

Model:

```text
ImportSpecification
  ImportType enum
  CsvSource required when ImportType == CsvFile
  XmlSource required when ImportType == XmlFile
  WebService1Source required when ImportType == WebService1
  WebService2Source required when ImportType == WebService2
```

Assertions:

```text
ImportType is enum scalar
required-when literals are EnumMember
enum member ids are preserved
owned sources remain ValueKind JSON properties
no enum member becomes semantic object type
EF maps ImportType as string column
JSON Schema emits conditional required semantics or a documented diagnostic
```

### Invalid Enum Literal

```text
ImportType == DoesNotExist
```

Expected:

```text
STM_TYPED_LITERAL_ENUM_MEMBER_NOT_FOUND
```

### String Literal

```text
ModeName == "CsvFile"
```

Expected:

```text
literal kind String
no enum coercion
```

### Boolean Literal

```text
IsEnabled == true
```

Expected:

```text
literal kind Boolean
invalid values diagnostic
```

### Numeric Literal

```text
Priority == 10
```

Expected:

```text
literal kind Integer or Decimal according to source type
invalid format diagnostic
overflow diagnostic
```

### Nullable Literal

```text
ApprovalProcessId == null
```

Expected:

```text
literal kind Null
valid only for nullable source
```

### Date/Time Literals

If supported in this milestone:

```text
StartDate == 2026-08-04
```

Expected:

```text
typed date literal using invariant representation
```

If not supported:

```text
deterministic unsupported diagnostic
```

### Strong Identifier Literal

If supported:

```text
TenantId == "..."
```

Expected:

```text
StrongIdentifier literal with provider scalar representation
```

If not supported:

```text
deterministic unsupported diagnostic
```

### Missing Source Property

Expected:

```text
STM_TYPED_LITERAL_SOURCE_NOT_FOUND
```

### Source Type Unsupported

Example:

```text
SourceComplexObject == "x"
```

Expected:

```text
STM_TYPED_LITERAL_SOURCE_TYPE_UNSUPPORTED
```

## Cross-Projection Regression Case

Use the real-life import specification shape:

```text
ImportType
CsvSource
XmlSource
WebService1Source
WebService2Source
PostProcessing
```

Required assertions:

```text
ImportType remains scalar enum
CsvSource remains JSON object
XmlSource remains JSON object
WebService1Source remains JSON object
WebService2Source remains JSON object
PostProcessing remains JSON object
all RequiredWhen conditions resolve typed enum literals
EF final entity set contains only semantic entities
JSON Schema conditional output is deterministic
public docs include the example
```

## Non-Goals

```text
runtime object validation engine
database CHECK constraints for conditionals
EF relational conditional required enforcement
full expression language
arbitrary predicates
method-call conditions
cross-object path conditions
collection quantifier conditions
localization of diagnostics beyond existing repository conventions
```

## Required Authority Documents

```text
AGENTS.md
README.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/specs/core-conditional-constraint-semantics.md
docs/specs/type-model-core.md
docs/specs/opinionated-ef-relational-projection-contract.md
docs/specs/ef-model-shape-test-matrix-and-member-placement.md
public-docs/guides/core-semantics.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/release-notes.md
```

## Source and Test Areas

```text
src/SemanticTypeModel.Abstractions/Model/
src/SemanticTypeModel.Core/
src/SemanticTypeModel.DotNet/
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.JsonSchema/
src/SemanticTypeModel.EFCore/
tests/fixtures/
tests/unit/SemanticTypeModel.Core.Tests.Unit/
tests/unit/SemanticTypeModel.DotNet.Tests.Unit/
tests/unit/SemanticTypeModel.Generators.Tests.Unit/
tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/
tests/unit/SemanticTypeModel.EFCore.Tests.Unit/
tests/integration/
samples/
```

## Validation Commands

### Focused Validation

```sh
./eng/test-filter.sh TypedLiteral
./eng/test-filter.sh RequiredWhen
./eng/test-filter.sh ConditionalConstraint
./eng/test-filter.sh EnumLiteral
./eng/test-filter.sh Literal
./eng/test-filter.sh Scalar
./eng/test-filter.sh M0058

./eng/test-project.sh tests/unit/SemanticTypeModel.Core.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
```

### Repository Completion

```sh
./eng/check.sh
```

### 2.6.0 Release Preparation

```sh
./eng/package.sh 2.6.0
./eng/package-smoke.sh 2.6.0
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.6.0
```

Inspect package inventory:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
```

Do not publish packages.

## Acceptance Criteria

### Model

- Typed literal representation exists.
- Conditional constraint representation references resolved source properties.
- Enum-member literals preserve enum type and member identity.
- String literals remain strings only for string source properties.
- Boolean, numeric, null, and unsupported literals are handled deterministically.

### Extraction

- `SemanticRequiredWhen` is normalized against the source property type.
- `nameof(Enum.Member)` resolves as enum-member literal.
- Invalid enum literals emit diagnostics.
- Missing source properties emit diagnostics.
- Unsupported source types emit diagnostics.
- No typed literal causes unwanted semantic root/object extraction.

### EF Core

- Enum properties map as string columns.
- Nullable enum properties map as nullable string columns.
- RequiredWhen metadata does not alter EF entity discovery.
- Owned ValueKind properties guarded by RequiredWhen remain JSON columns.
- Final EF entity inventory remains exact.

### JSON Schema

- Enum conditional constraints project deterministically, or emit documented unsupported diagnostics.
- RequiredWhen constraints are not silently dropped.
- Enum values align with enum projection representation.

### Tests

- Typed literal test matrix exists.
- Cross-projection import specification regression exists.
- Core, DotNet, generator, JSON Schema, and EF tests cover the same semantic fixture.
- Invalid literal cases produce stable diagnostics.

### Release

- `./eng/check.sh` passes.
- `./eng/package.sh 2.6.0` passes.
- `./eng/package-smoke.sh 2.6.0` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.6.0` passes.
- No package is published inside the milestone.

## Human Review Requirements

Human review is required for:

```text
typed literal model names
conditional constraint model shape
diagnostic codes and wording
enum literal normalization behavior
JSON Schema conditional output
unsupported literal policy
cross-projection fixture completeness
2.6.0 package inventory
release notes
publication approval
```
