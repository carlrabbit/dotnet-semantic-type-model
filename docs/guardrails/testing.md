# Testing Guardrails

## Goal

Keep tests useful, deterministic, and safe to run during local and agent-assisted development.

## Test Categories

### Short-Running Tests

Short-running tests are safe for:
- local development
- AI agent validation
- pre-commit validation
- pull request validation

Short-running tests should:
- complete quickly;
- avoid external dependencies unless explicitly isolated;
- avoid sleeps and timing assumptions;
- avoid large datasets;
- avoid network dependencies;
- avoid full-system benchmark behavior.

### Long-Running Tests

Long-running tests are reserved for:
- explicit developer execution;
- scheduled GitHub workflows;
- release validation;
- benchmark validation;
- expensive integration scenarios.

Long-running tests include:
- benchmarks;
- large dataset tests;
- stress tests;
- endurance tests;
- full browser suites;
- full database matrix tests;
- performance regression tests.

## Agent Rules

AI agents must:
- create short-running tests by default;
- run only the minimal relevant short-running test set;
- not run long-running tests unless explicitly instructed;
- not create expensive tests without marking them as long-running;
- not use arbitrary sleeps to stabilize tests;
- not expand test scope beyond the task;
- prefer deterministic test synchronization over timing delays.

## Minimal Validation Rule

For implementation work, the agent should run the smallest relevant validation set that can catch local regressions.

## Authority

This document is authoritative for:
- test classification;
- agent test execution limits;
- default validation expectations.

## Document Contract

When test execution policy changes, review and update:
- AGENTS.md
- .github/copilot-instructions.md
- docs/GUARDRAILS.md
- docs/ENGINEERING.md
- docs/engineering/dotnet.md
