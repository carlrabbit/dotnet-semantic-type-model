# Decision: Configuration is a Domain Projection, Options Registration is Target Behavior

## Status

Accepted for M0040 planning.

## Context

The repository already has projection-neutral core semantics and target-owned domain models for JSON Schema, EF Core, Power BI, and System.Text.Json.

Configuration support could be implemented incorrectly as a bag of target-specific annotations inside the core model, as a JSON Schema variant for appsettings files, or as direct generator-only code with no inspectable domain model.

## Decision

Add Configuration as a domain projection.

Core semantics continue to describe projection-neutral model meaning. Configuration-specific behavior such as section binding, named options, `ValidateDataAnnotations`, generated `IServiceCollection` helpers, and `ValidateOnStart` belongs to the Configuration domain.

Add a narrow core conditional constraint semantic for `RequiredWhen` because it represents model validity independent of a single projection and can be consumed by Configuration, JSON Schema, UI/form-oriented projections, diagnostics, query, and inspection.

## Consequences

- Configuration support gets an inspectable `ConfigurationSemanticModel`.
- Options registration can be generated or applied from the domain model.
- Core semantics are not polluted with Microsoft.Extensions.Options mechanics.
- Other projection packages must account for new core conditional constraints by consuming, preserving, explicitly ignoring, or diagnosing them.
- JSON Schema may map `RequiredWhen` to conditional schema constructs.
- EF Core and Power BI should preserve or ignore conditional metadata by default and must not generate active behavior unless explicit policies are added later.
- System.Text.Json must not silently add runtime validation from core conditional constraints unless explicit policy is added later.

## Alternatives Considered

### Treat Configuration as JSON Schema

Rejected. JSON Schema can describe appsettings shape, but it cannot represent Microsoft.Extensions.Options registration, startup validation, or generated service-registration helpers.

### Put Options Registration Metadata in Core

Rejected. Options registration is framework-specific behavior, not projection-neutral model meaning.

### Generate Options Code Directly from Attributes Without a Domain Model

Rejected. This would make behavior harder to inspect, test, diagnose, and align with existing domain derivation architecture.
