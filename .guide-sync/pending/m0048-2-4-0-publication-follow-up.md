# Guide Sync Hint: M0048 2.4.0 Publication Follow-Up

## Status

Pending human approval after M0048 release preparation succeeds.

## Purpose

Track explicit publication work after documentation synchronization and a passing non-publishing `2.4.0` release gate.

This file is synchronization metadata, not behavioral authority.

## Preconditions

```text
M0048 documentation synchronization complete
./eng/release-check.sh 2.4.0 passed
package inventory reviewed
compatibility and migration guidance approved
package contents reviewed
human publication approval recorded
```

## Publication Follow-Up

After explicit human approval:

```text
run the documented publish command or workflow
verify every intended NuGet package at 2.4.0
verify dependency metadata and README rendering
create the 2.4.0 tag
create the GitHub release
use approved 2.4.0 release notes
verify package and repository links
change candidate/preparation wording to published wording
```

Resolve the definitive package set from actual packable projects and canonical packaging scripts. Do not publish unexpected packages merely because they appear under `artifacts/nuget/`.

Human approval is required for feed/credentials, final package inventory, publish mechanism, tag, GitHub release, and post-publication wording.
