# Concepts

## Canonical Model

The canonical semantic type model is the common representation used by importers, generators, transformations, and projections.

## Transformations

Transformation pipelines normalize and validate runtime models before projection.

## Projections

Projections produce target-specific output (for example JSON Schema, Power BI-like metadata, or EF Core-like metadata).

Projection capability contracts document which canonical features are directly supported, option-dependent, annotation-preserved, or unsupported per target.
See [guides/projection-capabilities.md](guides/projection-capabilities.md).

The code-first sample (`samples/code-first-authoring`) shows one canonical model projected to JSON Schema, JSON Editor-compatible UI hints, and EF Core configuration.

## Stable Release Status

`1.0.0` is the first stable release. Documented public APIs, diagnostics, annotation keys, and package boundaries follow the compatibility policy. APIs from prerelease versions before 1.0 were not compatibility-stable.

## System.Text.Json Names

Semantic member names identify model concepts. `System.Text.Json` property names identify serialization contracts and are preserved as `systemTextJson.propertyName` unless explicitly promoted.
