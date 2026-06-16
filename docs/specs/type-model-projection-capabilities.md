# Type Model Projection Capabilities

## Purpose

Define projection capability contracts for the canonical semantic model and keep capability metadata deterministic across projection targets.

## Targets

1. JSON Schema Draft 2020-12 runtime projection
2. JSON editor/UI-hint projection mode
3. EF Core metadata projection
4. Power BI metadata projection

## Capability Metadata API

Capability metadata is defined by `SemanticTypeModel.Abstractions.Model` contracts:

- `SemanticModelFeature`
- `ProjectionFeatureSupportLevel`
- `ProjectionFeatureCapability`
- `ProjectionCompatibilityContract`
- `IProjectionCapabilityProvider`
- `ProjectionCapabilityCatalog`

Projection implementations expose capability metadata through `IProjectionCapabilityProvider.GetCapabilities()`.

`ProjectionCapabilityCatalog` is the authoritative deterministic matrix and includes a contract for every supported projection target.

## Capability Taxonomy

Support levels are:

- `Supported`
- `SupportedWithOptions`
- `PartiallySupported`
- `RepresentedAsAnnotation`
- `Ignored`
- `Unsupported`
- `UnsupportedWithDiagnostic`

## Core Feature Matrix

The matrix covers these core features:

- Object type
- Scalar property
- Required property
- Nullable property
- Array
- Dictionary
- Enum
- Union
- Reference
- Value object
- Entity role
- Primary key
- Alternate key
- Relationship
- Computed member
- Validation constraints
- Display metadata
- UI hints
- Projection-specific annotations
- Recursive type
- Closed generic type
- Open generic type

Per-target support values are maintained in `ProjectionCapabilityCatalog` and validated by unit tests for:

- target coverage;
- per-target feature completeness;
- deterministic output.

## Diagnostic Expectations

Unsupported or degraded shapes must emit stable projection diagnostics when output would otherwise be misleading.

Examples:

- JSON Schema runtime adapter warnings: `STM3202`, `STM3203`, `STM3204`.
- EF Core projection diagnostics: `EFCORE_*`.
- Power BI projection diagnostics: `POWERBI_*`.
- JSON-editor compatibility diagnostics in JSON Schema projection modes: `JSONSCHEMA_UI_*`.
