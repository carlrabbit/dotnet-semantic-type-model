# Guide System Adoption Cleanup

## Status

Pending documentation synchronization hint.

## Source

```text
docs/milestones/m0036-adopt-external-agentic-project-guide-system-v0.3.0.md
```

## Purpose

Track deferred cleanup after adopting external guide-system metadata.

This file is not ordinary implementation authority. Ordinary implementation agents should not read `.guide-sync/` unless explicitly assigned guide migration, documentation synchronization, or release-readiness planning work.

## Resolved Guide System

```text
Repository: carlrabbit/agentic-project-guides
Version: 0.3.0
```

## Deferred Cleanup Items

| Item | Classification | Action |
|---|---|---|
| `docs/research/project-setup-guide-*.md` | deprecated / manual-review | Keep only as historical research or delete in a focused cleanup; never treat as active project authority. |
| `docs/research/engineering-guide-*.md` | deprecated / manual-review | Keep only as historical research or delete in a focused cleanup; never treat as active project authority. |
| `docs/TBPS.md` | no-op unless present | Do not introduce unless a future project-specific milestone requires it as project truth. |
| `docs/GUARDRAILS.md` | no-op unless present | Do not introduce broad guardrail documents from external guide methodology by default. |
| `docs/WORKFLOWS.md` | no-op unless present | Do not introduce workflow docs unless project-specific workflow automation requires them. |
| `.github/ISSUE_TEMPLATE/*` | conditional / deprecated | Do not introduce default issue templates from older guide models; keep only project-specific templates if intentionally added. |
| `README.md` and `public-docs/*` | no-op unless affected | Do not mention external guides unless consumer-facing behavior changes, which this migration does not do. |

## Documentation Sync Notes

- Keep target repository documentation focused on SemanticTypeModel project truth.
- Keep external guide references limited to `.guide-profile.json`, this hint, and migration planning material.
- If copied guide documents remain under `docs/research/`, ensure their active authority headers are removed or superseded by clear historical/non-authoritative status.
