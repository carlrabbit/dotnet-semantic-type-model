# M0022: 1.0 Public API, Compatibility, Documentation, Samples, and Release Readiness

## Status

Implemented for 1.0 release-candidate readiness.

## Goal

Prepare the repository for a stable `1.0.0` release by canonicalizing the public API surface, removing pre-1.0 legacy compatibility code, stabilizing package boundaries and diagnostics, completing public documentation and samples, and ensuring release validation proves the intended 1.0 consumer experience.

This milestone is the 1.0 readiness gate. It is not the final release execution milestone.

The governing rule is:

```text
Every remaining public API must either be part of the intended 1.0 contract or explicitly marked as experimental according to repository policy.
```

## Main-Branch Observations to Account For

The implementation must check the current `main` branch before changing files. At the time this milestone was drafted, `main` showed these relevant conditions:

- `docs/MILESTONES.md` listed milestones through M0021 and did not yet list M0022 or M0023.
- `README.md` listed `SemanticTypeModel.JsonEditor` in install commands and the prerelease package list.
- `public-docs/packages.md` listed `SemanticTypeModel.JsonEditor` in the initial prerelease package set and described it as produced from `src/SemanticTypeModel.DependencyInjection`.
- `public-docs/release-notes.md` listed `SemanticTypeModel.JsonEditor` as a `0.1.0-alpha` package.
- `public-docs/nuget/SemanticTypeModel.DependencyInjection.md` existed as a NuGet README source.
- `public-docs/guides/projection-capabilities.md` and `docs/specs/type-model-projection-capabilities.md` treated JSON Editor as a projection target or mode.
- `public-docs/guides/power-bi-projection.md` documented `PowerBiProjectionModel` as the preferred Power BI output but also mentioned `PowerBiModelProjection` returning `PowerBiProjectionModel` for compatibility.
- Repository search found legacy Power BI names such as `PowerBiProjectionModel` and `PowerBiModelProjection` in source, tests, docs, and samples.

These observations are starting points, not a substitute for implementation-time inspection. The implementation must re-check current `main` and avoid obsolete changes if the repository has already moved on.

## Scope

This milestone covers:

- public API canonicalization across all packages;
- removal or internalization of pre-1.0 legacy public types;
- package list and package-boundary cleanup;
- removal of `SemanticTypeModel.JsonEditor` as a package concept unless a real package is introduced;
- documentation of JSON Editor compatibility as a feature/mode of `SemanticTypeModel.JsonSchema`;
- stabilization of diagnostics, annotation keys, model paths, projection option APIs, and package dependencies;
- public documentation completion for 1.0 readiness;
- sample completion and validation;
- package smoke-test alignment with the intended 1.0 package set;
- release-check readiness for `1.0.0-rc.1`.

## Non-Goals

This milestone does not cover:

- publishing `1.0.0`;
- publishing `1.0.0-rc.1`, except as a dry-run or validation target if useful;
- adding new projection families;
- adding new product features not needed for 1.0 stabilization;
- preserving pre-1.0 compatibility shims for hypothetical users;
- Power BI service publishing;
- PBIX generation;
- EF Core migration generation;
- TypeScript, OpenAPI, GraphQL, or runtime CLR type generation projections.

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

## Intended 1.0 Package Set

Define the package set intended for 1.0.

Unless implementation-time inspection shows a real `SemanticTypeModel.JsonEditor` package has been introduced, the intended 1.0 package set must exclude it:

```text
SemanticTypeModel.Abstractions
SemanticTypeModel.Core
SemanticTypeModel.JsonSchema
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
SemanticTypeModel.SystemTextJson
SemanticTypeModel.DependencyInjection
SemanticTypeModel.PowerBI
SemanticTypeModel.EFCore
```

`SemanticTypeModel.JsonEditor` has been removed from package lists, NuGet docs, package smoke tests, release docs, and README unless a real package project exists and is intentionally included.

JSON Editor compatibility must be documented as a feature of `SemanticTypeModel.JsonSchema`, not as a standalone package.

## JsonEditor Cleanup Requirements

Remove `SemanticTypeModel.JsonEditor` from:

- root `README.md` install commands;
- root `README.md` package list;
- `public-docs/packages.md` package list;
- `public-docs/installation.md` package guidance;
- `public-docs/release-notes.md` package lists;
- `public-docs/nuget/` package README source list;
- package-smoke test project references and package assertions;
- `eng/package-smoke.sh` package expectations;
- release workflow or script package expectations;
- any documentation that claims `SemanticTypeModel.DependencyInjection` produces `SemanticTypeModel.JsonEditor`.

