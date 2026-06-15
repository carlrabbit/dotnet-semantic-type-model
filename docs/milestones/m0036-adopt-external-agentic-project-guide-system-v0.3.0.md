# M0036: Adopt External Agentic Project Guide System v0.3.0

## Status

Planned.

## Resolved Guide Version

The latest available guide-system version resolved during planning is `0.3.0`.

Resolution source:

```text
carlrabbit/agentic-project-guides
README.md
CHANGELOG.md
meta/VERSIONING.md
meta/MIGRATION-MODEL.md
migrations/guide-system-v0.2.0-to-v0.3.0.md
templates/PROMPTS.md
```

Implementation agents must not read the external guide repository. This milestone contains the repository-local migration instructions needed to implement the adoption.

## Maturity Mode

Post-2.0 public .NET library repository with package, sample, public documentation, and agent-routing surfaces.

The repository already contains localized project authority documents such as `AGENTS.md`, `docs/ENGINEERING.md`, `docs/engineering/command-contract.md`, `docs/TERMINOLOGY.md`, `docs/SPECS.md`, `docs/MILESTONES.md`, and `docs/PUBLIC-DOCS.md`.

## Task Mode

Engineering/documentation migration.

The task is to adopt the external guide-system model as planning metadata without copying guide documents into the repository and without making external guide documents operational authority for implementation agents.

## Goal

After M0036:

- the repository records its guide-selection metadata in `.guide-profile.json`;
- ordinary implementation agents continue to start from `AGENTS.md` and target-repository authority documents only;
- copied guide documents under `docs/research/` are treated as legacy/non-authoritative research copies;
- `.guide-sync/` exists only as deferred documentation-synchronization metadata for planning and documentation agents;
- no TBP, guardrail, workflow, issue-template, or copied guide layer is reintroduced as default repository methodology;
- disconnected planning and implementation handoff is explicit through milestone authority lists and chat execution prompts.

## Required Authority

