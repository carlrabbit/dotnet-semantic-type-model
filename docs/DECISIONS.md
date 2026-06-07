# Decisions

## Purpose

Decision records capture rationale for significant choices.

Decision records are authoritative for:
- the reasoning behind choices;
- alternatives considered;
- constraints that drove decisions.

Decision records are not authoritative for:
- behavioral truth (see docs/SPECS.md);
- system structure (see docs/ARCHITECTURE.md).

## Available Decisions

| Decision | Purpose |
|---|---|
| decisions/json-schema-as-primary-dialect.md | Rationale for choosing JSON Schema Draft 2020-12 as the first adapter dialect |
| decisions/remove-system-text-json-context-generation.md | Rationale for removing SemanticTypeModel-generated `JsonSerializerContext` support and using resolver-centered System.Text.Json integration |
| decisions/consumer-facing-package-based-samples.md | Rationale for treating public samples as package-based consumer examples rather than internal development harnesses |
| decisions/code-first-only-model-source.md | Rationale for making annotated .NET code the supported canonical model authoring source |
| decisions/ef-core-integration-stops-at-modelbuilder-configuration.md | Rationale for limiting EF Core integration to domain semantic model derivation and provider-neutral `ModelBuilder` configuration |
| decisions/power-bi-integration-stops-at-local-metadata-projection.md | Rationale for limiting Power BI integration to domain semantic model derivation and deterministic local metadata output |
