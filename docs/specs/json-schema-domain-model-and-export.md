# JSON Schema Domain Model and Export Specification

## Status

Authoritative behavioral specification.

## Purpose

Define JSON Schema as a code-first domain semantic model projection and deterministic Draft 2020-12 export target.

This specification is authoritative for:

- JSON Schema domain semantic model behavior;
- JSON Schema derivation pipeline behavior;
- JSON Schema export behavior;
- semantic constraint representation;
- simple `oneOf` and `anyOf` export;
- selected STM semantic preservation through `x-stm`;
- unsupported JSON Schema composition diagnostics;
- JSON Schema import exclusion from canonical model creation.

Cross-target coordination with System.Text.Json is defined by `docs/specs/json-representation-fidelity.md`.

## Product Role

`SemanticTypeModel.JsonSchema` projects code-generated canonical semantic models into JSON Schema.

```text
Code-generated canonical Semantic Type Model
  -> JSON Schema derivation transformations
  -> JsonSchemaSemanticModel
  -> JSON Schema Draft 2020-12 export
```

JSON Schema is not a supported authoring source for canonical semantic models.

The package exports schema; it is not a production JSON validation engine.

## Source Boundary

Supported:

- export from a code-generated canonical semantic model;
- export from a snapshot-loaded canonical model originally generated from code;
- derivation through the normal domain derivation contract.

Unsupported:

- JSON Schema import as canonical model creation;
- public roundtrip as primary workflow;
- remote reference loading;
- schema registry behavior;
- production validation-engine behavior.

No JSON Schema import API or compatibility surface is retained.

## Domain Semantic Model

The package must define `JsonSchemaSemanticModel` or an equivalent package-owned domain model.

The domain model must represent enough information for deterministic export of:

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
constraints
conditional RequiredWhen metadata
annotations/extensions
additional-properties policy including typed extension data where representable
simple oneOf/anyOf composition
unsupported export diagnostics
```

The exporter operates on the JSON Schema domain model, not on scattered canonical annotations.

## Domain Derivation

The package must expose a derivation API equivalent to:

```csharp
var result = model.DeriveJsonSchemaModel(options =>
{
    options.UseDefaultTransformations();
});
```

The result follows the standard semantic derivation pattern:

```text
domain model
diagnostics
transformation trace
```

Users may configure derivation transformations in code.

## Default Derivation Transformations

The default derivation pipeline includes deterministic behavior equivalent to:

```text
derive document metadata
derive schema type names
derive scalar schemas
derive object schemas
derive properties
derive required metadata
derive nullability metadata
derive arrays
derive dictionaries
derive enums
derive formats
derive constraints
derive conditional RequiredWhen semantics
derive title and description metadata
derive annotations/extensions
derive simple composition
validate JSON Schema export compatibility
```

Unsupported meaning must produce diagnostics rather than silent loss when the exported result would otherwise be misleading.

## Baseline Export Features

The exporter supports deterministic Draft 2020-12 output for:

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
string/numeric/collection constraints
conditional RequiredWhen equality
additionalProperties including typed extension-data values where representable
annotations/extensions
$defs
$ref
simple oneOf
simple anyOf
```

