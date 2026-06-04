# Code-First Semantic Model Architecture Specification

## Status

Authoritative behavioral specification.

## Purpose

Define the code-first architecture for SemanticTypeModel.

This specification is authoritative for:

- supported canonical model sources;
- model persistence boundaries;
- runtime editing boundaries;
- semantic primitive and attribute extensibility direction;
- query and inspection product surfaces;
- transformation and projection model;
- domain-specific semantic model derivation.

## Core Positioning

SemanticTypeModel is a code-first semantic metadata framework for .NET.

The canonical model is generated from annotated .NET code through runtime extraction or compile-time source generation.

The canonical model may be persisted and loaded as a snapshot, but external schema formats are not supported as canonical model authoring sources.

## Supported Canonical Model Sources

Supported sources:

| Source | Meaning |
|---|---|
| Runtime .NET extraction | Build the canonical model from reflected/Roslyn .NET type metadata and SemanticTypeModel attributes at runtime. |
| Compile-time generation | Generate a static provider from annotated .NET code that creates the canonical model. |
| Persisted model snapshot | Load a previously generated canonical model without access to the original codebase. |

Unsupported sources:

| Source | Status |
|---|---|
| JSON Schema import | Unsupported as canonical model creation. JSON Schema is a projection/export target. |
| Runtime model editor | Unsupported as a consumer authoring surface. |
| OpenAPI import | Unsupported. |
| TypeScript model import | Unsupported. |
| Arbitrary external schema import | Unsupported unless a later decision explicitly adds a source adapter. |

## Model Snapshot Boundary

A persisted model snapshot is a serialized representation of a model that was originally generated from code.

Snapshot rules:

- snapshots may be loaded without access to the codebase;
- snapshots preserve canonical identifiers, shapes, annotations, diagnostics metadata when included, and version metadata where available;
- snapshots are not an authoring format;
- snapshots do not make external schema import a supported model source;
- snapshot compatibility must be explicit and versioned when introduced.

## Runtime Editing Boundary

Consumer-facing runtime editing of the canonical model is unsupported.

Builder APIs may exist for extraction implementation, generator implementation, tests, transformation internals, and controlled snapshot loading.

Builder APIs are not a general-purpose model authoring product.

## Semantic Primitives

The core model defines a canonical set of semantic primitives.

Baseline primitives include:

```text
Type role
Entity
Value object
Key
Relationship
Property
Requiredness
Nullability
Collection cardinality
Display name
Description
Category
Order
Format
Constraint
Annotation
Diagnostic
```

## Attribute Extensibility

The code-first model supports custom attributes.

Custom attributes may be:

| Attribute kind | Meaning |
|---|---|
| Core alias attribute | Maps directly to a core semantic primitive, such as `SemanticEntity` mapping to `SemanticType("Entity")`. |
| Core extension attribute | Adds structured metadata that extends core semantics while remaining projection-neutral. |
| Domain attribute | Adds domain-specific metadata such as EF Core, Power BI, JSON Schema, or System.Text.Json intent. |

Attribute rules:

- attributes declare intent;
- transformations derive canonical or domain-specific meaning from attributes;
- invalid, ambiguous, or unsupported attribute use emits diagnostics;
- attributes must not bypass model validation;
- domain-specific attributes must not mutate core semantic meaning silently.

## Transformation Model

Transformations are the primary mechanism for deriving meaning.

Transformations may:

- normalize extracted code metadata;
- derive core semantic primitives from convenience attributes;
- derive domain-specific metadata from core semantics;
- emit diagnostics when derivation is unsafe;
- create a new model snapshot or domain semantic model.

Transformations must be deterministic, configurable from code, and diagnostic-producing rather than silently lossy.

## Projection Model

A projection creates a new domain-specific semantic model before domain-specific functionality is applied.

Projection pattern:

```text
Canonical semantic model
  -> domain derivation transformations
  -> domain semantic model
  -> domain functionality
```

Examples:

```text
Canonical semantic model
  -> JSON Schema semantic model
  -> JSON Schema document export
```

```text
Canonical semantic model
  -> EF Core semantic model
  -> ModelBuilder configuration
```

```text
Canonical semantic model
  -> Power BI semantic model
  -> local Power BI metadata output
```

```text
Canonical semantic model + System.Text.Json metadata
  -> System.Text.Json resolver customization model
  -> IJsonTypeInfoResolver behavior
```

## Domain Semantic Models

Each domain package owns its domain semantic model.

Domain semantic models must:

- be derived from the canonical semantic model;
- represent domain concepts explicitly;
- expose diagnostics for unsupported or ambiguous derivation;
- avoid relying on ad hoc annotation lookup in final domain functionality;
- keep external system behavior isolated from core contracts.

Required domain directions:

| Domain | Direction |
|---|---|
| JSON Schema | Export from code-generated semantic models. No canonical model import. |
| EF Core | Derive an EF Core semantic model before configuring `ModelBuilder`. |
| Power BI | Derive a local Power BI semantic model before exporting local metadata. |
| System.Text.Json | Use metadata import and resolver customization; do not generate serializers or contexts. |

## Query Surface

Operating with the model is a core use case.

The query surface should prefer type-based access and allow string fallback.

Required query direction:

```csharp
model.Type<Customer>();
model.Types.AssignableTo<Customer>();
model.Property<Customer>(x => x.Email);
model.PropertiesOf<Customer>();
model.RelationshipsFrom<Customer>();
model.FindByClrType(typeof(Customer));
model.FindByIdentifier("global::MyApp.Customer");
```

String-based access remains the canonical low-level fallback because internal identifiers are string-based.

Query APIs must be deterministic and suitable for tests.

## Inspection Surface

Inspection is a core development-loop feature.

The library must support deterministic text-based inspection for:

- canonical model summaries;
- type summaries;
- property summaries;
- relationship summaries;
- diagnostics;
- transformation results;
- domain semantic model summaries.

Example intent:

```text
Type Customer [Entity]
  Key: Id
  Property Id: string, required, format uuid
  Property Email: string, required, format email
  Relationship Orders: one-to-many CustomerOrder

Diagnostics:
  STM5008 warning /types/Customer/properties/Email: Missing display name.
```

The exact format may evolve, but it must be deterministic and useful in tests/console development loops.

## Development Loop

The primary consumer development loop is:

```text
Annotate .NET types.
Run a test or console program.
Inspect diagnostics and model output.
Adjust annotations or transformation configuration.
Repeat.
```

Samples, package READMEs, and diagnostics should support this loop.

## Documentation Boundary

The project does not require broad website-style documentation.

Primary documentation surfaces are:

- package README sources;
- quickstarts;
- configuration option listings;
- consumer samples;
- diagnostics references;
- XML/API documentation where appropriate.

Broad conceptual websites, marketing pages, and long-form essays are out of scope unless a later decision changes public documentation maturity.

## Unsupported Scope

The following are outside the project scope unless a later accepted decision changes it:

```text
JSON Schema import as canonical model creation
runtime canonical model editing
OpenAPI import/export
TypeScript generation
Power BI service integration
PBIX generation
full TOM parity
EF Core database creation or migration execution
custom JSON serializer
standalone JsonEditor package/runtime
broad website-style docs
```

## Invariants

- Code is the only supported authoring source for canonical semantic models.
- Persisted snapshots are loaded representations of code-generated models.
- Canonical models are immutable to consumers.
- Custom attributes declare intent; transformations derive meaning.
- Domain features operate through domain-specific semantic models.
- Diagnostics are the primary feedback mechanism for unsafe derivation.
- Query and inspection are first-class development-loop features.
