# Guide Sync Hint: M0052 2.4.4 Publication Follow-Up

## Status

Pending human approval after M0052 implementation and release validation succeed.

## Purpose

Track publication work remaining after closed EF Core semantic model application and `EfCoreSemanticModel` source-lineage enrichment pass the non-publishing `2.4.4` release gate.

This file is synchronization metadata, not behavioral authority.

## Preconditions

```text
EfCoreSemanticModel lineage tests pass
ApplySemanticTypeModel and ApplyEfCoreSemanticModel converge
closed ModelBuilder application tests pass
shared-type projection remains explicit
extension-data and value-object boundary regressions pass
docs and release notes are synchronized
./eng/release-check.sh 2.4.4 passes
final package inventory reviewed
human publication approval recorded
```

## Publication Follow-Up

After explicit human approval:

```text
publish intended 2.4.4 packages
verify NuGet metadata and dependencies
verify package README rendering
verify generator/analyzer assets where applicable
create the 2.4.4 tag
create the GitHub release
use the approved 2.4.4 release notes
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
