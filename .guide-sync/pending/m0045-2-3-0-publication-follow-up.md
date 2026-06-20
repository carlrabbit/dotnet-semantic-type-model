# Guide Sync Hint: M0045 2.3.0 Publication Follow-Up

## Status

Pending human approval after M0045 release preparation succeeds.

## Purpose

Track the explicit publication work remaining after documentation synchronization and the non-publishing `2.3.0` release gate pass.

This file is not behavioral authority and is not required reading for ordinary implementation agents.

## Preconditions

```text
M0045 documentation synchronization completed
./eng/release-check.sh 2.3.0 passed
expected artifacts/nuget package inventory reviewed
2.3.0 release notes reviewed
compatibility and migration wording approved
human publication approval recorded
```

## Publication Follow-Up

After explicit human approval:

```text
run the documented publish command or workflow
verify NuGet publication for every intended package
create the 2.3.0 tag
create the GitHub release
verify package README rendering
verify repository release links
update release notes from candidate/preparation wording to published wording
```

## Packages to Verify

Resolve the definitive list from the actual packable project inventory. It is expected to include the established `SemanticTypeModel.*` package set and the intended Configuration runtime/generator packages.

Do not publish an unexpected package merely because it appears under `artifacts/nuget/`.

## Human Review

Human review is required for publication authorization, credentials/feed, final package inventory, tag name, GitHub release notes, published metadata, and post-publication documentation wording.