Delete or retire:

```text
public-docs/nuget/SemanticTypeModel.DependencyInjection.md
```

unless a real `SemanticTypeModel.JsonEditor` package project is introduced before this milestone is implemented.

Document JSON Editor compatibility under JSON Schema documentation, for example:

```text
public-docs/guides/json-editor-compatibility.md
```

or an equivalent section in an existing JSON Schema guide.

The documentation must explain:

- JSON Editor support is a JSON Schema compatibility mode or UI-hint convention;
- no standalone `SemanticTypeModel.JsonEditor` package is required;
- users consume the feature through `SemanticTypeModel.JsonSchema`;
- UI hints and JSON-editor-oriented annotations are represented through JSON Schema output and documented compatibility rules.

Update projection capability docs so JSON Editor is represented as a JSON Schema compatibility mode if that is the intended design.

## Legacy Public Surface Removal

Remove or internalize public types that exist only for pre-1.0 compatibility.

The default rule is:

```text
Before 1.0.0, remove or internalize legacy public APIs unless they are required by current package smoke tests, samples, or documented 1.0 scenarios.
```

Use this decision rule for each public type:

```text
Keep
  if it is part of the intended 1.0 public API.

Rename
  if the concept is valid but the current name reflects an older design.

Internalize
  if implementation code still needs it but users should not see it.

Remove
  if it exists only for migration from prototype or prerelease code.

Obsolete
  only if there are known external users or published prerelease compatibility must be preserved temporarily.
```

Given the current project state, prefer remove/internalize over obsolete for legacy prototype APIs.

## Power BI Legacy Cleanup

Review the hardened Power BI projection after M0021.

If the intended 1.0 API is `PowerBiProjectionModel` and related `PowerBi*` types, then old tabular compatibility structures should not remain public.

Review and remove, rename, or internalize as appropriate:

```text
PowerBiProjectionModel
PowerBiModelProjection
legacy table/column/relationship DTOs
legacy TOM prototype names
old Power BI projection options
old tests validating compatibility-only APIs
old sample code using compatibility APIs
old docs recommending compatibility APIs
```

The final Power BI public documentation must use only the intended 1.0 API.

If any legacy type is retained, the implementation must document why it is part of the 1.0 contract and add it to public API compatibility documentation deliberately.

## Public API Stabilization

Review public APIs in all packages:

```text
SemanticTypeModel.Abstractions
SemanticTypeModel.Core
SemanticTypeModel.JsonSchema
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
SemanticTypeModel.SystemTextJson
SemanticTypeModel.DependencyInjection
SemanticTypeModel.PowerBI
SemanticTypeModel.EFCore
```

For each package:

- remove accidental public types;
- internalize implementation-only types;
- make extension methods consistent;
- make options APIs consistent;
- make result types consistent;
- make diagnostic return patterns consistent;
- ensure public XML documentation exists;
- update public API compatibility documentation;
- ensure package dependencies match the intended layering.

Public API compatibility documentation files must be updated intentionally.

If the repository uses `API review artifacts`, use those files consistently.

## Compatibility Policy

Update public documentation to define 1.0 compatibility rules.

At minimum, document:

- semantic versioning expectations after 1.0;
- public API compatibility policy;
- diagnostic ID compatibility policy;
- annotation key compatibility policy;
- package split compatibility policy;
- experimental API policy;
- prerelease compatibility policy for `0.x` and `1.0.0-rc.*`;
- migration expectations from `0.1.0-alpha` to `1.0.0`.

Suggested document:

```text
public-docs/versioning.md
```

or a dedicated compatibility document linked from it.

## Diagnostic Stabilization

Validate diagnostic stability across:

- core model validation;
- JSON Schema projection;
- JSON Schema JSON Editor compatibility mode;
- .NET extraction;
- source generator;
- System.Text.Json integration;
- EF Core projection;
- Power BI projection.

Ensure diagnostics have:

- stable IDs;
- documented categories;
- severity policy;
- source location where available;
- model path where available;
- actionable messages;
- public documentation entries.

Update:

```text
public-docs/diagnostics.md
public-docs/diagnostics/**
docs/specs/diagnostics.md
```

as needed.

## Annotation Key Stabilization

Review annotation key namespaces and constants across all packages.

Expected namespaces include, or should be aligned to repository conventions:

```text
semantic.*
jsonSchema.*
systemTextJson.*
efCore.*
powerBi.*
```

Ensure:

