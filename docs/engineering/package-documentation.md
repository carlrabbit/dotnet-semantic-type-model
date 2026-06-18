# Package Documentation

## Purpose

Define the repository standard for NuGet package descriptions, package README sources, usage guides, compatibility documentation, release notes, and specs.

Package documentation keeps NuGet README content, public usage guides, and release validation aligned with the packages that are actually produced by this repository.

## Document Types

| Document type | Purpose | Location |
|---|---|---|
| NuGet package description | Short package metadata for search results and package cards. | Project/package metadata |
| NuGet package README | Install-and-first-use package landing page. | `public-docs/nuget/*.md` |
| Usage guide | Scenario-oriented walkthrough with options, diagnostics, and mistakes. | `public-docs/guides/*.md` |
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

### What this package does

Use one short paragraph. Name the concrete capability and the package boundary.

Do not describe the full architecture unless the package cannot be understood without it.

### Install

Use one `dotnet add package` command.

The package ID and version must align with `README.md`, `public-docs/installation.md`, and current package metadata.

### Use when

Use three to five bullets for good-fit scenarios.

Each bullet should begin with a concrete user need.

### Minimal example

Use one minimal code or command example that shows the first useful call.

Avoid full tutorials, long setup, or unrelated package examples.

### Main APIs

Use a compact table with API names and one-line purpose descriptions.

### Works with

List sibling packages or framework integrations that commonly combine with the package.

### Does not do

List common wrong assumptions and package boundaries.

### More documentation

Link to usage guides, sample pages, and compatibility docs relevant to this package.

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

### Goal

State the user problem the guide solves.

Do not start with “This guide explores...” or broad product positioning.

### Prerequisites

List required package knowledge, model source, generated provider, runtime assumptions, and sample assumptions.

### Packages

List exact packages used in the guide and explain why each is needed.

### Minimal path

Give the shortest successful sequence.

This section should help the user verify they are on the right path before reading the full example.

### Full example

Provide a complete practical example for the scenario.

Do not use pseudo-marketing code. Use realistic names and current namespaces.

### How it works

Explain the relevant flow, usually:

```text
annotated .NET code
  -> source-generated semantic model
  -> optional transformations
  -> domain semantic model
  -> target projection/output
```

### Options and policies

Describe supported configuration points and explain defaults.

### Diagnostics

Describe expected diagnostics, common causes, and fixes.

### Common mistakes

List wrong packages, wrong authoring paths, stale namespaces, unsupported projections, and common incorrect assumptions.

### Limitations

State what the scenario does not support.

### Related docs

Link only to directly relevant package READMEs, sample docs, specs, compatibility docs, and release notes.

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
