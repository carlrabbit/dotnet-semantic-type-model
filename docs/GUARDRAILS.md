# Guardrails

## Purpose

Guardrails define project-wide constraints that apply across tasks.

Guardrails are not specs.
Guardrails are not TBPs.
Guardrails constrain how implementation, testing, and documentation are performed.

## Available Guardrails

| Guardrail | Purpose |
|---|---|
| guardrails/testing.md | Test classification and execution rules |
| guardrails/implementation.md | General implementation rules |
| guardrails/languages/dotnet.md | .NET-specific rules |

## Rules

- General guardrails apply to all work.
- Language guardrails apply when touching that language or runtime.
- More specific guardrails may refine general guardrails.
- More specific guardrails must not silently contradict general guardrails.

## Related Engineering Documents

- docs/ENGINEERING.md
- docs/engineering/dotnet.md
