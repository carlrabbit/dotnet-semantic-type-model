# .NET Engineering Profile

## Purpose

This document defines the .NET 10 engineering profile for this repository.

## Stack

- .NET 10 SDK
- Microsoft Testing Platform (MTP)
- TUnit
- BenchmarkDotNet

## SDK

The .NET SDK version is pinned in `global.json`.

## Build Configuration

See `Directory.Build.props` and `Directory.Packages.props` at the repository root.

## Test Framework

TUnit is used for all unit and integration tests.

Tests run through `dotnet test` with Microsoft Testing Platform enabled via `global.json`.

Short-running tests are selected by default using `--treenode-filter "/**[(TestCategory!=Slow)&(TestCategory!=E2E)]"`.

## Benchmark Framework

BenchmarkDotNet is used for all performance benchmarks.

Benchmarks live in `benchmarks/` and run in Release configuration only.

Benchmarks are never part of `dotnet test` execution.

## .NET Rules

- Use file-scoped namespaces.
- Keep nullable reference types enabled.
- Keep implicit usings enabled.
- Treat warnings as errors.
- Use central package management through `Directory.Packages.props`; do not add package versions inline in project files.
- Use `sealed` classes unless inheritance is explicitly required.
- Use primary constructors where idiomatic.
- Use `async`/`await` for asynchronous code.
- Use `CancellationToken` parameters for cancellable operations.
- Follow standard .NET naming conventions and canonical repository terminology.

## Test Rules

- Use TUnit for unit and integration tests.
- Use Microsoft Testing Platform as the test runner.
- Short-running tests must not access the network.
- Short-running tests must not access the filesystem except for explicit test fixtures.
- Tests must be deterministic.

## Related Documents

- docs/ENGINEERING.md
- docs/engineering/command-contract.md
