# .NET Guardrails

## Goal

Define .NET-specific implementation constraints for this repository.

## Rules

- Use file-scoped namespaces.
- Enable nullable reference types.
- Enable implicit usings.
- Treat warnings as errors.
- Use central package management (Directory.Packages.props).
- Do not add package versions inline in project files.
- Use `sealed` classes unless inheritance is explicitly required.
- Use primary constructors where idiomatic.
- Use `async/await` for all asynchronous code.
- Use `CancellationToken` parameters for cancellable operations.

## Test Rules

- Use TUnit for all unit and integration tests.
- Use Microsoft Testing Platform (MTP) as the test runner.
- Short-running tests must not access the network.
- Short-running tests must not access the filesystem except for test fixtures.
- Tests must be deterministic.

## Naming

- Follow standard .NET naming conventions.
- Use canonical naming from docs/TERMINOLOGY.md for domain concepts.

## Authority

This document is authoritative for:
- .NET-specific implementation rules;
- .NET test rules;
- .NET naming conventions in this repository.

## Document Contract

When .NET rules change, review and update:
- docs/GUARDRAILS.md
- AGENTS.md
- .github/copilot-instructions.md
