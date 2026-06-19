# Package Documentation

## Purpose

Define the repository standard for NuGet package descriptions, package README sources, usage guides, compatibility documentation, release notes, and specs.

Package documentation keeps NuGet README content, public usage guides, and release validation aligned with the packages that are actually produced by this repository.

## Document Types

| Document type | Purpose | Location |
|---|---|---|
| NuGet package description | Short package metadata for search results and package cards. | Project/package metadata |
| NuGet package README | Install-and-first-use package landing page. | `public-docs/nuget/*.md` |
| Usage guide | Scenario-oriented walkthrough with concrete options, policies, diagnostics, and mistakes. | `public-docs/guides/*.md` |
| Compatibility documentation | Versioning, breaking-change, and compatibility policy. | `public-docs/api/compatibility.md` |
| Release notes | Version-specific changes and migration notes. | `public-docs/release-notes.md` |
| Specification | Behavioral authority for implementation. | `docs/specs/*.md` |

## NuGet Package Description Standard

A NuGet package description must be short, concrete, and consumer-facing.

It should contain:

```text
what the package does
who should install it
its role in the SemanticTypeModel ecosystem
primary integration point or output
one important non-goal when confusion is likely
```

It must not contain:

```text
architecture history
milestone references
full examples
complete feature lists
release notes
compatibility essays
marketing claims unsupported by the package alone
```

Template:

```text
<PackageName> provides <capability> for <consumer/use case>. Use it when <primary scenario>. It integrates with <main package/system> and produces <main output>. It does not <important non-goal>.
```

## Package README Standard

Package README sources under `public-docs/nuget/*.md` must be short package landing pages.

Required sections:

```text
# <PackageName>

## What this package does
## Install
## Use when
## Minimal example
## Main APIs
## Works with
## Does not do
## More documentation
```

Package READMEs must not become full scenario guides. Link to usage guides for extended walkthroughs.

## Usage Guide Standard

Usage guides under `public-docs/guides/*.md` must be task-oriented.

Required sections:

```text
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

## Guide Precision Standard

A usage guide must not satisfy the section checklist with generic prose only.

### Options and policies

Every guide must include either:

- a concrete options/policies table; or
- a short statement that the scenario has no user-selectable options and why.

Required table shape:

```markdown
| Item / policy | Default | Allowed values / supported items | Effect | Diagnostics / unsupported cases |
|---|---|---|---|---|
```

Rules:

- name actual options or supported semantic items;
- state the default or say `No default`;
- list allowed values or supported items;
- state what changes when the option is selected;
- state what can fail or how unsupported cases are reported;
- avoid broad phrases such as “configure naming, ownership, and relationships” unless followed by a concrete table.

### Supported items

Guides that describe projection behavior must include a supported-items table when more than one semantic item is involved.

Recommended shape:

```markdown
| Semantic item | Target behavior | Default | Override / policy | Diagnostics |
|---|---|---|---|---|
```

### Diagnostics

Every guide must use a cause/fix table:

```markdown
| Symptom / diagnostic | Likely cause | Fix |
|---|---|---|
```

### Common mistakes

Common mistakes must be guide-specific. Do not repeat the same generic bullets in every guide unless the guide also includes package-specific mistakes.

### Examples

Every projection guide should include at least one example that changes an option or policy when meaningful options exist.

Do not invent API names. Verify examples against current source, specs, samples, or package documentation.

### Planned versus shipped behavior

If a guide discusses a planned package or planned API, mark it as planned and avoid presenting it as current shipped behavior.

## Compatibility Documentation Standard

Compatibility documentation must describe current repository practice honestly.

If the repository does not maintain generated public API baselines, compatibility docs must not imply that static shipped/unshipped files enforce public API compatibility.

Breaking changes must be documented through:

```text
release notes
compatibility docs
package README updates when usage changes
migration notes when required
human review during release readiness
```

## Release Notes Standard

Release notes must describe:

```text
version
release date or status
added features
changed behavior
removed APIs or packages
breaking changes
migration notes
validation status or known limitations
```

Release notes must not replace package READMEs or usage guides.

## Specification Boundary

Specs define behavior, contracts, invariants, diagnostics, and projection rules.

Specs must not be rewritten into tutorials.

Public guides may link to specs when a consumer needs authoritative detail, but guides must remain user-task oriented.

## Writing Rules

- Lead with the user problem.
- Prefer short concrete sentences.
- Avoid “this guide explores” and similar throat-clearing.
- Avoid unsupported marketing claims.
- Do not mention milestones in public consumer docs unless release notes require it.
- Do not mention the external guide repository in public consumer docs.
- Do not add README files outside the repository root.

## Validation

Run public documentation validation after package documentation changes:

```sh
./eng/public-docs.sh
```

When package docs change examples or sample commands, also run:

```sh
./eng/samples.sh
```

## Document Contract

When this document changes, review:

```text
docs/PUBLIC-DOCS.md
docs/engineering/public-documentation.md
public-docs/nuget/*.md
public-docs/guides/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
eng/public-docs.sh
```
