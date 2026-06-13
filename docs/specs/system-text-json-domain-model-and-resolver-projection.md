# System.Text.Json Domain Model and Resolver Projection Spec

## Status

Authoritative behavioral specification.

## Purpose

Define the System.Text.Json domain semantic model and resolver projection behavior used by `SemanticTypeModel.SystemTextJson`.

This spec complements `docs/specs/system-text-json-contract-integration.md` and is authoritative for:

- System.Text.Json domain semantic model shape;
- derivation from the current canonical semantic model;
- resolver customization driven by the domain model;
- diagnostics and inspection behavior;
- sample and test expectations for the current projection methodology.

## Pipeline

The System.Text.Json package must follow the domain projection pipeline:

```text
Canonical semantic model
  -> System.Text.Json derivation transformations
  -> System.Text.Json domain semantic model
  -> resolver customization
  -> IJsonTypeInfoResolver / JsonSerializerOptions behavior
```

The resolver must not use scattered annotation lookups over the canonical model as its primary behavior model.

Convenience APIs may accept the canonical semantic model, but they must derive the System.Text.Json domain semantic model internally before applying resolver behavior.

## Domain Semantic Model

The System.Text.Json domain semantic model must contain deterministic, package-owned metadata needed for resolver behavior.

At minimum it must represent:

- projected JSON contract types;
- CLR type identifiers used for resolver matching;
- property-level CLR member matching data;
- existing JSON contract preservation policy;
- selected property-name source policy;
- imported `System.Text.Json` metadata relevant to resolver behavior;
- unsupported metadata diagnostics;
- duplicate final-name detection inputs;
- extension-data metadata when present;
- inspection-friendly summaries.

The domain model must not mutate the canonical semantic model.

## Derivation Entry Point

The package must expose a derivation entry point from the current canonical semantic model to the System.Text.Json domain semantic model.

The exact public API may vary, but the supported shape is:

```csharp
var stjModel = semanticModel.DeriveSystemTextJsonModel(options =>
{
    options.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName;
});
```

The derivation result should include:

- the System.Text.Json domain semantic model;
- diagnostics;
- transformation trace when available.

## Resolver Entry Points

Resolver customization should accept the System.Text.Json domain semantic model directly.

Supported resolver composition patterns:

```csharp
IJsonTypeInfoResolver resolver = AppJsonContext.Default.WithSemanticTypeModelJson(stjModel);
```

and optionally:

```csharp
jsonOptions.AddSemanticTypeModelJson(stjModel);
```

Convenience overloads accepting the canonical semantic model are allowed only when they derive the domain semantic model internally before composing the resolver.

## Property Name Source

The resolver must support the property-name source policies defined in `docs/specs/system-text-json-contract-integration.md`:

| Policy | Meaning |
|---|---|
| `ExistingJsonContract` | Preserve the base resolver or user-owned context property name. |
| `SystemTextJsonPropertyNameAnnotation` | Use imported `JsonPropertyNameAttribute` metadata when present. |
| `SemanticPropertyName` | Use the canonical semantic property name as the JSON property name. |

The default must preserve the existing JSON contract.

## Matching Strategy

The derived domain model must provide stable matching data so resolver customization does not rely only on the current `JsonPropertyInfo.Name`.

Matching should use, in order when available:

- original CLR member metadata captured during extraction;
- `JsonPropertyInfo.AttributeProvider` member identity;
- imported System.Text.Json property-name metadata;
- conservative fallback to current JSON name only when it is known to match the semantic property.

If a property cannot be matched safely, resolver customization must leave it unchanged and emit or preserve an explicit diagnostic when the API can surface diagnostics.

## Extension Data

When M0034 extension-data semantics are present, the System.Text.Json domain model must represent extension-data members explicitly.

Projection behavior:

- imported `JsonExtensionDataAttribute` metadata may normalize into extension-data metadata when configured;
- unsupported extension-data member shapes must produce diagnostics;
- extension-data metadata must not be treated as a normal serializable property-name override unless explicitly configured;
- resolver customization must preserve System.Text.Json's extension-data behavior unless the user explicitly chooses a supported override.

## Generated Contexts

SemanticTypeModel must not generate `JsonSerializerContext` declarations.

Consumers who use System.Text.Json source generation own their context declarations. SemanticTypeModel composes with the user-owned context as an `IJsonTypeInfoResolver`.

## Diagnostics

Diagnostics must be deterministic and target System.Text.Json behavior.

At minimum, implementation must cover:

- duplicate final JSON property names;
- conflicting semantic-name and JSON-name policy;
- required semantic member ignored by System.Text.Json metadata;
- unsupported converter metadata when behavior cannot be modeled;
- unsupported extension-data member type;
- ambiguous or unsafe resolver-property matching;
- removed generated-context settings or references if still detected.

New public diagnostics must follow `docs/specs/diagnostics.md` and update the public diagnostics reference.

## Inspection

The System.Text.Json domain semantic model must support deterministic inspection output.

Inspection should include:

- projected type name or CLR identifier;
- matched properties;
- selected JSON name source;
- final JSON names when determinable;
- ignored or unsupported properties;
- extension-data members;
- diagnostics.

## Samples

System.Text.Json samples must demonstrate current code-first usage.

Samples must:

- obtain a canonical semantic model from code-first extraction or a generated provider;
- derive the System.Text.Json domain semantic model;
- compose a user-owned `JsonSerializerContext` or default resolver with SemanticTypeModel;
- show default contract preservation;
- show explicit semantic-property-name projection when configured;
- avoid manual old model shape construction.

Samples must not:

- construct old model shapes as the primary model source;
- demonstrate JSON Schema import as a model source;
- imply that SemanticTypeModel generates `JsonSerializerContext` declarations.

## Tests

Tests must cover both domain derivation and resolver behavior.

Required test categories:

- derivation produces deterministic System.Text.Json domain model metadata;
- default resolver behavior preserves existing JSON contract names;
- explicit semantic property names are applied;
- imported `JsonPropertyNameAttribute` metadata is applied when selected;
- duplicate final names fail deterministically;
- unsafe property matching does not mutate unrelated properties;
- extension-data metadata is represented and validated;
- samples use the current code-first flow.

## Invariants

- System.Text.Json metadata is projection-specific metadata, not projection-neutral core structure.
- Resolver behavior is driven by a System.Text.Json domain semantic model.
- The canonical semantic model remains immutable during derivation and resolver composition.
- Existing serializer behavior is preserved by default.
- Source-generated contexts are user-owned and never generated by SemanticTypeModel.
- Unsupported or ambiguous behavior must be explicit; silent lossy behavior is not allowed.