- keys are stable;
- keys are documented;
- constants exist where appropriate;
- projection-specific keys stay in projection packages;
- canonical semantic keys stay in abstractions/core as appropriate;
- no duplicate or obsolete keys remain public.

## Projection Capability Stabilization

Update the projection capability matrix after JsonEditor cleanup and Power BI hardening.

The matrix must represent:

- JSON Schema;
- JSON Schema JSON Editor compatibility mode, if documented separately;
- System.Text.Json;
- EF Core;
- Power BI.

Do not represent `SemanticTypeModel.JsonEditor` as a standalone package unless the package exists.

Capability metadata and docs must agree.

Update:

```text
docs/specs/type-model-projection-capabilities.md
public-docs/guides/projection-capabilities.md
```

and related tests.

## Documentation Readiness

Complete or update public documentation for 1.0 readiness:

```text
README.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/samples.md
public-docs/diagnostics.md
public-docs/versioning.md
public-docs/release-notes.md
public-docs/guides/json-schema.md
public-docs/guides/json-editor-compatibility.md
public-docs/guides/system-text-json.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/projection-capabilities.md
public-docs/nuget/SemanticTypeModel.Abstractions.md
public-docs/nuget/SemanticTypeModel.Core.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
public-docs/nuget/SemanticTypeModel.DotNet.md
public-docs/nuget/SemanticTypeModel.Generators.md
public-docs/nuget/SemanticTypeModel.SystemTextJson.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/nuget/SemanticTypeModel.EFCore.md
```

Remove any `SemanticTypeModel.JsonEditor` NuGet README source unless a real package exists.

Public docs must be user-facing. They must not merely restate internal milestone tasks.

## Sample Readiness

Ensure samples cover the intended 1.0 usage story:

- code-first authoring;
- JSON Schema export/import where supported;
- JSON Schema JSON Editor compatibility mode;
- System.Text.Json integration;
- EF Core projection;
- Power BI projection;
- package consumer smoke usage.

Samples must build and run through the canonical sample validation command if available:

```sh
./eng/samples.sh
```

Samples must not rely on non-root README files.

## Release Readiness Validation

Ensure release validation proves 1.0 readiness.

Required commands:

```sh
./eng/check.sh
./eng/public-docs.sh
./eng/public-docs.sh
./eng/package-smoke.sh 1.0.0-rc.1
./eng/release-check.sh 1.0.0-rc.1
```

If a command does not exist, implement it or update this milestone according to the repository's current command contract.

Package smoke tests must consume packed package artifacts, not project references.

## Documentation Index Updates

Update indexes:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/PUBLIC-DOCS.md
docs/ENGINEERING.md
public-docs/packages.md
```

Ensure M0022 and M0023 are listed in `docs/MILESTONES.md`.

## Constraints

Do not create README files outside repository root.

Do not keep `SemanticTypeModel.JsonEditor` in package lists unless a real package is introduced.

Do not keep compatibility-only public APIs solely because prerelease code once used them.

Do not silently drop unsupported projection behavior.

Do not weaken package-smoke validation to make cleanup easier.

## Acceptance Criteria

The milestone is complete when:

- all intended 1.0 packages are explicitly defined;
- `SemanticTypeModel.JsonEditor` is removed from package lists, NuGet docs, package smoke tests, release docs, and README unless a real package exists;
- JSON Editor compatibility is documented as a feature of `SemanticTypeModel.JsonSchema`;
- no public type remains solely for pre-1.0 backward compatibility;
- Power BI legacy compatibility APIs are removed, internalized, renamed, or explicitly justified as 1.0 contract;
- public API compatibility documentation are updated intentionally;
- diagnostics are stable and documented;
- annotation keys are stable and documented;
- projection capability docs match implementation and package boundaries;
- public docs cover the intended 1.0 user journey;
- NuGet README sources exist only for actual packages;
- samples validate intended 1.0 usage;
- package smoke tests validate the intended package set from package artifacts;
- `docs/MILESTONES.md` lists M0022 and M0023;
- `./eng/check.sh` passes;
- `./eng/public-docs.sh` passes;
- `./eng/public-docs.sh` passes;
- `./eng/release-check.sh 1.0.0-rc.1` passes or documented repository-equivalent release validation passes.

## Completion Report

When closing this milestone, report:

- final intended 1.0 package set;
- removed packages or package references;
- removed/internalized legacy public APIs;
- remaining experimental APIs, if any;
- public API compatibility documentation changes;
- diagnostic changes;
- annotation key changes;
- documentation files updated;
- samples validated;
- package smoke validation performed;
- release-readiness commands run.
