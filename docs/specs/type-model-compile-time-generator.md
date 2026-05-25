# Type Model Compile-Time Generator Specification

## Purpose

Define the baseline incremental source generator that emits deterministic model-provider code from extracted .NET type metadata.

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
  - direct projection output generation (JSON Schema/UI/EF Core/Power BI/TOM/OpenAPI).

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

## Configuration Baseline

M0009 supports configuration via attributes:

- `[assembly: SemanticTypeModelGeneratorOptions(...)]`
  - generated namespace;
  - generated provider name;
  - include internal types;
  - XML documentation requirement toggle.

Additional configuration mechanisms (MSBuild/additional files/analyzer config) are deferred.

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
