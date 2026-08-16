# Core Semantics

## Use

Core semantics describe projection-neutral domain meaning once so target packages can make their own
representation decisions.

```csharp
using SemanticTypeModel.DotNet;

[SemanticType(SemanticTypeRole.Entity)]
[SemanticMutable]
public sealed class Customer
{
    [SemanticKey]
    [SemanticImmutable]
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
| SemanticMutability | Optional lifecycle mutability intent: `Mutable` or `Immutable` |
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
| UI annotation | Open JSON-compatible `ui.*` presentation metadata |

General entity relationships are intentionally not a canonical STM primitive. Object references and collections
describe structural shape; applications and target projections own target-specific relationship configuration.

For generator-wide configuration such as discovery, naming, or generated namespace, use
[SemanticTypeModel Configuration](../configuration.md).

## Lifecycle mutability

`SemanticMutability` is optional lifecycle intent declared with `[SemanticMutable]` or `[SemanticImmutable]` on
object types and members.

Resolution is:

```text
property declaration
  ?? containing-object declaration
  ?? unspecified
```

A mutable member in an immutable object is valid. This permits explicitly mutable technical/operational state
inside an otherwise immutable semantic object.

CLR setter accessibility, `init`, getter-only members, `readonly`, and record syntax do not infer lifecycle
mutability.

## Descriptions

User and technical descriptions are separate semantic contracts:

- `UserDescription` is intended for business/end-user facing targets;
- `TechnicalDescription` describes implementation/integration/operational concerns;
- neither silently supplies the other.

JSON Schema maps `UserDescription` to standard `description` and can preserve technical text separately in
`x-stm.technicalDescription`.

## UI annotations

`ui.*` is an open annotation namespace. Dedicated attributes map common concepts such as title, category, and
order; other JSON-compatible `ui.*` values can pass through without belonging to a closed widget vocabulary.

JSON Schema preserves these values beneath `x-stm.ui` when semantic annotations are enabled.

## Diagnose

| Symptom | Likely cause | Fix |
|---|---|---|
| Semantic type/member missing | Discovery/accessibility configuration excludes it | Review [Configuration](../configuration.md). |
| Key diagnostic | Identity metadata is incomplete or ambiguous | Prefer explicit `[SemanticKey]` metadata when identity matters. |
| Conflicting mutability diagnostic | Both mutable and immutable semantics were declared on one target | Keep one explicit lifecycle declaration. |
| `RequiredWhen` typed-literal diagnostic | Source member/value cannot be normalized safely | Use a supported scalar/enum source and a valid typed value. |
| Target ignores a semantic concept | Target cannot represent/enforce it directly | Review target guide/capability matrix and diagnostics. |

## Reference

Target projections may preserve, approximate, ignore, or diagnose a semantic concept. See
[Projection capabilities](projection-capabilities.md) before assuming a core annotation implies identical
runtime behavior in every target.
