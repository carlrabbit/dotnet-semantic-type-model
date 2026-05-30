# Project Setup Guide V6

## Status

Authoritative project-setup guide.

## Purpose

This guide defines a context-sensitive repository knowledge model for AI-assisted engineering.

Version 6 replaces the broad mandatory documentation model with a maturity-based and task-mode-based model.

The core change is separation of concerns:

```text
Planning and design define authority.
Milestones transport implementation intent.
Implementation changes code and directly affected authority only.
PR workflows validate integration.
Documentation synchronization is a separate pass.
Release readiness is explicit release work.
```

This guide is stack-independent. The companion engineering guide defines concrete build, test, validation, packaging, release, and toolchain rules.

## Relationship to the Engineering Guide

Use this guide for:

- repository knowledge structure;
- documentation maturity;
- task modes;
- authority boundaries;
- milestone design;
- public documentation policy;
- agent routing.

Use the engineering guide for:

- command contracts;
- validation tiers;
- `.NET`, TypeScript, Blazor, Playwright, packaging, samples, CI, release, and tooling setup;
- concrete `eng/` scripts.

---

# 1. Core model

The repository should contain durable project truth, not every piece of process methodology.

## Durable repository truth

| Layer | Responsibility |
|---|---|
| `README.md` | First-contact user and contributor entry point. |
| `AGENTS.md` | Concise task routing for AI agents. |
| `docs/TERMINOLOGY.md` | Canonical vocabulary. |
| `docs/SPECS.md` | Index for behavioral truth and invariants. |
| `docs/ARCHITECTURE.md` | Index for structural system design, when warranted. |
| `docs/DECISIONS.md` | Index for durable design rationale, when warranted. |
| `docs/MILESTONES.md` | Index for staged implementation intent, when warranted. |
| `docs/ENGINEERING.md` | Index for local engineering and validation rules. |
| `docs/PUBLIC-DOCS.md` | Public documentation coordination, when public documentation is active. |
| `docs/RESEARCH.md` | Non-authoritative research notes, when useful. |
| `public-docs/` | Public consumer-facing documentation source, when active. |
| `eng/` | Canonical engineering command surface. |

## Removed from the default model

The following are not part of the default repository model:

```text
docs/TBPS.md
docs/tbps/
docs/GUARDRAILS.md
docs/guardrails/
.github/ISSUE_TEMPLATE/
```

They may be added only when the repository has repeated, repository-specific process complexity that cannot be handled by milestones, specs, engineering docs, or external guides.

The default rule is:

```text
Specs define truth.
Architecture defines structure.
Decisions define rationale.
Milestones define sequencing and focus.
Engineering defines how to build and validate.
Public docs explain supported usage.
AGENTS routes work.
External guides teach methodology.
```

---

# 2. Maturity modes

A repository does not need the same documentation surface at every stage.

## Mode A — Exploration / active design

Use when the product shape is still forming.

Required:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/specs/
docs/ENGINEERING.md
eng/
```

Optional:

```text
docs/MILESTONES.md
docs/milestones/
docs/RESEARCH.md
docs/research/
```

Do not require:

```text
public-docs/
docs/PUBLIC-DOCS.md
release readiness
package smoke tests
full architecture or ADR layers
```

## Mode B — Implementation-ready internal repository

Use when behavior is clear enough for focused implementation.

Required:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/specs/
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/MILESTONES.md when staged work is used
eng/
```

Optional:

```text
docs/ARCHITECTURE.md
docs/DECISIONS.md
```

## Mode C — Architecture-rich repository

Use when subsystem boundaries, runtime ownership, protocols, compilers, generators, browser/runtime boundaries, or durable integration choices matter.

Add:

```text
docs/ARCHITECTURE.md
docs/architecture/
docs/DECISIONS.md
docs/decisions/
```

Only create these layers when they contain real content.

Do not create empty architecture or decisions indexes merely for structural symmetry.

## Mode D — Public package / public artifact preparation

Use when the repository is preparing packages, APIs, source generators, CLIs, websites, or other externally consumed artifacts.

Add:

```text
docs/PUBLIC-DOCS.md
public-docs/
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/samples.md
public-docs/diagnostics.md
public-docs/versioning.md
public-docs/release-notes.md
```

Use “preview” status if public docs are intentionally incomplete.

## Mode E — Release-ready public repository

Use when the repository is ready to publish external artifacts.

Add release controls:

```text
public API baseline validation
package smoke tests
sample validation
release notes validation
public documentation validation
release-check command
```

