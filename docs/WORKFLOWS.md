# Workflows

## Purpose

Workflow specifications describe operational intent before GitHub Actions implementation.

Workflow specs are authoritative for:
- workflow goal;
- workflow constraints;
- high-level behavior;
- validation expectations.

GitHub workflow YAML files are implementation artifacts.

## Available Workflows

| Workflow | Purpose |
|---|---|
| workflows/ci.md | CI workflow: restore, build, test, format check |
| workflows/public-docs.md | Public documentation validation workflow intent |
| workflows/release-check.md | Pre-release validation workflow intent |
| workflows/release.md | Release orchestration workflow intent |

## Related Documents

- docs/ENGINEERING.md
- docs/PUBLIC-DOCS.md
- docs/guardrails/testing.md
