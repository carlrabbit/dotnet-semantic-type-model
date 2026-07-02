# M0047: Audience-Specific Descriptions and Projection Description Policies

## Status

Planned.

## Goal

Replace the ambiguous single-description model with two first-class, projection-neutral description semantics:

```text
TechnicalDescription
UserDescription
```

The milestone intentionally removes the old undifferentiated description API and the optional XML-summary import switch. XML `<summary>` documentation becomes the automatic fallback source for `TechnicalDescription`; explicit technical metadata overrides it. `UserDescription` is authored explicitly and is the default source for user-facing projections such as JSON Schema and Power BI.

Every projection must deliberately select and map the appropriate description kind instead of reading one global `Description` value.

The same Order Fulfillment model must prove that one property can produce different correct descriptions in different targets. For example:

```text
Customer.Name XML <summary>
  -> TechnicalDescription
  -> EF Core column comment

Customer.Name [SemanticUserDescription]
  -> UserDescription
  -> JSON Schema description
  -> Power BI description
```

This is an intentionally breaking feature milestone for the `2.4.0` line. Backward compatibility with `SemanticDescriptionAttribute`, a general canonical `Description`, `IncludeXmlDocumentation`, or `SemanticTypeModelIncludeXmlDocumentation` is not required.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Published post-2.3.0 package set with shared Order Fulfillment samples from M0046 |
| Execution mode | `ai-executed-broad` |
| Feature line | `2.4.0` |
| Capability-provider scope | Canonical description semantics, authoring attributes, extraction, generation, inspection, target policies, diagnostics, tests, samples, and public contracts |
| Consumer/dogfood scope | Shared package-based samples demonstrate different target descriptions from the same annotated model |

## Execution Mode

`ai-executed-broad`.

The design authority is explicit, the breaking boundary is approved, implementation is systematic across established model/projection layers, and validation can cover every affected package. Human review is concentrated on terminology, projection defaults, public API shape, target mapping, migration guidance, and final sample wording.

## Scope

- Add canonical `TechnicalDescription` and `UserDescription` semantics to supported type/member model surfaces.
- Remove the undifferentiated canonical `Description` field/property and associated query/inspection output.
- Remove `SemanticDescriptionAttribute` without an obsolete compatibility alias.
- Add `SemanticTechnicalDescriptionAttribute` and `SemanticUserDescriptionAttribute`.
- Make XML `<summary>` the automatic technical-description fallback.
- Make explicit `SemanticTechnicalDescriptionAttribute` override XML `<summary>`.
- Remove `IncludeXmlDocumentation` and all equivalent generator/MSBuild/analyzer configuration.
- Replace `RequireXmlDocumentation` with a semantic requirement policy such as `RequireTechnicalDescription`.
- Update generated providers and runtime extraction.
- Update canonical queries, inspection, transformations, serialization/persistence representations, and diagnostics.
- Add explicit description-selection/mapping policies to relevant projections.
- Map technical descriptions to EF Core table/column comments.
- Map user descriptions to JSON Schema `description` by default.
- Support optional JSON Schema emission of technical descriptions to a configured extension property such as `x-technical-description`.
- Map user descriptions to Power BI descriptions by default.
- Define Configuration description use by output surface rather than one global default.
- Update the shared Order Fulfillment domain and samples to demonstrate different descriptions from the same type/property.
- Remove old APIs, tests, docs, examples, and configuration keys.
- Add breaking migration guidance for the `2.4.0` line.

## Non-Goals

- No compatibility alias for `SemanticDescriptionAttribute`.
- No general/default/neutral third description kind.
- No automatic migration of old description text to one of the new audiences.
- No silent fallback from user description to technical description in user-facing projections by default.
- No raw XML or raw JSON-fragment injection into target projections.
- No arbitrary custom XML element mapping in this milestone.
- No provider-specific EF Core database behavior beyond standard table/column comments.
- No translation/localization framework.
- No documentation-sync or release-readiness pass for `2.4.0`.
- No package publication.
- No broad unrelated cleanup, copied guides, TBPs, issue templates, workflow documents, or non-root READMEs.

