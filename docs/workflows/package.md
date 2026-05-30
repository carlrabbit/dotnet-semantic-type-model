# Package Workflow

## Goal

Create inspectable release NuGet package artifacts without publishing.

## Constraints

- Manual (`workflow_dispatch`) only.
- Must call `./eng/package.sh <version>`.
- Must upload `.nupkg` and `.snupkg` artifacts.
- Must never publish.

## Inputs

- release version string

## Outputs

- package artifacts under `artifacts/nuget`
- uploaded GitHub Actions artifacts
