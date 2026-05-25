# Public API

## Scope

Public API in this repository includes consumer-visible package types and extension points in `SemanticTypeModel.*` assemblies.

## Baseline Strategy

- Document baseline references in this page and companion compatibility page.
- Run `./eng/public-api.sh` as part of release readiness.
- Treat undocumented additions/removals as release-review items.

## Current Surfaces

- JSON Schema import/export APIs
- Runtime DI APIs
- .NET extraction attributes and generator integration points
- Projection APIs for Power BI-like and EF Core-like targets
