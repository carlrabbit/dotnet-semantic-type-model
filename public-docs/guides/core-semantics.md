# Core Semantics

## Use

Core semantics describe projection-neutral domain meaning once so target packages can make their own
representation decisions.

```csharp
using SemanticTypeModel.DotNet;

[SemanticType(SemanticTypeRole.Entity)]
public sealed class Customer
{
    [SemanticKey]
    public required string Id { get; init; }

    [SemanticDisplayName("Customer")]
    [SemanticUserDescription("A customer that can place orders.")]
    public required string Name { get; init; }
}
```

Generate the canonical model and pass it to whichever target projection you need.

## Configure

Core semantics themselves do not choose EF storage, JSON Schema UI behavior, Power BI layout, serializer
contract names, or Options provider setup. Put domain meaning in core attributes and representation choices in
target-specific configuration.

Common semantic concepts include:

| Concept | Meaning |
|---|---|
| Entity | Independently identifiable domain object |
| ValueObject | Value contained by another semantic boundary, without independent identity by default |
| Key | Identity member/group |
| Relationship | Projection-neutral association metadata |
| Required / Nullable | Presence and nullability semantics |
| Constraint | Validation/shape constraint |
| RequiredWhen | Conditional presence rule against a typed source value |
| Format | Semantic scalar format hint |
| DisplayName | User-facing label, not stable identity |
| UserDescription | User-facing explanatory text |
| TechnicalDescription | Technical explanatory text; XML summaries can contribute technical fallback |
| Ownership | Lifecycle containment; target storage remains target-specific |
| Envelope | Wrapper carrying a distinguished payload plus contextual/lifecycle metadata |
| ExtensionData | Forward-compatible/unmodeled key/value data boundary |
| Version / Revision / temporal validity | Evolution/lifecycle metadata, not automatic migrations/concurrency |

For generator-wide configuration such as discovery, naming, or generated namespace, use
[SemanticTypeModel Configuration](../configuration.md).

## Diagnose

| Symptom | Likely cause | Fix |
|---|---|---|
| Semantic type/member missing | Discovery/accessibility configuration excludes it | Review [Configuration](../configuration.md). |
| Key/relationship diagnostics | Metadata is incomplete/ambiguous | Prefer explicit semantic metadata over inference. |
| `RequiredWhen` typed-literal diagnostic | Source member/value cannot be normalized safely | Use a supported scalar/enum source and a valid typed value. |
| Target ignores a semantic concept | Target cannot represent/enforce it directly | Review target guide/capability matrix and diagnostics. |

## Reference

Target projections may preserve, approximate, ignore, or diagnose a semantic concept. See
[Projection capabilities](projection-capabilities.md) before assuming a core annotation implies identical
runtime behavior in every target.
