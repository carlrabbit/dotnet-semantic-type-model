# Guide Sync Hint: M0050 2.4.2 Publication Follow-Up

## Status

Pending human approval after M0050 implementation and release validation succeed.

## Purpose

Track publication work remaining after format-compatible scalar support and role-aware EF owned storage corrections pass the non-publishing `2.4.2` release gate.

This file is synchronization metadata, not behavioral authority.

## Preconditions

```text
Uri scalar/format tests pass
role-aware EF owned storage tests pass
owned value object JSON-column sample passes
public docs and release notes are synchronized
./eng/release-check.sh 2.4.2 passes
final package inventory reviewed
human publication approval recorded
```

## Publication Follow-Up

After explicit human approval:

```text
publish intended 2.4.2 packages
verify NuGet metadata and dependencies
verify package README rendering
verify generator/analyzer assets where applicable
create the 2.4.2 tag
create the GitHub release
use the approved 2.4.2 release notes
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
