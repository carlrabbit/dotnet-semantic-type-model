# Decision: Remove Canonical Strong Scalar Semantics

## Status

Accepted for M0076.

## Context

Strong Scalar introduced projection-neutral nominal scalar semantics for a CLR readonly struct/record struct whose complete representation is one supported scalar `Value`.

That model initially looked attractive for strongly typed identifiers and similar values:

```text
CLR wrapper
-> canonical Strong Scalar
-> target uses underlying scalar representation
```

As the package suite expanded, the abstraction required each target/runtime integration to reconstruct primitive behavior across different CLR and target models:

- System.Text.Json required wrapper converters;
- EF Core required value conversion and exposed query-translation limits for member access through converted wrappers;
- JSON Schema and Power BI required explicit underlying-scalar classification;
- shared projection fixtures required a parallel scalar/Strong-Scalar matrix;
- TestData required a separate nominal wrapper value node;
- future dynamic-query/UI integrations would need further wrapper transparency rules.

The issue is not that strongly typed CLR identifiers are invalid application design. The issue is that making their CLR wrapper shape a canonical STM scalar contract causes projection-neutral meaning to depend on every consumer simulating primitive transparency.

## Decision

SemanticTypeModel removes Strong Scalar as a canonical semantic primitive and removes `[SemanticStrongScalar]` as a special CLR authoring mechanism.

The canonical semantic model will not define a CLR single-value wrapper as an underlying primitive merely because the wrapper has one `Value` member.

A target package may independently support compatible CLR wrapper shapes when that behavior is native or useful for the target. Such behavior:

- is target-specific;
- does not create canonical nominal scalar identity;
- does not require other packages to emulate the same wrapper behavior.

The existing EF Core single-value-wrapper convenience is retained on that basis.

A future nominal semantic scalar facility, if added, must be designed as semantic information independent of CLR wrapper transparency. It is not part of M0076.

## Rationale

SemanticTypeModel should preserve semantic meaning that is useful across representations without requiring every target to simulate arbitrary CLR behavior.

The durable distinction is:

```text
semantic representation
!=
behavioral substitutability
```

STM can describe scalar representation without promising that a CLR wrapper behaves like its primitive for:

- arbitrary LINQ operators;
- dynamic query builders;
- framework editors;
- reflection-driven UI components;
- provider-specific query translation;
- every future projection.

Removing Strong Scalar now prevents that behavioral promise from becoming architectural debt while the project has no external compatibility requirement that justifies retaining it.

## Consequences

- `TypeKind.StrongScalar`, `StrongScalarTypeDefinition`, `[SemanticStrongScalar]`, and package-specific Strong Scalar behavior are removed.
- The removal is a public breaking change and establishes a 6.0 development boundary.
- The ephemeral manifest advances to a shape with no Strong Scalar contract.
- Strong Scalar-specific diagnostics, tests, fixtures, public guidance, and current authority are removed or reshaped.
- `SemanticLiteralKind.StrongIdentifier` remains a separate existing typed-literal concept and is not removed by this decision.
- EF Core may continue supporting compatible single-value CLR wrappers as an EF-only convenience, without Strong Scalar terminology or canonical semantics.
- Strongly typed identifiers remain an application-level choice and may be supported by specialized libraries or application code without becoming STM canonical semantics.
- Future property-level or externally enriched nominal scalar semantics remain possible, but require a separate accepted contract.

## Alternatives Considered

### Continue Strong Scalar and patch each target

Rejected.

Every additional target increases wrapper-specific integration and test obligations, and some targets cannot provide full primitive behavior without invasive provider-specific machinery.

### Keep canonical Strong Scalar but remove CLR wrapper authoring

Rejected for M0076.

This would retain a canonical concept whose future authoring, identity, constraints, persistence, and projection contract would need immediate redesign. There is no current requirement that justifies preserving that abstraction.

### Replace Strong Scalar immediately with property-level nominal scalar annotations

Deferred.

Property-level nominal identity is a potentially cleaner direction because CLR representation can remain primitive, but its naming, identity sharing, constraint composition, external enrichment, code-generation relationship, and canonical contract deserve a separate design milestone.

### Add EF query rewriting to make wrappers transparent

Rejected.

That would expand STM into EF query translation/interception to compensate for a CLR representation choice. It increases coupling and does not solve equivalent problems in other dynamic-query or UI consumers.
