# Milestones

## Purpose

Milestones are **active implementation work orders**. They route an implementation agent to the scope, authority, acceptance criteria, and validation needed for the next coherent change.

Milestones are not permanent project truth.

## Current

At the M0060 baseline used for this documentation reset, there is no next active milestone in this overlay.

If an active milestone has been added after that baseline, preserve it and list it here while applying the reset.

## Next Number

```text
M0061
```

Never restart or reuse milestone numbers. Historical commit/PR/conversation references must remain unambiguous.

## Lifecycle

```text
plan
  -> active docs/milestones/mNNNN-*.md
  -> implement
  -> synchronize durable truth
  -> validate
  -> complete
  -> delete completed milestone file
```

Before deleting a completed milestone, promote durable outcomes as appropriate:

- current behavior -> specification;
- structural change -> architecture;
- enduring non-obvious rationale -> decision;
- consumer-visible behavior -> public/package documentation;
- architectural evolution -> concise `HISTORY.md` entry when warranted;
- regression contract -> tests.

Do not move completed milestones into an archive directory. Git history retains them.

## Milestone Creation Rule

Create a milestone only for active implementation sequencing. Do not create milestone files to preserve release history, completed implementation notes, or documentation that belongs in a current subsystem contract.
