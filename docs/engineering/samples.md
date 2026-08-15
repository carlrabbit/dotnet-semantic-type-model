# Samples Engineering

## Purpose

Define executable package-consumer samples as documentation and representative compatibility canaries.
Samples do not replace exhaustive tests.

## Public documentation boundary

- Executable samples live under `samples/`.
- Detailed sample behavior is documented by readable sample source and project configuration.
- Do not add README files under sample projects.
- Do not maintain `public-docs/samples/*.md` pages.
- `public-docs/samples.md` is the only sample documentation index and links directly to projects.

## Package consumption

Public samples consume SemanticTypeModel through package references, not `src/*` project references.
The shared domain project may be referenced by projection sample projects when it is part of the sample design.

All `SemanticTypeModel.*` package references in a sample must use the same exact version.

## Validation

Prepare local packages, then run:

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

Samples must be deterministic and must not depend on external services, secrets, network access, or a
production database.

## Sample quality

A sample should make the consumer pattern obvious from source:

- package references;
- minimum semantic model definition;
- generation/configuration point;
- target call;
- diagnostic handling;
- representative output/assertion.

Do not compensate for unclear sample code with a duplicate Markdown walkthrough; improve the sample itself or
the relevant public guide.