The release-ready surface belongs to release tasks, not ordinary implementation tasks.

## Mode F — Process-heavy repository

Use only when the repository itself must version operational methodology for recurring contributor work.

Optional additions:

```text
docs/WORKING-MODEL.md
docs/WORKFLOWS.md
docs/workflows/
docs/GUARDRAILS.md
docs/guardrails/
docs/TBPS.md
docs/tbps/
.github/ISSUE_TEMPLATE/
```

This mode is exceptional.

---

# 3. Task modes

The repository should route agents by task mode.

## Planning task

Purpose:

```text
Explore, compare options, decide scope, identify uncertainty.
```

May read broadly:

```text
README.md
docs/*
docs/research/*
external project setup guide
external engineering guide
```

Outputs:

```text
research notes
planning notes
draft milestone
questions
```

## Design normalization task

Purpose:

```text
Move planning output into durable repository authority.
```

May update:

```text
docs/TERMINOLOGY.md
docs/SPECS.md
docs/specs/*
docs/ARCHITECTURE.md
docs/architecture/*
docs/DECISIONS.md
docs/decisions/*
docs/MILESTONES.md
```

This task may read broadly.

## Milestone authoring task

Purpose:

```text
Create implementation-heavy milestones that route focused work.
```

A milestone should contain:

```text
goal
scope
non-goals
focus areas
required authority
files likely affected
validation tier
direct documentation impact
deferred documentation impact
acceptance criteria
```

A milestone should not contain:

```text
full setup guide references
full engineering guide references
large required-reading lists
inline copies of specs that already exist
broad "update all docs" obligations
release readiness unless the milestone is release-oriented
```

## Focus-area implementation task

Purpose:

```text
Implement one bounded part of a milestone or spec.
```

Read only:

```text
README.md when needed
AGENTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
the relevant milestone section
the relevant specs
the relevant source/test files
```

Do not read by default:

```text
all specs
all milestones
all public docs
all architecture docs
all decisions
research guide copies
external setup/engineering guides
```

Documentation during implementation is direct only:

```text
update a spec if behavior changes
update public docs if the task explicitly changes public user behavior
update terminology if a canonical term changes
```

Broad synchronization waits for a documentation synchronization task.

## PR integration task

Purpose:

```text
Let repository workflows validate integration and quality gates.
```

The PR gate may run broader checks than the local implementation task.

## Documentation synchronization task

Purpose:

```text
Normalize documentation after implementation stabilizes.
```

