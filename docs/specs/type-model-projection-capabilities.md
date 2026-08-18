# Type Model Projection Capabilities

## Purpose

Define projection capability contracts for the canonical semantic model and keep capability metadata deterministic across projection targets.

## Targets

1. JSON Schema Draft 2020-12 projection and export
2. EF Core metadata inspection and generated configuration
3. Power BI local metadata projection
4. System.Text.Json resolver customization

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
- JSON Schema semantic-annotation diagnostics, including invalid open `ui.*` values: `JSONSCHEMA_*`.

JSON Editor compatibility is not a projection target or capability mode. Open `ui.*` annotations may be
preserved beneath JSON Schema `x-stm.ui` without editor-specific translation or widget inference.
