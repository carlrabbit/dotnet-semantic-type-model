# M0040: Configuration Domain Model and Options Registration Projection

## Status

Planned.

## Goal

Add Configuration as a first-class domain projection that derives configuration options, section binding, options validation, startup validation, and generated Microsoft.Extensions.Options registration helpers from the canonical semantic model.

The flagship scenario is to generate or apply behavior equivalent to:

```csharp
builder.Services
    .AddOptions<ColdStorageOptions>()
    .Bind(builder.Configuration.GetSection(ColdStorageOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options =>
    {
        return options.Provider != ColdStorageProvider.File
               || !string.IsNullOrWhiteSpace(options.TargetFilePath);
    }, "TargetFilePath is required when ColdStorage:Provider is File.")
    .ValidateOnStart();
```

from code-first semantic options types.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Post-2.2.0 public package set |
| Capability-provider scope | The repository implements model contracts, extraction, generators, transformations, domain models, projections, diagnostics, samples, public docs, and package docs. |
| Consumer/dogfood scope | Samples validate bounded consumer usage of generated configuration helpers. |

## Execution Mode

`ai-executed-broad`.

The design authority is normalized enough for systematic implementation: existing architecture already uses canonical model -> domain model -> projection behavior. M0040 applies that pattern to Configuration and adds one narrow core conditional-constraint semantic.

Human review is still required for public API additions, package naming, diagnostic ID allocation, generator output shape, and release-facing documentation.

## Scope

### In Scope

- Add core conditional constraint support for `RequiredWhen`.
- Add Configuration domain semantic model derivation.
- Add Configuration-specific metadata for section names, bind policy, named options, data-annotation validation, startup validation, and generated registration helpers.
- Add Configuration package surface and generator package surface as needed.
- Add source extraction and generator support for configuration annotations.
- Generate deterministic `IServiceCollection` extension methods for options registration.
- Support the Cold Storage sample scenario.
- Add diagnostics and inspection support for configuration and conditional constraints.
- Update JSON Schema, EF Core, Power BI, and System.Text.Json handling for new core conditional constraints.
- Add tests, samples, public docs, package README sources, public API compatibility documentation, and release notes for the feature.

### Out of Scope

```text
external guide migration
copying guide documents into this repository
release publication
NuGet publishing
workflow YAML changes
configuration provider loading
secret-management behavior
appsettings file generation as the primary feature
arbitrary validation expression parsing
general purpose validation engine
host-specific deployment behavior
provider-specific EF Core behavior
Power BI service/PBIX behavior
JSON Schema import revival
TBPs
issue templates
non-root README files
```

## Non-Goals

- Do not make Options registration behavior a core semantic.
- Do not treat Configuration as a JSON Schema sub-feature.
- Do not silently generate behavior in EF Core, Power BI, or System.Text.Json from conditional constraints.
- Do not require consumers to use generated helpers when runtime application APIs are sufficient.
- Do not make ordinary implementation agents read the external guide repository, `.guide-profile.json`, or `.guide-sync/`.
- Do not broaden this milestone into release-readiness.

## Required Authority Documents

Implementation agents must read only the documents relevant to their focus area.

Always read:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/SPECS.md
docs/specs/configuration-domain-model-and-options-projection.md
docs/specs/core-conditional-constraint-semantics.md
docs/decisions/configuration-domain-is-options-registration-projection.md
```

Read when modifying core model, annotations, transformation, query, inspection, or diagnostics:

```text
docs/specs/type-schema-model.md
docs/specs/type-model-core.md
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-annotations.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-query-and-inspection.md
docs/specs/diagnostics.md
```

Read when modifying .NET extraction, attributes, conventions, or source generation:

```text
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
```

Read when modifying existing projection packages for conditional-constraint handling:

```text
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/system-text-json-contract-integration.md
```

Read when modifying public docs, package README sources, public API compatibility documentation, samples, or release notes:

```text
docs/PUBLIC-DOCS.md
docs/engineering/public-documentation.md
docs/engineering/samples.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Focus Areas

### Focus Area 1 — Add Core Conditional Constraint Semantics

Add a narrow projection-neutral `RequiredWhen` semantic that other domains can consume, preserve, ignore, or diagnose.

Implementation requirements:

- Add model representation or annotation normalization for `RequiredWhen`.
- Reserve and implement canonical annotation keys from `docs/specs/core-conditional-constraint-semantics.md`.
- Support simple equality conditions against string, boolean, numeric, and enum symbolic values.
- Reject arbitrary expression trees and callbacks.
- Add diagnostics for unresolved members, incompatible literals, unsupported operators, and unsupported target projection.
- Add deterministic inspection output.

Validation:

- Tier 1 focused model/core tests, diagnostics tests, and inspection tests.
- Tier 2 before completion.

### Focus Area 2 — Add Configuration Domain Model

Derive an inspectable Configuration Domain Model from the canonical semantic model.

Implementation requirements:

- Add `ConfigurationSemanticModel` and related domain contracts.
- Derive configuration types from core `Configuration` role or configuration-specific inclusion metadata.
- Derive section, bind, named-options, validation, and startup-validation metadata.
- Consume existing core semantics listed in the Configuration spec.
- Emit diagnostics for unresolved or unsupported configuration metadata.

Validation:

- Tier 1 configuration domain derivation tests, diagnostics tests, and inspection tests.
- Tier 2 before completion.

### Focus Area 3 — Add Configuration Attributes and Extraction

Enable code-first authors to declare configuration semantics in annotated .NET code.

Implementation requirements:

- Add configuration attributes such as section, validate-on-start, validate-data-annotations, and registration generation markers.
- Add `SemanticRequiredWhen` or equivalent conditional constraint authoring support.
- Update Roslyn extraction and source generator pipelines.
- Preserve projection-neutral conditional constraints as core metadata/model members.
- Keep configuration-specific metadata in `configuration.*` namespace.

