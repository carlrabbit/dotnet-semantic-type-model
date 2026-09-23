# Milestones

## Purpose

Milestones are active implementation work orders. They are deleted after durable outcomes are synchronized; Git retains completed history.

## Current

M0080 — Programmatic Canonical Model Authoring, M0081 — Dynamic Model Sample Catalog & TestData Inspection, M0082 — Deterministic Diverse TestData Generation, M0083 — TestData Profiles & Sampling Policies, and M0084 — Coordinated TestData Generation are completed history.

There is no active implementation milestone. 6.1.0 is the current release-candidate line; 6.0.0 is the released stable predecessor.

The next implementation milestone is M0085.

## Next Number

```text
M0085
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
