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

## Prerelease Status

`0.1.0-alpha` is the initial prerelease. APIs, projection details, and package split may change before 1.0.

## System.Text.Json Names

Semantic member names identify model concepts. `System.Text.Json` property names identify serialization contracts and are preserved as `systemTextJson.propertyName` unless explicitly promoted.