## Focus Areas

### 1. Canonical Description Semantics

Add exactly two canonical description concepts:

```text
TechnicalDescription
UserDescription
```

Definitions:

| Kind | Canonical meaning |
|---|---|
| Technical description | Implementation, storage, integration, operational, developer, administrator, or maintainer-facing explanation. |
| User description | Business, end-user, editor/form, report-consumer, or product-facing explanation. |

Required invariants:

- The two descriptions are independent.
- One may be present without the other.
- Neither overwrites the other.
- No canonical `Description` fallback property remains.
- Absence remains absence; projections must not invent wording.
- Description values are normalized deterministically according to one documented whitespace policy.
- Empty or whitespace-only declarations are diagnosed or treated as absent according to the new spec.

Update all canonical model contracts that currently contain `Description`, including types, properties, enum values, relationships, model metadata, annotations, snapshots, builders, transformations, query results, and inspection output where applicable.

### 2. Breaking .NET Authoring Surface

Remove:

```csharp
SemanticDescriptionAttribute
```

Add:

```csharp
SemanticTechnicalDescriptionAttribute
SemanticUserDescriptionAttribute
```

Both attributes must support the same symbol targets that legitimately supported semantic descriptions before the change, including types, properties, fields, and enum members where current extraction supports them.

Do not retain an obsolete type, type forwarder, compatibility alias, or automatic interpretation of the old attribute name.

Compiler failure for old source is the intended migration signal.

### 3. XML Summary as Technical Fallback

XML `<summary>` is always considered when extracting a technical description.

Precedence:

```text
explicit SemanticTechnicalDescriptionAttribute
  > XML <summary>
  > absent
```

Rules:

- XML summary never creates `UserDescription`.
- XML summary never directly creates `schema.description` or any projection-specific metadata.
- Explicit technical description suppresses the XML-derived technical description.
- XML summary handling must be deterministic for whitespace, `<para>`, `<see>`, `<paramref>`, `<typeparamref>`, and other currently supported XML documentation nodes.
- Unsupported or malformed XML documentation must produce stable diagnostics or deterministic plain-text degradation.
- Inherited documentation behavior must be explicitly defined; do not infer inheritance unless current Roslyn extraction already has a supported deterministic rule.

Remove:

```text
IncludeXmlDocumentation
SemanticTypeModelIncludeXmlDocumentation
```

from runtime options, assembly attributes, analyzer/MSBuild properties, generator option parsing, tests, diagnostics, and docs.

Replace the old XML-authoring requirement with:

```text
RequireTechnicalDescription
```

The requirement is satisfied by either an explicit technical-description attribute or an XML summary. It validates semantic output, not a particular authoring mechanism.

### 4. Canonical Query and Inspection Surfaces

Update query and inspection APIs so consumers can retrieve and distinguish both description kinds.

Inspection output must label both explicitly, for example:

```text
User description: The name shown for this customer.
Technical description: Stored in the customer_name column and indexed for prefix search.
```

Do not collapse them into one line or a target-specific label.

Update deterministic text snapshots and public query contracts.

### 5. Projection Description Policies

Each projection owns target selection and mapping.

A projection policy may use a description-kind enum or another strongly typed contract, but it must not use arbitrary string keys for the built-in kinds.

Recommended public concept:

```csharp
public enum SemanticDescriptionKind
{
    User,
    Technical,
}
```

Target policies must define:

- target property/output slot;
- selected description kind;
- fallback behavior;
- omission behavior;
- conflict/invalid configuration diagnostics.

No user-facing projection silently falls back to technical descriptions by default.

### 6. JSON Schema Description Mapping

Default:

```text
JSON Schema description <- UserDescription
```

Rules:

- If user description is absent, omit `description` by default.
- Do not substitute technical description unless an explicit projection option requests that fallback.
- Provide optional configured routing:

```text
x-technical-description <- TechnicalDescription
```

The exact API may be target-specific, but it must validate extension names and produce deterministic output.

