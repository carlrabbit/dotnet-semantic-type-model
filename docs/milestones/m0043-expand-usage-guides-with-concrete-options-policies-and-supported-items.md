# M0043: Expand Usage Guides with Concrete Options, Policies, and Supported Items

## Status

Planned.

## Goal

Expand the public usage guides so they contain concrete supported-items, options, policies, defaults, diagnostics, and limitations instead of broad prose descriptions.

M0042 improved the structure of public guides. M0043 fills the structure with precise instructions.

After M0043:

```text
Every usage guide has an Options and policies section that works as a small reference table.
Every guide names concrete supported items, defaults, effects, diagnostics, and unsupported cases.
The Configuration guide includes the ColdStorageOptions scenario end to end.
Projection-capabilities.md contains a real cross-domain capability matrix.
```

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Post-2.2.0 public package set after M0042 documentation rewrite |
| Capability-provider scope | The repository owns the behavior and documentation of SemanticTypeModel packages, projections, generators, diagnostics, and samples. |
| Consumer/dogfood scope | Usage guides and samples demonstrate bounded consumer usage of shipped or explicitly planned packages. |

## Execution Mode

`ai-executed-human-reviewed`.

The work is documentation-only and scoped, but requires human review because it changes consumer-facing instructions, examples, package positioning, and feature support statements.

## Scope

### In Scope

- Strengthen `docs/engineering/package-documentation.md` with a guide precision standard for supported-items and options/policies tables.
- Expand every current `public-docs/guides/*.md` file with concrete supported items, options, policies, defaults, effects, diagnostics, common mistakes, and limitations.
- Add concrete policy/reference tables to each guide.
- Add at least one option-changing example per projection guide where a meaningful option exists.
- Expand guide diagnostics into cause/fix tables instead of generic diagnostic prose.
- Expand common mistakes into guide-specific mistakes instead of repeated generic bullets.
- Add a concrete capability matrix to `public-docs/guides/projection-capabilities.md`.
- Add an end-to-end Cold Storage configuration scenario to `public-docs/guides/configuration.md`.
- Update `public-docs/packages.md`, `public-docs/samples.md`, `public-docs/release-notes.md`, and package README sources only when links, snippets, or support claims conflict with the expanded guides.

### Out of Scope

```text
implementation source changes
public API changes
new package features
new samples unless required only for documentation accuracy
new API baseline tooling
release publication
NuGet publishing
workflow YAML changes
TBPs
issue templates
copied guide documents
non-root README files
rewriting all package README sources again
rewriting behavioral specs into tutorials
broad unrelated documentation cleanup
```

## Non-Goals

- Do not perform another full M0042-style rewrite.
- Do not change package README structure unless links or support claims are incorrect.
- Do not invent options that are not supported by specs, code, or current package behavior.
- Do not document planned Configuration behavior as shipped behavior unless the package is implemented and shipped.
- Do not rewrite specs into public guides.
- Do not require ordinary implementation agents to read `.guide-profile.json`, `.guide-sync/`, or the external guide repository.

## Focus Areas

### Focus Area 1 — Add Guide Precision Rules to Package Documentation Engineering

#### Intent

Make the repository documentation standard precise enough to prevent generic "Options and policies" sections.

#### Implementation Requirements

Update `docs/engineering/package-documentation.md` with a new `Guide Precision Standard` requiring:

- an options/policies table in every guide;
- a supported-items table where a guide has more than one supported semantic or projection item;
- default values or explicit "no default" statements;
- effects of each option;
- diagnostics for invalid or unsupported values;
- unsupported combinations;
- at least one option-changing example when a meaningful option exists;
- guide-specific common mistakes.

The standard must define this required table shape:

```markdown
| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
```

#### Validation

- Tier 0 documentation review.
- `./eng/public-docs.sh`.

### Focus Area 2 — Expand the Configuration Guide

#### Intent

Turn the Configuration guide from a broad overview into an end-to-end Options registration guide.

#### Required Concrete Content

`public-docs/guides/configuration.md` must include:

- an annotated `ColdStorageOptions` type;
- a `ColdStorageProvider` enum;
- section-name declaration;
- bind policy explanation;
- data annotations validation explanation;
- `ValidateOnStart` explanation;
- `RequiredWhen` / conditional requiredness explanation;
- generated helper example such as `builder.Services.AddColdStorageOptions(builder.Configuration);` when implemented;
- runtime registration example when generated helpers are not used;
- equivalent generated Options registration behavior;
- table of configuration metadata points:
  - configuration options marker;
  - section name;
  - bind policy;
  - named options;
  - data annotations validation;
  - validate on start;
  - generated extension method;
  - conditional requiredness;
- diagnostics cause/fix table.

#### Important Wording Constraint

If Configuration packages or generated helpers are planned but not shipped, the guide must say that clearly and route users to the current supported package behavior.

