# Architecture

## Purpose

Architecture documentation describes the stable structural boundaries of SemanticTypeModel: authoring source, canonical model, package responsibilities, transformation/projection flow, compile-time/runtime boundaries, and dependency direction.

Architecture does not duplicate detailed behavioral specifications or decision rationale.

## Current Architecture

| Document | Authority |
|---|---|
| [Code-First Domain Projection Pipeline](architecture/code-first-domain-projection-pipeline.md) | Authoritative system architecture for code-first model acquisition, canonical semantics, domain projections, compile-time generation, and application composition. |

## Reading Rule

Read architecture when changing how major pieces fit together. For exact behavior, read `SPECS.md`. For why a non-obvious choice exists, read `DECISIONS.md`.

Legacy architecture notes are not retained in the working tree after their durable content is incorporated into the current architecture. Git history contains their detailed evolution.