May read broadly and update:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ARCHITECTURE.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
public-docs/*
```

This is the correct place for cross-reference cleanup, index updates, public-doc alignment, and removal of stale planning text.

## Release readiness task

Purpose:

```text
Prepare external artifacts for publication.
```

May run release validation and update:

```text
public-docs/*
NuGet package README source
release notes
versioning policy
public API baselines
package metadata
samples
release workflows
```

---

# 4. Repository structure

## Minimal base structure

```text
/
├─ README.md
├─ AGENTS.md
├─ docs/
│  ├─ TERMINOLOGY.md
│  ├─ SPECS.md
│  ├─ ENGINEERING.md
│  ├─ specs/
│  └─ engineering/
│     └─ command-contract.md
├─ eng/
├─ src/
└─ tests/
```

## Behavior-rich structure

```text
docs/
  TERMINOLOGY.md
  SPECS.md
  specs/
```

## Architecture-rich structure

```text
docs/
  ARCHITECTURE.md
  architecture/
  DECISIONS.md
  decisions/
```

## Milestone-driven structure

```text
docs/
  MILESTONES.md
  milestones/
```

## Public documentation structure

```text
docs/
  PUBLIC-DOCS.md

public-docs/
  getting-started.md
  installation.md
  concepts.md
  packages.md
  samples.md
  diagnostics.md
  versioning.md
  release-notes.md
  guides/
  api/
  diagnostics/
  nuget/
  samples/
  website/
```

## Research structure

```text
docs/
  RESEARCH.md
  research/
```

Research files are not operational authority.

Do not place active setup or engineering rules only in `docs/research/`.

---

# 5. README rule

Only the root-level repository `README.md` is allowed.

Do not create additional `README.md` files under:

```text
docs/
public-docs/
eng/
samples/
tools/
site/
src/
tests/
```

Use named Markdown documents instead.

Examples:

```text
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/engineering/command-contract.md
public-docs/getting-started.md
public-docs/nuget/Fletched.Core.md
```

---

# 6. Index document convention

A documentation folder may have exactly one index document.

The index document is:

```text
docs/<FOLDER>.md
```

where `<FOLDER>` is the uppercase folder name.

Examples:

```text
docs/SPECS.md indexes docs/specs/
docs/ARCHITECTURE.md indexes docs/architecture/
docs/DECISIONS.md indexes docs/decisions/
docs/MILESTONES.md indexes docs/milestones/
docs/ENGINEERING.md indexes docs/engineering/
docs/RESEARCH.md indexes docs/research/
```

Do not create folder-local README files.

## Index responsibilities

An index should:

- state purpose;
- define what belongs there;
- list available documents;
- identify authority boundaries;
- avoid duplicating document contents.

Do not create empty index layers unless they are expected to contain content immediately.

---

# 7. Authority sections

Use authority sections for documents that define durable behavior, structure, or process.

Template:

```md
## Authority

This document is authoritative for:
- ...

This document is not authoritative for:
- ...
```

Do not put authority sections in every document by reflex. Use them where ambiguity is likely.

---

# 8. Document synchronization

Do not require every implementation task to update every related document.

Use two categories.

## Direct documentation update

Required during implementation when the code change would otherwise contradict current authority.

Examples:

```text
spec-defined behavior changed
diagnostic contract changed
public API changed
canonical terminology changed
package usage changed
```

## Deferred documentation synchronization

Handled by a later documentation task.

Examples:

```text
index cleanup
cross-reference cleanup
README polish
public-doc narrative update
release notes
sample walkthrough expansion
architecture recap after several implementation changes
```

Implementation tasks may leave a documentation impact note instead of completing deferred documentation work.

---

# 9. Specifications

Specs define durable behavioral truth.

Rules:

- specs must be implementation-independent enough to survive refactoring;
- specs may name expected public behavior, invariants, invalid states, diagnostics, and compatibility expectations;
- specs must not become implementation work logs;
- specs should be updated before or during implementation when behavior changes;
- public docs may link to specs but must not replace specs.

---

# 10. Architecture and decisions

Use architecture docs when subsystem boundaries are durable and non-obvious.

Use decision records when rationale matters and future contributors are likely to reconsider the choice.

Do not create architecture and decision layers merely to satisfy a template.

Architecture docs answer:

```text
What are the major parts and ownership boundaries?
```

Decision docs answer:

```text
Why did we choose this over plausible alternatives?
```

---

# 11. Milestones

Milestones are implementation routers.

They should be detailed enough that implementation agents do not need to reconstruct planning context.

## Milestone template

```md
# M0000 — <Title>

## Status

<Planned | Active | Implemented | Superseded>

## Goal

...

## Scope

...

## Non-Goals

...

## Required Authority

- docs/specs/...
- docs/architecture/... when relevant
- docs/decisions/... when relevant

## Focus Areas

### Focus Area A — <Name>

#### Goal

...

#### Scope

...

#### Non-Goals

...

#### Likely Files

- src/...
- tests/...

#### Validation Tier

<Tier 0 | Tier 1 | Tier 2 | Tier 3 | Tier 4>

#### Direct Documentation Impact

- ...

#### Deferred Documentation Impact

- ...

#### Acceptance Criteria

- ...

## Integration Notes

...

## Completion Notes

...
```

## Milestone rules

- A milestone may reference specs; it must not duplicate full spec bodies once specs exist.
- A milestone may split implementation into focus areas.
- A milestone may state validation tiers.
- A milestone may identify direct and deferred documentation impact.
- A milestone should not require reading the whole repository documentation set.
- A milestone should not reference external setup or engineering guides as operational authority.

---

# 12. Public documentation

Public documentation is optional by maturity.

Use it when the repository has public consumers or is preparing for public release.

Public docs explain supported usage. Internal specs define truth.

## Public documentation statuses

```text
Preview
  Public docs exist but are incomplete and not release-ready.

Active
  Public docs are maintained for current consumers.

Release-ready
  Public docs are part of the release gate.
```

## Public docs rules

- public docs must be user-first;
- public docs must not duplicate internal specs verbatim;
- public docs must use canonical terminology;
- public docs must distinguish planned behavior from supported behavior;
- package README source should live under `public-docs/nuget/`;
- diagnostics should be documented by diagnostic ID when diagnostics are public;
- release notes should describe externally visible changes.

---

# 13. Engineering documentation

`docs/ENGINEERING.md` is the local index for build, test, validation, tooling, packaging, release, samples, and CI behavior.

It should link to:

```text
docs/engineering/command-contract.md
docs/engineering/dotnet.md when needed
docs/engineering/typescript-tools.md when needed
docs/engineering/packaging.md when needed
docs/engineering/release-readiness.md when needed
docs/engineering/samples.md when needed
```

Do not split guardrails into a separate layer unless repository-specific constraints are large enough to justify it.

Testing, implementation, language, and validation rules normally belong under `docs/ENGINEERING.md` or `docs/engineering/`.

---

# 14. AGENTS.md

`AGENTS.md` should be a routing accelerator, not a documentation index.

## Template

```md
# Agent Instructions

## Default Implementation Path

Read:
- docs/ENGINEERING.md
- docs/engineering/command-contract.md
- the relevant milestone section or task description
- relevant specs under docs/specs/

Use canonical `eng/` commands only.

For narrow implementation work:
- use the validation tier named by the milestone or task;
- do not run release validation unless explicitly requested;
- do not perform broad documentation synchronization unless explicitly requested;
- update directly affected specs only when behavior changes.

## Conditional Reading

Read `docs/TERMINOLOGY.md` when terminology is introduced, changed, or unclear.

Read `docs/PUBLIC-DOCS.md` and `public-docs/` only when changing public API, package behavior, samples, diagnostics, public docs, or externally visible behavior.

Read `docs/ARCHITECTURE.md` and `docs/decisions/` only when changing subsystem boundaries or durable design choices.

Read `docs/MILESTONES.md` only when planning, splitting, or completing milestone work.

Read `docs/RESEARCH.md` and `docs/research/` only for planning or documentation tasks.

## Repository Rules

- Do not create README files outside the repository root.
- Do not invent build, test, format, package, release, or documentation validation commands.
- Keep changes scoped to the task.
- Prefer specs over inferred behavior.
- Use focused validation for focused implementation.
- Let PR workflows perform integration validation.
- Treat research guide copies as non-authoritative.
```

---

# 15. GitHub Copilot instructions

`.github/copilot-instructions.md` should be short.

Template:

```md
# GitHub Copilot Instructions

Follow `AGENTS.md`.

Use documented `eng/` commands.

For implementation:
- read the relevant milestone/task;
- read relevant specs;
- keep changes scoped;
- use the requested validation tier.

Do not perform broad documentation synchronization unless requested.

Do not treat docs/research guide copies as operational authority.
```

---

# 16. Issue templates

Issue templates are not required.

If the repository uses milestones and specs to transport work, issue templates should be absent or minimal.

Recommended default:

```text
No .github/ISSUE_TEMPLATE/ folder.
```

If issue templates are kept, they must not carry broad methodology or required-reading lists.

A minimal implementation issue can be:

```md
# Implementation Task

Milestone:
Focus area:
Relevant specs:
Validation tier:
Notes:
```

---

# 17. TBPs and process methodology

Task Best Practices are not part of the default repository model.

Use TBPs only in process-heavy repositories where recurring task methodology must be versioned inside the repository.

For most repositories, do not create:

```text
docs/TBPS.md
docs/tbps/
```

Methodology should live in external guides or be encoded directly in:

```text
milestones
specs
docs/ENGINEERING.md
AGENTS.md
```

---

# 18. Workflows

Workflow documentation is optional.

Use workflow docs only when CI, release, publishing, or multi-step operational flows are non-trivial.

If used:

```text
docs/WORKFLOWS.md
docs/workflows/
```

Workflow docs should describe workflow intent and relationship to `eng/` scripts, not duplicate full YAML.

---

# 19. Research

Research is non-authoritative.

Use `docs/research/` for:

```text
planning notes
external guide copies
investigation results
alternative designs
historical rationale
```

Rules:

- do not require implementation agents to read research by default;
- do not treat copied setup or engineering guides as operational authority;
- extract active rules into repository docs before relying on them.

---

# 20. Completion model

A repository aligned with this guide is complete when:

- required maturity-mode files exist;
- optional layers exist only when they carry real content;
- `AGENTS.md` is concise and conditional;
- milestones route implementation without duplicating specs;
- implementation tasks use focused validation;
- PR workflows perform integration validation;
- documentation synchronization is a separate task type;
- release readiness is explicit release work;
- no non-root README files exist;
- no TBP or issue-template layer exists unless process-heavy mode is explicitly chosen.
