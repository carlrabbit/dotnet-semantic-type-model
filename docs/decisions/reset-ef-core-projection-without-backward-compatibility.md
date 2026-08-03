# Decision: Reset EF Core Projection Without Backward Compatibility

## Status

Accepted for M0055.

## Context

The 2.4.x EF implementation accumulated multiple modes, source-lineage machinery, shared-type projection, convention suppression, owned-navigation behavior, and generic unsupported-shape handling. Repeated real-life failures showed that the combined system was too broad and difficult to reason about.

## Decision

Replace the EF implementation with a lean opinionated relational projection.

The reset is intentionally breaking.

No backward compatibility is required.

Do not retain old APIs through `[Obsolete]`, aliases, wrappers, compatibility enum values, or legacy execution branches.

Delete superseded code.

## Fixed Relational Rules

```text
Entity -> table
Inheritance -> TPT
Owned ValueKind object -> JSON object
Owned ValueKind collection -> JSON array
ExtensionData -> JSON object
Entity links -> identifiers
```

## Consequences

- `2.5.0` is a breaking release.
- The EF public API becomes smaller.
- Advanced EF concepts are excluded.
- Existing consumers must update.
- The implementation becomes testable against real application models.
- Old code and documentation are removed rather than maintained.

## Rejected Alternatives

### Continue patching 2.4.x

Rejected because recurring failures stem from architectural breadth.

### Preserve old APIs with Obsolete attributes

Rejected because this would retain concepts that no longer belong to the design.

### Support multiple relational strategies

Rejected because the package is intentionally opinionated.

### Use EF owned entities for semantic ownership

Rejected because semantic ownership is persisted as JSON value containment.
