# Projection Capabilities

The canonical semantic model is richer than any single projection target.

Use projection capability contracts to understand what each target supports directly, supports with options, or degrades with diagnostics.

## Capability API

Projection capability metadata is available through:

- `ProjectionCapabilityCatalog` for deterministic matrix access;
- `IProjectionCapabilityProvider.GetCapabilities()` on projection implementations.

## Targets

- JSON Schema
- JSON Editor/UI-hints mode
- EF Core metadata projection
- Power BI/TOM-like metadata projection

## Diagnostics and Compatibility

When a shape is unsupported or lossy, projections emit stable diagnostics instead of silently dropping semantics.

- JSON Schema runtime diagnostics: `STM320x`
- EF Core projection diagnostics: `EFCORE_*`
- Power BI projection diagnostics: `POWERBI_*`
- JSON editor compatibility diagnostics: `JSONSCHEMA_UI_*`

For detailed internal contracts and feature-level taxonomy, see:

- `docs/specs/type-model-projection-capabilities.md`