#### Validation

- `./eng/public-docs.sh`.
- `./eng/samples.sh` if snippets refer to runnable samples.

### Focus Area 3 — Expand Projection Guides with Concrete Policy Tables

#### Intent

Make JSON Schema, EF Core, Power BI, System.Text.Json, and JSON Editor guides actionable.

#### JSON Schema Guide Requirements

`public-docs/guides/json-schema.md` must enumerate:

```text
Draft 2020-12 dialect
root selection policy
envelope wrapper vs payload projection
reference policy
additionalProperties policy
required/nullability mapping
enum mapping
format mapping
extension-data policy
conditional constraints / RequiredWhen mapping
UI export mode
JSON Editor compatibility delegation
diagnostic severity / unsupported projection behavior
```

Include an option-changing export example.

#### EF Core Guide Requirements

`public-docs/guides/ef-core-projection.md` must enumerate:

```text
entity/value-object handling
key discovery
alternate keys
required/nullability
owned object
owned collection
relationship endpoints
table naming
column naming
enum conversion policy
scalar conversion policy
envelope storage policy
extension-data behavior
ignored/unsupported semantics
```

Include a table with EF output, default, override, and diagnostic behavior.

#### Power BI Guide Requirements

`public-docs/guides/power-bi-projection.md` must enumerate:

```text
Dimension
Fact
DisplayName
Description
Format
Enum / categorical data
Relationships
Measures
Calculated tables
Owned objects
Extension data
Envelope payloads
Table visibility
Display folders
Data categories
Sort-by-column metadata
Summarization hints
```

Include a table with semantic input, Power BI output, default, supported override, and diagnostics.

#### System.Text.Json Guide Requirements

`public-docs/guides/system-text-json.md` must enumerate:

```text
PropertyNameSource values
resolver wrapping order
required marker handling
ignored members
extension-data handling
existing JsonSerializerContext behavior
unsupported generated-context behavior
converter boundaries
duplicate final name diagnostics
```

Include the default property-name policy and a policy-changing example.

#### JSON Editor Compatibility Guide Requirements

`public-docs/guides/json-editor-compatibility.md` must enumerate:

```text
UiMode values
IncludeJsonEditorCompatibilityAnnotations
title/description mapping
ordering metadata
enum display labels
widget hints
unsupported widget diagnostics
version compatibility warning
```

Include a before/after or fragment example showing what changes in JSON Editor compatibility mode.

#### Validation

- `./eng/public-docs.sh`.
- `./eng/samples.sh` if sample snippets change.

### Focus Area 4 — Expand Core Semantics Guide into a Vocabulary Inventory

#### Intent

Make the Core Semantics guide explain actual semantic primitives, not only the architecture.

#### Required Content

`public-docs/guides/core-semantics.md` must include a table with at least:

```text
Entity
ValueObject
Configuration
Dimension
Fact
Event
Key
AlternateKey
Relationship
Required
Nullable
Constraint
RequiredWhen
Enum
Format
DisplayName
Description
Category
Order
Ownership
OwnedObject
OwnedCollection
Envelope
EnvelopePayload
Version
Revision
CurrentVersion
TemporalValidity
LifecycleState
ExtensionData
```

For each row include:

```text
use when
authoring shape / attribute
projection-neutral meaning
major projection effects
common mistake
```

If a semantic primitive is planned but not implemented, mark it as planned instead of presenting it as current.

#### Validation

- `./eng/public-docs.sh`.

### Focus Area 5 — Expand Projection Capabilities into a Real Matrix

#### Intent

Make `projection-capabilities.md` the cross-domain decision table users expected.

#### Required Content

`public-docs/guides/projection-capabilities.md` must include a capability matrix with columns:

```text
Capability
Core semantic?
JSON Schema
EF Core
Power BI
System.Text.Json
Configuration
Default behavior
Diagnostics
```

Rows must include at least:

```text
Entity
ValueObject
Configuration
Required / Nullable
Constraint
RequiredWhen
Enum
Format
DisplayName / Description
Ownership
Envelope payload
Version / Revision
Temporal validity
Lifecycle state
Extension data
Target-specific metadata
```

Use explicit values such as:

```text
supported
supported with policy
preserved as metadata
ignored by default
unsupported with diagnostics
planned
not applicable
```

Do not leave matrix cells as vague prose.

#### Validation

- `./eng/public-docs.sh`.

### Focus Area 6 — Make Diagnostics and Common Mistakes Guide-Specific

#### Intent

Remove repeated generic bullets and make each guide help users fix likely mistakes in that specific area.

#### Implementation Requirements

Each guide must have:

- diagnostics table with columns:

```markdown
| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
```

- guide-specific common mistakes, not just repeated generic bullets;
- limitations that are specific to the guide's package and target.

