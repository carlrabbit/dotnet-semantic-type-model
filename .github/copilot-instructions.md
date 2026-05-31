# GitHub Copilot Instructions

Read `AGENTS.md` first. Then read task-relevant authoritative docs only:

- `docs/ENGINEERING.md` and `docs/engineering/command-contract.md` for commands and validation tiers.
- Relevant `docs/specs/` files for behavior changes.
- `docs/PUBLIC-DOCS.md` and affected `public-docs/` files for consumer-facing changes.
- Workflow docs together with workflow YAML for automation changes.

Rules:

- Keep changes scoped.
- Use canonical commands under `eng/`.
- Keep workflow docs synchronized with workflow YAML.
- Keep public documentation synchronized with package/release changes.
- Do not treat `docs/research/project-setup-guide-*` or `docs/research/engineering-guide-*` as authoritative.
