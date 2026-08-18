# Milestones

## Purpose

Milestones are active implementation work orders. They are deleted after durable outcomes are synchronized; Git retains completed history.

## Current

- `M0066` — `docs/milestones/m0066-json-representation-fidelity.md` — `ready`

## Next Number

```text
M0067
```

Never restart or reuse milestone numbers.

## Lifecycle

```text
draft/planning -> ready -> implementing -> done
```

Planning resolves material architecture, semantic, compatibility, scope, acceptance, and validation decisions before a coding milestone becomes `ready`.

Implementation starts from the ready milestone and repository-local authority, re-inspects the live source and tests, and owns concrete implementation mechanics that fit the contract.

If implementation discovers a material unresolved decision, return that issue to planning rather than silently changing project policy.

After implementation, synchronize durable outcomes into specifications, architecture, decisions, engineering, tests, and public documentation as appropriate. Delete the completed milestone file; Git retains history.
