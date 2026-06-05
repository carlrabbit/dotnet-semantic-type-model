# JSON Schema Domain Model and Export Specification

## Status

Authoritative behavioral specification.

## Purpose

Define JSON Schema as a code-first domain semantic model projection and deterministic Draft 2020-12 export target.

This specification is authoritative for:

- JSON Schema domain semantic model behavior;
- JSON Schema derivation pipeline behavior;
- JSON Schema export behavior;
- simple `oneOf` and `anyOf` export;
- unsupported JSON Schema composition diagnostics;
- JSON Schema import exclusion from canonical model creation.

## Product Role

`SemanticTypeModel.JsonSchema` projects code-generated canonical semantic models into JSON Schema.

The package flow is:

```text
Code-generated canonical Semantic Type Model
  -> JSON Schema derivation transformations
  -> JsonSchemaSemanticModel
  -> JSON Schema Draft 2020-12 export
```

JSON Schema is not a supported authoring source for canonical semantic models.

## Source Boundary

Supported:

- export from a code-generated canonical semantic model;
- export from a snapshot-loaded canonical model originally generated from code;
- derivation through the M0028 domain derivation contract.

Unsupported:

- JSON Schema import as canonical model creation;
- public roundtrip as primary workflow;
- remote reference loading;
- schema registry behavior;
- validation engine behavior.

Retained JSON Schema import APIs, if any, are legacy/internal compatibility behavior and must not be presented as public authoring.

## Domain Semantic Model

The package must define `JsonSchemaSemanticModel` or an equivalent package-owned domain model.

The domain model must represent:

```text
document metadata
root schema
$defs
references
object schemas
scalar schemas
array schemas
dictionary schemas
enum schemas
required property metadata
nullable property metadata
format metadata
title/description metadata
annotations/extensions
simple oneOf/anyOf composition
unsupported export diagnostics
```

The exporter must operate on the JSON Schema domain model, not directly on scattered canonical annotations.

## Domain Derivation

The package must expose a derivation API equivalent to:

```csharp
var result = model.DeriveJsonSchemaModel(options =>
{
    options.UseDefaultTransformations();
});
```

The result must follow the M0028 pattern:

```csharp
SemanticDerivationResult<JsonSchemaSemanticModel>
```

or provide equivalent behavior:

```text
domain model
diagnostics
transformation trace
```

Users must be able to configure derivation transformations in code.

## Default Derivation Transformations

The default JSON Schema derivation pipeline should include deterministic transformations equivalent to:

```text
Derive JSON Schema document metadata.
Derive JSON Schema type names.
Derive JSON Schema scalar schemas.
Derive JSON Schema object schemas.
Derive JSON Schema properties.
Derive JSON Schema required metadata.
Derive JSON Schema nullability metadata.
Derive JSON Schema arrays.
Derive JSON Schema dictionaries.
Derive JSON Schema enums.
Derive JSON Schema formats.
Derive JSON Schema title and description metadata.
Derive JSON Schema annotations/extensions.
Derive simple JSON Schema composition.
Validate JSON Schema export compatibility.
```

Transformations must emit diagnostics rather than silently dropping unsupported meaning.

## Baseline Export Features

The exporter must support deterministic Draft 2020-12 output for:

```text
object types
scalar types
required properties
nullable properties
arrays
dictionaries
enums
format
title
description
annotations/extensions
$defs
$ref
simple oneOf
simple anyOf
```

