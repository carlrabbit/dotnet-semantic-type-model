# M0019: Projection Capability Matrix and Compatibility Contracts

## Goal

Define formal projection capability metadata, compatibility rules, and public documentation for how semantic type model features map to each supported projection.

The library supports multiple projections:

```text
JSON Schema
JSON Editor / UI hints
EF Core
Power BI / TOM
```

Users need to know what each projection can represent, what it cannot represent, and what diagnostics or degradation behavior to expect.

## Context

The canonical semantic type model is projection-neutral. That only works if projection boundaries are explicit.

Without a capability matrix, projection behavior can become ambiguous:

- JSON Schema can represent some structural features that EF Core cannot.
- EF Core can represent relationships that JSON Editor does not need.
- Power BI can represent Power BI relationships and measures but not arbitrary nested object graphs.
- JSON Editor can consume UI hints that should not become database metadata.

This milestone makes those boundaries explicit and testable.

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

- Existing projection packages for JSON Schema, JSON Editor, EF Core, and Power BI.
- M0015 EF Core Projection Hardening should be complete or sufficiently stable.
- M0017 JSON Schema Projection Hardening should be complete or sufficiently stable.
- M0018 Diagnostics Documentation and Analyzer Experience should be complete or sufficiently stable.

## Scope

Create a projection capability system that defines support, limitations, diagnostics, and compatibility expectations per projection.

The milestone must cover:

- capability taxonomy;
- feature matrix;
- projection capability metadata API;
- unsupported-shape diagnostics;
- degradation rules;
- compatibility contracts;
- public documentation;
- tests for capability consistency.

## Implementation Tracking

- [ ] Update milestone and index documents.
- [ ] Define capability taxonomy and feature matrix contract types.
- [ ] Implement projection capability metadata API and per-projection contracts.
- [ ] Validate unsupported/degraded projection diagnostics against the capability matrix.
- [ ] Update internal specs and public projection-capability documentation.
- [ ] Add tests for capability coverage, determinism, and projection metadata access.

## Non-Goals

- Adding a new projection.
- Making all projections support all model features.
- Hiding unsupported shapes silently.
- Replacing projection-specific specs.
- Guaranteeing permanent 1.0 compatibility before the project is ready.
- Implementing full semantic equivalence across projections.

## Capability Taxonomy

Define support levels such as:

```text
Supported
SupportedWithOptions
PartiallySupported
RepresentedAsAnnotation
Ignored
Unsupported
UnsupportedWithDiagnostic
```

Exact names may differ, but the taxonomy must distinguish:

- fully supported behavior;
- supported behavior requiring explicit options;
- degraded support;
- annotation-only preservation;
- intentional ignoring;
- unsupported shape with diagnostic.

## Feature Matrix

Create a matrix covering at least:

```text
Object type
Scalar property
Required property
Nullable property
Array
Dictionary
Enum
Union
Reference
Value object
Entity role
Primary key
Alternate key
Relationship
Computed member
Validation constraints
Display metadata
UI hints
Projection-specific annotations
Recursive type
Closed generic type
Open generic type
```

For each feature, define capability for:

```text
JSON Schema
JSON Editor
EF Core
Power BI
```

## Capability Metadata API

Add an API that allows projections to expose their capability metadata.

Conceptual shape:

```csharp
var capabilities = projection.GetCapabilities();

var support = capabilities.GetSupport(SemanticModelFeature.Relationship);
```

Exact API shape may differ, but it should allow:

- documentation generation or validation;
- tests to assert projection support;
- runtime inspection;
- stable capability identifiers.

## Projection Compatibility Contracts

Each projection must define:

- supported features;
- unsupported features;
- degradation behavior;
- required annotations;
- optional annotations;
- diagnostics emitted for unsupported shapes;
- compatibility notes for public docs.

## Diagnostics

Unsupported or degraded projection behavior must produce stable diagnostics when it affects output correctness or user expectations.

Diagnostics should include:

- feature name;
- projection name;
- model path;
- severity;
- suggested fix or alternative;
- linkable diagnostic ID if public.

## Documentation

Create or update:

```text
docs/specs/projection-capabilities.md
docs/SPECS.md
public-docs/concepts.md
public-docs/guides/projection-capabilities.md
public-docs/diagnostics.md
public-docs/packages.md
public-docs/release-notes.md
```

Projection-specific docs should link to the capability matrix.

The public documentation must explain that the canonical model is richer than any single projection.

## Tests

Add tests for:

- capability metadata completeness;
- every projection declares support level for every core feature;
- unsupported shapes emit expected diagnostics;
- supported shapes do not emit unsupported-shape diagnostics;
- documentation fixture or snapshot stays synchronized with capability metadata where practical;
- compatibility matrix remains deterministic.

## Public Documentation Impact

This milestone affects public documentation.

Review and update:

- `README.md`;
- `public-docs/concepts.md`;
- `public-docs/packages.md`;
- `public-docs/guides/projection-capabilities.md`;
- `public-docs/diagnostics.md`;
- `public-docs/release-notes.md`;
- relevant package README sources under `public-docs/nuget/`.

## Acceptance Criteria

- A projection capability taxonomy exists.
- A capability matrix covers core model features and supported projections.
- Projection capability metadata is available through code or deterministic fixtures.
- Each current projection declares its support level for each core feature.
- Unsupported shapes produce stable diagnostics where output would otherwise be misleading.
- Public docs explain projection support and limitations.
- Tests validate capability metadata completeness and deterministic output.
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