Always read:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/MILESTONES.md
```

Read only when the selected focus area touches public documentation or compatibility wording:

```text
docs/PUBLIC-DOCS.md
README.md
.github/copilot-instructions.md
```

Read only to identify legacy copied-guide state:

```text
docs/research/project-setup-guide-*.md
docs/research/engineering-guide-*.md
```

Do not read the external guide repository during implementation.

Do not require ordinary implementation agents to read:

```text
.guide-profile.json
.guide-sync/
docs/research/project-setup-guide-*.md
docs/research/engineering-guide-*.md
```

unless the task explicitly assigns guide migration, documentation synchronization, or release-readiness planning.

## Migration Classification

| Work item | Classification | Required action |
|---|---|---|
| Add `.guide-profile.json` | Required | Add guide-selection metadata for planning/documentation agents. |
| Add `.guide-sync/pending/` hints | Required | Add deferred cleanup hints without making them operational authority for implementation agents. |
| Update `AGENTS.md` routing | Required | Ensure ordinary implementation agents ignore guide metadata and external guide docs unless explicitly assigned guide work. |
| Update `docs/TERMINOLOGY.md` | Required | Add guide-profile and guide-sync terminology used by repository-local docs. |
| Update `docs/MILESTONES.md` | Required | Index this migration milestone. |
| Copied setup/engineering guides under `docs/research/` | Deprecated/manual-review | Keep only as historical research or remove in a focused documentation cleanup; never treat as operational authority. |
| TBPs, broad guardrails, workflows, and default issue templates from older guide models | Conditional/deprecated | Remove or leave absent unless they are project-specific truth and explicitly required by a later milestone. |
| `.github/copilot-instructions.md` | No-op unless stale | It may remain concise if it points to `AGENTS.md` and does not duplicate methodology. |
| Public docs | No-op unless affected | Do not update consumer docs merely because guide metadata changed. |
| Source code, tests, build scripts, workflows | No-op | This migration is documentation/metadata only. |

## Focus Areas

### Focus Area 1 — Add Guide Profile Metadata

#### Intent

Record external guide-system selection metadata without making the external guide repository an operational dependency for implementation agents.

#### Implementation Requirements

- Add `.guide-profile.json`.
- Record:
  - guide repository;
  - resolved guide-system version `0.3.0`;
  - target repository role;
  - inferred profile;
  - maturity mode;
  - execution mode;
  - authority rule that implementation agents use target-local docs only.
- Keep the file small and machine-readable.
- Do not require ordinary implementation agents to read it.

#### Validation

- Tier 0:
  - verify `.guide-profile.json` is valid JSON;
  - verify `AGENTS.md` tells ordinary implementation agents to ignore `.guide-profile.json` unless assigned guide work.

### Focus Area 2 — Add Deferred Guide-Sync Hints

#### Intent

Create a small `.guide-sync/pending/` queue for documentation cleanup that should not block ordinary implementation.

#### Implementation Requirements

- Add one or more Markdown hints under `.guide-sync/pending/`.
- Each hint must:
  - state that it is deferred synchronization metadata;
  - identify affected files or file patterns;
  - classify work as required, conditional, deprecated, manual-review, or no-op;
  - avoid copying external guide content.
- Do not require ordinary implementation agents to read `.guide-sync/`.

#### Validation

- Tier 0:
  - verify hints are Markdown files;
  - verify `AGENTS.md` says `.guide-sync/` is not required reading for ordinary implementation work.

### Focus Area 3 — Localize Agent Routing

#### Intent

Ensure the repository’s agent routing uses local project authority only.

#### Implementation Requirements

- Update `AGENTS.md` only as needed.
- Keep it concise.
- Preserve conditional-reading behavior.
- Keep `docs/research/project-setup-guide-*` and `docs/research/engineering-guide-*` non-authoritative.
- Explicitly prohibit ordinary implementation agents from reading the external guide repository.
- Do not copy methodology from external guides into `AGENTS.md`.
- Do not reintroduce TBPs, issue templates, broad guardrail documents, or workflow documents as default repository layers.

#### Validation

- Tier 0:
  - inspect `AGENTS.md`;
  - verify `.github/copilot-instructions.md` remains concise and points to `AGENTS.md`.

### Focus Area 4 — Review Legacy Guide Copies

#### Intent

Prevent copied setup or engineering guides from appearing to govern current repository behavior.

#### Implementation Requirements

- Inspect `docs/research/project-setup-guide-*.md` and `docs/research/engineering-guide-*.md` only to identify legacy state.
- If copied guides claim active authority, either:
  - mark them as historical/non-authoritative research copies; or
  - delete them in a focused documentation cleanup if no longer useful.
- Do not move copied guide text into active docs.
- Do not make current docs reference copied guides as operational authority.

#### Validation

- Tier 0:
  - search for active required-reading references to copied guide files;
  - verify `AGENTS.md` and `.github/copilot-instructions.md` do not route implementation agents to copied guides.

### Focus Area 5 — Index and Terminology Synchronization

#### Intent

Keep project-local index and terminology docs current without broad methodology import.

#### Implementation Requirements

- Add M0036 to `docs/MILESTONES.md`.
- Add guide metadata terms to `docs/TERMINOLOGY.md`.
- Do not add new `docs/TBPS.md`, `docs/GUARDRAILS.md`, `docs/WORKFLOWS.md`, or default issue templates.
- Do not update public docs unless a consumer-visible behavior changes.

#### Validation

- Tier 0:
  - verify all terms used by M0036 and `AGENTS.md` exist in `docs/TERMINOLOGY.md`;
  - verify `docs/MILESTONES.md` links to this milestone.

## Direct Documentation Impact

Resolve during this milestone:

```text
AGENTS.md
.guide-profile.json
.guide-sync/pending/guide-system-adoption-cleanup.md
docs/TERMINOLOGY.md
docs/MILESTONES.md
docs/milestones/m0036-adopt-external-agentic-project-guide-system-v0.3.0.md
```

## Deferred Documentation Impact

Keep as deferred documentation-sync hints unless implementation discovers active guide leakage:

```text
docs/research/project-setup-guide-*.md
docs/research/engineering-guide-*.md
.github/ISSUE_TEMPLATE/*
docs/TBPS.md
docs/GUARDRAILS.md
docs/WORKFLOWS.md
docs/PUBLIC-DOCS.md
README.md
public-docs/*
```

## Validation Summary

This is a documentation/metadata migration.

Required completion validation:

```text
Tier 0
```

Suggested commands:

```sh
python -m json.tool .guide-profile.json > /tmp/guide-profile.validated.json
./eng/check-affected.sh AGENTS.md docs/TERMINOLOGY.md docs/MILESTONES.md docs/milestones/m0036-adopt-external-agentic-project-guide-system-v0.3.0.md
```

Run `./eng/public-docs.sh` only if public documentation files are changed.

Run Tier 2 only if implementation broadens beyond documentation/metadata changes.

Do not run release validation; this is not a release-readiness milestone.

## Acceptance Criteria

- `.guide-profile.json` exists and records guide-system version `0.3.0`.
- `.guide-sync/pending/` contains deferred guide-cleanup metadata.
- `AGENTS.md` tells ordinary implementation agents to use target-local authority docs and not the external guide repository.
- `AGENTS.md` keeps copied setup/engineering guides under `docs/research/` non-authoritative.
- `docs/TERMINOLOGY.md` contains terms used by this milestone and guide metadata.
- `docs/MILESTONES.md` indexes M0036.
- No copied external guide documents are added.
- No TBPs, issue templates, broad guardrails, or workflow documents are introduced.
- No public docs are changed unless a consumer-facing behavior change is identified.