Output must include:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema"
}
```

when exporting a full document unless an explicit option suppresses it.

## Determinism

Export must be deterministic.

Required deterministic ordering:

```text
root schema first
$defs by canonical type identifier or configured schema name
properties by canonical declaration/order metadata when available, then name
required entries by property order
composition branches by canonical alternative order
annotations/extensions by key
```

Export must not include timestamps, environment-specific paths, random identifiers, or culture-sensitive formatting.

## Nullability and Requiredness

Requiredness and nullability remain distinct.

Rules:

- absent property is represented by optional property semantics;
- present `null` is represented by nullable semantics;
- required nullable property is both required and nullable;
- nullable scalar/property export should use the configured nullability strategy;
- default nullability strategy should be deterministic and documented by options.

Allowed nullability export strategies:

```text
type array when representable
oneOf with null branch
```

The implementation may start with one default strategy and expose the other through options if feasible.

## Simple `oneOf` Export

`oneOf` is supported for exclusive code-derived alternatives.

Supported shape:

```text
oneOf over named alternatives
branches emitted as $ref to $defs when possible
deterministic branch order
optional annotations on the composition node
```

Example output:

```json
{
  "oneOf": [
    { "$ref": "#/$defs/CreditCard" },
    { "$ref": "#/$defs/BankTransfer" }
  ]
}
```

Diagnostics are required for:

```text
empty alternatives
unresolved alternatives
unsupported inline complex alternatives
unsupported nested composition
unsupported discriminator behavior
```

## Simple `anyOf` Export

`anyOf` is supported for non-exclusive code-derived alternatives.

Supported shape:

```text
anyOf over named alternatives
branches emitted as $ref to $defs when possible
deterministic branch order
optional annotations on the composition node
```

Example output:

```json
{
  "anyOf": [
    { "$ref": "#/$defs/SearchById" },
    { "$ref": "#/$defs/SearchByEmail" }
  ]
}
```

Diagnostics are required for the same unsupported cases as `oneOf`.

## Unsupported Composition

M0029 does not support full JSON Schema composition semantics.

Unsupported unless a later milestone adds explicit support:

```text
arbitrary nested oneOf/anyOf expressions
boolean schemas inside composition branches
not
if/then/else
dependentSchemas
unevaluatedProperties semantics
dynamicRef/dynamicAnchor
full discriminator semantics
automatic polymorphism inference from arbitrary inheritance
full allOf reduction
```

Unsupported cases must emit diagnostics when encountered in derivation/export.

## JSON Editor Compatibility

JSON Editor compatibility remains an optional export mode.

Rules:

- JSON Editor-compatible keywords are emitted only when the option is enabled.
- JSON Editor compatibility must not alter canonical requiredness/nullability semantics.
- M0029 does not introduce a JSON Editor runtime or standalone package.

## Export API

The exporter should support a clear two-step flow:

```csharp
var derived = model.DeriveJsonSchemaModel();
derived.Diagnostics.ThrowIfErrors();

var document = JsonSchemaExporter.Export(derived.Model);
```

A convenience one-step API may exist if it preserves access to diagnostics and trace.

Do not hide derivation diagnostics behind an export-only method.

## Diagnostics

JSON Schema derivation/export diagnostics must include where available:

```text
code
severity
message
model path
transformation id
projection target
related model paths
```

Diagnostic categories should include:

```text
unsupported export shape
unsupported composition shape
unresolved reference
empty alternatives
ambiguous schema name
duplicate schema name
unsupported annotation/extension value
legacy import API usage when public API is retained
```

## Inspection Integration

The JSON Schema domain model and derivation result must integrate with M0027/M0028 inspection.

Required behavior:

```csharp
derived.Diagnostics.ToDiagnosticText();
derived.Trace.ToTransformationText();
derived.Model.ToSemanticText();
```

or equivalent package-specific inspection methods.

Inspection output must be deterministic and suitable for tests.

## Sample Requirements

The primary public JSON Schema sample must be code-first.

The sample should demonstrate:

```text
annotated C# types
generated or runtime-extracted canonical model
query or text inspection
diagnostics inspection
JSON Schema domain model derivation
JSON Schema export
deterministic output file
```

The primary public sample must not use JSON Schema import or roundtrip as its main flow.

## Test Requirements

Short-running tests must cover:

```text
domain model derivation from generated model
object export
scalar export
required property export
nullable property export
array export
dictionary export
enum export
format export
title/description export
annotations/extensions export
$defs/$ref export
simple oneOf export
simple anyOf export
unsupported composition diagnostics
deterministic branch ordering
deterministic document output
legacy import boundary if retained
```

## Non-Goals

M0029 does not define:

```text
JSON Schema import as canonical model source
public JSON Schema roundtrip workflow
full Draft 2020-12 parity
OpenAPI
JSON Editor runtime
schema registry
remote reference loading
JSON validation engine
full allOf reduction
dynamicRef/dynamicAnchor
if/then/else
not
dependentSchemas
unevaluatedProperties semantics
full discriminator semantics
```
