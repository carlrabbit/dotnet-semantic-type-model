# Milestones

## Purpose

Milestones are active implementation work orders. They are deleted after durable outcomes are synchronized; Git retains completed history.

## Current

M0080 — Programmatic Canonical Model Authoring, M0081 — Dynamic Model Sample Catalog & TestData Inspection, M0082 — Deterministic Diverse TestData Generation, M0083 — TestData Profiles & Sampling Policies, and M0084 — Coordinated TestData Generation are completed history.

M0085 — EF Nested JSON Dictionaries and JSON Schema Contract Clarification (6.1.1 Patch Preparation) is **ready** at `docs/milestones/M0085-ef-nested-json-dictionaries-schema-contract-patch.md`. Its planning-seeded ledger is `.execution/M0085.md`.

NuGet currently lists 6.1.0 as published (checked 2026-11-16); the planned patch target is 6.1.1, subject to release-channel preflight. Preparation does not authorize publication.

The next unallocated milestone number is M0086.

## Next Number

```text
M0086
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
