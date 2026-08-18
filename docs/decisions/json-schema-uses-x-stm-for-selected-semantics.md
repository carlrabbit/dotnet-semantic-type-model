# JSON Schema Uses `x-stm` for Selected Semantic Preservation

## Status

Accepted.

## Decision

`SemanticTypeModel.JsonSchema` uses one optional JSON Schema extension object named `x-stm` to preserve selected canonical STM semantics that standard JSON Schema cannot represent faithfully.

`x-stm` is not a serialized canonical model and is not an alternative authoring format.

Standard JSON Schema keywords remain authoritative for concepts JSON Schema already represents.

The approved extension vocabulary is:

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

The detailed stable shapes are defined by `docs/specs/json-schema-domain-model-and-export.md` and the cross-target fidelity contract.

## Structured Group Semantics

Semantics that are property groups in the canonical model are exported in consumer-usable object-level form rather than leaking the internal annotation encoding.

Examples:

```text
Display Identity -> ordered emitted-property-name array
Access Paths     -> path-name to ordered emitted-property-name arrays
Keys             -> structured key records
Envelope         -> purpose/payload/metadata structure
```

This preserves semantic meaning without serializing canonical STM internals such as property IDs or annotation keys.

## UI Metadata

UI metadata is transported under:

```json
{
  "x-stm": {
    "ui": {}
  }
}
```

Canonical `ui.*` annotations map into the `ui` object by removing the leading `ui.` prefix.

Existing explicit semantics such as `ui.title`, `ui.category`, and `ui.order` remain meaningful. Other JSON-compatible `ui.*` annotations are pass-through metadata and are not required to belong to a closed vocabulary.

Do not add JSON Editor-specific behavior, widget inference, strict widget vocabularies, or editor-specific keyword translation to the core JSON Schema projection.

## Description Semantics

JSON Schema `description` uses canonical `UserDescription`.

Canonical `TechnicalDescription` remains independent and, when semantic annotations are enabled, is preserved as:

```json
{
  "x-stm": {
    "technicalDescription": "..."
  }
}
```

Technical description must not substitute for missing user description.

Enum-value user/technical descriptions may be preserved through the structured `enumValues` semantic metadata defined by the JSON Schema specification.

## Extension Boundary

Do not duplicate native JSON Schema semantics in `x-stm`, including:

```text
requiredness
nullability
string/numeric/collection constraints
format
enum JSON values
oneOf/anyOf/allOf
title
user description
additionalProperties
```

Do not put source/compiler or target-specific implementation details in `x-stm`, including:

```text
CLR setter/init shape
Roslyn identities
generator/manifest versions
System.Text.Json contract annotations
EF mappings/database names
Power BI target metadata
Configuration/Options metadata
```

Arbitrary custom semantic annotations are not generically serialized into `x-stm`; only explicitly supported semantic namespaces/vocabulary are exported.

## Compatibility and Versioning

The exporter allows callers to disable STM semantic annotations and obtain plain JSON Schema.

No `x-stm` protocol version, compatibility negotiation, reader ranges, or adapters are introduced.

Unknown future `x-stm` members must be tolerated by consumers.

Because tolerant unknown fields are part of the extension contract, expanding the approved semantic vocabulary is additive and does not require a protocol-version field.

If `x-stm` later becomes a separately versioned interoperability protocol, that requires a new explicit decision.
