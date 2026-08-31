# System.Text.Json Domain Model and Resolver Projection Spec

## Status

Authoritative behavioral specification.

## Purpose

Define the System.Text.Json domain semantic model and runtime resolver projection behavior used by `SemanticTypeModel.SystemTextJson`.

This spec complements `docs/specs/system-text-json-contract-integration.md` and is authoritative for:

- System.Text.Json domain semantic model shape;
- derivation from the current canonical semantic model;
- runtime `JsonSerializerOptions` / resolver customization driven by the domain model;
- Strong Scalar runtime representation descriptors;
- automatic semantic-Entity polymorphism descriptors;
- multi-model runtime composition;
- diagnostics and inspection behavior;
- runtime-focused sample and test expectations.

M0071 does not establish new behavioral coverage for source-generated `JsonSerializerContext` consumption.

## Pipeline

The System.Text.Json package follows:

```text
Canonical semantic model
  -> System.Text.Json derivation transformations
  -> System.Text.Json domain semantic model
  -> runtime composition
  -> IJsonTypeInfoResolver / JsonSerializerOptions behavior
```

Convenience APIs accepting `TypeSchemaModel` derive the System.Text.Json domain semantic model internally and delegate to the same runtime composition behavior as the domain-model overload.

The canonical-model overload must not maintain hidden runtime semantics absent from `SystemTextJsonSemanticModel`.

## Domain Semantic Model

The domain model contains deterministic package-owned metadata required for supported runtime behavior.

At minimum it represents:

- projected JSON contract object types;
- stable CLR type identity used for resolver matching;
- property-level CLR member matching data;
- existing JSON contract preservation policy;
- selected property-name source policy;
- imported System.Text.Json metadata relevant to runtime behavior;
- unsupported metadata diagnostics;
- duplicate final-name detection inputs;
- extension-data metadata when present;
- Strong Scalar wrapper CLR identity and underlying scalar CLR representation;
- semantic Entity hierarchy descriptors required for automatic polymorphism;
- inspection-friendly summaries.

The domain model does not mutate the canonical semantic model.

## Primary Runtime Entry Point

Normal usage is runtime `JsonSerializerOptions` configuration:

```csharp
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
options.AddSemanticTypeModelJson(model);
```

No `JsonSerializerContext` is required for normal STM runtime behavior.

When `JsonSerializerOptions.TypeInfoResolver` is null, STM composes over `DefaultJsonTypeInfoResolver`.

When an existing resolver is present, STM preserves it as the base resolver and adds STM behavior around it.

## Canonical/Domain Overload Equivalence

For equivalent projection options/model content:

```text
AddSemanticTypeModelJson(TypeSchemaModel)
```

and:

```text
AddSemanticTypeModelJson(SystemTextJsonSemanticModel)
```

provide the same supported runtime behavior.

This includes:

- property-name projection;
- Strong Scalar conversion;
- automatic semantic-Entity polymorphism;
- runtime diagnostics/failures required by this contract.

Strong Scalar converter discovery must therefore be represented by or derivable from the System.Text.Json domain model rather than being a canonical-overload-only side path.

## Runtime Composition and Materialization

STM runtime behavior must not depend on appending callbacks to `DefaultJsonTypeInfoResolver.Modifiers`.

Reason: the modifier collection becomes immutable after the resolver has materialized metadata, and tying STM registration to that mutable collection would make repeated composition timing-sensitive.

Required composition model:

```text
base IJsonTypeInfoResolver
  -> STM runtime composition
  -> baseResolver.GetTypeInfo(...)
  -> STM customizes still-mutable JsonTypeInfo before returning it
```

The concrete internal implementation is package-owned, but observable behavior must satisfy this contract.

All STM models intended for one `JsonSerializerOptions` instance must be registered before that options instance is first used for serialization/deserialization. Registration after STJ freezes the options/metadata is unsupported and may surface the normal System.Text.Json immutability exception.

## Multi-Model Runtime Composition

This is a first-class supported runtime scenario:

```csharp
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
options.AddSemanticTypeModelJson(modelA);
options.AddSemanticTypeModelJson(modelB);
```

For non-conflicting models registered before first serializer use:

- behavior is independent of registration order;
- property customization applies only to the owning model's CLR types;
- Strong Scalar mappings from both models coexist;
- automatic Entity hierarchies remain model-local;
- a derived type from one model is never added to another model's hierarchy merely because names match;
- registration must not create ordering-dependent converter precedence between the models.

When the same CLR contract type or Strong Scalar wrapper is claimed incompatibly by multiple registered STM models, runtime registration must fail deterministically rather than select a winner by registration order.

When possible, additional STM registrations should extend an existing package-owned STM composition layer rather than blindly nesting independent package-owned wrappers/factories. Exact internal mechanics remain implementation-owned.

## Property Name Source

The runtime projection supports:

| Policy | Meaning |
|---|---|
| `ExistingJsonContract` | Preserve the base resolver property name. |
| `SystemTextJsonPropertyNameAnnotation` | Use imported `JsonPropertyNameAttribute` metadata when present. |
| `SemanticPropertyName` | Use the canonical semantic property name as the JSON property name. |

Default behavior preserves the existing JSON contract.

## Matching Strategy

Matching uses stable identity rather than only the current `JsonPropertyInfo.Name`.

Use, in order when available:

- original CLR member metadata captured during extraction;
- `JsonPropertyInfo.AttributeProvider` member identity;
- imported System.Text.Json property-name metadata;
- conservative current-name fallback only when known safe.

Unsafe matching must leave unrelated properties unchanged and produce/surface an explicit failure/diagnostic where the public API can do so.

## Strong Scalars

The domain model carries enough information to install the supported Strong Scalar runtime representation without returning to the canonical model at options-application time.

Supported Strong Scalars serialize as their underlying scalar representation and deserialize through the supported wrapper constructor.

A Guid-backed Strong Scalar therefore writes a JSON Guid string rather than an object containing `Value`.

Strong Scalar conversion composes with automatic Entity polymorphism and multi-model registration.

## Automatic Semantic-Entity Polymorphism

Semantic Entity inheritance is opinionated runtime representation input.

For a modeled CLR inheritance hierarchy in which a semantic Entity base has modeled concrete semantic Entity descendants, STM automatically establishes a System.Text.Json polymorphism contract when the effective base resolver has not already supplied one.

This applies to abstract or concrete semantic Entity bases with modeled concrete Entity descendants.

Default generated contract:

```text
discriminator property: "$type"
derived discriminator:  canonical semantic type Name
comparison:             ordinal, case-sensitive
unknown discriminator:   normal STJ failure/default handling
undeclared runtime type:  normal STJ failure/default handling
```

The discriminator property name may be exposed as a projection option if implementation needs/benefits from public customization, but automatic Entity polymorphism itself is not opt-in.

No key/relationship/display semantics are inferred from the discriminator.

### Explicit Application Polymorphism Wins

If the effective base resolver already supplies `JsonTypeInfo.PolymorphismOptions` for the base type, STM leaves that explicit application-owned contract unchanged.

STM does not merge, augment, or overwrite an existing application polymorphism contract.

Therefore precedence is:

```text
explicit base-resolver/application polymorphism
  -> preserve as-is
otherwise semantic Entity hierarchy
  -> STM automatic polymorphism
```

### Invalid Semantic Hierarchy

Reserve `STJ009` for invalid automatic semantic-hierarchy construction, including at least:

- duplicate discriminator values among descendants of one base;
- unresolved CLR type identity needed to construct the runtime hierarchy;
- canonical/CLR inheritance disagreement that prevents safe assignment;
- incompatible duplicate hierarchy ownership across registered STM models.

A hierarchy that cannot be represented safely must fail explicitly; silently dropping a descendant is not allowed.

## Extension Data

Extension-data behavior remains base-resolver/STJ-owned unless the current supported STM projection explicitly customizes matching metadata.

Imported `JsonExtensionDataAttribute` metadata may normalize into extension-data metadata when configured. Unsupported shapes produce diagnostics.

## Resolver-Only API

Resolver-only APIs remain valid for resolver metadata customization that can be expressed by `JsonTypeInfo`.

Normal complete runtime usage should prefer `JsonSerializerOptions.AddSemanticTypeModelJson(...)`, because supported Strong Scalar converter installation requires options-level composition.

M0071 does not add source-generated-context-specific behavioral guarantees.

## Diagnostics

Diagnostics must be deterministic and target System.Text.Json behavior.

Current required categories include:

- duplicate final JSON property names;
- conflicting semantic-name and JSON-name policy;
- required semantic member ignored by System.Text.Json metadata;
- unsupported converter metadata when behavior cannot be modeled;
- unsupported extension-data member type;
- ambiguous or unsafe resolver-property matching;
- invalid automatic semantic Entity hierarchy (`STJ009`).

New/changed public diagnostics update the public diagnostics reference.

## Inspection

Deterministic inspection includes, as applicable:

- projected type/CLR identity;
- matched properties and selected name source;
- final JSON names when determinable;
- Strong Scalar runtime descriptors;
- automatic hierarchy base/descendant/discriminator metadata;
- existing explicit-polymorphism preservation marker;
- ignored/unsupported properties;
- extension-data members;
- diagnostics.

## Tests

M0071 runtime tests use real generated annotated fixture assemblies rather than hand-built canonical models for positive boundary behavior.

Required STJ-focused categories:

- plain `JsonSerializerOptions` + default resolver behavior;
- canonical/domain options-overload equivalence;
- Strong Scalar backing-kind matrix, including Guid;
- abstract Entity base -> concrete Entity round-trip;
- concrete Entity base with descendants where applicable;
- derived type containing Guid Strong Scalar;
- explicit application STJ polymorphism preserved unchanged;
- Model A + Model B registered on one options instance;
- registration order independence;
- deterministic duplicate/conflict failure;
- registration before first use succeeds and post-freeze mutation is not claimed/supported;
- imported naming/ignore/extension-data behavior already supported by runtime projection.

Source-generated `JsonSerializerContext` coverage is out of scope for M0071.

## Invariants

- System.Text.Json metadata remains projection-specific, not projection-neutral canonical structure.
- Resolver/runtime behavior is driven by `SystemTextJsonSemanticModel`.
- The canonical semantic model remains immutable during derivation/runtime composition.
- Existing serializer behavior is preserved by default except for opinionated automatic semantic-Entity polymorphism when no explicit application contract exists.
- STM does not depend on mutable `DefaultJsonTypeInfoResolver.Modifiers` composition.
- Multiple independent STM models can coexist on one runtime options instance before first use.
- Unsupported or ambiguous behavior is explicit; silent lossy behavior is not allowed.
