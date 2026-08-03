# Guide Sync Hint: M0054 2.4.6 Publication Follow-Up

## Status

Pending human approval after M0054 implementation and release validation succeed.

## Purpose

Track publication work remaining after real-application EF regression fixture coverage, source-lineage scope filtering, ModelBuilder tests, and SQLite integration tests pass the non-publishing `2.4.6` release gate.

This file is synchronization metadata, not behavioral authority.

## Preconditions

```text
anonymized real-application fixtures are reviewed
source-lineage scope filtering tests pass
real ModelBuilder tests pass
SQLite integration tests pass
public docs and release notes are synchronized
./eng/release-check.sh 2.4.6 passes
final package inventory reviewed
human publication approval recorded
```

## Publication Follow-Up

After explicit human approval:

```text
publish intended 2.4.6 packages
verify NuGet metadata and dependencies
verify package README rendering
verify generator/analyzer assets where applicable
create the 2.4.6 tag
create the GitHub release
use the approved 2.4.6 release notes
verify repository and package links
change candidate/preparation wording to published wording
```

Do not publish unexpected packages merely because they exist under `artifacts/nuget/`.

## Human Review

Human approval is required for:

```text
NuGet feed and credentials
final package inventory
publish command or workflow
tag name
GitHub release title and notes
post-publication verification
```
