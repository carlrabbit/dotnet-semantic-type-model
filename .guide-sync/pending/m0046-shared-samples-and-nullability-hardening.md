# Guide Sync Hint: M0046 Shared Samples and Nullability Hardening

## Status

Pending until M0046 completes.

## Purpose

Track later documentation synchronization and patch-release preparation. This is metadata, not behavioral authority, and ordinary implementation agents do not need to read it.

## Areas to Check

```text
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/engineering/samples.md
EF and other changed projection specs
public-docs/samples.md
public-docs/samples/*.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/guides/system-text-json.md
public-docs/guides/power-bi-projection.md
public-docs/guides/configuration.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

## Required Follow-Up

```text
shared Order Fulfillment model and overlap
Customer JSON editing scenario
complete-model versus target-selection explanation
explicit Configuration registration
unregistered unrelated options
EF nullable value-type correction
cross-projection nullable audit
samples as canaries; tests as exhaustive matrices
2.3.0 defect disclosure
next patch-release readiness
```

## Validation Hints

```sh
./eng/check.sh
./eng/package.sh 0.0.0-m0046
./eng/package-smoke.sh 0.0.0-m0046
./eng/samples.sh
./eng/public-docs.sh
```
