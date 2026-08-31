# JSON Representation Fidelity Specification

## Status

Authoritative cross-target behavioral specification.

## Purpose

Define the supported relationship between:

```text
canonical TypeSchemaModel
    -> System.Text.Json runtime JSON representation

canonical TypeSchemaModel
    -> JSON Schema Draft 2020-12 representation/validation contract
```

The goal is for the two projections to walk in tandem without merging their responsibilities.

This specification is authoritative for:

- the System.Text.Json / JSON Schema responsibility boundary;
- the supported one-way output-conformance guarantee;
- the baseline configuration under which that guarantee applies;
- JSON Schema classification of current semantic authoring attributes;
- STM-only semantic preservation under `x-stm`;
- representation-changing System.Text.Json metadata that is outside the guarantee;
- cross-package fidelity validation requirements.

Target-specific details remain in the JSON Schema and System.Text.Json subsystem specifications.

## Core Contract

System.Text.Json owns actual JSON serialization/deserialization representation.

JSON Schema owns the declarative structural and semantic validation contract for that representation.

For the supported fidelity configuration:

> JSON that is successfully emitted by System.Text.Json for a modeled CLR value MUST validate against the Draft 2020-12 JSON Schema derived from the same canonical semantic model, subject to the explicit exclusions in this specification.

This is a **one-way output-conformance guarantee**.

It does not mean:

- every JSON document accepted by the schema must be deserializable by System.Text.Json;
- every JSON document accepted by System.Text.Json must be schema-valid;
- JSON Schema describes every read-side serializer behavior;
- System.Text.Json enforces canonical validation constraints;
- the two target packages share a new runtime model or depend on one another.

## Architectural Boundary

No new `SemanticTypeModel.Json` package or canonical JSON wire-contract model is introduced.

`SemanticTypeModel.JsonSchema` and `SemanticTypeModel.SystemTextJson` remain sibling projections over the canonical model.

Neither target package may depend on the other.

A future shared JSON representation model requires a separate architecture decision if concrete duplication or incompatibility proves that it is necessary.

## Baseline Fidelity Configuration

The guaranteed baseline is explicit; it is not the current default System.Text.Json behavior.

Required baseline behavior:

1. System.Text.Json property names are projected from canonical semantic property names (`SemanticJsonPropertyNameSource.SemanticPropertyName` or equivalent existing behavior).
2. Canonical enum values generated from .NET enums are serialized as their string semantic member values, using ordinary supported System.Text.Json string-enum configuration.
3. Required members are not conditionally omitted by serializer settings.
4. Default/null ignore policies that can omit schema-required properties are not enabled.
5. Reference-preservation or other serializer features that inject representation metadata such as `$id`, `$ref`, or collection wrappers are not part of the baseline.
6. Representation-changing custom converters are not part of the baseline unless a future explicit mapping contract proves their JSON shape.
7. Representation-changing number handling, including writing numbers as strings, is not part of the baseline.
8. Explicit System.Text.Json polymorphism/discriminator metadata is not part of the baseline in this milestone.
9. Extension data is supported only when the effective System.Text.Json contract and canonical `ExtensionData` semantics describe the same extension-data member and compatible value shape.
10. Successfully serialized CLR members outside the canonical model are outside the guarantee unless the effective base System.Text.Json contract also omits them.

The existing System.Text.Json default `ExistingJsonContract` property-name policy remains the default for compatibility. It does **not** carry a general JSON Schema fidelity guarantee because an application-owned resolver/context may make representation choices unknown to the JSON Schema projection.

The baseline does not require a new public "fidelity mode" API. A later convenience API may be added only if experience demonstrates a need.

## Validation Boundary

System.Text.Json MUST NOT reimplement general semantic validation.

Canonical constraints such as:

```text
minimum / maximum
minLength / maxLength
pattern
collection cardinality
RequiredWhen
```

belong to JSON Schema representation/validation when representable.

System.Text.Json may still own serializer-native read/write contract behavior such as required-on-deserialization metadata, ignore conditions, converters, number handling, extension data, object creation handling, unmapped-member handling, and polymorphism.

A semantic property can therefore serialize successfully and still be semantically invalid; JSON Schema validation is the layer that detects such constraint failure.

