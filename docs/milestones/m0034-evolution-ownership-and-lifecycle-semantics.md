# M0034: Evolution, Ownership, and Lifecycle Semantics

## Status

Completed.

## Completion Note

M0037 synchronized public documentation and release notes for ownership, versioning, temporal-validity, lifecycle-state, and extension-data semantics after repository validation passed with `./eng/release-check.sh 2.1.0`.

## Maturity Mode

Post-2.0 core semantic hardening for public package behavior.

The repository already supports a code-first canonical semantic model, core semantic vocabulary, envelope semantics, JSON Schema projection, EF Core projection, Power BI projection, and deterministic diagnostics/inspection. M0034 adds a small set of widely useful projection-neutral semantics that describe model evolution, lifecycle state, temporal validity, ownership containment, and extension-data compatibility behavior.

## Task Mode

Milestone implementation routing and behavioral specification.

This package defines implementation-ready planning and authority for new semantics and their projection implications. It does not add source code, generated code, tests, workflow YAML, TBPs, issue templates, or broad documentation synchronization edits.

## Goal

After M0034, consumers can model common cross-domain concepts without target-specific annotations:

```text
Ownership / owned object containment
Versioned model data
Revision and version members
Current version markers
Temporal validity intervals
Lifecycle state
Extension-data / unknown member compatibility bags
```

Each semantic must have deterministic projection behavior for JSON Schema, EF Core, and Power BI, with safe opinionated defaults and explicit policy hooks for non-default behavior.

## Non-Goals

M0034 does not generate business workflow behavior.

It must not silently add:

```text
EF Core global query filters
SQL Server temporal-table configuration
automatic EF primary-key replacement
automatic Power BI DAX measures
Power BI service publishing behavior
PBIX generation
JSON Schema validation runtime behavior
provider-specific JSON indexes
provider-specific SQL tuning
```

The milestone defines model meaning, projection metadata, safe target defaults, diagnostics, and explicit policy hooks.

## Required Authority

Implementation agents must read only the documents relevant to their focus area.

Always read:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/SPECS.md
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-core.md
docs/specs/type-model-annotations.md
docs/specs/type-model-transformation-and-domain-derivation.md
```

Read when adding or changing .NET attributes or generator extraction:

```text
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
```

Read when changing projection behavior:

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/envelope-projection-policies.md
docs/specs/type-model-projection-capabilities.md
```

Read when adding diagnostics or public consumer behavior:

```text
docs/specs/diagnostics.md
docs/PUBLIC-DOCS.md
public-docs/guides/core-semantics.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/json-schema.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Add canonical semantics for:
  - `Ownership`;
  - `OwnedObject` / owned reference property;
  - `OwnedCollection`;
  - `Versioned`;
  - `Version`;
  - `Revision`;
  - `CurrentVersion`;
  - `TemporalValidity`;
  - `ValidFrom`;
  - `ValidTo`;
  - `LifecycleState`;
  - `ExtensionData`.
- Add canonical annotation keys or structured model members for these semantics.
- Add .NET attribute or fluent authoring support for these semantics.
- Add deterministic transformations that normalize attribute/configuration intent into canonical model semantics.
- Add target projection defaults for JSON Schema, EF Core, and Power BI.
- Add diagnostics for unsupported, ambiguous, conflicting, or unsafe projections.
- Add deterministic inspection output for the new semantics.
- Add focused tests for extraction, transformations, projection policy, diagnostics, and inspection.

### Out of Scope

- Workflow engine behavior.
- Lifecycle transition execution.
- Automatic query filters.
- Automatic current-record filtering.
- Provider-specific temporal-table configuration.
- Provider-specific JSON/index tuning.
- Automatic DAX measure generation.
- Power BI Service/Fabric integration.
- JSON Schema validation engine behavior.
- Migration generation or database lifecycle work.
- Broad documentation synchronization.

## Semantic Boundaries

### Ownership vs Envelope

Ownership and envelope are intentionally separate semantics.

```text
Envelope:
  identifies a wrapper boundary, distinguished payload, and envelope metadata.

Ownership:
  identifies lifecycle containment and composition under an owner.
```

Projection mechanisms may overlap. EF Core may use `OwnsOne`, `OwnsMany`, JSON columns, or table splitting for either scenario, but the semantics are not the same.

### ExtensionData vs Annotation

Extension data and annotation are intentionally separate semantics.

```text
Annotation:
  metadata about the model.

ExtensionData:
  instance-level unknown, unmodeled, forward-compatible, or externally supplied data carried by the object.
