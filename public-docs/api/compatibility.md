# Compatibility

## Public API Compatibility

- Public API baseline is currently documentation-driven and validated by `./eng/public-api.sh` presence checks.
- Breaking changes require explicit milestone/release documentation.
- `SemanticTypeModel.SystemTextJson` 1.1.0 removed generated `JsonSerializerContext` support as a documented compatibility correction because the 1.0 design depended on unsupported source-generator chaining.
- 2.1.0 candidate documentation records M0035 as a release-review boundary; retained old model compatibility APIs and JSON Schema import APIs must not be presented as the primary canonical authoring path unless a later implementation milestone removes or replaces them.

## Diagnostics Compatibility

- Diagnostics are currently preview/unstable unless explicitly declared stable in release notes.

## Runtime Compatibility

- Preserve canonical command and package naming constraints:
  - solution `SemanticTypeModel.slnx`
  - root namespace `SemanticTypeModel`
  - package prefix `SemanticTypeModel.*`