`format` remains a JSON Schema Draft 2020-12 format annotation unless a validator explicitly enables format assertion. The baseline fidelity guarantee does not promise conformance to validator-specific format assertions beyond the JSON Schema document emitted by SemanticTypeModel.

## JSON Schema Semantic Classification

Every current public attribute in the `SemanticTypeModel.DotNet` semantic authoring vocabulary has one explicit JSON Schema classification.

### Complete authoring-attribute matrix

| Attribute | JSON Schema classification |
|---|---|
| `SemanticTypeAttribute` | Native canonical type shape; declared role is additionally preserved as `x-stm.role` when applicable. |
| `SemanticIgnoreAttribute` | Acquisition-only exclusion; ignored elements are absent from the canonical model and receive no schema marker. |
| `SemanticNameAttribute` | Native emitted canonical type/property name. |
| `SemanticUserDescriptionAttribute` | Native `description`. |
| `SemanticTechnicalDescriptionAttribute` | `x-stm.technicalDescription`. |
| `SemanticRoleAttribute` | `x-stm.role`; standard JSON Schema has no equivalent domain-role keyword. |
| `SemanticKeyAttribute` | Structured `x-stm.keys`; normal requiredness remains native and model-wide uniqueness is not claimed. |
| `SemanticMutableAttribute` | `x-stm.mutability = "mutable"` at the declared node. |
| `SemanticImmutableAttribute` | `x-stm.mutability = "immutable"` at the declared node. |
| `SemanticTypeModelGeneratorOptionsAttribute` | Acquisition/generator configuration; not exported. |
| `SemanticDisplayNameAttribute` | Native `title`. |
| `SemanticCategoryAttribute` | `x-stm.ui.category`. |
| `SemanticOrderAttribute` | `x-stm.ui.order` and deterministic exporter ordering; not validation semantics. |
| `SemanticDisplayIdentityAttribute` | Structured object-level `x-stm.displayIdentity`. |
| `SemanticAccessPathAttribute` | Structured object-level `x-stm.accessPaths`. |
| `SemanticFormatAttribute` | Native `format` when compatible. |
| `SemanticStringConstraintsAttribute` | Native string constraint keywords. |
| `SemanticNumericConstraintsAttribute` | Native numeric constraint keywords. |
| `SemanticCollectionConstraintsAttribute` | Native array/collection constraint keywords. |
| `SemanticEnumValueAttribute` | Native enum value plus semantic member metadata in `x-stm.enumValues` when metadata is present. |
| `SemanticAnnotationAttribute` | Namespace-driven only: `ui.*` -> `x-stm.ui`; `jsonSchema.keyword.*` -> existing keyword passthrough; arbitrary other keys are not generically exported. |
| `SemanticEnvelopeAttribute` | Structured object-level `x-stm.envelope`; ordinary object shape remains native. |
| `SemanticEnvelopePayloadAttribute` | Preserved as the emitted `x-stm.envelope.payload` property name. |
| `SemanticEnvelopeMetadataAttribute` | Preserved in the deterministic `x-stm.envelope.metadata` emitted-property-name array. |
| `SemanticVersionedAttribute` | `x-stm.versioned = true`. |
| `SemanticOwnedAttribute` | Native nested/reference object or array shape plus property-level `x-stm.ownership`. |
| `SemanticVersionAttribute` | Property-level `x-stm.version = true`. |
| `SemanticRevisionAttribute` | Property-level `x-stm.revision = true`. |
| `SemanticCurrentVersionAttribute` | Property-level `x-stm.currentVersion = true`. |
| `SemanticTemporalValidityAttribute` | Type-level `x-stm.temporalValidity = true`; temporal property shapes/formats remain native. |
| `SemanticValidFromAttribute` | Property-level `x-stm.validFrom = true`. |
| `SemanticValidToAttribute` | Property-level `x-stm.validTo = true`. |
| `SemanticLifecycleStateAttribute` | Property-level `x-stm.lifecycleState = true`; enum/scalar shape remains native. |
| `SemanticExtensionDataAttribute` | Native `additionalProperties` representation plus containing-object `x-stm.extensionData = true`; bag is not a normal schema property. |
| `SemanticRequiredWhenAttribute` | Native conditional schema generated from canonical typed equality semantics. |

