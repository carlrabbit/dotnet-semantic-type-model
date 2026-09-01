# Milestones

## Purpose

Milestones are active implementation work orders. They are deleted after durable outcomes are synchronized; Git retains completed history.

## Current

- None. M0073 is complete; its durable test-matrix and defect-fix outcomes are synchronized in the repository.

## Next Number

```text
M0074
```

Never restart or reuse milestone numbers.

## Lifecycle

```text
draft/planning -> ready -> implementing -> done
```

For AI-executed coding milestones, implementation creates or reconciles `.execution/<milestone-id>.md` before production edits. The ledger is operational state only.

Implementation owns closure:

```text
read milestone + authority
-> execution decomposition
-> implement/validate/update ledger
-> freshly reread milestone
-> reconcile milestone <-> ledger <-> repository/evidence
-> completion audit
-> continue or terminate
```

After completion, synchronize durable authority and delete the completed milestone file and operational ledger.
