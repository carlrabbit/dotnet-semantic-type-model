# Specifications

## Purpose

Specs define behavioral truth.

Specs are authoritative for:
- behavior;
- invariants;
- contracts;
- inputs and outputs;
- failure semantics;
- validation expectations.

## Rules

- Specs must use canonical terminology.
- Specs must define invariants explicitly.
- Specs must avoid implementation plans.
- Specs should exist before implementation whenever practical.

## Available Specs

| Spec | Purpose |
|---|---|
| specs/current-canonical-model-surface.md | Current canonical semantic model surface, legacy-removal boundary, and active terminology rules |
| specs/type-schema-model.md | Canonical semantic type model contracts, invariants, and traversal behavior |
| specs/type-model-core.md | Canonical type model layers, contracts, invariants, and diagnostics model |
| specs/core-semantic-vocabulary.md | Projection-neutral core semantic vocabulary, usage guidance, and envelope/evolution semantics |
| specs/type-model-annotations.md | Annotation namespace policy, preservation rules, and projection separation |
| specs/type-model-json-schema-mapping.md | JSON Schema Draft 2020-12 mapping baseline for code-first semantic models |
| specs/json-schema-domain-model-and-export.md | JSON Schema domain semantic model derivation and deterministic export behavior |
| specs/envelope-projection-policies.md | Envelope projection policies for JSON Schema, EF Core, and Power BI, including payload representation/storage defaults |
| specs/evolution-ownership-and-lifecycle-semantics.md | Ownership, versioning, temporal validity, lifecycle state, and extension-data semantics and projection implications |
| specs/type-model-dotnet-extraction.md | Roslyn-based .NET type-system discovery and extraction baseline into canonical model contracts |
| specs/type-model-dotnet-attributes.md | Stable attribute vocabulary, including envelope and evolution attributes, precedence, and diagnostics for .NET extraction |
| specs/type-model-dotnet-conventions.md | Deterministic discovery, naming, inference, and configuration conventions for .NET extraction |
| specs/type-model-compile-time-generator.md | Incremental generator baseline for deterministic compile-time model-provider generation |
| specs/type-model-runtime-api.md | Runtime model provider, service, result, caching, and diagnostics contracts |
| specs/type-model-di-integration.md | Dependency-injection registration, lifetime, and projection integration pattern for runtime model services |
| specs/type-model-query-and-inspection.md | Query and inspection API behavior for canonical and domain semantic models |
| specs/type-model-transformation-and-domain-derivation.md | Transformation pipeline, domain derivation, diagnostics, and trace behavior |
| specs/type-model-ui-hints.md | Generic UI hint vocabulary, normalization, and JSON-editor compatibility mapping |
| specs/type-model-projection-capabilities.md | Projection capability matrix, compatibility contracts, and capability metadata API across targets |
| specs/type-model-powerbi-tom-projection.md | Power BI domain semantic model derivation and local deterministic metadata projection |
| specs/type-model-ef-core-projection.md | EF Core domain semantic model derivation and provider-neutral `ModelBuilder` projection |
| specs/system-text-json-contract-integration.md | System.Text.Json contract metadata import, annotation keys, resolver customization, unsupported generated-context behavior, and diagnostics |
| specs/system-text-json-domain-model-and-resolver-projection.md | System.Text.Json domain semantic model derivation and resolver customization projection |
| specs/diagnostics.md | Diagnostic ID, severity, stability, and public documentation rules |
