# M0042: Rewrite Package READMEs and Usage Guides to Documentation Standard

## Status

Planned.

## Goal

Rewrite the public NuGet package README sources and public usage guides so they follow the documentation standard introduced after M0041.

This is a pure documentation rewrite milestone. It corrects the incomplete M0041 documentation outcome: package README sources and usage guides must become useful consumer-facing documents, not short notes, partial feature lists, or implementation-spec summaries.

After M0042:

```text
package README sources
  are concise install-and-first-use package landing pages.

usage guides
  are scenario-oriented walkthroughs with minimal path, full example, options, diagnostics, common mistakes, and limitations.

specs
  remain behavioral authority and are not rewritten into tutorials.
```

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Post-2.2.0 public package set after M0041 API-baseline purge |
| Capability-provider scope | The repository ships multiple `SemanticTypeModel.*` packages and owns public package documentation, usage guides, samples, diagnostics references, compatibility docs, and release notes. |
| Consumer/dogfood scope | Public samples and usage guides demonstrate bounded consumer usage of shipped packages; they do not define new product behavior. |

## Execution Mode

`ai-executed-human-reviewed`.

The implementation is documentation-only but medium-to-large in surface area. Design authority is clear, but human review is required because the rewrite changes consumer-facing messaging, examples, package positioning, and guide structure.

## Scope

### In Scope

- Expand `docs/engineering/package-documentation.md` into the authoritative package README and usage guide standard.
- Rewrite every current NuGet package README source under `public-docs/nuget/*.md` to the required package README structure.
- Rewrite every current public usage guide under `public-docs/guides/*.md` to the required usage guide structure.
- Update `docs/PUBLIC-DOCS.md` if the public documentation surface list or documentation standards need synchronization.
- Update `README.md`, `public-docs/getting-started.md`, `public-docs/installation.md`, `public-docs/packages.md`, and `public-docs/samples.md` only when they link to or summarize rewritten package/guides incorrectly.
- Update `public-docs/release-notes.md` only if it contains stale public API baseline or guide/package README claims.
- Keep examples aligned with current package IDs, current version line, current generated-model flow, and current post-2.2 model surface.
- Preserve the distinction between package README, usage guide, compatibility docs, release notes, and specs.

### Out of Scope

```text
source code changes
public API changes
new package features
new samples unless needed only to fix broken docs links
new API baseline tooling
release publication
NuGet publishing
workflow YAML changes
TBPs
issue templates
copied guide documents
non-root README files
rewriting behavioral specs into tutorials
broad unrelated documentation cleanup
```

## Non-Goals

- Do not create a new package documentation architecture.
- Do not add new public behavior claims that are not supported by shipped code or existing specs.
- Do not turn package README sources into long tutorials.
- Do not turn usage guides into package metadata pages.
- Do not duplicate full examples in every package README.
- Do not rewrite `docs/specs/*.md` unless a direct factual correction is required.
- Do not require ordinary implementation agents to read `.guide-profile.json`, `.guide-sync/`, or the external guide repository.

## Focus Areas

### Focus Area 1 — Strengthen the Documentation Standard

#### Intent

Make `docs/engineering/package-documentation.md` specific enough that future documentation agents can validate package README and usage guide shape without reconstructing prior chat context.

#### Implementation Requirements

- Define the required NuGet package description content.
- Define the required package README sections:
  - `What this package does`
  - `Install`
  - `Use when`
  - `Minimal example`
  - `Main APIs`
  - `Works with`
  - `Does not do`
  - `More documentation`
- Define the required usage guide sections:
  - `Goal`
  - `Prerequisites`
  - `Packages`
  - `Minimal path`
  - `Full example`
  - `How it works`
  - `Options and policies`
  - `Diagnostics`
  - `Common mistakes`
  - `Limitations`
  - `Related docs`
- Define what belongs in compatibility docs, release notes, specs, and sample docs.
- Include wording rules:
  - lead with the user problem;
  - no “this guide explores” / “this package provides comprehensive” filler;
  - short concrete paragraphs;
  - no milestone or internal-planning references in public docs;
  - no unsupported marketing claims.

#### Validation

- Tier 0 documentation review.
- `./eng/public-docs.sh` after related validation script changes, if any.

### Focus Area 2 — Rewrite NuGet Package README Sources

#### Intent

Make every package README source a concise package landing page that helps a consumer decide whether to install the package and complete the first working call.

#### Candidate Files

