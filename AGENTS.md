# Agent Instructions

## Required Reading

Before non-trivial work, read:

1. docs/TERMINOLOGY.md
2. docs/GUARDRAILS.md
3. docs/TBPS.md
4. docs/SPECS.md
5. docs/ENGINEERING.md
6. docs/MILESTONES.md
7. Relevant architecture, specs, decisions, milestones, workflows, TBPs, guardrails, and engineering documents

## Required Workflow

Before completing implementation work, run:

```sh
./eng/check.sh
```

If this command fails, fix the failure or document exactly why it could not be fixed.

## Repository Rules

- Do not add README files outside the root README.md.
- Use eng/ scripts instead of inventing commands.
- Do not add new root-level folders without updating documentation.
- Do not add package versions directly to project files. Use Directory.Packages.props.
- Do not use npm. Use Bun for JavaScript/TypeScript tooling.
- Do not add ESLint or Prettier. Use Biome unless explicitly instructed otherwise.
- Do not add slow tests to the default test path.
- Do not run benchmarks during normal validation.
- Do not introduce Playwright unless the Playwright building block is applied.
- Prefer small, vertical changes over broad rewrites.
- Preserve the command contract under eng/.
- Follow docs/guardrails/testing.md before creating or running tests.
- Follow docs/guardrails/implementation.md before implementation.
- Keep specs authoritative for behavior.
- Keep workflow specs synchronized with workflow implementations.
- Keep engineering command contracts synchronized with scripts.

## Validation

Use the minimal relevant validation from docs/ENGINEERING.md and docs/guardrails/testing.md.

Do not run long-running tests unless explicitly requested.
