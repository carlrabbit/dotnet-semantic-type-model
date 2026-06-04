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
