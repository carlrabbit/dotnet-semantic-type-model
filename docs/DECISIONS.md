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
| decisions/envelope-projection-policies-are-target-specific.md | Rationale for keeping envelope semantics projection-neutral while target packages own JSON Schema, EF Core, and Power BI representation policies |
| decisions/evolution-semantics-remain-projection-neutral.md | Rationale for keeping ownership, versioning, temporal validity, lifecycle state, and extension-data semantics projection-neutral |
| decisions/remove-legacy-model-compatibility-and-hardened-terminology.md | Rationale for removing old model compatibility APIs, stale transition terminology, and System.Text.Json ad hoc model access |
| decisions/unify-public-model-surface-under-model-namespace.md | Rationale for making `SemanticTypeModel.Abstractions.Model` the sole public model namespace and removing the old shape graph |
| decisions/configuration-domain-is-options-registration-projection.md | Rationale for adding Configuration as a domain projection while keeping Options registration behavior outside core semantics |
| decisions/remove-fake-public-api-baselines.md | Rationale for removing stale public API baseline files and script-only baseline checks |
