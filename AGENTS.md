# Agent Instructions

Use this file as the repository router. Read only the authority required for the task.

## Route the task

- Behavior: `docs/SPECS.md` + relevant spec.
- Structural/package/compile-time boundaries: `docs/ARCHITECTURE.md` + relevant architecture.
- Non-obvious current rationale: `docs/DECISIONS.md` + relevant decision.
- Terminology: `docs/TERMINOLOGY.md`.
- Commands, testing, packaging, documentation lifecycle: `docs/ENGINEERING.md` + relevant `docs/engineering/*`.
- Consumer-facing behavior/configuration/diagnostics: `docs/PUBLIC-DOCS.md` + affected `public-docs/*`.
- CI/release automation: `docs/WORKFLOWS.md`, workflow docs, and workflow YAML together.
- Active milestone work: `docs/MILESTONES.md` + active milestone only when one exists.
- Architectural evolution: `docs/HISTORY.md` only when historical context is relevant.

## Authority roles

```text
Specification = required behavior now
Architecture  = how major pieces fit
Decision      = why a non-obvious current choice exists
Engineering   = how repository work is built/validated/packaged
Public docs   = supported consumer guidance
Milestone     = active implementation sequencing
History       = concise architectural evolution
Git           = detailed completed/superseded history
```

## Consumer-documentation rule

For every public capability touched, ensure users can find:

1. how to use it;
2. how to configure/customize it;
3. what can fail and how to fix it.

Public generator options belong in `public-docs/configuration.md`. Public diagnostics belong in
`public-docs/diagnostics.md` or a diagnostic range page. Projection-specific use/configuration/failures belong
in the target guide.

Do not create per-package NuGet README sources: all packable packages use
`public-docs/nuget/SemanticTypeModel.md`.

Do not create per-sample Markdown pages. The sample project is the detailed example; route through
`public-docs/samples.md`.

All `SemanticTypeModel.*` package versions used together must match exactly.

## Repository rules

- Use canonical `eng/` commands.
- Keep changes scoped; avoid opportunistic refactoring.
- Preserve public contracts unless current authority changes them.
- Do not add non-root `README.md` files.
- Prefer extending current authority over creating new documentation.
- Do not retain obsolete documentation as an archive; Git is history.
- Run the validation tier required by `docs/ENGINEERING.md`.