```

Projection packages must not treat extension-data bags as ordinary metadata annotations.

## Focus Areas

### Focus Area 1 — Core Semantics and Vocabulary

#### Intent

Add the M0034 semantics as projection-neutral model meaning.

#### Required Authority

```text
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/core-semantic-vocabulary.md
docs/TERMINOLOGY.md
docs/specs/type-model-core.md
docs/specs/type-model-annotations.md
```

#### Implementation Requirements

- Add canonical model support or canonical annotations for M0034 semantics.
- Preserve model meaning independently of JSON Schema, EF Core, Power BI, or System.Text.Json representation.
- Ensure all new terms are present in `docs/TERMINOLOGY.md` before broad use.
- Do not overload envelope semantics to mean ownership.
- Do not overload annotation semantics to mean extension data.

#### Acceptance Criteria

- The canonical model can represent each M0034 semantic.
- The model can be queried and inspected for each semantic.
- Invalid combinations emit diagnostics instead of silent fallback.

### Focus Area 2 — Ownership Semantics

#### Intent

Represent lifecycle containment and composition separately from envelope payload semantics.

#### Required Authority

```text
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/envelope-projection-policies.md
docs/specs/type-model-ef-core-projection.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-powerbi-tom-projection.md
```

#### Implementation Requirements

- Support owned reference properties.
- Support owned collection properties.
- Support explicit ownership kind and optional projection policy.
- Default projection behavior:
  - JSON Schema: owned object as structured schema or `$defs` reference according to configured object export policy;
  - EF Core: `OwnsOne` same-table for owned references by default;
  - EF Core: owned collections require explicit policy unless a package default is configured;
  - Power BI: flatten simple owned references by default; owned collections require explicit table/ignore policy.
- Detect circular ownership, shared ownership conflicts, and owned object used as independent entity without explicit policy.

#### Acceptance Criteria

- Owned reference and owned collection semantics are represented deterministically.
- Ownership does not imply envelope semantics.
- EF Core, JSON Schema, and Power BI derive safe default projection behavior or diagnostics.

### Focus Area 3 — Versioning and Revision Semantics

#### Intent

Represent model evolution, revision identity, and version markers without forcing storage keys or business behavior.

#### Required Authority

```text
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-ef-core-projection.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-powerbi-tom-projection.md
```

#### Implementation Requirements

- Support `Versioned` on types.
- Support `Version`, `Revision`, and `CurrentVersion` on members.
- Preserve identity vs revision identity distinction.
- EF Core must not change primary keys by default.
- EF Core may derive provider-neutral required columns and optional alternate key/index metadata when explicitly configured.
- JSON Schema must expose version/revision semantics as deterministic schema metadata and normal property schema.
- Power BI must expose version/revision/current markers as analytical columns.

#### Acceptance Criteria

- Revision/version fields are not treated as generic unrelated scalars when semantics are declared.
- Versioned types remain projection-neutral.
- Target packages expose diagnostics for unsupported or ambiguous version policies.

### Focus Area 4 — Temporal Validity Semantics

#### Intent

Represent effective dating and validity intervals without automatically enabling provider-specific temporal-table or query-filter behavior.

#### Required Authority

```text
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/type-model-ef-core-projection.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-powerbi-tom-projection.md
```

#### Implementation Requirements

- Support `TemporalValidity` on a type or property group.
- Support `ValidFrom` and `ValidTo` members.
- Validate that validity endpoints use temporal-compatible types.
- Default projection behavior:
  - JSON Schema: date-time property schemas and semantic annotations;
  - EF Core: scalar columns with optional provider-neutral index policy only when configured;
  - Power BI: timeline/effective-date columns.
- Do not add global query filters by default.
- Do not enable SQL Server temporal tables by default.

#### Acceptance Criteria

- Validity endpoints are identifiable in query/inspection output.
- Missing/invalid endpoints emit diagnostics.
- Target projections do not silently claim unsupported interval constraints.

### Focus Area 5 — Lifecycle State Semantics

#### Intent

Represent status/lifecycle state fields for contracts, storage, and analytical grouping without generating workflow behavior.

#### Required Authority

```text
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
```

#### Implementation Requirements

- Support `LifecycleState` on enum/scalar state members.
- JSON Schema derives enum/state schema and semantic metadata.
- EF Core maps lifecycle state as a scalar/enum column according to existing enum policy.
- Power BI marks lifecycle state as categorical/slicer-suitable metadata where representable.
- Do not generate transition rules or workflow behavior.

#### Acceptance Criteria

- Lifecycle state is visible in derived domain semantic models.
- Invalid state member types or conflicting role declarations emit diagnostics.

### Focus Area 6 — Extension Data Compatibility Bags

#### Intent

Represent unknown/unmodeled forward-compatible instance data that may be preserved, ignored, summarized, or projected by target policy.

#### Required Authority

```text
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/system-text-json-contract-integration.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
```

#### Implementation Requirements

- Support `ExtensionData` on dictionary-like property shapes.
- Key type must be string-like unless explicitly configured.
- Value type must be JSON-compatible, scalar, object, or target-diagnosed.
- System.Text.Json `[JsonExtensionData]` may normalize to `ExtensionData` when the STJ integration package or configured pipeline is active.
- Default projection behavior:
  - JSON Schema: use `additionalProperties` or `unevaluatedProperties` policy; do not expose the bag as a normal property by default;
  - EF Core: ignore by default unless configured for serialized JSON, hash, count, or boolean summary;
  - Power BI: ignore by default unless configured for `HasExtensionData`, count, hash, or known-key summary.
- Do not flatten arbitrary unknown keys into stable columns by default.

#### Acceptance Criteria

- Extension data is distinct from annotation metadata.
- Default projections are safe and deterministic.
- Opt-in summary projections are represented in domain semantic models.
- Invalid bag shapes and unsupported flattening requests emit diagnostics.

### Focus Area 7 — Diagnostics and Inspection

#### Intent

Make all M0034 semantics visible and diagnosable.

#### Required Authority

```text
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/diagnostics.md
docs/specs/type-model-query-and-inspection.md
```

#### Implementation Requirements

- Add stable diagnostic IDs for new public diagnostics.
- Add deterministic inspection output for ownership, versioning, temporal validity, lifecycle state, and extension data.
- Diagnostics must include model path, semantic name, projection target when applicable, and related paths where available.

#### Acceptance Criteria

- Diagnostics are queryable and stable.
- Inspection output is deterministic and snapshot-testable.
- No semantic ambiguity is silently dropped.

## Validation Plan

Use the smallest validation tier that can catch the expected regression.

### Tier 0

Use for documentation-only changes:

```sh
./eng/public-docs.sh
```

Use format/static validation for touched files when applicable.

### Tier 1

Use focused validation for affected implementation areas:

```sh
./eng/test-project.sh <core-test-project>
./eng/test-project.sh <dotnet-extraction-test-project>
./eng/test-project.sh <json-schema-test-project>
./eng/test-project.sh <ef-core-test-project>
./eng/test-project.sh <powerbi-test-project>
./eng/test-filter.sh <evolution-or-ownership-filter>
./eng/check-affected.sh src/SemanticTypeModel.Core src/SemanticTypeModel.DotNet src/SemanticTypeModel.JsonSchema src/SemanticTypeModel.EFCore src/SemanticTypeModel.PowerBI tests
```

Use actual repository project names after inspecting the solution.

### Tier 2

Run before completing implementation work when code changes:

```sh
./eng/check.sh
```

### Tier 3

Run only if package layout, public API compatibility documentation, samples, package README sources, or release-candidate behavior changes:

```sh
./eng/package.sh <version>
./eng/package-smoke.sh <version>
./eng/public-docs.sh
./eng/public-docs.sh
./eng/samples.sh
```

M0034 is not a publish/release milestone. Do not require Tier 4.

## Direct Documentation Impact

The implementation should directly update:

```text
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/TERMINOLOGY.md
```

Update these only when implementation changes their behavior directly:

```text
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-annotations.md
docs/specs/type-model-core.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-contract-integration.md
docs/specs/diagnostics.md
```

## Deferred Documentation Synchronization

Leave the broad synchronization pass for later:

```text
docs/SPECS.md
docs/MILESTONES.md
README.md
public-docs/guides/core-semantics.md
public-docs/guides/json-schema.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/system-text-json.md
public-docs/nuget/*.md
public-docs/samples.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md if public diagnostics are added
public-docs/release-notes.md
```

## Required Acceptance Criteria

M0034 is complete when:

- all M0034 terms are present in `docs/TERMINOLOGY.md`;
- `docs/specs/evolution-ownership-and-lifecycle-semantics.md` exists and is authoritative;
- ownership semantics are distinct from envelope semantics;
- extension data semantics are distinct from annotations;
- version/revision/current-version semantics are represented and inspectable;
- temporal validity semantics are represented and inspectable;
- lifecycle state semantics are represented and inspectable;
- JSON Schema, EF Core, and Power BI derive safe default behavior or diagnostics;
- System.Text.Json `[JsonExtensionData]` normalization is handled or explicitly diagnosed when relevant;
- unsupported target behavior emits diagnostics instead of silent omission;
- Tier 2 validation passes for implementation changes, or exact lower-tier validation and environment limitations are documented;
- no TBPs, issue templates, workflow YAML, non-root README files, or generated source files are introduced by this planning package.
