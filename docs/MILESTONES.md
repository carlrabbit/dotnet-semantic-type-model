# Milestones

## Purpose

Milestones are active implementation work orders. They are deleted after durable outcomes are synchronized; Git retains completed history.

## Current

No active milestone.

## Next Number

```text
M0071
```

Never restart or reuse milestone numbers across maintenance or mainline work.

## Lifecycle

```text
draft/planning -> ready -> implementing -> done
```

Planning resolves material architecture, semantic, compatibility, scope, acceptance, and validation decisions before a coding or release-readiness milestone becomes `ready` and must make the milestone executable by the configured baseline implementation model.

Implementation starts from the ready milestone and repository-local authority, re-inspects the live repository, and owns concrete implementation mechanics that fit the contract.

If implementation discovers a material unresolved decision, return that issue to planning rather than silently changing project policy.

Implementation owns milestone closure:

```text
implement -> validate -> completion audit -> continue or terminate
```

Passing tests or completing listed focus areas is evidence, not completion by itself. The executor continues resolving every milestone obligation that can be completed without changing the ready contract and terminates only with a terminal outcome permitted by the active milestone.

After implementation, synchronize durable outcomes into specifications, architecture, decisions, engineering, tests, public documentation, and release evidence as appropriate. Delete the completed milestone file; Git retains history.