The classification is exhaustive for the current `SemanticTypeModel.DotNet` public semantic authoring attributes. New semantic attributes added later must receive an explicit JSON Schema classification before the JSON Schema projection can claim semantic-fidelity coverage for them.

### Native or structural JSON Schema representation

| Semantic authoring contract | Required JSON Schema behavior |
|---|---|
| `SemanticTypeAttribute` | Canonical type shape projects through normal object/scalar/enum/array/dictionary/reference/composition behavior. |
| `SemanticIgnoreAttribute` | The ignored member/type is absent because it is absent from the canonical model; no `x-stm` marker is emitted. |
| `SemanticNameAttribute` / `SemanticTypeAttribute.Name` | Canonical emitted type/property identity/name. |
| `SemanticDisplayNameAttribute` | `title`. |
| `SemanticUserDescriptionAttribute` | `description`. |
| `SemanticFormatAttribute` | `format` when compatible. |
| `SemanticStringConstraintsAttribute` | Native string constraint keywords. |
| `SemanticNumericConstraintsAttribute` | Native numeric constraint keywords. |
| `SemanticCollectionConstraintsAttribute` | Native array/collection constraint keywords. |
| `SemanticRequiredWhenAttribute` | Supported equality form projects to conditional `if`/`then` requiredness. |
| Requiredness/nullability derived from CLR/core semantics | Native `required` and configured null representation. |
| Enum serialized values | Native `enum`. |
| Owned object/collection shape | Normal nested/reference object or array shape. |
| Extension-data openness/value shape | `additionalProperties` behavior; the bag itself is not a normal property. |

Native JSON Schema semantics MUST NOT be redundantly copied into `x-stm`.

### STM-only semantics preserved under `x-stm`

The following projection-neutral meaning is not faithfully represented by standard JSON Schema keywords and MUST be preserved when semantic annotations are enabled:

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

The exact stable shapes are defined below and by the JSON Schema subsystem specification.

### Target-specific or acquisition-only semantics

The following must not be generically copied into `x-stm`:

- `SemanticTypeModelGeneratorOptionsAttribute`;
- `systemTextJson.*` annotations;
- EF Core or Power BI target metadata;
- CLR/Roslyn/source identities;
- arbitrary `SemanticAnnotationAttribute` entries outside explicitly supported namespaces.

`SemanticAnnotationAttribute` remains namespace-driven:

- `ui.*` -> `x-stm.ui`;
- `jsonSchema.keyword.*` -> explicit JSON Schema keyword passthrough under existing projection rules;
- other arbitrary annotations are not automatically serialized into `x-stm`.

## `x-stm` Structured Semantics

`x-stm` is semantic preservation, not serialized canonical STM.

Standard JSON Schema keywords remain authoritative for concepts they represent.

### Display Identity

On an object schema:

```json
{
  "x-stm": {
    "displayIdentity": ["customerNumber", "name"]
  }
}
```

Rules:

- value is an array of emitted JSON Schema property names;
- order follows canonical Display Identity order;
- empty arrays are not emitted;
- names are emitted schema names, never CLR member names or canonical IDs.

### Access Paths

On an object schema:

```json
{
  "x-stm": {
    "accessPaths": {
      "ByCustomer": ["customerNumber"],
      "ByCustomerAndDate": ["customerNumber", "date"]
    }
  }
}
```

Rules:

- the object key is the canonical case-sensitive Access Path name;
- values are ordered arrays of emitted property names;
- Access Path keys are emitted in ordinal deterministic order;
- an empty path is not emitted.

### Ownership

On an owned property schema:

```json
{
  "x-stm": {
    "ownership": "object"
  }
}
```

or:

```json
{
  "x-stm": {
    "ownership": "collection"
  }
}
```

The structural JSON Schema shape remains authoritative for object/array representation. `ownership` preserves lifecycle containment meaning only.

### Envelope

On an envelope object schema:

```json
{
  "x-stm": {
    "envelope": {
      "purpose": "management",
      "payload": "payload",
      "metadata": ["revision", "modifiedBy"]
    }
  }
}
```

