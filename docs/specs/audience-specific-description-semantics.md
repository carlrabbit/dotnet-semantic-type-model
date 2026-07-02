# Audience-Specific Description Semantics

## Status

Authoritative behavioral specification.

## Purpose

Define projection-neutral user and technical descriptions, .NET authoring and XML-summary derivation, precedence, normalization, projection-selection obligations, diagnostics, and breaking removal of the former undifferentiated description model.

## Core Model

The canonical model supports exactly two description kinds:

```text
UserDescription
TechnicalDescription
```

There is no canonical general/default `Description`.

### User Description

A user description explains a modeled concept for business users, end users, form/editor users, report consumers, or product-facing documentation.

### Technical Description

A technical description explains implementation, storage, integration, operation, maintenance, development, administration, or other technical concerns.

## Independence

- The two values are independent.
- Either may be absent.
- Neither value overwrites or implicitly supplies the other.
- Projection targets select descriptions explicitly.
- Absence is preserved unless an explicit target policy defines a fallback.

## .NET Authoring

Supported explicit attributes:

```csharp
[SemanticUserDescription("...")]
[SemanticTechnicalDescription("...")]
```

The former `SemanticDescriptionAttribute` is removed and unsupported.

## XML Summary Derivation

XML `<summary>` is the built-in fallback source for technical description.

Precedence:

```text
SemanticTechnicalDescriptionAttribute
  > XML <summary>
  > absent
```

XML summary never creates a user description and never directly creates target-specific metadata.

XML extraction is always enabled. No `IncludeXmlDocumentation` switch exists.

## Required Technical Description

`RequireTechnicalDescription` validates the derived technical description, regardless of whether it came from an explicit attribute or XML summary.

A missing required technical description emits a stable extraction/generator diagnostic.

## Normalization

Description normalization must be deterministic.

At minimum:

- trim leading/trailing whitespace;
- normalize line endings;
- collapse indentation introduced by XML documentation formatting;
- preserve meaningful paragraph boundaries;
- convert supported documentation references to deterministic readable text;
- reject or diagnose empty effective values.

The exact normalization algorithm must be shared by runtime extraction and generator-backed extraction.

## Projection Obligations

Targets must not read a generic description.

Default mapping:

| Target output | Default source |
|---|---|
| JSON Schema `description` | User description |
| EF Core table comment | Technical description |
| EF Core column comment | Technical description |
| Power BI description | User description |
| Configuration consumer-facing docs | User description |
| Configuration technical inspection | Technical description where emitted |
| Canonical inspection | Both, explicitly labeled |

No user-facing target falls back to technical description by default.

## JSON Schema Technical Extension

JSON Schema may expose an explicit option that maps technical description to a validated extension property such as `x-technical-description`.

The extension name must be deterministic and valid according to repository JSON Schema extension policy.

Raw JSON injection is forbidden.

## Diagnostics

Required diagnostic categories:

```text
empty user description
empty technical description
conflicting duplicate user description
conflicting duplicate technical description
missing required technical description
malformed XML summary that cannot be normalized
invalid target description kind
invalid JSON Schema extension mapping
```

## Breaking Compatibility Boundary

The following are removed for the 2.4.0 line:

```text
SemanticDescriptionAttribute
canonical Description properties/fields
IncludeXmlDocumentation
SemanticTypeModelIncludeXmlDocumentation
RequireXmlDocumentation
XML summary direct mapping to schema.description
```

No compatibility aliases or automatic migration are required.

## Migration Rule

Old description text cannot be automatically classified safely. Consumers must choose whether each former description is user-facing, technical, or should be split into both.

## Invariants

- Description kinds remain projection-neutral.
- Target mapping remains projection-owned.
- XML documentation is an authoring source, not a projection output.
- Generated and runtime extraction produce equivalent canonical values.
- Inspection exposes both kinds without collapsing them.
- Unsupported mapping never silently drops an explicitly configured description.
