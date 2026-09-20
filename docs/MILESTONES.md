# Milestones

## Purpose

Milestones are active implementation work orders. They are deleted after durable outcomes are synchronized; Git retains completed history.

## Current

M0081 — Dynamic Model Sample Catalog & TestData Inspection is ready on the 6.1.0 development line.

M0080 — Programmatic Canonical Model Authoring is complete. 6.0.0 remains the released stable baseline for the 6.1 development work.

Primary milestone:

```text
docs/milestones/m0081-dynamic-model-sample-catalog-and-testdata-inspection.md
```

## Next Number

```text
M0082
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
