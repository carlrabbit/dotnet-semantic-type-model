# Milestones

## Purpose

Milestones are active implementation work orders. They are deleted after durable outcomes are synchronized; Git retains completed history.

## Current

No milestone is active.

## Next Number

```text
M0062
```

Never restart or reuse milestone numbers.

## Lifecycle

```text
plan -> implement -> synchronize durable truth -> validate -> complete -> delete completed milestone
```

Promote current behavior to specifications, structural outcomes to architecture, durable rationale to decisions, and consumer-visible behavior to public documentation before deleting a completed milestone. Do not archive milestone files in the working tree.