A full document includes:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema"
}
```

unless an explicit option suppresses it.

## Determinism

Required ordering:

```text
root schema first
$defs by canonical type identifier or configured schema name
object properties by declared SemanticOrder when present, then emitted property name
required entries by emitted property order
composition branches by canonical alternative order
x-stm structured members by the rules in this specification
annotations/extensions by key
```

JSON object property order does not become validation semantics merely because deterministic export uses an order.

Output must not contain timestamps, environment-specific paths, random identifiers, or culture-sensitive formatting.

## Nullability and Requiredness

Requiredness and nullability remain distinct.

Rules:

- absent property is represented by optional property semantics;
- present `null` is represented by nullable semantics;
- required nullable property is both required and nullable;
- nullable scalar/property export uses the configured deterministic nullability strategy.

Supported nullability strategies may use:

```text
type array when representable
oneOf with null branch
```

## Native Validation Semantics

Canonical validation semantics map to standard JSON Schema keywords when faithfully representable.

This includes:

- string length/pattern constraints;
- numeric minimum/maximum/exclusive bounds/multipleOf;
- collection size/uniqueness;
- requiredness;
- nullability;
- scalar format metadata;
- enum values.

These semantics are not duplicated under `x-stm`.

`format` follows JSON Schema Draft 2020-12 vocabulary semantics. SemanticTypeModel does not promise validator-specific format assertion behavior unless a future contract explicitly adds it.

## Conditional `RequiredWhen`

Canonical `RequiredWhen` with the supported equality operator is exported as conditional schema behavior equivalent to:

```text
if source property equals the typed canonical literal
then target property is required
```

The comparison literal uses canonical typed-literal meaning.

This supported generated conditional behavior is distinct from unrestricted user-authored JSON Schema composition.

Unsupported conditional operators emit projection diagnostics.

## Simple `oneOf` Export

`oneOf` is supported for exclusive code-derived alternatives.

Supported behavior:

```text
named alternatives
branches emitted as $ref to $defs when possible
deterministic branch order
optional annotations on the composition node
```

Diagnostics are required for empty alternatives, unresolved alternatives, unsupported inline complex alternatives, unsupported nested composition, and unsupported discriminator behavior.

## Simple `anyOf` Export

`anyOf` is supported for non-exclusive code-derived alternatives with the same named/reference/deterministic constraints as `oneOf`.

## Unsupported Composition

The package does not provide unrestricted Draft 2020-12 composition authoring.

Unsupported unless separately specified:

```text
arbitrary nested oneOf/anyOf expressions
boolean schemas inside arbitrary composition branches
not
general user-authored if/then/else
dependentSchemas
unevaluatedProperties semantics
dynamicRef/dynamicAnchor
full discriminator semantics
automatic polymorphism inference from arbitrary inheritance
full allOf reduction
```

The canonical `RequiredWhen` equality mapping is an explicit supported exception to the general `if/then/else` non-goal.

Unsupported cases emit diagnostics when encountered in derivation/export.

## Extension Data

Canonical extension data is instance-level compatibility data.

Default JSON Schema representation:

1. the extension-data bag property is omitted from normal `properties`;
2. the object permits additional members;
3. when the canonical extension-data value type is known and representable, `additionalProperties` contains the corresponding value schema;
4. when the value type cannot be represented faithfully, export remains safely permissive and emits a projection diagnostic;
5. when semantic annotations are enabled, the containing object carries `x-stm.extensionData: true`.

The exporter must not expose CLR/source extension-data implementation details.

This contract does not add general `unevaluatedProperties` support.

## STM Semantic Extension

`JsonSchemaExportOptions.IncludeSemanticAnnotations` defaults to `true`.

When disabled, no `x-stm` is emitted. Native JSON Schema representation remains unchanged.

When enabled, one `x-stm` object preserves approved STM-only semantics.

Approved vocabulary:

```text
role
aggregateRoot
mutability
technicalDescription
keys
unit
ui
displayIdentity
accessPaths
ownership
envelope
versioned
version
revision
currentVersion
temporalValidity
validFrom
validTo
lifecycleState
extensionData
enumValues
```

Standard JSON Schema keywords remain authoritative for meaning they already represent.

### Existing fields

Existing rules remain:

- `role`: canonical role;
- `aggregateRoot`: true only when declared/derived;
- `mutability`: declaration-preserving at the node where declared;
- `technicalDescription`: independent of standard user-facing `description`;
- `keys`: structured key metadata using emitted property names;
- `unit`: scalar unit metadata;
- `ui`: deterministic open JSON-compatible `ui.*` pass-through with prefix removed.

### Display Identity

Object-level:

```json
"x-stm": {
  "displayIdentity": ["customerNumber", "name"]
}
```

The array is ordered by canonical Display Identity order and uses emitted property names.

### Access Paths

Object-level:

```json
"x-stm": {
  "accessPaths": {
    "ByCustomer": ["customerNumber"],
    "ByCustomerAndDate": ["customerNumber", "date"]
  }
}
```

Path names are ordinal/case-sensitive and emitted deterministically. Member arrays use canonical path order and emitted property names.

### Ownership

Property-level:

```text
ownership = "object" | "collection"
```

This preserves lifecycle containment only; structural schema shape remains native.

### Envelope

Object-level structured metadata:

```text
purpose    optional
payload    emitted property name
metadata   deterministic emitted-property-name array
```

Envelope projection-root policy does not erase envelope semantics.

### Evolution and lifecycle

Type-level booleans:

```text
versioned
temporalValidity
```

Property-level booleans:

```text
version
revision
currentVersion
validFrom
validTo
lifecycleState
```

No target business behavior is inferred.

### Extension Data

Containing-object:

```text
extensionData = true
```

Native `additionalProperties` owns the actual additional-member shape.

### Enum-value metadata

When enum members carry semantic metadata not represented by native `enum`, emit `enumValues` as an array positionally aligned with the native `enum` values.

Each entry contains semantic member `name` plus optional:

```text
displayName
description
technicalDescription
```

Do not duplicate the native enum JSON value inside `x-stm`.

## Annotation Boundary

Do not duplicate native JSON Schema semantics in `x-stm`, including:

```text
requiredness
nullability
string/numeric/collection constraints
format
enum values
oneOf/anyOf/allOf
title
user description
additionalProperties
```

Do not put source/compiler or other target implementation details in `x-stm`, including:

```text
CLR setter/init shape
Roslyn identities
generator/manifest versions
systemTextJson.* metadata
EF mappings/database names
Power BI target metadata
Configuration/Options metadata
```

`SemanticAnnotationAttribute` is handled only through supported namespaces:

- `ui.*` -> `x-stm.ui`;
- `jsonSchema.keyword.*` -> projection keyword passthrough;
- arbitrary other annotations are not generically serialized into `x-stm`.

## Compatibility and Versioning

`x-stm` remains an optional extension object, not a serialized canonical model and not an authoring format.

No `x-stm` protocol version, reader-range negotiation, or compatibility adapters are introduced.

Consumers of `x-stm` are expected to tolerate unknown future fields.

Adding new `x-stm` members is therefore an additive minor-version capability.

## Export API

The exporter supports the two-step flow:

```csharp
var derived = model.DeriveJsonSchemaModel();
derived.Diagnostics.ThrowIfErrors();

