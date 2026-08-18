# Decision: Configuration Role Does Not Imply Options Integration

## Status

Accepted for M0067.

## Context

`SemanticTypeRole.Configuration` expresses projection-neutral domain meaning: a modeled type represents configurable behavior or settings. That meaning remains useful to generic model inspection and targets such as JSON Schema.

The separate `SemanticTypeModel.Configuration` capability adds Microsoft.Extensions.Configuration and Microsoft.Extensions.Options behavior: section metadata, binding policy, named options, DataAnnotations/startup validation policy, registration helpers, and runtime `AddSemanticOptions<TOptions>` integration.

Maintaining that framework-specific capability is not required for the core semantic-model purpose and would require substantially stronger integration and compatibility coverage to justify its public surface.

## Decision

Keep the projection-neutral `Configuration` semantic role.

Keep projection-neutral `SemanticRequiredWhen` conditional validity semantics.

Remove the STM-owned Microsoft.Extensions.Configuration / Microsoft.Extensions.Options capability completely, including its package, target domain model, runtime registration API, Configuration-specific authoring attributes/annotation namespace, tests, samples, capability metadata, and current documentation authority.

Do not ship a tombstone package, forwarding package, obsolete API shim, or compatibility adapter. Applications that bind configuration use Microsoft.Extensions.Configuration / Microsoft.Extensions.Options directly.

The `Configuration` semantic role alone must not imply section binding, options registration, startup validation, DataAnnotations policy, named options, or any other application-configuration behavior.

## Consequences

- A semantic model may continue to contain types whose role is `Configuration`.
- JSON Schema and other generic projections may consume or preserve that role according to their own contracts.
- `SemanticRequiredWhen` remains available independently of application configuration.
- SemanticTypeModel no longer owns application configuration source, section, binding, Options registration, or Options validation policy.
- The published package suite becomes smaller and the removal is a breaking change requiring the next major release boundary.
- Reintroducing an STM-owned Options integration in the future requires a new explicit architecture/compatibility decision; it must not be inferred merely because the core `Configuration` role exists.

## Rejected Alternatives

### Remove the Configuration role too

Rejected. The role describes domain meaning independent of Microsoft.Extensions.Options and remains useful to projection-neutral modeling and JSON Schema.

### Keep the package and add integration tests

Rejected. The decision is to reduce the supported capability surface rather than invest in a framework-specific integration that is not central to the library.

### Keep compatibility/tombstone APIs

Rejected. Retaining dead packages or obsolete registration APIs would preserve complexity without preserving supported behavior.