Inspect `docs/PUBLIC-DOCS.md` and current project files before finalizing the list. Expected files include:

```text
public-docs/nuget/SemanticTypeModel.Abstractions.md
public-docs/nuget/SemanticTypeModel.Core.md
public-docs/nuget/SemanticTypeModel.JsonSchema.md
public-docs/nuget/SemanticTypeModel.DotNet.md
public-docs/nuget/SemanticTypeModel.Generators.md
public-docs/nuget/SemanticTypeModel.DependencyInjection.md
public-docs/nuget/SemanticTypeModel.Configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.Generators.md
public-docs/nuget/SemanticTypeModel.PowerBI.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/nuget/SemanticTypeModel.SystemTextJson.md
```

#### Required Package README Shape

Each package README must follow this structure exactly unless the package has a documented reason to omit a section:

```markdown
# SemanticTypeModel.<Package>

## What this package does

## Install

## Use when

## Minimal example

## Main APIs

## Works with

## Does not do

## More documentation
```

#### Content Requirements

- `What this package does`: one short paragraph naming the concrete capability.
- `Install`: one `dotnet add package` command using the current documented version line.
- `Use when`: three to five bullets for good-fit scenarios.
- `Minimal example`: one minimal, plausible code path or package-use snippet.
- `Main APIs`: compact table of core entry points.
- `Works with`: sibling packages or framework integrations.
- `Does not do`: common wrong assumptions for that package.
- `More documentation`: links to relevant usage guides and sample docs.

#### Validation

- `./eng/public-docs.sh`.
- `./eng/samples.sh` if any README snippet references runnable sample behavior.

### Focus Area 3 — Rewrite Public Usage Guides

#### Intent

Make every usage guide a scenario walkthrough, not a capability list or thin note.

#### Candidate Files

Inspect `docs/PUBLIC-DOCS.md` before finalizing the list. Expected files include:

```text
public-docs/guides/core-semantics.md
public-docs/guides/json-schema.md
public-docs/guides/json-editor-compatibility.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/projection-capabilities.md
public-docs/guides/system-text-json.md
public-docs/guides/configuration.md
```

If `configuration.md` does not exist but `SemanticTypeModel.Configuration` is shipped or documented in package docs, create it.

#### Required Usage Guide Shape

Each guide must follow this structure exactly unless the guide has a documented reason to omit a section:

```markdown
# <Scenario>

## Goal

## Prerequisites

## Packages

## Minimal path

## Full example

## How it works

## Options and policies

## Diagnostics

## Common mistakes

## Limitations

## Related docs
```

#### Content Requirements

- `Goal`: user problem, not a feature description.
- `Prerequisites`: model source, generated provider, package assumptions, runtime assumptions.
- `Packages`: exact packages used in the guide.
- `Minimal path`: shortest working sequence.
- `Full example`: complete scenario example, not pseudo-marketing prose.
- `How it works`: explain `annotated .NET code -> generated model -> domain model -> projection/output` when relevant.
- `Options and policies`: configuration points the user can choose.
- `Diagnostics`: expected diagnostics and what to do.
- `Common mistakes`: wrong package, wrong authoring source, wrong projection assumptions, stale namespace usage.
- `Limitations`: explicit non-goals and unsupported behavior.
- `Related docs`: only directly relevant links.

#### Validation

- `./eng/public-docs.sh`.
- `./eng/samples.sh` when snippets or command paths depend on samples.

### Focus Area 4 — Synchronize Entry Pages and Package Lists

#### Intent

Ensure root and public-doc entry pages point users to the rewritten docs without duplicating the full guides.

#### Implementation Requirements

Review and update only as needed:

```text
README.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/packages.md
public-docs/samples.md
docs/PUBLIC-DOCS.md
```

The entry pages should:

- route new users to the right package and guide;
- not contain stale package lists;
- not duplicate full usage-guide content;
- not mention internal milestones or guide-system planning;
- not refer to fake public API baseline files.

#### Validation

- `./eng/public-docs.sh`.
- Link/reference review.

### Focus Area 5 — Remove Public Documentation Drift

#### Intent

Make public docs reflect current post-2.2 behavior and M0040 planning state without inventing unimplemented behavior.

#### Implementation Requirements