Rules:

- `purpose` is omitted when absent;
- `payload` is the emitted payload property name;
- `metadata` is a deterministic array of emitted envelope-metadata property names;
- payload/metadata meaning is preserved even when projection policy selects the payload as document root and the envelope remains in `$defs`.

### Evolution and lifecycle

Use stable booleans at the semantic declaration location:

Type-level:

```text
versioned
temporalValidity
```

Property-level:

```text
version
revision
currentVersion
validFrom
validTo
lifecycleState
```

Only declared semantic markers are emitted. Do not invent behavior such as temporal-table semantics, current-version filtering, or lifecycle transitions.

### Extension Data

An object with canonical extension data emits:

```json
{
  "x-stm": {
    "extensionData": true
  }
}
```

The source bag property is not exposed as a normal schema property.

The actual allowed additional-member value shape is represented natively through `additionalProperties` when representable.

### Enum-value metadata

Native `enum` remains authoritative for JSON values.

When at least one enum member carries semantic metadata not otherwise represented by native `enum`, `x-stm.enumValues` is emitted as an array aligned positionally with the native `enum` array.

Each entry contains:

```text
name                     required semantic enum member name
displayName              optional
description              optional user-facing description
technicalDescription     optional
```

The JSON enum value is not duplicated inside `x-stm`.

## Extension Data Representation

Canonical `SemanticExtensionData` has two simultaneous effects in JSON Schema:

1. the extension-data bag itself is omitted from normal `properties`;
2. the object is open to additional members.

When the canonical extension-data value type is known and representable, `additionalProperties` MUST carry the corresponding value schema.

When the value type cannot be represented faithfully:

- export remains safely permissive rather than falsely restrictive;
- an explicit JSON Schema projection diagnostic is emitted;
- the exporter must not invent a CLR-specific or serializer-specific schema.

This milestone does not add general `unevaluatedProperties` semantics.

## Semantic Order and Determinism

JSON object member order is not validation semantics.

For deterministic document output, object properties are emitted using:

1. declared semantic presentation/order metadata when present;
2. then stable emitted property name as tie-breaker/fallback.

`SemanticOrder` continues to be preserved as UI/presentation metadata where currently supported. It does not imply database order, serializer order, or validation behavior.

## Conditional Requiredness

Canonical `RequiredWhen` with the supported equality operator is a supported JSON Schema derivation and MUST emit conditional requiredness equivalent to:

```text
if source property equals literal
then target property is required
```

This does not add arbitrary JSON Schema composition authoring.

General user-authored `if/then/else`, arbitrary nested conditions, `dependentSchemas`, `not`, and other unrestricted composition remain outside the baseline unless separately specified.

## System.Text.Json Contract Metadata

The System.Text.Json domain model must represent enough imported metadata to inspect and classify fidelity impact.

At minimum, where present in the canonical annotations, it must retain deterministic domain metadata for:

```text
propertyName
ignore / ignoreCondition
include
converter
numberHandling
required
extensionData
objectCreationHandling
unmappedMemberHandling
polymorphism marker
```

This requirement does not mean the SemanticTypeModel resolver reimplements System.Text.Json.

The application-owned/default base resolver/context remains authoritative for serializer-native behavior except for supported SemanticTypeModel customization such as explicit property-name projection.

### Fidelity impact

| STJ metadata | Baseline interpretation |
|---|---|
| `JsonPropertyName` | May be imported/preserved, but baseline fidelity uses canonical semantic property names at runtime. |
| `JsonIgnore` / ignore condition | Compatible only when omission cannot cause a schema-required output member to disappear; otherwise existing conflict diagnostics apply and fidelity is not claimed. |
| `JsonInclude` | Base-contract behavior; no independent JSON Schema semantic. |
| `JsonRequired` | Read-side serializer contract; no additional JSON Schema semantic beyond canonical requiredness. |
| `JsonExtensionData` | Compatible when it identifies the same effective extension-data member and value shape as canonical extension-data semantics. |
| `JsonObjectCreationHandling` | Read-side behavior; no JSON Schema representation. |
| `JsonUnmappedMemberHandling` | Read-side behavior; no JSON Schema representation. |
| `JsonNumberHandling` | Any representation-changing write behavior is outside baseline fidelity. |
| custom `JsonConverter` | Outside baseline fidelity unless a future explicit wire-shape contract models it. |
| `JsonPolymorphic` / `JsonDerivedType` | Outside baseline fidelity in this milestone. |

