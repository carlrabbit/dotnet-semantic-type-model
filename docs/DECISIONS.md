# Decisions

## Purpose

Decision documents preserve the rationale for **current, non-obvious choices** when knowing that rationale is likely to prevent a future implementation from undoing the choice accidentally.

A decision is not a specification and must not duplicate detailed behavioral rules already owned by a spec.

## Lifecycle

A decision remains in `docs/decisions/` only while its rationale still constrains current or future work.

When a decision is superseded:

1. move any still-current behavioral requirement into the relevant specification;
2. retain any still-current replacement rationale in the new/current decision;
3. summarize only architecturally significant evolution in `HISTORY.md` when useful;
4. delete the superseded decision from the working tree.

Git history retains the detailed superseded record.

## Current Decisions

### Model and semantic architecture

- [Code is the only supported canonical model source](decisions/code-first-only-model-source.md)
- [Conditional constraint literals are typed](decisions/conditional-constraint-literals-are-typed.md)
- [Envelope projection policies are target-specific](decisions/envelope-projection-policies-are-target-specific.md)
- [Evolution semantics remain projection-neutral](decisions/evolution-semantics-remain-projection-neutral.md)
- [General relationships are not canonical semantics](decisions/general-relationships-are-not-canonical-semantics.md)
- [Remove old model compatibility and transition terminology](decisions/remove-legacy-model-compatibility-and-hardened-terminology.md)
- [Replace the general description with audience-specific descriptions](decisions/replace-general-description-with-audience-specific-descriptions.md)
- [Unify the public model surface under the Model namespace](decisions/unify-public-model-surface-under-model-namespace.md)

### EF Core

- [EF Core application is generated configuration code](decisions/efcore-application-is-generated-configuration-code.md)
- [EF ownership uses target role and storage policy](decisions/ef-ownership-uses-target-role-and-storage-policy.md)
- [Real application fixtures are required for EF compatibility](decisions/real-application-fixtures-are-required-for-ef-compatibility.md)

### Other projections

- [Configuration role does not imply Options integration](decisions/configuration-role-does-not-imply-options-integration.md)
- [JSON Schema uses `x-stm` for selected semantic preservation](decisions/json-schema-uses-x-stm-for-selected-semantics.md)
- [Power BI integration stops at local metadata projection](decisions/power-bi-integration-stops-at-local-metadata-projection.md)
- [Do not generate a System.Text.Json context](decisions/remove-system-text-json-context-generation.md)

### Engineering and samples

- [Consumer-facing samples consume packages](decisions/consumer-facing-package-based-samples.md)
- [Remove stale public API baselines](decisions/remove-fake-public-api-baselines.md)
- [Use the shared order-fulfillment sample domain](decisions/shared-order-fulfillment-sample-domain.md)

## Creation Rule

Do not create a decision merely because a feature was implemented. Add a decision only when all of these are true:

- there was a meaningful choice among viable alternatives;
- the rationale is not obvious from the resulting specification;
- preserving the rationale is likely to prevent a future architectural regression.
