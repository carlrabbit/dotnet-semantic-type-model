# Milestones

## Purpose

Milestones are active implementation work orders. They are deleted after durable outcomes are synchronized; Git retains completed history.

## Current

- `M0068` — `docs/milestones/m0068-strong-scalar-and-4-1.md` — `ready`

## Branch-Lineage Note

`M0067` is already allocated to the Configuration/Options removal on the later mainline and is intentionally absent from the 4.1 feature line. Never reuse it on this branch.

The 4.1 line starts from the post-M0066, pre-M0067 repository state.

## Next Number

```text
M0069
```

Never restart or reuse milestone numbers across divergent release lines.

## Lifecycle

```text
draft/planning -> ready -> implementing -> done
```

Planning resolves material architecture, semantic, compatibility, scope, acceptance, validation, human-review, and project-invariant decisions before a coding milestone becomes `ready`.

A `ready` milestone must also be executable by the configured baseline implementation model using the milestone, referenced project authority, the live repository, and normal repository tooling without inventing a new material project decision.

Implementation starts from the ready milestone and repository-local authority, re-inspects the live source and tests, and owns concrete implementation mechanics that fit the contract.

If implementation discovers a material unresolved decision, return that issue to planning rather than silently changing project policy.

Implementation owns milestone closure:

```text
implement -> validate -> completion audit -> continue or terminate
```

Passing tests or completing listed focus areas is evidence, not completion by itself. The executor continues resolving every milestone obligation that can be completed without changing the ready contract and terminates only as `COMPLETE`, `AWAITING HUMAN REVIEW`, or `BLOCKED`.

After implementation, synchronize durable outcomes into specifications, architecture, decisions, engineering, tests, and public documentation as appropriate. Delete the completed milestone file; Git retains history.
