# M0024: System.Text.Json Contract Correction

## Status

Implemented.

## Maturity Mode

Public package correction.

## Task Mode

Documentation-synchronized completed milestone.

## Goal

Correct the `SemanticTypeModel.SystemTextJson` 1.0 contract before it becomes durable API debt.

The completed contract is:

```text
SemanticTypeModel.SystemTextJson imports, preserves, validates, and applies supported System.Text.Json contract metadata where supported.

It does not generate JsonSerializerContext types.

When source generation is desired, the consumer owns the JsonSerializerContext declaration.

SemanticTypeModel may wrap or modify an existing IJsonTypeInfoResolver to apply supported semantic metadata.
```

## Authoritative Documents

- `docs/specs/system-text-json-contract-integration.md`
- `docs/decisions/remove-system-text-json-context-generation.md`
- `docs/ENGINEERING.md`
- `docs/engineering/command-contract.md`

## Completed Outcomes

- SemanticTypeModel-generated `JsonSerializerContext` support is removed from the supported contract.
- Generated context options are removed or rejected with explicit guidance.
- User-authored `JsonSerializerContext` declarations are the supported source-generation pattern.
- `SemanticTypeModel.SystemTextJson` is resolver-centered.
- Existing `JsonSerializerOptions.TypeInfoResolver` values and user-authored context resolvers are preserved by wrapping/composition.
- Semantic property names can be used as JSON serialization names through explicit resolver configuration.
- Duplicate final JSON property names fail deterministically.
- System.Text.Json sample guidance no longer claims generated context support.
- Package README and public guide documentation describe the corrected 1.1.0 behavior.

## Direct Documentation Impact Status

Resolved by the M0024 implementation and documentation synchronization pass:

```text
docs/specs/system-text-json-contract-integration.md
public-docs/guides/system-text-json.md
public-docs/nuget/SemanticTypeModel.SystemTextJson.md
public-docs/release-notes.md
public-docs/api/compatibility.md
```

## Deferred Documentation Impact Status

Resolved or intentionally not required:

```text
README.md
public-docs/getting-started.md
public-docs/packages.md
public-docs/samples.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
```

No follow-up documentation work remains for M0024 unless later code changes alter System.Text.Json public behavior.

## Validation Guidance

This was implementation work and should have completed with Tier 2 validation:

```sh
./eng/check.sh
```

Package-related changes should additionally use the appropriate package validation tier from `docs/engineering/command-contract.md`.

## Non-Goals Preserved

- No custom JSON serializer.
- No generated `JsonSerializerContext` declaration from SemanticTypeModel.
- No MSBuild pre-generation path for `JsonSerializerContext`.
- No broad documentation rewrite.
- No TBPs or issue templates.