## Polymorphism Boundary

All polymorphic and discriminator output is outside the JSON Schema/System.Text.Json fidelity baseline,
including automatic semantic-Entity polymorphism supplied by the System.Text.Json runtime projection.

Explicit System.Text.Json polymorphism is deliberately **not** completed by this milestone.

Current imported polymorphism metadata is not rich enough to reconstruct discriminator property names, discriminator values, derived-type mappings, unknown-derived-type behavior, or fallback behavior faithfully.

Therefore:

- existing explicit polymorphism diagnostics remain;
- M0066 must not convert the current boolean/marker metadata into a guessed JSON Schema discriminator contract;
- types requiring explicit System.Text.Json polymorphism are outside the tandem fidelity guarantee;
- canonical JSON Schema `oneOf` / `anyOf` support remains independently available for canonical union models;
- a future milestone may define a structured polymorphism/discriminator contract.

## Converter Boundary

SemanticTypeModel must not inspect arbitrary converter implementation code to guess wire shape.

A representation-changing converter may serialize a CLR object as a scalar, string, number, or unrelated object shape. Without an explicit representation contract, JSON Schema cannot safely derive that wire shape.

Such cases are outside the fidelity guarantee and must remain explicit through existing metadata/diagnostics.

## Test-Only Validation

`SemanticTypeModel.JsonSchema` remains an exporter, not a production JSON validation engine.

M0066 tests MUST nevertheless validate the cross-target guarantee using a standards-compliant Draft 2020-12 validator.

A validator package may be added to test projects only.

It MUST NOT become:

- a dependency of any publishable SemanticTypeModel package;
- a new public validation API;
- a runtime dependency required merely to export schema.

The exact test-only validator and test organization are implementation-owned.

## Required Fidelity Scenarios

Cross-target tests must begin from real annotated CLR source and the real generated or extracted canonical model for successful boundary proof.

The minimum tandem matrix includes:

```text
object with semantic property naming
required + optional + nullable members
string/numeric constraints
collection
nested/value-object shape
string-valued enum representation
semantic display/user/technical metadata
Display Identity
Access Paths
ownership
version/revision/lifecycle markers
extension data with compatible STJ extension-data contract
```

Additional JSON Schema-only tests must prove:

```text
RequiredWhen conditional requiredness
expanded x-stm deterministic shape
enum-value semantic metadata
typed additionalProperties where representable
IncludeSemanticAnnotations=false suppresses all x-stm without suppressing native validation semantics
```

Negative fidelity coverage must prove that at least:

```text
representation-changing custom converter
representation-changing number handling
explicit STJ polymorphism
required semantic member omitted by serializer contract
```

does not silently receive a false fidelity claim.

## Compatibility

This work is additive for the 4.1 release line.

Compatibility requirements:

- existing System.Text.Json default `ExistingJsonContract` behavior does not change;
- no existing public API is removed or renamed;
- plain JSON Schema export remains available by disabling semantic annotations;
- `x-stm` gains new members, relying on the established tolerant-consumer rule for unknown future extension members;
- standard JSON Schema keyword behavior remains authoritative;
- no canonical persisted-model version change is required solely for these projection changes;
- no EF semantic-manifest schema/version change is required;
- no new cross-dependency between JSON Schema and System.Text.Json packages is introduced.

## Non-Goals

- production JSON validation engine/API;
- JSON Schema import;
- bidirectional serializer/schema equivalence;
- a new shared JSON package or canonical JSON wire model;
- automatic inference of arbitrary converter wire shapes;
- complete System.Text.Json polymorphism/discriminator support;
- reference-preservation schema support;
- arbitrary global `JsonSerializerOptions` compatibility guarantees;
- unrestricted JSON Schema composition authoring;
- OpenAPI;
- schema registry or remote reference loading;
- global projection-capability taxonomy expansion for every newer semantic concept.
