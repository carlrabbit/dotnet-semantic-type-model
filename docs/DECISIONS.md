# Decisions

## Purpose

Decision records capture rationale for significant choices.

Decision records are authoritative for reasoning, alternatives, and constraints. They are not authoritative for behavioral truth or architecture.

## Available Decisions

- [Separate STM-owned EF projection from CLR EF convention augmentation](decisions/separate-stm-ef-projection-from-clr-ef-convention-augmentation.md)

| Decision | Purpose |
|---|---|
| decisions/real-application-fixtures-are-required-for-ef-compatibility.md | Require anonymized application-shaped EF regression fixtures and provider-backed validation |
| decisions/ef-source-lineage-is-diagnostic-first-and-application-policy-aware.md | Diagnostic-first source lineage and derivation application policy |
| decisions/ef-convention-discovery-is-corrected-before-validation.md | Correct EF convention discovery before the final entity-set audit |
| decisions/efcore-semantic-model-is-the-ef-application-contract.md | EF semantic model application authority |
| decisions/json-schema-as-primary-dialect.md | JSON Schema dialect choice |
| decisions/remove-system-text-json-context-generation.md | Resolver-centered System.Text.Json integration |
| decisions/consumer-facing-package-based-samples.md | Package-based public samples |
| decisions/shared-order-fulfillment-sample-domain.md | Shared overlapping sample model |
| decisions/replace-general-description-with-audience-specific-descriptions.md | Replace the ambiguous description model with user and technical descriptions |
| decisions/code-first-only-model-source.md | Annotated .NET code as canonical source |
| decisions/ef-core-integration-stops-at-modelbuilder-configuration.md | Provider-neutral EF scope |
| decisions/power-bi-integration-stops-at-local-metadata-projection.md | Local Power BI metadata scope |
| decisions/envelope-projection-policies-are-target-specific.md | Target-specific envelope representation |
| decisions/evolution-semantics-remain-projection-neutral.md | Projection-neutral evolution semantics |
| decisions/remove-legacy-model-compatibility-and-hardened-terminology.md | Legacy compatibility removal |
| decisions/unify-public-model-surface-under-model-namespace.md | Unified model namespace |
| decisions/configuration-domain-is-options-registration-projection.md | Configuration as domain projection |
| decisions/configuration-registration-is-explicit-per-options-type.md | Explicit Configuration registration |
| decisions/remove-fake-public-api-baselines.md | Remove stale API baselines |
| decisions/conditional-constraint-literals-are-typed.md | Normalize conditional literals against their resolved source type |
