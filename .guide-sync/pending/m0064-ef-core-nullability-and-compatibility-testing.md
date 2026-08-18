# M0064 Documentation Synchronization Hint

## Source milestone

`M0064 — EF Core Nullability and Compatibility Testing`

## Status

Pending after implementation.

## Purpose

Carry consumer/release documentation work that is not required for the implementation agent to fix and validate the regression.

Ordinary implementation agents must not read this file.

## Synchronize after implementation

Verify current public documentation against the implemented fix.

At minimum inspect:

- `public-docs/guides/ef-core.md`
- `public-docs/api/compatibility.md`
- `public-docs/release-notes.md`
- shared NuGet README only if package usage guidance changed

Expected durable consumer message:

- 4.0.0 had a generated EF compilation regression for nullable owned JSON properties;
- the patch preserves nullable CLR property typing in generated EF converter/comparer usage;
- no new relationship/storage model or public configuration API was introduced;
- owned JSON required/nullable behavior is covered by the strengthened compatibility test matrix;
- the fix is intended for the 4.0.1 patch line once release readiness is performed.

Do not add implementation/test-infrastructure detail to consumer guides unless it helps users diagnose or understand supported behavior.

## Release synchronization

When preparing 4.0.1:

- add a concise bug-fix entry;
- do not describe M0064 milestone mechanics as release content;
- run normal release-readiness validation separately;
- publication remains a separate explicit action.

Delete this hint after the documentation/release surfaces are synchronized.
