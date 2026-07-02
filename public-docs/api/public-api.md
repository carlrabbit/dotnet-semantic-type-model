# Public API

## Scope

Public API in this repository includes consumer-visible package types and extension points in `SemanticTypeModel.*` assemblies.

## Review Strategy

- Document public API expectations in this page and the companion compatibility page.
- Review compatibility through package smoke tests, runnable samples, public documentation, release notes, compatibility documentation, and human review.
- Treat undocumented additions/removals as release-review items.
- The repository does not currently maintain text API baseline files as release gates.

## Current Surfaces

- JSON Schema import/export APIs
- Runtime DI APIs
- .NET extraction attributes and generator integration points
- Configuration domain model and options registration projection APIs
- Projection APIs for Power BI-like and EF Core-like targets


## Audience-specific description APIs

The current public model surface exposes `UserDescription` and `TechnicalDescription` on semantic types and properties. .NET extraction uses `SemanticUserDescriptionAttribute`, `SemanticTechnicalDescriptionAttribute`, XML `<summary>` as technical fallback, and `RequireTechnicalDescription` for validation. The removed general description API is documented as a 2.4.0 breaking compatibility boundary rather than a supported current surface.
