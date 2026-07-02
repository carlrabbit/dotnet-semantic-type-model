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
| decisions/remove-system-text-json-context-generation.md | Rationale for removing generated `JsonSerializerContext` support and using resolver-centered integration |
| decisions/consumer-facing-package-based-samples.md | Rationale for package-based public samples |
| decisions/shared-order-fulfillment-sample-domain.md | Rationale for one overlapping Order Fulfillment model across code-first samples |
| decisions/code-first-only-model-source.md | Rationale for annotated .NET code as canonical authoring source |
| decisions/ef-core-integration-stops-at-modelbuilder-configuration.md | Rationale for provider-neutral EF ModelBuilder scope |
| decisions/power-bi-integration-stops-at-local-metadata-projection.md | Rationale for local Power BI metadata scope |
| decisions/envelope-projection-policies-are-target-specific.md | Rationale for target-specific envelope representation |
| decisions/evolution-semantics-remain-projection-neutral.md | Rationale for projection-neutral evolution semantics |
| decisions/remove-legacy-model-compatibility-and-hardened-terminology.md | Rationale for removing legacy compatibility surfaces |
| decisions/unify-public-model-surface-under-model-namespace.md | Rationale for the unified model namespace |
| decisions/configuration-domain-is-options-registration-projection.md | Rationale for Configuration as a domain projection |
| decisions/configuration-registration-is-explicit-per-options-type.md | Rationale for explicit per-type Configuration registration |
| decisions/remove-fake-public-api-baselines.md | Rationale for removing stale API baseline files |