Example desired result:

```json
{
  "description": "The name shown for this customer.",
  "x-technical-description": "Stored in the customer_name column and indexed for prefix search."
}
```

Do not allow raw JSON fragments from description metadata.

### 7. EF Core Table and Column Comments

Default:

```text
entity TechnicalDescription -> table comment
property TechnicalDescription -> column comment
```

Requirements:

- Carry technical descriptions through the EF Core domain semantic model.
- Apply comments through provider-neutral EF Core metadata APIs supported by the referenced EF Core version.
- Do not use user description as a fallback by default.
- Preserve domain-model-to-ModelBuilder consistency.
- Inspection must show projected comments.
- Provider-specific SQL generation and migration SQL verification remain out of scope unless an existing provider-neutral test can verify generated annotations without adding a provider package.

### 8. Power BI Description Mapping

Default:

```text
Power BI table/column/model descriptions <- UserDescription
```

Rules:

- Omit descriptions when user descriptions are absent.
- Do not substitute technical descriptions by default.
- Allow an explicit technical-selection policy if the current projection options architecture supports it cleanly.
- Keep service publishing and TOM service operations out of scope.

### 9. Configuration Description Mapping

Configuration has multiple output audiences.

Define and test at least:

```text
consumer-facing generated/sample documentation -> UserDescription
technical inspection and developer diagnostics -> TechnicalDescription where explanatory text is appropriate
```

Do not rewrite validation error messages with descriptions unless the specification explicitly adopts that behavior.

Configuration runtime binding and validation behavior must remain unchanged.

### 10. Other Projection Audit

Audit:

```text
System.Text.Json
JSON Schema
EF Core
Power BI
Configuration
core inspection/query
runtime DI
model snapshots/serialization if present
```

System.Text.Json may have no target description output; it must still tolerate and preserve the canonical fields where model contracts flow through it.

Every affected package receives one of:

```text
implemented mapping plus tests
explicit no-output behavior plus tests
explicit unsupported behavior plus diagnostics and docs
```

### 11. Shared Order Fulfillment Demonstration

Update the shared sample domain so representative types and properties have both audiences.

Required Customer example shape:

```csharp
/// <summary>
/// Stored in the customer_name column and indexed for prefix search.
/// </summary>
[SemanticUserDescription("The name shown for this customer.")]
public required string Name { get; init; }
```

The samples must prove:

- JSON Schema Customer editing output uses the user description.
- EF Core Customer table/column comments use technical descriptions.
- Power BI Customer description uses the user description.
- inspection output exposes both.
- no sample relies on the removed `SemanticDescriptionAttribute`.

Use additional examples on Order, Product, Address, configuration options, and analytical fields where useful, but keep sample narratives readable.

### 12. Tests and Diagnostics

Required test layers:

1. Roslyn extraction from XML summary only.
2. Explicit technical description only.
3. Explicit user description only.
4. Both descriptions present.
5. Explicit technical description overriding XML summary.
6. Missing technical description under `RequireTechnicalDescription`.
7. Removed generator/MSBuild option rejection or absence according to repository option policy.
8. Generated provider containing both description fields.
9. Canonical query and inspection output.
10. JSON Schema default user mapping.
11. JSON Schema omission without user description.
12. JSON Schema explicit technical extension output.
13. EF Core table and column comments.
14. Power BI user-description mapping.
15. Configuration audience-specific output behavior.
16. Package-based shared sample assertions.
17. Repository-wide search proving old APIs/options are removed outside historical migration text.

Add stable diagnostics for at least:

```text
empty explicit technical description
empty explicit user description
missing required technical description
conflicting duplicate declaration of one description kind
invalid JSON Schema technical-extension name
unsupported projection description policy value
malformed XML summary when it cannot be normalized safely
```

Reuse existing diagnostic ranges and stability rules.

### 13. Breaking Migration

Document an explicit `2.4.0` migration.

Before:

```csharp
[SemanticDescription("Customer name.")]
public string Name { get; init; }
```

After, user-facing only:

