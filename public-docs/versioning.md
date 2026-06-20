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

`2.3.0` is the current release-preparation target. It includes the Configuration packages, explicit per-type options registration, selected-type Configuration derivation, required section presence validation, generated-helper delegation guidance, and documentation synchronization for package publication review. Publication, tag creation, and GitHub release creation remain separate human-approved actions.
