# M0018: Diagnostics Documentation and Analyzer Experience

## Goal

Make diagnostics coherent, stable, documented, and useful across source generation, semantic annotations, .NET type extraction, model validation, and projections.

The user experience target is:

```text
invalid C# annotations or unsupported model shape
  -> clear diagnostic ID
  -> precise source/model location
  -> actionable message
  -> documented fix
```

Diagnostics should feel like a supported product surface, not an incidental implementation detail.

## Context

The library now has several diagnostic-producing layers:

- semantic annotation usage;
- source generator and analyzer behavior;
- .NET type-system extraction;
- canonical model validation;
- JSON Schema projection;
- EF Core projection;
- JSON Editor projection;
- Power BI projection;
- package smoke validation.

Without a unified diagnostic strategy, errors will be hard to interpret and hard to document.

## Implementation Router

Read only the authoritative documents needed for the focus area being implemented:

- relevant specs from `docs/specs/`;
- `docs/ENGINEERING.md` and `docs/engineering/command-contract.md` for validation-tier selection;
- `docs/PUBLIC-DOCS.md` and affected `public-docs/` pages only when the change is consumer-facing;
- architecture or decision records only when the change alters structure or rationale.

Historical research guide copies are non-authoritative references and are not required milestone reading.

## Focus Areas

Use the milestone scope to choose one or more focused implementation slices instead of treating the whole milestone as a single work item:

| Focus area | Validation tier | Documentation impact |
|---|---|---|
| Behavior or API implementation | Tier 1 during development, Tier 2 before completion | Direct when behavior is consumer-facing; otherwise update specs only when contracts change. |
| Tests and diagnostics | Tier 1 for the affected test project or diagnostic filter, Tier 2 before completion | Direct for public diagnostics; deferred only when examples require a later feature slice. |
| Public documentation, samples, or release readiness | Tier 0 for documentation checks, Tier 3 for package/release readiness | Direct for changed public docs and package README sources; record deferred docs explicitly. |

## Validation Tier

- Default implementation focus areas: Tier 1 during the inner loop, then Tier 2 before completion.
- Documentation-only focus areas: Tier 0 plus `./eng/public-docs.sh` when public documentation changes.
- Packaging or release focus areas: Tier 3 or Tier 4 as described by the release-readiness documents.

## Dependencies

- M0014 Semantic Type Annotation Usability.
- M0015 EF Core Projection Hardening.
- M0017 JSON Schema Projection Hardening is useful but not strictly required.

## Scope

Define and implement a diagnostics framework and documentation model that covers current packages and projections.

The milestone must cover:

- diagnostic ID scheme;
- diagnostic categories;
- severity policy;
- source-generator diagnostics;
- analyzer diagnostics;
- model validation diagnostics;
- projection diagnostics;
- source location mapping;
- model path mapping;
- diagnostic descriptors;
- public diagnostic reference pages;
- suppression guidance;
- invalid/fixed examples;
- tests for diagnostic stability.

## Non-Goals

- Building a full IDE extension.
- Creating custom UI for diagnostics.
- Rewriting projection implementations solely for diagnostic aesthetics.
- Adding diagnostics for speculative future projections.
- Guaranteeing 1.0 diagnostic compatibility before the project is ready.
- Emitting diagnostics for every warning-like condition where documentation is sufficient.

## Diagnostic ID Scheme

Define a stable ID scheme.

Recommended prefix:

```text
STM
```

Recommended category ranges:

```text
STM1xxx  Semantic model and validation
STM2xxx  .NET type extraction
STM3xxx  Source generator and analyzer
STM4xxx  Annotation misuse
STM5xxx  JSON Schema projection
STM6xxx  EF Core projection
STM7xxx  JSON Editor projection
STM8xxx  Power BI projection
STM9xxx  Packaging, samples, and integration validation
```

Exact ranges may differ, but they must be documented and consistently applied.

## Diagnostic Model

Each diagnostic must define:

- ID;
- title;
- message format;
- category;
- severity;
- default enabled state;
- source location when available;
- model path when available;
- invalid example;
- fixed example;
- related documentation.

The internal diagnostic representation should be reusable across runtime model validation and projection validation.

## Severity Policy

Define when to use:

```text
Error
Warning
Info
Hidden
```

Rules must clarify:

- when generation should fail;
- when projection should continue with degraded output;
- when runtime APIs should return diagnostics instead of throwing;
- when exceptions are appropriate.

## Source and Model Locations

Diagnostics should provide the best available location:

- C# symbol location for source generator/analyzer diagnostics;
- member/property location for attribute misuse;
- model path for runtime validation and projection diagnostics;
- projection output path where useful.

Model paths must remain stable and documented.

Example:

```text
/types/Customer/properties/email
```

## Analyzer and Source Generator Experience

For compile-time diagnostics:

- use `DiagnosticDescriptor` consistently;
- messages must be actionable;
- diagnostics should point to the most relevant attribute/type/member;
- conflicting annotations should identify both conflict source and affected semantic target where practical;
- invalid examples should be covered in tests.

## Public Diagnostics Reference

Create or update:

```text
public-docs/diagnostics.md
public-docs/diagnostics/
```

Each public diagnostic page should include:

- severity;
- message;
- cause;
- invalid example;
- fixed example;
- related docs;
- package/source where emitted.

Do not create README files under `public-docs/diagnostics/`.

## Documentation

Create or update:

```text
docs/specs/diagnostics.md
docs/ENGINEERING.md
public-docs/diagnostics.md
public-docs/diagnostics/
public-docs/release-notes.md
```

The specification should define the diagnostic contract. Public docs should explain diagnostics from the user perspective.

## Tests

Add tests for:

- diagnostic ID uniqueness;
- descriptor metadata stability;
- source-generator diagnostic location;
- semantic annotation misuse;
- model path diagnostics;
- JSON Schema projection diagnostics;
- EF Core projection diagnostics;
- public docs coverage for public diagnostics;
- invalid/fixed examples where practical.

Tests should prevent accidental diagnostic ID reuse.

## Public Documentation Impact

This milestone affects public documentation.

Review and update:

- `README.md`;
- `public-docs/diagnostics.md`;
- `public-docs/diagnostics/`;
- `public-docs/getting-started.md` if diagnostics affect first-use experience;
- `public-docs/release-notes.md`;
- relevant package README sources under `public-docs/nuget/`.

## Acceptance Criteria

- A documented diagnostic ID scheme exists.
- Diagnostic categories and severity policy are implemented.
- Analyzer/source-generator diagnostics use stable descriptors.
- Runtime/projection diagnostics include stable model paths where available.
- Public diagnostics reference pages exist for public diagnostics.
- Tests prevent duplicate diagnostic IDs.
- Tests cover representative diagnostics from annotations, generator, model validation, JSON Schema, and EF Core.
- Public docs explain how users should interpret and fix diagnostics.
- The milestone is indexed from `docs/MILESTONES.md`.

## Authority

This document is authoritative for:

- milestone scope;
- milestone deliverables;
- milestone implementation sequence;
- milestone acceptance criteria.

This document is not authoritative for:

- permanent feature behavior after implementation;
- architecture decisions beyond this milestone;
- projection semantics outside the milestone scope.

## Document Contract

### Related Documents

- `docs/TERMINOLOGY.md`
- `docs/MILESTONES.md`
- `docs/SPECS.md`
- `docs/ENGINEERING.md`
- `docs/PUBLIC-DOCS.md`
- `docs/WORKFLOWS.md`

### Must Be Updated Together

When this milestone is added or materially changed, review and update:

- `docs/MILESTONES.md`;
- related specification documents under `docs/specs/`;
- related public documentation under `public-docs/`;
- related engineering or workflow documents if validation commands change;
- release notes when public behavior changes.

