# M0012 - Documentation, Samples, Release Readiness, and First End-to-End Scenarios

## Purpose

Align the repository with Project Setup Guide V5 and Engineering Guide V4, then introduce the first consumer-facing public documentation, runnable samples, and release-readiness command surface.

## Delivered Scope

- Research guide alignment to:
  - `docs/research/project-setup-guide-v5.md`
  - `docs/research/engineering-guide-v4.md`
- Public documentation authority and source structure:
  - `docs/PUBLIC-DOCS.md`
  - `public-docs/`
- User-first root `README.md` with install, quick start, packages, concepts, samples, public docs, and contributor docs.
- Documentation governance updates across terminology, guardrails, engineering docs, TBPs, workflows, and issue templates.
- New engineering commands:
  - `./eng/public-docs.sh`
  - `./eng/package-smoke.sh <version>`
  - `./eng/public-api.sh`
  - `./eng/release-check.sh <version>`
  - `./eng/samples.sh`
- Runnable samples for:
  - JSON Schema roundtrip;
  - .NET generator to JSON Schema;
  - runtime DI usage;
  - Power BI projection;
  - EF Core projection.

## End-to-End Scenario Coverage

M0012 covers first end-to-end consumer scenarios through runnable samples and release engineering commands:

1. runtime JSON Schema import/export;
2. compile-time generated model to JSON Schema;
3. runtime DI model consumption with transformations and projection;
4. local package smoke flow (when local packages are produced).

## Non-goals Preserved

- no NuGet publishing;
- no website deployment;
- no Power BI service deployment;
- no database-server integration work;
- no benchmark expansion;
- no full generated API reference site;
- no analyzer/fixer package expansion.
