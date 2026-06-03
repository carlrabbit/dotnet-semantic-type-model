# Decision: Remove System.Text.Json Context Generation

## Status

Accepted for M0024.

## Context

`SemanticTypeModel.SystemTextJson` 1.0 included a source-generator path intended to generate a `JsonSerializerContext` declaration for `System.Text.Json` source generation.

The implementation depended on this sequence:

```text
SemanticTypeModel.Generators emits a JsonSerializerContext declaration.
System.Text.Json source generator discovers and processes that generated declaration.
```

That sequence is not a reliable source-generator contract. Source generators are not an ordered pipeline for cross-generator input production. A generator should not depend on another generator processing declarations that it emitted in the same compilation.

The feature therefore creates a consumer-visible promise that cannot be implemented reliably with the chosen mechanism.

## Decision

Remove `JsonSerializerContext` generation from SemanticTypeModel.

`SemanticTypeModel.SystemTextJson` will be resolver-centered:

```text
Consumers author JsonSerializerContext declarations when they want System.Text.Json source generation.
SemanticTypeModel may wrap or compose IJsonTypeInfoResolver instances to apply supported semantic metadata.
```

Generated context support must not be advertised, tested as supported, or used by samples.

## Rationale

- The previous design relied on unsupported generator chaining.
- The feature does not provide reliable consumer value.
- Keeping a broken stable API creates more compatibility debt than removing it early.
- Resolver composition is the supported extension point for runtime contract customization.
- User-authored `JsonSerializerContext` declarations remain fully compatible with the corrected design.

## Consequences

- `SemanticTypeModelGenerateSystemTextJsonContext` and `SemanticTypeModelSystemTextJsonContextName` must be removed, obsolete, or rejected.
- `GenerateJsonSerializerContext` and `GeneratedContextName` must be removed, obsolete, or made non-operational with explicit guidance.
- Generator code must no longer emit classes deriving from `JsonSerializerContext`.
- Samples must use user-authored contexts when demonstrating `System.Text.Json` source generation.
- Public docs and package README sources must stop claiming generated context support.
- The release notes must call out this 1.1 compatibility correction.
- The System.Text.Json integration spec must define resolver-centered behavior as the supported contract.

## Alternatives Considered

### Keep Generated Context Support

Rejected because the implementation mechanism is unreliable.

### Replace Roslyn Generation With MSBuild Pre-Generation

Rejected for M0024 because it would introduce a different build-time architecture, additional complexity, and new packaging behavior. It can be reconsidered in a later milestone if generated context declarations become strategically important.

### Keep the API as No-Op Compatibility Surface

Allowed only as a short compatibility bridge if public API baselines or compatibility policy require it. If retained, the API must be marked obsolete or documented as unsupported and must not imply functional generated context support.

### Require Consumers to Author Contexts

Accepted. This matches `System.Text.Json` source-generation ownership and keeps SemanticTypeModel focused on metadata import, validation, and resolver customization.
