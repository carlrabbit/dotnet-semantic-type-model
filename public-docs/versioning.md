# Versioning

## Package suite rule

All `SemanticTypeModel.*` packages are treated as one aligned package suite.

**Use the same exact version for every SemanticTypeModel package in one application. Mixing suite versions is
unsupported.**

This includes runtime, abstractions, projection, and generator/analyzer packages.

## Consumer examples

Version-neutral evergreen snippets are preferred:

```sh
dotnet add package SemanticTypeModel.DotNet
dotnet add package SemanticTypeModel.Generators
dotnet add package SemanticTypeModel.JsonSchema
```

When a consumer pins versions explicitly, pin all SemanticTypeModel packages to the same value. For example,
with central package management use one shared version property rather than independently maintained values.

## Compatibility

Semantic versioning applies to stable releases. See [Compatibility](api/compatibility.md) for current
compatibility boundaries and [Release notes](release-notes.md) for version-specific chronology.

## Publication truth

Repository source or documentation for a prospective release does not prove that a package was published. Current publication state must be verified from the actual package/release channel during release
work.

Evergreen usage/configuration guides therefore do not declare a guessed current publication version.
