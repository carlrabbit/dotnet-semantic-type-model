# Guide Sync Hint: M0049 2.4.1 Publication Follow-Up

## Status

Pending human approval after M0049 implementation and release validation succeed.

## Purpose

Track publication work remaining after the emergency dictionary extraction fix and a passing non-publishing `2.4.1` release gate.

This file is synchronization metadata, not behavioral authority.

## Preconditions

```text
dictionary key/value extraction regression tests pass
canonical validation no longer emits STM0002 for valid dictionaries
EF Core ignores extension data before target type resolution
cross-projection tests and samples pass
./eng/release-check.sh 2.4.1 passes
final package inventory is reviewed
2.4.1 release notes are approved
human publication approval is recorded
```

## Publication Follow-Up

After explicit human approval:

```text
publish every intended 2.4.1 package
verify NuGet package metadata and dependencies
verify package README rendering
verify generator/analyzer assets where applicable
create the 2.4.1 tag
create the GitHub release
use the approved emergency patch release notes
verify package and repository links
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
