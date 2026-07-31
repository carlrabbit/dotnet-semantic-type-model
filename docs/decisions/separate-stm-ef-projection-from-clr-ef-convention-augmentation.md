# Decision: Separate STM-Owned EF Projection from CLR-Backed EF Convention Augmentation

## Status

Accepted for M0051.

## Context

SemanticTypeModel EF projection can ignore semantic-only members such as `[SemanticExtensionData]`. However, EF Core conventions may still inspect user CLR types directly through `DbSet<T>`, `modelBuilder.Entity<T>()`, or navigation reachability.

EF Core does not understand SemanticTypeModel annotations. This can cause inherited semantic-only members, such as extension-data dictionaries declared on a non-semantic abstract base class, to be treated as EF properties or navigations and fail model building.

## Decision

The EF integration will distinguish two modes:

```text
STM-owned shared-type projection
CLR-backed EF convention augmentation
```

Shared-type projection remains STM-owned.

CLR-backed augmentation must suppress semantic-only members and harden value-object boundaries where source CLR metadata is available.

Semantic `ValueObject` types are not root EF entities by STM projection and are unsupported as `DbSet<T>` roots unless a future explicit policy is added.

## Consequences

- Consumers receive a clearer EF integration contract.
- `[SemanticExtensionData]` remains semantic metadata and does not require `[NotMapped]` in supported CLR-backed mode.
- Non-semantic base classes can contribute inherited semantic members without becoming EF roots.
- Some mixed-mode EF usages will produce diagnostics instead of ambiguous EF convention failures.
- Documentation must clearly identify when manual `[NotMapped]` or `Ignore` remains a workaround.

## Rejected Alternatives

### Require `[NotMapped]` everywhere

Rejected because it leaks EF-specific concerns into semantic model authoring and contradicts the expected EF projection behavior.

### Let EF conventions handle STM annotations implicitly

Rejected because EF does not know STM annotations.

### Treat `ValueObject` as an EF entity when used in `DbSet<T>`

Rejected because it violates semantic role boundaries and encourages incorrect persistence models.

### Ignore all unknown CLR properties broadly

Rejected because it is unsafe; only semantic-only members with explicit STM meaning should be suppressed.
