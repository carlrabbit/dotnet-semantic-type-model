# Compatibility

## Public API Compatibility

- Public API baseline is currently documentation-driven and validated by `./eng/public-api.sh` presence checks.
- Breaking changes require explicit milestone/release documentation.

## Diagnostics Compatibility

- Diagnostics are currently preview/unstable unless explicitly declared stable in release notes.

## Runtime Compatibility

- Preserve canonical command and package naming constraints:
  - solution `SemanticTypeModel.slnx`
  - root namespace `SemanticTypeModel`
  - package prefix `SemanticTypeModel.*`
