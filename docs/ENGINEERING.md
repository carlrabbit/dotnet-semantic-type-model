# Engineering

## Purpose

Engineering documents define repository commands, toolchain setup, validation commands, optional modules, and stack-specific setup rules.

Engineering documents are authoritative for:
- command contracts;
- build, restore, test, format, benchmark, package, samples, and release-readiness commands;
- toolchain setup;
- stack-specific building blocks;
- optional engineering modules.

Engineering documents are not authoritative for:
- domain behavior;
- application architecture;
- milestone scope;
- long-term product semantics.

## Available Engineering Documents

| Document | Purpose |
|---|---|
| engineering/dotnet.md | .NET 10 + MTP + TUnit + BenchmarkDotNet engineering profile |
| engineering/command-contract.md | Canonical repository command contract |
| engineering/building-blocks.md | Building block summary and selection rules |
| engineering/public-documentation.md | Public documentation validation and synchronization rules |
| engineering/release-readiness.md | Release gate command and release checklist rules |
| engineering/packaging.md | Packaging and package smoke validation policy |
| engineering/samples.md | Runnable sample engineering policy |

## Rules

- Humans, agents, and CI must use canonical engineering commands.
- CI must call engineering scripts instead of duplicating logic.
- Optional modules are absent by default.
- Tooling must be pinned or explicit.
- Command changes must update this index and affected engineering documents.

## Related Documents

- docs/GUARDRAILS.md
- docs/PUBLIC-DOCS.md
- docs/guardrails/testing.md
- docs/guardrails/implementation.md
- docs/WORKFLOWS.md
