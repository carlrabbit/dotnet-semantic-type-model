---
name: Release
about: Plan and execute a release
labels: release
---

## Release Version

<!-- e.g., 1.0.0-preview.1 -->

## Release Notes Summary

<!-- Summary of changes in this release -->

## Public Documentation Checklist

- [ ] README.md is user-first and current
- [ ] package README source is current (`public-docs/nuget/package-readme.md`)
- [ ] getting started docs are current (`public-docs/getting-started.md`)
- [ ] installation docs are current (`public-docs/installation.md`)
- [ ] public API docs are current (`public-docs/api/public-api.md`)
- [ ] compatibility docs are current (`public-docs/api/compatibility.md`)
- [ ] diagnostics docs are current (`public-docs/diagnostics.md` and `public-docs/diagnostics/`)
- [ ] sample docs are current (`public-docs/samples.md` and `public-docs/samples/`)
- [ ] versioning policy is current (`public-docs/versioning.md`)
- [ ] release notes are current (`public-docs/release-notes.md`)

## Release Checklist

- [ ] All milestone exit criteria satisfied
- [ ] Release-readiness command passes
- [ ] Version updated
- [ ] Tag planned/created

## Validation

```sh
./eng/release-check.sh <version>
```

## Related Documents

- [ ] docs/MILESTONES.md
- [ ] docs/PUBLIC-DOCS.md
- [ ] docs/engineering/release-readiness.md
