# Compatibility

## Public API Compatibility

- Public API baseline is currently documentation-driven and validated by `./eng/public-api.sh` presence checks.
- Breaking changes require explicit milestone/release documentation.
- `SemanticTypeModel.SystemTextJson` 1.1.0 removed generated `JsonSerializerContext` support as a documented compatibility correction because the 1.0 design depended on unsupported source-generator chaining.
- 2.2.0 release-preparation documentation records M0038 as the model-surface cleanup boundary; removed `Canonical` namespace and old shape-graph APIs must not be presented as supported current usage.

## 2.2.0 Model Surface Compatibility

- `SemanticTypeModel.Abstractions.Model` is the supported public model surface for canonical semantic model contracts.
- `SemanticTypeModel.Abstractions.Canonical` is removed from shipped source and public API baselines.
- The old `TypeShape` / `ObjectShape` / `PropertyShape` / `ShapeRef` shape graph is removed rather than retained as a compatibility shim.
- Source-generated providers return `SemanticTypeModel.Abstractions.Model.TypeSchemaModel` directly for projection packages.
- JSON Schema import remains compatibility-oriented and is not the supported canonical authoring path; use annotated .NET code plus generated providers for public samples and package guidance.

## Diagnostics Compatibility

- Diagnostics are currently preview/unstable unless explicitly declared stable in release notes.

## Runtime Compatibility

- Preserve canonical command and package naming constraints:
  - solution `SemanticTypeModel.slnx`
  - root namespace `SemanticTypeModel`
  - package prefix `SemanticTypeModel.*`
