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

## Related Documents

- docs/ENGINEERING.md
- docs/guardrails/testing.md
- docs/guardrails/languages/dotnet.md
- docs/engineering/command-contract.md
