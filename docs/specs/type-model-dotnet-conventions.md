# Type Model .NET Convention Specification

## Purpose

Define deterministic, opt-in convention behavior for .NET type extraction.

## Discovery Modes

`DotNetTypeDiscoveryMode`:

- `ExplicitAttributes` (default): `[SemanticType]` roots only.
- `Namespace`: include roots from configured namespace prefixes.
- `ReachableFromRoots`: explicit roots plus reachable graph expansion.
- `AssemblyPublicTypes`: conservative full-assembly opt-in.

Rules:

- convention scanning is opt-in;
- `Namespace` mode requires `IncludedNamespaces`, otherwise `STM5009`;
- unsupported mode values emit `STM5008`.

## Inclusion and Exclusion

- Include match: exact namespace or namespace prefix.
- Exclude match: exact namespace or namespace prefix.
- Exclusion wins over inclusion.
- `[SemanticIgnore]` wins over both (`STM5010` may be emitted to explain convention suppression).

## Member Selection

Default included:

- public instance properties.

Optional via configuration:

- internal instance properties when `IncludeInternalMembers=true`.

Always excluded:

- static properties;
- indexers;
- compiler-generated members;
- ignored members (`[SemanticIgnore]`).

## Naming Policy

`DotNetNamingPolicy` options:

- `Preserve`;
- `CamelCase`;
- `SnakeCase`;
- `KebabCase`.

Rules:

- explicit `[SemanticName]` overrides policy;
- collisions after policy emit `STM5006`;
- unsupported policy emits `STM5018`.

## Key Inference

When `InferKeys=true`:

- `Id` and `{TypeName}Id` are primary-key candidates.
- Single candidate is selected deterministically.
- Multiple candidates emit `STM5013`.
- `[SemanticKey]` takes precedence over inferred keys.

Composite keys:

- supported through shared `[SemanticKey(Name=..., Order=...)]`;
- duplicate or missing order emits `STM5016`.

## Relationship Inference

When `InferRelationships=true`:

- reference members to object-like types infer `ManyToOne`;
- collections of object-like types infer `OneToMany`;
- multiple candidates on a type emit `STM5014`.

Explicit relationships:

- `[SemanticRelationship]` maps explicit annotations and overrides inference.

Target validation:

- relationship targets missing from the extracted model emit `STM5015`.

## XML Documentation Convention

When `IncludeXmlDocumentation=true`:

- type/property summaries map to `schema.description` if no explicit semantic description exists.

When `RequireXmlDocumentation=true`:

- missing XML docs emit `STM5012`.

## Generator Configuration Surface

Convention and generation options are supported through analyzer/MSBuild properties:

- `SemanticTypeModelDiscoveryMode`
- `SemanticTypeModelIncludedNamespaces`
- `SemanticTypeModelExcludedNamespaces`
- `SemanticTypeModelIncludeInternalTypes`
- `SemanticTypeModelIncludeInternalMembers`
- `SemanticTypeModelNamingPolicy`
- `SemanticTypeModelInferKeys`
- `SemanticTypeModelInferRelationships`
- `SemanticTypeModelIncludeXmlDocumentation`
- `SemanticTypeModelRequireXmlDocumentation`
- `SemanticTypeModelGeneratedNamespace`
- `SemanticTypeModelGeneratedProviderName`
