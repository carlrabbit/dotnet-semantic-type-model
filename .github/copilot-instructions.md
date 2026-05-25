# GitHub Copilot Instructions

Read:

- AGENTS.md
- docs/TERMINOLOGY.md
- docs/GUARDRAILS.md
- docs/ENGINEERING.md
- docs/TBPS.md
- docs/MILESTONES.md
- relevant specs
- relevant architecture documents
- relevant language guardrails

Rules:

- Keep changes scoped.
- Prefer documented behavior over inferred behavior.
- Validate against specs.
- Use canonical engineering commands from docs/ENGINEERING.md.
- Use short-running tests by default.
- Do not run long-running tests unless explicitly requested.
- Document public API intent, not implementation mechanics.
- Keep public documentation surfaces (`README.md`, `docs/PUBLIC-DOCS.md`, `public-docs/`) synchronized with consumer-facing changes.
