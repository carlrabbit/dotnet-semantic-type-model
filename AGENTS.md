# Agent Instructions

## Authority and Conditional Reading

Start with this file. Read additional documents only when they are relevant to the task:

- Read `docs/ENGINEERING.md` and `docs/engineering/command-contract.md` when choosing validation commands or changing engineering policy.
- Read relevant specs under `docs/specs/` when changing behavior covered by a specification.
- Read `docs/PUBLIC-DOCS.md` and affected `public-docs/` pages when changing consumer-facing behavior, package metadata, diagnostics, samples, or release guidance.
- Read workflow docs and workflow YAML together when changing CI, packaging, release, or publishing automation.
- Read architecture or decision records only when changing repository structure or durable rationale.

Do not treat `docs/research/project-setup-guide-*` or `docs/research/engineering-guide-*` as authoritative. They are historical research copies and are not required reading.

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