var document = JsonSchemaExporter.Export(derived.Model);
```

A convenience one-step API may exist if it preserves access to diagnostics and trace.

Do not hide derivation diagnostics behind an export-only method.

## Diagnostics

Diagnostics include, where available:

```text
code
severity
message
model path
transformation id
projection target
related model paths
```

Diagnostic categories include:

```text
unsupported export shape
unsupported composition shape
unresolved reference
empty alternatives
ambiguous/duplicate schema name
unsupported annotation/extension value
unsupported extension-data value shape
unsupported conditional operator
```

## Inspection Integration

The JSON Schema domain model and derivation result integrate with deterministic inspection for model, diagnostics, and transformation trace.

## Sample Requirements

The primary public JSON Schema sample remains code-first and demonstrates:

```text
annotated C# types
generated or runtime-extracted canonical model
diagnostics inspection
JSON Schema domain model derivation
JSON Schema export
deterministic output
```

The primary public sample must not use JSON Schema import as its main flow.

A later public documentation sync may add a tandem System.Text.Json/schema example without turning JSON Schema validation into a package runtime dependency.

## Test Requirements

Short-running tests cover:

```text
domain model derivation from generated model
object/scalar/required/nullable export
array/dictionary/enum/format export
title/description export
constraints
RequiredWhen equality export
typed additionalProperties for extension data
expanded x-stm fields
enum-value metadata
$defs/$ref
simple oneOf/anyOf
unsupported composition diagnostics
deterministic semantic order/property output
deterministic document output
IncludeSemanticAnnotations=false
```

Cross-target output-conformance tests are specified in `docs/specs/json-representation-fidelity.md`.

## Non-Goals

- JSON Schema import as canonical model source;
- public JSON Schema roundtrip workflow;
- full Draft 2020-12 parity;
- OpenAPI;
- JSON Editor runtime;
- schema registry;
- remote reference loading;
- production JSON validation engine;
- full allOf reduction;
- dynamicRef/dynamicAnchor;
- general unevaluatedProperties semantics;
- arbitrary user-authored if/then/else;
- not/dependentSchemas;
- full discriminator semantics;
- automatic System.Text.Json polymorphism translation.