```csharp
[SemanticUserDescription("The name shown for this customer.")]
public string Name { get; init; }
```

After, technical XML plus user-facing attribute:

```csharp
/// <summary>
/// Stored in the customer_name column.
/// </summary>
[SemanticUserDescription("The name shown for this customer.")]
public string Name { get; init; }
```

Do not prescribe automatic conversion because the old text's intended audience cannot be inferred safely.

## Implementation Constraints

- Treat technical and user descriptions as projection-neutral semantics.
- Keep target mapping in target packages.
- Do not retain the old generic description API.
- Do not retain optional XML-summary import behavior.
- Do not add compatibility aliases.
- Do not route XML summaries directly to target annotations.
- Do not add arbitrary custom XML-tag mapping.
- Keep generated output deterministic.
- Preserve package layering and dependency direction.
- Use current public package APIs in samples.
- Keep samples package-based and deterministic.
- Do not weaken diagnostics or tests to ease migration.
- Use canonical `eng/` commands.
- Do not publish packages.

## Required Authority Documents

### Always Read

```text
AGENTS.md
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ARCHITECTURE.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/samples.md
docs/engineering/packaging.md
public-docs/api/public-api.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

### Description Semantics

```text
docs/specs/audience-specific-description-semantics.md
docs/specs/type-schema-model.md
docs/specs/type-model-core.md
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-annotations.md
```

### Target Projections

```text
docs/specs/type-model-json-schema-mapping.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/configuration-domain-model-and-options-projection.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
```

### M0046 Samples

```text
docs/milestones/m0046-shared-order-fulfillment-samples-and-scalar-nullability-compatibility-hardening.md
docs/decisions/shared-order-fulfillment-sample-domain.md
samples/OrderFulfillment.Domain/
samples/code-first-json-schema/
samples/code-first-ef-core/
samples/code-first-powerbi/
samples/system-text-json-resolver/
samples/configuration-options/
public-docs/samples.md
public-docs/samples/*.md
```

### Source and Tests

Read affected files under:

```text
src/SemanticTypeModel.Abstractions/
src/SemanticTypeModel.Core/
src/SemanticTypeModel.DotNet/
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.JsonSchema/
src/SemanticTypeModel.EFCore/
src/SemanticTypeModel.PowerBI/
src/SemanticTypeModel.Configuration/
src/SemanticTypeModel.SystemTextJson/
tests/unit/
tests/package-smoke/
```

Ordinary implementation agents must not read `.guide-profile.json` or `.guide-sync/`.

## Files or Areas Likely Affected

```text
src/SemanticTypeModel.Abstractions/
src/SemanticTypeModel.Core/
src/SemanticTypeModel.DotNet/
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.JsonSchema/
src/SemanticTypeModel.EFCore/
src/SemanticTypeModel.PowerBI/
src/SemanticTypeModel.Configuration/
src/SemanticTypeModel.SystemTextJson/
tests/unit/SemanticTypeModel.*.Tests.Unit/
tests/package-smoke/
samples/OrderFulfillment.Domain/
samples/code-first-json-schema/
samples/code-first-ef-core/
samples/code-first-powerbi/
samples/configuration-options/
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/specs/*.md
public-docs/guides/*.md
public-docs/nuget/*.md
public-docs/api/*.md
public-docs/diagnostics*.md
public-docs/samples*.md
public-docs/release-notes.md
README.md
.guide-sync/pending/
```

## Validation Tiers and Concrete Commands

### Tier 1 — Focused Inner Loop

Confirm actual project paths before running.

```sh
./eng/test-filter.sh Description
./eng/test-filter.sh XmlDocumentation
./eng/test-filter.sh EFCore
./eng/test-filter.sh JsonSchema
./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Core.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.PowerBI.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Configuration.Tests.Unit
```

Run the System.Text.Json test project when model-contract changes flow through it.

Repository-wide removal search:

```sh
grep -R "SemanticDescription\|IncludeXmlDocumentation\|SemanticTypeModelIncludeXmlDocumentation\|RequireXmlDocumentation\|schema.description" src tests samples docs public-docs --exclude-dir=.git
```

Every retained match must be an intentional migration/history reference or a valid target-level `schema.description` concept.

### Tier 2 — Repository Completion Gate

```sh
./eng/check.sh
```

### Tier 3 — Package and Sample Validation

Use a non-release validation version:

```sh
./eng/package.sh 0.0.0-m0047
./eng/package-smoke.sh 0.0.0-m0047
./eng/samples.sh
./eng/public-docs.sh
```

Do not publish packages.

## Acceptance Criteria

### Canonical Semantics

- Exactly two built-in description kinds exist: user and technical.
- No canonical generic/default `Description` remains.
- Both descriptions are independently queryable and inspectable.
- Deterministic normalization is specified and tested.

### Authoring and XML

- `SemanticDescriptionAttribute` is removed.
- `SemanticUserDescriptionAttribute` and `SemanticTechnicalDescriptionAttribute` exist.
- XML summary automatically supplies technical description when no explicit technical description exists.
- Explicit technical description overrides XML summary.
- XML summary never supplies user description.
- `IncludeXmlDocumentation` and `SemanticTypeModelIncludeXmlDocumentation` are removed.
- `RequireTechnicalDescription` replaces authoring-mechanism-specific XML requirements.

### Projections

- JSON Schema uses user description for `description` by default.
- JSON Schema omits `description` when user description is absent by default.
- JSON Schema can emit technical description to an explicitly configured valid extension.
- EF Core applies technical descriptions to table and column comments.
- Power BI uses user descriptions by default.
- Configuration distinguishes user-facing docs from technical inspection where descriptions are emitted.
- Other projections have tested explicit no-output or mapping behavior.

### Shared Samples

- Customer has distinct technical and user-facing descriptions.
- JSON Schema Customer editing output uses user-facing text.
- EF Core Customer table/column comments use technical text.
- Power BI Customer metadata uses user-facing text.
- inspection output shows both descriptions.
- all sample code uses the new attributes and semantics.

### Breaking Cleanup

- Old attributes, fields, options, generator properties, and tests are removed.
- Public API and compatibility docs identify the intentional `2.4.0` break.
- Migration guidance does not claim automatic audience inference.
- Repository search contains no accidental old API references.

### Validation

- Focused extraction, generation, projection, query, and inspection tests pass.
- `./eng/check.sh` passes.
- `./eng/package.sh 0.0.0-m0047` passes.
- `./eng/package-smoke.sh 0.0.0-m0047` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.

## Direct Documentation Impact

Implementation must update:

```text
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/specs/audience-specific-description-semantics.md
docs/specs/type-schema-model.md
docs/specs/type-model-core.md
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-dotnet-conventions.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-query-and-inspection.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/configuration-domain-model-and-options-projection.md
README.md
public-docs/guides/core-semantics.md
public-docs/guides/json-schema.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/configuration.md
public-docs/nuget/*.md
public-docs/api/public-api.md
public-docs/api/compatibility.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

A deferred synchronization hint is included at:

```text
.guide-sync/pending/m0047-audience-specific-descriptions.md
```

It tracks the later comprehensive documentation synchronization and `2.4.0` release-readiness pass. It must not cause M0047 to publish packages.

Ordinary implementation agents are not required to read `.guide-sync/`.

## Human Review Requirements

Human review is required for:

- final public type/property names;
- exact canonical model shape;
- removal completeness;
- XML summary normalization rules;
- `RequireTechnicalDescription` naming and scope;
- JSON Schema default omission and extension API;
- EF Core table/column comment behavior;
- Power BI mapping defaults;
- Configuration audience behavior;
- diagnostic IDs and severities;
- shared sample wording;
- breaking migration guidance;
- readiness for a later `2.4.0` documentation-sync and release-readiness milestone.

## Out-of-Scope Guide Migration Work

M0047 is not a guide migration.

Do not read, copy, or modify external guide documents during implementation. Do not make target-repository documentation reference the guide repository as operational authority.
