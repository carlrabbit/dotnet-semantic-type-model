# M0017: JSON Schema Projection Hardening for Code-First Models

## Status

Active implementation milestone.

## Goal

Harden JSON Schema Draft 2020-12 projection for models created from annotated .NET code and source generation.

The target flow is:

```text
annotated C# types
  -> TypeSchemaModel
  -> JSON Schema Draft 2020-12
```

The exported schema should be deterministic, inspectable, and aligned with the semantic model rather than with incidental CLR implementation details.

## Context

The runtime JSON Schema import/export path already provides a baseline. Code-first authoring introduces different edge cases: nullable reference types, C# `required` members, `init` setters, records, enums, collections, dictionaries, generics, cycles, naming policies, and attribute-driven constraints.

This milestone ensures that JSON Schema output remains a high-confidence projection for code-first users.

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
- M0016 End-to-End Code-First Schema Authoring Samples is useful but not strictly required.
- Existing JSON Schema projection package must build.

## Engineering and Release Readiness

Required validation commands:

```sh
./eng/check.sh
./eng/release-check.sh 0.1.0-alpha
```

If `release-check` is not appropriate during implementation, run the minimal relevant subset and explain why in the completion report.

## Scope

Harden the JSON Schema projection for code-first semantic models.

The milestone must cover:

- required versus optional properties;
- nullable versus non-nullable values;
- C# `required` and init-only members as represented by the canonical model;
- records and classes;
- scalar kind mapping;
- string formats;
- numeric constraints;
- string constraints;
- array and collection mapping;
- dictionary mapping;
- enum value mapping and enum storage policy;
- object references and `$defs`;
- stable definition naming;
- cycles and recursive references;
- annotation preservation where supported;
- unsupported semantic shapes and diagnostics;
- projection options for deterministic output.

## Non-Goals

- Changing the internal canonical type model unless a bug is discovered.
- Runtime JSON Schema import hardening unrelated to code-first output.
- OpenAPI generation.
- TypeScript generation.
- Direct source-generator-to-JSON-Schema output.
- Supporting every JSON Schema keyword.
- Creating a JSON Schema validator.

## Required Projection Rules

### Required and Nullable Semantics

Projection must distinguish:

```text
property absent
property present with null
property present with non-null value
```

The implementation must define how the canonical model maps to:

- JSON Schema `required`;
- union with `null`;
- omitted properties;
- nullable array items;
- nullable dictionary values.

### Scalar Mapping

Define and test mappings for at least:

```text
Boolean
String
Integer
Number
Decimal
Date
Time
DateTime
DateTimeOffset
Duration
Guid
Binary
Json
Unknown
```

Unsupported or ambiguous scalar kinds must produce diagnostics or documented fallback behavior.

### Constraint Mapping

Map canonical constraints where supported:

- `minLength`;
- `maxLength`;
- `pattern`;
- `minimum`;
- `maximum`;
- `exclusiveMinimum`;
- `exclusiveMaximum`;
- `multipleOf`;
- array min/max items;
- object required properties.

### Enum Mapping

Support enum output for:

- string enum values;
- numeric enum values where configured;
- display metadata as annotations where supported;
- unsupported enum shapes with diagnostics.

### References and `$defs`

Projection must produce stable `$defs` names.

Rules must cover:

- duplicate type display names;
- nested types;
- generic closed types;
- recursive types;
- references to shared value objects;
- deterministic ordering.

### Collections and Dictionaries

Projection must define mapping for:

- arrays;
- `IReadOnlyList<T>` / list-like canonical arrays;
- dictionaries with string keys;
- unsupported dictionary key shapes;
- nullable item/value behavior.

### Unions and Composition

Projection must define supported boundaries for:

- `oneOf`;
- `anyOf`;
- nullable as composition;
- unsupported union semantics;
- discriminator metadata if present.

### Annotation Handling

Projection must preserve or emit supported annotation namespaces intentionally.

Projection must not leak internal-only annotations unless explicitly configured.

## Projection Options

Add or refine JSON Schema projection options where needed:

```csharp
var schema = JsonSchemaExporter.Export(
    model,
    options =>
    {
        options.DefinitionNaming = JsonSchemaDefinitionNamingPolicy.Stable;
        options.IncludeAnnotations = true;
    });
```

Exact API names may differ, but options should support deterministic, user-configurable output.

## Diagnostics Requirements

Add stable diagnostics for at least:

- unsupported scalar kind;
- unsupported dictionary key;
- unsupported union semantics;
- duplicate `$defs` name after normalization;
- invalid nullability/required combination;
- unsupported annotation value;
- recursive shape that cannot be projected;
- conflicting projection annotations.

Diagnostics must include:

- stable code;
- severity;
- message;
- model path;
- source location if available through code-first extraction;
- suggested fix where practical.

## Tests

Add golden tests for code-first-originating models covering:

- simple record/class;
- required and optional properties;
- nullable and non-nullable properties;
- string constraints;
- numeric constraints;
- arrays;
- dictionaries;
- enums;
- value objects;
- entity references;
- cycles;
- closed generic type;
- naming collisions;
- unsupported shape diagnostics;
- deterministic output.

Golden outputs must be stable across runs.

## Documentation

Create or update:

```text
docs/specs/json-schema-projection.md
public-docs/guides/json-schema-export.md
public-docs/packages.md
public-docs/diagnostics.md
public-docs/release-notes.md
```

Document:

- code-first JSON Schema export;
- supported mappings;
- unsupported mappings;
- common diagnostics;
- versioning/stability expectations.

## Public Documentation Impact

This milestone affects public docs because JSON Schema output is a user-facing behavior.

Review and update:

- `README.md`;
- `public-docs/getting-started.md`;
- `public-docs/concepts.md`;
- `public-docs/packages.md`;
- `public-docs/guides/json-schema-export.md`;
- `public-docs/diagnostics.md`;
- `public-docs/release-notes.md`;
- relevant package README sources under `public-docs/nuget/`.

## Acceptance Criteria

- Code-first semantic models export deterministic Draft 2020-12 JSON Schema.
- Required and nullable semantics are explicitly tested.
- Collections, dictionaries, enums, value objects, references, and cycles are covered.
- Unsupported shapes produce stable diagnostics.
- `$defs` naming is deterministic.
- Projection options support stable configurable output.
- Golden tests cover representative code-first scenarios.
- Public docs describe supported JSON Schema mappings and limitations.
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

## Completion Report

When closing this milestone, report:

- implemented JSON Schema projection hardening changes for code-first models;
- diagnostics added or refined and their stability expectations;
- test coverage added for deterministic behavior and unsupported shapes;
- documentation surfaces updated under `docs/` and `public-docs/`;
- validation commands run;
- follow-up issues required.
