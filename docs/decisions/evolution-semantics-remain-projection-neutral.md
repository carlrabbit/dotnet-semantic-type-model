# Decision: Evolution, Ownership, and Lifecycle Semantics Remain Projection-Neutral

## Status

Planned.

## Context

M0034 adds semantics that are useful across JSON Schema, EF Core, Power BI, and System.Text.Json:

```text
Ownership
Versioning / Revision
TemporalValidity
LifecycleState
ExtensionData
```

These concepts have useful target projections, but none of them should directly imply target-specific behavior such as SQL Server temporal tables, EF Core global query filters, DAX measure generation, JSON Schema runtime validation, or arbitrary extension-data flattening.

## Decision

M0034 semantics remain projection-neutral core semantics.

Target packages own target-specific representation policies:

```text
JSON Schema owns schema/export representation.
EF Core owns storage/model-builder representation.
Power BI owns analytical metadata representation.
System.Text.Json owns contract/extension-data integration representation.
```

Core semantics provide meaning, diagnostics, queryability, and inspection.

Target packages may provide safe opinionated defaults and explicit policy hooks, but must diagnose unsupported or unsafe behavior instead of silently generating active behavior.

## Consequences

- Ownership is distinct from envelope semantics.
- Extension data is distinct from annotation metadata.
- Version/revision semantics do not change EF Core primary keys by default.
- Temporal validity does not enable temporal tables or global query filters by default.
- Lifecycle state does not generate workflow transitions by default.
- Power BI does not generate DAX measures by default from lifecycle/version/temporal semantics.
- JSON Schema does not claim to enforce cross-property temporal interval rules unless explicit validation metadata exists.

## Follow-up

Projection packages should document their default policies and expose explicit overrides when behavior beyond safe defaults is required.
