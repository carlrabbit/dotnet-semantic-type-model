# Milestones

## Purpose

Milestones are active implementation work orders. They are deleted after durable outcomes are synchronized; Git retains completed history.

## Current

- None. M0072 is complete; durable implementation, documentation, and release-readiness evidence are preserved in Git history.

## Next Number

```text
M0073
```

Never restart or reuse milestone numbers across maintenance or mainline work.

## Lifecycle

```text
draft/planning -> ready -> implementing -> done
```

Planning resolves material architecture, semantic, compatibility, scope, acceptance, validation, and human-review decisions before a coding or release-readiness milestone becomes `ready` and must make the milestone executable by the configured baseline implementation model.

For AI-executed coding milestones, implementation begins by decomposing the ready contract into bounded work packages and creating or reconciling:

```text
.execution/<milestone-id>.md
```

The execution ledger is operational state only. It does not amend or replace milestone or project authority.

Implementation owns milestone closure:

```text
read milestone + authority
-> execution decomposition
-> create/reconcile execution ledger
-> implement/validate/update ledger
-> freshly reread milestone
-> reconcile milestone <-> ledger <-> repository/evidence
-> completion audit
-> continue or terminate
```

Passing tests or completing listed focus areas is evidence, not completion by itself. The executor continues resolving every milestone obligation that can be completed without changing the ready contract and terminates only with a terminal outcome permitted by the active milestone.

For distributable artifacts, internal/source-project validation alone is insufficient when the milestone affects the consumer surface. Required validation includes a representative path through the current packed/published artifact using its intended consumption mechanism.

After implementation, synchronize durable outcomes into specifications, architecture, decisions, engineering, tests, public documentation, and release evidence as appropriate. Delete the completed milestone file and its operational execution ledger; Git retains history.
