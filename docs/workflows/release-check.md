# Release Check Workflow

## Goal

Run release-readiness validation without publishing artifacts.

## Constraints

- Must be manual (`workflow_dispatch`).
- Must not publish.
- Must run canonical engineering commands.

## Validation Steps

1. `./eng/release-check.sh <version>`

`./eng/release-check.sh <version>` must run:

1. `./eng/check.sh`
2. `dotnet build --configuration Release`
3. `./eng/package.sh <version>`
4. `./eng/package-smoke.sh <version>`
5. `./eng/samples.sh` when present
6. `./eng/public-docs.sh`
7. `./eng/public-docs.sh`
