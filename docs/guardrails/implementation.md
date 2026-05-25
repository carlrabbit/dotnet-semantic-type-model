# Implementation Guardrails

## Goal

Keep implementation work scoped, readable, maintainable, and aligned with repository documentation.

## General Rules

- Prefer documented behavior over inferred behavior.
- Keep changes scoped to the task.
- Avoid opportunistic refactoring.
- Avoid introducing new abstractions without documented need.
- Prefer simple explicit control flow over cleverness.
- Preserve existing public contracts unless the spec changes.
- Do not silently change behavior.
- Do not introduce terminology that is absent from docs/TERMINOLOGY.md.
- Update specs when behavior changes.
- Update architecture documents when structure changes.
- Update decisions when meaningful rationale is introduced.
- Update public documentation surfaces when consumer-facing behavior changes.

## Public API Documentation

Public API surface must be documented.

Documentation should describe intent, contract, and usage constraints.

Documentation should not merely restate what the code does.

## Public Documentation Synchronization

Consumer-visible changes must be reflected in:
- `README.md`;
- `docs/PUBLIC-DOCS.md` when mapping or authority changes;
- relevant files under `public-docs/`.

## Comments

Use comments for:
- intent;
- invariants;
- non-obvious constraints;
- external contracts;
- safety assumptions.

Avoid comments that:
- narrate obvious code;
- repeat names;
- explain syntax;
- compensate for unclear structure.

## Validation

Implementation is valid when:
- relevant specs remain satisfied;
- relevant guardrails are followed;
- minimal relevant tests pass;
- public API documentation reflects intent;
- documentation synchronization obligations are satisfied;
- public documentation validation passes when consumer-facing docs changed.

## Authority

This document is authoritative for:
- general implementation constraints;
- public API documentation rules;
- public documentation synchronization rules;
- code comment intent rules.

## Document Contract

When implementation rules change, review and update:
- docs/GUARDRAILS.md
- AGENTS.md
- .github/copilot-instructions.md
- docs/PUBLIC-DOCS.md
- relevant language guardrails
