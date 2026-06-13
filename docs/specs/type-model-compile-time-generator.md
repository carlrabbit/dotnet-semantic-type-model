# Type Model Compile-Time Generator Specification

## Purpose

Define the baseline incremental source generator that emits deterministic model-provider code from extracted .NET type metadata.

Compile-time generation is one of the supported code-first canonical model creation paths.

## Authority

This specification is authoritative for:

- generator package boundary and responsibilities;
- generated provider API baseline;
- deterministic output expectations;
- generator configuration baseline;
- generator diagnostics expectations;
- required short-running generator test coverage.

## Package Boundary

- Generator package: `SemanticTypeModel.Generators`.
- Responsibilities:
  - discover configured extraction roots;
  - invoke `SemanticTypeModel.DotNet` extraction logic;
  - emit deterministic provider code that constructs canonical `TypeSchemaModel`;
  - surface extraction/generation diagnostics.
- Non-responsibilities:
  - direct projection output generation that bypasses the canonical model and domain semantic model pipeline.

## Generated API Baseline

Generated provider baseline:

```csharp
namespace SemanticTypeModel.Generated;

public static partial class AppSemanticTypeModel
{
    public static TypeSchemaModel Create();
}
```

Rules:

- generated output must be deterministic;
- generated output must compile without runtime reflection;
- generated output must use canonical library namespaces (`SemanticTypeModel.*`);
- generated output must produce canonical model instances consumable by existing transformations/projections.

## Runtime Compatibility

M0011 keeps the generated static-factory shape as the compatibility baseline.

Rules:

- runtime and DI integration must consume generated providers through the static `Create()` pattern rather than through generator-specific projection shortcuts;
- current generated factories may be adapted by runtime registration helpers before entering the runtime canonical semantic model service pipeline;
- transformations and projections still operate on the canonical runtime model boundary after any compatibility adaptation is complete.

## Configuration Baseline

M0009 supports configuration via attributes:

- `[assembly: SemanticTypeModelGeneratorOptions(...)]`
  - generated namespace;
  - generated provider name;
  - include internal types;
  - XML documentation requirement toggle.

Additional configuration mechanisms (MSBuild/additional files/analyzer config) are deferred.

M0010 adds analyzer/MSBuild `build_property` options with `SemanticTypeModel*` prefix:

- `SemanticTypeModelGeneratedNamespace`
- `SemanticTypeModelGeneratedProviderName`
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

When both attribute and analyzer/MSBuild options are present, extraction still merges assembly attribute options and resolved analyzer options deterministically.

## Diagnostics

Generator diagnostics use stable `STM5xxx` codes surfaced from extraction and include:

- invalid attribute usage;
- unsupported type shapes;
- unsupported open generics;
- unsupported dictionary key types;
- enum ambiguity;
- deferred/unsupported extraction scenarios.

## Test Coverage Requirements

Short-running tests must cover at least:

1. simple annotated object baseline;
2. scalar mapping baseline;
3. collection/dictionary baseline and unsupported key diagnostics;
4. enum extraction baseline;
5. nested object reachability baseline;
6. generic identity baseline and open generic diagnostics;
7. inheritance/interface metadata baseline;
8. generated-model-to-JSON-Schema export composition baseline.
