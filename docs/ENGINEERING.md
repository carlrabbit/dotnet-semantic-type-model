# Engineering

## Purpose

Engineering documents define repository commands, toolchain setup, validation commands, optional modules, and stack-specific setup rules.

Engineering documents are authoritative for:
- command contracts;
- build, restore, test, format, benchmark, package, and release commands;
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

## Rules

- Humans, agents, and CI must use canonical engineering commands.
- CI must call engineering scripts instead of duplicating logic.
- Optional modules are absent by default.
- Tooling must be pinned or explicit.
- Command changes must update this index and affected engineering documents.

## Related Documents

- docs/GUARDRAILS.md
- docs/guardrails/testing.md
- docs/guardrails/implementation.md
- docs/WORKFLOWS.md