Validation:

- Tier 1 extraction tests, generator tests, diagnostics tests, and generated-source baseline tests.
- Tier 2 before completion.

### Focus Area 4 — Add Options Registration Projection

Generate or apply Microsoft.Extensions.Options registration behavior from the Configuration Domain Model.

Implementation requirements:

- Add runtime application API or generated extension method API.
- Generate deterministic `IServiceCollection` helper methods when generator support is enabled.
- Bind selected configuration section to selected options type.
- Apply data-annotations validation when enabled.
- Apply `RequiredWhen` validation when supported.
- Apply `ValidateOnStart` when enabled.
- Emit diagnostics for unsupported or unsafe generated output.

Validation:

- Tier 1 generated code compile tests, Options registration behavior tests, and Cold Storage scenario tests.
- Tier 2 before completion.

### Focus Area 5 — Cross-Domain Handling of RequiredWhen

Ensure new core conditional constraints do not drift across domains.

Implementation requirements:

- JSON Schema must map `RequiredWhen` when supported by selected dialect/policy or emit unsupported diagnostics.
- EF Core must ignore or preserve conditional metadata by default and must not emit check constraints without explicit policy.
- Power BI must ignore or preserve conditional metadata by default and must not emit measures or filters.
- System.Text.Json must preserve metadata or ignore by policy and must not silently add runtime validation.
- Query and inspection surfaces must expose conditional constraints.

Validation:

- Tier 1 projection-specific focused tests for JSON Schema, EF Core, Power BI, and System.Text.Json.
- Tier 2 before completion.

### Focus Area 6 — Samples, Public Docs, Package Docs, and Compatibility

Make the feature understandable and safe for consumers.

Implementation requirements:

- Add a Cold Storage configuration sample.
- Add package README source for new Configuration package(s).
- Update public guides for core semantics, Configuration, JSON Schema, System.Text.Json, and samples.
- Update package list and installation docs.
- Update public API compatibility documentation.
- Update release notes for the next release line.
- Document explicit non-goals.

Validation:

- Tier 0/Tier 1 during docs and sample work.
- Tier 2 before completion.
- Run public docs and samples validation before completion.

## Implementation Constraints

- Keep core semantics projection-neutral.
- Keep Options registration behavior in Configuration domain.
- Do not add target behavior to other domains silently.
- Use canonical `eng/` scripts.
- Avoid opportunistic refactoring.
- Do not introduce non-root README files.
- Do not add copied external guide documents.
- Do not add TBPs, issue templates, or broad workflow documents.
- Use deterministic tests and deterministic generated output.
- Add diagnostics before silently ignoring unsupported semantics.

## Files or Areas Likely Affected

```text
src/SemanticTypeModel.Abstractions/
src/SemanticTypeModel.Core/
src/SemanticTypeModel.DotNet/
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.JsonSchema/
src/SemanticTypeModel.EFCore/
src/SemanticTypeModel.PowerBI/
src/SemanticTypeModel.SystemTextJson/
src/SemanticTypeModel.Configuration/
src/SemanticTypeModel.Configuration.Generators/
tests/unit/
samples/
docs/specs/
docs/decisions/
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/TERMINOLOGY.md
public-docs/
```

## Validation Tiers and Commands

Use focused Tier 1 validation during implementation.

Examples:

```sh
./eng/test-project.sh tests/unit/SemanticTypeModel.Core.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit
./eng/test-filter.sh Configuration
```

Completion gate:

```sh
./eng/check.sh
./eng/package.sh 0.0.0-m0040
./eng/package-smoke.sh 0.0.0-m0040
./eng/samples.sh
./eng/public-docs.sh
./eng/public-docs.sh
```

Do not publish packages.

## Acceptance Criteria

- `RequiredWhen` or the equivalent initial conditional constraint is represented as projection-neutral semantic meaning.
- Configuration-specific metadata remains in the Configuration domain.
- A Configuration Domain Model can be derived from annotated code-first model metadata.
- Options registration behavior equivalent to the Cold Storage baseline scenario is generated or applied.
- `ValidateDataAnnotations`, `ValidateOnStart`, section binding, and conditional validation are supported according to spec.
- JSON Schema, EF Core, Power BI, and System.Text.Json account for new core conditional semantics explicitly.
- Unsupported cases emit diagnostics.
- Query and inspection surfaces expose configuration and conditional-constraint information.
- Public samples demonstrate generated or projected Options registration.
- Public docs and package docs are updated for consumer usage.
- Tier 2 plus package, smoke, samples, public API, and public docs validation pass.

## Direct Documentation Impact

Implementation must update:

```text
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/TERMINOLOGY.md
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-compile-time-generator.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/system-text-json-contract-integration.md
docs/specs/diagnostics.md
docs/PUBLIC-DOCS.md
public-docs/
public-docs/nuget/
public-docs/samples/
public-docs/api/compatibility.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

A deferred documentation-sync hint is included at:

```text
.guide-sync/pending/m0040-configuration-domain-and-options-projection.md
```

Ordinary implementation agents do not need to read `.guide-sync/`, but a documentation-sync or release-readiness agent may use it after implementation.

## Human Review Requirements

Human review is required for:

- whether `RequiredWhen` is the correct first conditional primitive;
- public package naming;
- public API compatibility documentation changes;
- diagnostic ID allocation;
- generated Options registration API ergonomics;
- cross-domain default behavior for conditional constraints;
- release-note and compatibility wording.

## Out-of-Scope Guide Migration Work

M0040 is not a guide migration.

Do not read the external guide repository during implementation. Do not copy guide documents into the repository. Do not make target repository docs reference guide documents as operational authority.
