# Release Check Workflow

## Goal

Run release-readiness validation without publishing artifacts.

## Constraints

- Must be manual (`workflow_dispatch`).
- Must not publish.
- Must run canonical engineering commands.
- Must validate the exact requested candidate version.

## Validation Steps

The workflow runs:

```bash
./eng/release-check.sh <version>
```

That aggregate command must run:

1. `./eng/check.sh`
2. `dotnet build --configuration Release`
3. `./eng/package.sh <version>`
4. `./eng/package-smoke.sh <version>`
5. `./eng/samples.sh` when present
6. `./eng/public-docs.sh`

A successful workflow run is release-readiness evidence for the commit and version it actually validated. It does not publish packages or authorize publication.
