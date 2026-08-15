# Agent Instructions

## Start Here

Use this file as the repository router. Read only the additional authority needed for the assigned task.

### Current Authority

- Read `docs/TERMINOLOGY.md` before introducing or changing project terminology.
- Read the relevant current specification under `docs/specs/` when changing behavior.
- Read `docs/ARCHITECTURE.md` and the relevant document under `docs/architecture/` when changing structural boundaries, package responsibilities, dependency direction, or compile-time/runtime composition.
- Read `docs/DECISIONS.md` and relevant current decisions when a non-obvious architectural choice constrains the work.
- Read `docs/ENGINEERING.md` and `docs/engineering/command-contract.md` when choosing validation commands or changing engineering policy.
- Read `docs/PUBLIC-DOCS.md` and affected `public-docs/` pages when changing consumer-facing behavior, package metadata, diagnostics, samples, installation, migration, or release guidance.
- Read workflow docs and workflow YAML together when changing CI, packaging, release, or publishing automation.
- Read `docs/MILESTONES.md` and an active milestone only when the task is routed through that milestone.
- Read `docs/HISTORY.md` only when architectural evolution is relevant.
- Read `.guide-profile.json` and `.guide-sync/` only when explicitly assigned guide migration, documentation synchronization, or release-readiness planning work.

## Authority Roles

```text
Specification  = what behavior is required now.
Architecture   = how the major pieces fit together.
Decision       = why a non-obvious current choice exists.
Engineering    = how repository work is built, validated, packaged, and synchronized.
Public docs    = supported consumer guidance.
Milestone      = what active implementation work should happen next.
History        = concise architectural evolution.
Git history    = detailed completed work and superseded designs.
```

A milestone does not override a current specification or decision. Historical Git content does not override current working-tree authority.

## Documentation Lifecycle

- The working tree should contain current truth and active work, not an archive of every prior design.
- Completed milestone files are deleted after durable results are synchronized into current authority.
- Superseded decisions/specifications are deleted after any still-current requirement or rationale is promoted into replacement authority.
- Do not create `docs/archive/` merely to retain material already preserved by Git.
- Do not copy external project-setup or engineering guide documents into this repository.
- `.guide-sync/pending/` contains unresolved current synchronization work only and should normally be empty.
- Prefer extending an existing authoritative document over creating a new document.

Create a new documentation file only when it represents a distinct authority boundary: a new subsystem contract, an independently durable architectural decision, a new consumer/package/audience entry point, an active milestone, or a genuinely separate research subject.

## Validation Tiers

Use the smallest validation tier that can catch the expected regression during the inner loop, then run the completion tier required by the task.

- **Tier 0 — static/documentation check:** formatting, documentation-only checks, or script linting for files touched.
- **Tier 1 — focused validation:** a targeted project, filtered test run, or affected-area command such as `./eng/test-project.sh <project>` or `./eng/test-filter.sh <filter>`.
- **Tier 2 — repository check:** `./eng/check.sh` (`restore`, `build`, short-running tests, and format verification). This is the standard completion gate for implementation work, not mandatory for every inner-loop edit.
- **Tier 3 — package/release candidate validation:** package, package-smoke, public API, public docs, samples, and release-readiness commands appropriate to the release candidate.
- **Tier 4 — publish validation:** final release validation plus the explicit publish workflow/command.

Before completing implementation work, run Tier 2 unless the task is documentation-only or an environment limitation prevents it. For release and packaging work, also run:

```sh
./eng/release-check.sh <version>
```

## Repository Rules

- Use canonical `eng/` scripts.
- Keep workflow docs and workflow YAML synchronized.
- Keep public docs synchronized with consumer-facing changes.
- Do not add README files outside repository root.
- Keep changes scoped and avoid opportunistic refactoring.
- Preserve public contracts unless an authoritative spec changes.
- Do not introduce terminology that is absent from `docs/TERMINOLOGY.md`.
- Prefer deterministic, short-running tests by default; do not add network, timing-dependent, or expensive tests to the short-running suite.
- Do not introduce TBPs, broad guardrail documents, default issue templates, or workflow documents unless a project-specific current requirement makes them repository truth.
