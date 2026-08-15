# Architectural History

## Purpose

This document records the **architectural evolution** of SemanticTypeModel at a level useful to current contributors and agents.

It is not a milestone archive, changelog, release history, or substitute for Git history. Detailed completed work orders and superseded designs remain available through Git.

## Foundation

The repository established a projection-neutral semantic type model, shared abstractions/core behavior, diagnostics, transformations, and initial adapter/projection experiments. Early work used JSON Schema to prove that a canonical model could sit between source and target representations.

The durable result was the separation between semantic meaning and target representation.

## Code-First Model Authority

The project later made annotated .NET code the supported authoring source. Runtime extraction and compile-time generation produce the canonical `TypeSchemaModel`; persisted snapshots preserve access to generated meaning but are not a second authoring language.

JSON Schema import ceased to be a supported canonical-model authoring path. The canonical model became the common semantic input for inspection, transformation, and target-specific derivation.

## Domain Projection Architecture

Projection packages evolved toward explicit package-owned domain models rather than ad-hoc interpretation of annotations at the final integration boundary.

The pattern became:

```text
Annotated .NET code
    -> canonical TypeSchemaModel
    -> target-specific semantic/domain model
    -> target-specific representation or runtime behavior
```

This architecture is used for JSON Schema, EF Core, Power BI, System.Text.Json, Configuration, and related integrations. The canonical model owns semantic meaning; projection packages own representation choices.

## Semantic Stabilization

The semantic vocabulary expanded and stabilized around concepts such as type roles, stable property identity, requiredness/nullability, ownership/containment, constraints, envelopes, lifecycle/evolution metadata, extension data, audience-specific descriptions, and typed conditional-constraint literals.

Testing also moved beyond hand-built semantic models where integration boundaries mattered. Real CLR extraction, generated code, provider metadata, runnable samples, package smoke, and release validation became part of the confidence model.

## EF Core Composition Reset

The most substantial recent architecture correction was the removal of runtime global `ModelBuilder` cleanup/application as the supported EF composition mechanism.

The previous runtime approach attempted to suppress convention-discovered metadata and enforce an exact semantic entity set on the global mutable EF model. That boundary could not compose safely when multiple semantic models or ordinary application entities shared one `DbContext`.

The current architecture instead uses a compile-time semantic manifest and explicit persistence-project selection. `SemanticTypeModel.EFCore.Generators` emits ordinary `IEntityTypeConfiguration<TEntity>` implementations for semantic Entities plus deterministic model registration extensions.

The durable ownership rule is:

> A generated semantic EF model configures only the CLR entities it owns. The application owns final `DbContext` composition.

Generated configuration exposes `ConfigureBeforeGenerated` and `ConfigureAfterGenerated` partial hooks. The provider-neutral relational model remains useful for derivation/inspection, while migrations, database lifecycle, provider composition, and unrelated entities stay application-owned.

## History Policy

Add to this file only when a change alters the project's architectural mental model or a major system boundary. Do not add one entry per milestone, release, bug fix, diagnostic, or mapping option.

## Post-EF consolidation

Engineering policy moved behind the stable `eng/` command API into tested repository-local .NET code. The compile-time semantic manifest remained ephemeral and gained exact producer/consumer suite-version enforcement. The unused Configuration generator placeholder and JSON Schema import compatibility path were removed, leaving code-first canonical acquisition and target-owned projection boundaries explicit.
