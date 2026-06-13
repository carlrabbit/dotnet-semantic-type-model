# Compatibility

## Public API Compatibility

- Public API baseline is currently documentation-driven and validated by `./eng/public-api.sh` presence checks.
- Breaking changes require explicit milestone/release documentation.
- `SemanticTypeModel.SystemTextJson` 1.1.0 removed generated `JsonSerializerContext` support as a documented compatibility correction because the 1.0 design depended on unsupported source-generator chaining.
- M0035 is the planned cleanup boundary for removing old model compatibility APIs, stale transition terminology, and System.Text.Json implementation paths that bypass the domain-projection architecture.

## Diagnostics Compatibility

- Diagnostics are currently preview/unstable unless explicitly declared stable in release notes.

## Runtime Compatibility

- Preserve canonical command and package naming constraints:
  - solution `SemanticTypeModel.slnx`
  - root namespace `SemanticTypeModel`
  - package prefix `SemanticTypeModel.*`
