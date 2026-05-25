# M0010 - Attribute and Convention Model for .NET Type Extraction

## Purpose

Harden M0009 extraction/generator behavior with a minimal attribute vocabulary and explicit deterministic convention system.

## Delivered Scope

- Stable .NET extraction attributes:
  - `SemanticType`
  - `SemanticIgnore`
  - `SemanticName`
  - `SemanticDescription`
  - `SemanticRole`
  - `SemanticKey`
  - `SemanticRelationship`
- Deterministic attribute precedence:
  - explicit attribute;
  - XML documentation (opt-in);
  - naming policy;
  - symbol fallback.
- Convention/discovery options:
  - `ExplicitAttributes` default;
  - `Namespace` discovery;
  - `ReachableFromRoots`;
  - conservative `AssemblyPublicTypes`.
- Namespace include/exclude behavior with exclusion precedence.
- Naming policy support:
  - preserve, camelCase, snake_case, kebab-case;
  - collision diagnostics.
- Safe key inference (`Id`, `{TypeName}Id`) with ambiguity diagnostics.
- Composite key grouping by shared semantic key name and order.
- Conservative relationship inference plus explicit relationship metadata.
- Analyzer/MSBuild generator configuration options with `SemanticTypeModel*` prefix.
- Stable extraction/generator diagnostics (`STM5xxx`) for invalid configuration and conflicts.

## Diagnostics Summary

M0010 extends `STM5xxx` diagnostics for:

- invalid/conflicting semantic attributes;
- invalid discovery configuration;
- duplicate semantic names;
- convention-included-but-ignored types;
- key/relationship ambiguity;
- relationship target omissions;
- invalid composite key ordering;
- unsupported semantic argument values;
- unsupported naming policy;
- provider-name collisions;
- XML documentation requirements.

## Fixture Coverage

Short-running generator fixtures cover:

1. attribute precedence;
2. ignore behavior;
3. namespace include/exclude discovery;
4. naming policy and collision diagnostics;
5. key inference and ambiguity;
6. composite key ordering;
7. relationship inference and missing targets;
8. generator option configuration and invalid options;
9. XML documentation description precedence;
10. transformation-pipeline + JSON Schema composition.

## Non-goals Preserved

- no projection-specific attribute framework;
- no EF Core/Power BI/TOM/OpenAPI attribute surface additions;
- no runtime DI or ASP.NET integration;
- no analyzer/fixer package;
- no long-running benchmark or integration test expansion.
