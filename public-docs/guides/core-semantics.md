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

    [SemanticDisplayIdentity(Order = 0)]
    [SemanticAccessPath("ByName", Order = 0)]
    public required string Name { get; init; }

    [SemanticUserDescription("A customer that can place orders.")]
    public required string Description { get; init; }
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
| DisplayIdentity | Ordered human-recognition property group; does not define keys, formatting, or UI behavior |
| AccessPath | Named ordered lookup/narrowing property group; does not define keys, indexes, operators, or priorities |
| Strong Scalar | Explicit nominal wrapper with one underlying scalar representation |
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

For generator-wide configuration such as discovery, naming, or generated namespace, use the
[generator configuration reference](../configuration.md). This is distinct from application configuration
binding: STM does not provide Options registration or binding APIs.

`SemanticTypeRole.Configuration` remains projection-neutral domain meaning, and `SemanticRequiredWhen`
remains an independently supported conditional semantic constraint.

### Display Identity and Access Path boundaries

`SemanticDisplayIdentity` describes ordered properties humans can use to recognize an instance.
`SemanticAccessPath` describes a named, ordered intended locate/filter route. They are annotation-only core
semantics. Neither one generates an EF index, API query parameter, UI/list/form behavior, Power BI behavior,
relationship behavior, or other target-specific runtime feature. JSON Schema may preserve them under `x-stm`,
but does not turn them into query or UI contracts.

### Strong Scalar

Opt in explicitly with `[SemanticStrongScalar]` on a readonly struct or readonly record struct exposing one
supported scalar `Value` and a matching public constructor:

```csharp
[SemanticStrongScalar]
public readonly record struct SpecificationVersionId(Guid Value);
```

The canonical model and supported projections treat it as the underlying scalar, not as an object with a
`Value` property. It does not imply Identifier, Key, Entity, ownership, or automatic one-property inference.

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
| `STM5049` | Display Identity order is negative or ambiguous | Use non-negative, unique orders; the invalid group is omitted. |
| `STM5050` | Access Path name/order/membership is invalid or ambiguous | Use a valid name and unique non-negative orders for each path. |
| `STM5051` | Strong Scalar declaration is invalid | Use a non-generic readonly struct/record struct with exactly one supported scalar `Value` and a matching constructor. |
| Target ignores a semantic concept | Target cannot represent/enforce it directly | Review target guide/capability matrix and diagnostics. |

## Reference

Target projections may preserve, approximate, ignore, or diagnose a semantic concept. See
[Projection capabilities](projection-capabilities.md) before assuming a core annotation implies identical
runtime behavior in every target.