- Ensure post-M0038 unified model surface names are used consistently.
- Ensure `SemanticTypeModel.Abstractions.Canonical` is not presented as current usage.
- Ensure old `TypeShape` / `ObjectShape` / `PropertyShape` / `ShapeRef` names are not presented as current usage.
- Ensure JSON Schema import is not presented as the primary authoring path.
- Ensure Configuration docs distinguish implemented behavior from planned behavior if M0040 is not implemented yet.
- Ensure package README sources and usage guides do not make claims that conflict with samples, specs, or release notes.

#### Validation

Run targeted searches:

```sh
grep -R "Abstractions.Canonical\|Canonical.TypeSchemaModel\|TypeShape\|ObjectShape\|PropertyShape\|ShapeRef\|PublicAPI.Shipped\|public-api.sh" README.md docs public-docs samples --exclude-dir=.git
```

Explain any retained historical references.

## Implementation Constraints

- Documentation-only milestone.
- Do not change implementation source files.
- Do not change tests unless a docs validation script requires explicit fixture updates.
- Do not add non-root README files.
- Do not copy external guide documents.
- Do not add TBPs or issue templates.
- Do not change package IDs.
- Keep package READMEs short.
- Keep usage guides scenario-oriented.
- Keep specs authoritative and non-tutorial.
- Public docs must not mention the external guide repository as operational authority.

## Required Authority Documents

Always read:

```text
AGENTS.md
docs/ENGINEERING.md
docs/engineering/public-documentation.md
docs/engineering/package-documentation.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

Read to verify package list and package README mapping:

```text
src/*/*.csproj
eng/public-docs.sh
public-docs/nuget/*.md
public-docs/packages.md
public-docs/installation.md
README.md
```

Read to rewrite usage guides accurately:

```text
public-docs/guides/*.md
public-docs/samples.md
public-docs/samples/*.md
docs/specs/model-surface-unification.md
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-compile-time-generator.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/configuration-domain-model-and-options-projection.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Files or Areas Likely Affected

```text
docs/engineering/package-documentation.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
README.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/packages.md
public-docs/samples.md
public-docs/nuget/*.md
public-docs/guides/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
.guide-sync/pending/
```

## Validation Tiers and Concrete Commands

Use Tier 0 throughout:

```sh
./eng/public-docs.sh
```

Run samples validation if snippets or sample command references changed:

```sh
./eng/samples.sh
```

Run repository completion gate only if validation scripts, package metadata, or root docs change in a way that may affect packaging:

```sh
./eng/check.sh
```

Recommended final validation:

```sh
./eng/public-docs.sh
./eng/samples.sh
```

If `./eng/samples.sh` cannot run in the environment, document why and provide the exact commands/snippets reviewed manually.

## Acceptance Criteria

- `docs/engineering/package-documentation.md` defines all required package README and usage guide sections with per-section intent.
- Every `public-docs/nuget/*.md` file follows the package README structure.
- Every `public-docs/guides/*.md` file follows the usage guide structure, or the implementation records a specific reason for a scoped exception.
- Package READMEs are concise install-and-first-use documents.
- Usage guides are scenario walkthroughs.
- Package READMEs and usage guides do not duplicate each other.
- Each package README names main APIs and common non-goals.
- Each usage guide contains diagnostics, common mistakes, and limitations.
- Entry pages route users to the right package READMEs and usage guides.
- Public docs avoid stale `Abstractions.Canonical`, old shape-graph, and fake public API baseline language as current usage.
- `./eng/public-docs.sh` passes.
- `./eng/samples.sh` passes or any inability to run is explicitly documented.
- Human review confirms the docs are useful to a new consumer.

## Direct Documentation Impact

Implementation must update:

```text
docs/engineering/package-documentation.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
README.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/packages.md
public-docs/samples.md
public-docs/nuget/*.md
public-docs/guides/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

## Deferred Documentation Synchronization Hints

A deferred documentation-sync hint is included at:

```text
.guide-sync/pending/m0042-package-readme-and-usage-guide-rewrite.md
```

Ordinary implementation agents do not need to read `.guide-sync/`, but a documentation-sync or release-readiness agent may use it after implementation.

## Human Review Requirements

Human review is required for:

- package positioning and package descriptions;
- whether each README is short enough;
- whether each usage guide is useful enough;
- whether examples are accurate and current;
- whether Configuration docs describe implemented versus planned behavior correctly;
- whether any omitted section is justified;
- whether stale terms are retained only as historical compatibility notes.

## Out-of-Scope Guide Migration Work

M0042 is not a guide migration.

Do not read the external guide repository during implementation. Do not copy guide documents into the repository. Do not make target repository docs reference guide documents as operational authority.
