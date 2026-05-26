---
name: Release
about: Plan and execute a prerelease publication
labels: release
---

## Release Version

<!-- e.g., 0.1.0-alpha -->

## Package Set

- [ ] SemanticTypeModel.Abstractions
- [ ] SemanticTypeModel.Core
- [ ] SemanticTypeModel.JsonSchema
- [ ] SemanticTypeModel.DotNet
- [ ] SemanticTypeModel.Generators
- [ ] SemanticTypeModel.JsonEditor
- [ ] SemanticTypeModel.PowerBI
- [ ] SemanticTypeModel.EFCore

## Validation

```sh
./eng/check.sh
./eng/release-check.sh <version>
```

## Publishing

- [ ] `publish-nuget.yml` manual run executed
- [ ] `NUGET_API_KEY` configured
- [ ] publish was duplicate-safe (`--skip-duplicate`)