Repeated generic warnings may appear once in a shared getting-started page, but not as the main content in every guide.

#### Validation

- `./eng/public-docs.sh`.

## Implementation Constraints

- Documentation-only milestone.
- Do not change implementation source files.
- Do not change public APIs.
- Do not add tests unless documentation validation fixtures require it.
- Do not add non-root README files.
- Do not copy external guide documents.
- Do not add TBPs or issue templates.
- Do not claim planned behavior is implemented.
- Keep package READMEs stable unless links or support claims conflict with expanded guides.
- Use specs and current source only to verify actual supported items.

## Required Authority Documents

Always read:

```text
AGENTS.md
docs/ENGINEERING.md
docs/engineering/public-documentation.md
docs/engineering/package-documentation.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
public-docs/guides/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

Read to verify semantic vocabulary and projection behavior:

```text
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-annotations.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-compile-time-generator.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/system-text-json-contract-integration.md
docs/specs/configuration-domain-model-and-options-projection.md
docs/specs/core-conditional-constraint-semantics.md
docs/specs/type-model-projection-capabilities.md
docs/specs/diagnostics.md
```

Read to verify package/sample references:

```text
public-docs/nuget/*.md
public-docs/samples.md
public-docs/samples/*.md
samples/
src/*/*.csproj
```

Do not treat `docs/research/` guide copies as operational authority.

## Files or Areas Likely Affected

```text
docs/engineering/package-documentation.md
docs/MILESTONES.md
public-docs/guides/configuration.md
public-docs/guides/core-semantics.md
public-docs/guides/json-schema.md
public-docs/guides/json-editor-compatibility.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/projection-capabilities.md
public-docs/guides/system-text-json.md
public-docs/packages.md
public-docs/samples.md
public-docs/release-notes.md
public-docs/nuget/*.md
.guide-sync/pending/
```

## Validation Tiers and Concrete Commands

Use Tier 0 throughout:

```sh
./eng/public-docs.sh
```

Run samples validation if examples, commands, or sample references change:

```sh
./eng/samples.sh
```

Run targeted stale-term search:

```sh
grep -R "Abstractions.Canonical\|Canonical.TypeSchemaModel\|TypeShape\|ObjectShape\|PropertyShape\|ShapeRef\|PublicAPI.Shipped\|PublicAPI.Unshipped\|public-api.sh" README.md docs public-docs samples --exclude-dir=.git
```

Recommended final validation:

```sh
./eng/public-docs.sh
./eng/samples.sh
```

If `./eng/samples.sh` cannot run in the environment, document why and provide the exact commands/snippets reviewed manually.

## Acceptance Criteria

- No usage guide has an `Options and policies` section that is only a prose list.
- Every usage guide has a concrete options/policies table or a justified statement that no options exist.
- Every applicable guide has a supported-items or capability table.
- Every projection guide includes at least one option-changing example when meaningful options exist.
- Configuration guide includes the Cold Storage scenario end to end or explicitly marks it as planned if not yet implemented.
- Core Semantics guide includes a real vocabulary inventory.
- Projection Capabilities guide includes a real matrix across JSON Schema, EF Core, Power BI, System.Text.Json, and Configuration.
- Diagnostics sections use cause/fix tables.
- Common mistakes are guide-specific.
- Limitations are guide-specific.
- Package README sources are changed only when links or support claims conflict with expanded guides.
- Public docs avoid stale `Abstractions.Canonical`, old shape graph, and fake public API baseline language as current usage.
- `./eng/public-docs.sh` passes.
- `./eng/samples.sh` passes or any inability to run is explicitly documented.
- Human review confirms the guides are precise enough for a new consumer to know what knobs are available and what each knob does.

## Direct Documentation Impact

Implementation must update:

```text
docs/engineering/package-documentation.md
docs/MILESTONES.md
public-docs/guides/*.md
```

Implementation may update when needed:

```text
public-docs/packages.md
public-docs/samples.md
public-docs/release-notes.md
public-docs/nuget/*.md
README.md
```

## Deferred Documentation Synchronization Hints

A deferred documentation-sync hint is included at:

```text
.guide-sync/pending/m0043-usage-guide-supported-items-and-policy-precision.md
```

Ordinary implementation agents do not need to read `.guide-sync/`, but a documentation-sync or release-readiness agent may use it after implementation.

## Human Review Requirements

Human review is required for:

- the supported-items and policy tables;
- any claim that a package supports, ignores, preserves, or rejects a semantic primitive;
- Configuration implemented-versus-planned wording;
- examples that imply specific generated APIs or option names;
- capability matrix values;
- retained historical references to removed APIs.

## Out-of-Scope Guide Migration Work

M0043 is not a guide migration.

Do not read the external guide repository during implementation. Do not copy guide documents into the repository. Do not make target repository docs reference guide documents as operational authority.
