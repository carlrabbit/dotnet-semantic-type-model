# Concepts

## Canonical Model

The canonical semantic type model is the common representation used by importers, generators, transformations, and projections.

## Transformations

Transformation pipelines normalize and validate runtime models before projection.

## Projections

Projections produce target-specific output (for example JSON Schema, Power BI-like metadata, or EF Core-like metadata).

Projection capability contracts document which canonical features are directly supported, option-dependent, annotation-preserved, or unsupported per target. See [guides/projection-capabilities.md](guides/projection-capabilities.md).

The code-first JSON Schema sample (`samples/code-first-json-schema`) shows annotated C# types flowing through the packaged source generator into the canonical model and then into JSON Schema output.

## Stable Release Status

`1.0.0` is the first stable release. Documented public APIs, diagnostics, annotation keys, and package boundaries follow the compatibility policy. APIs from prerelease versions before 1.0 were not compatibility-stable. `1.1.0` corrects the System.Text.Json contract and sample validation model.

## System.Text.Json Names

Semantic member names identify model concepts. `System.Text.Json` property names identify serialization contracts and are preserved as `systemTextJson.propertyName` unless explicitly promoted. Semantic names may be used as JSON serialization names only through explicit resolver configuration.
