# Milestones

## Purpose

Milestones are active implementation work orders. They are deleted after durable outcomes are synchronized; Git retains completed history.

## Current

M0083 — TestData Profiles & Sampling Policies is completed on the 6.1.0 development line.

M0080 — Programmatic Canonical Model Authoring, M0081 — Dynamic Model Sample Catalog & TestData Inspection, and M0082 — Deterministic Diverse TestData Generation are completed history. 6.0.0 remains the released stable baseline for the 6.1 development work.

M0083 durable outcomes are synchronized in the TestData source, tests, sample catalog, public documentation, and validation evidence. No primary milestone is active.

## Next Number

```text
M0084
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
