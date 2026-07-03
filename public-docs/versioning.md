# Versioning

## Policy

- `1.0.0` is the first stable release.
- Semantic versioning applies after `1.0.0`.
- Documented public APIs are covered by the compatibility policy.
- Experimental APIs are explicitly marked when present.
- Diagnostic IDs are stable unless the compatibility policy explicitly says otherwise.
- Annotation keys are stable unless the compatibility policy explicitly says otherwise.
- Prerelease APIs before 1.0 were not compatibility-stable.

## Compatibility

Public API compatibility expectations are tracked in [api/compatibility.md](api/compatibility.md).

## Current Release Candidate

`2.4.1` is the current patch release-preparation target. It corrects the 2.4.0 dictionary key extraction defect that could omit key type definitions and produce `STM0002` for valid dictionary models, most visibly `[SemanticExtensionData] Dictionary<string, JsonElement>?`. Publication, tag creation, and GitHub release creation remain separate human-approved actions.
