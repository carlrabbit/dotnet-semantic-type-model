# M0023: 1.0 Release Execution

## Status

Planned milestone content document.

## Goal

Execute the final `1.0.0` release after the M0022 release-readiness gate has stabilized the public API, package set, documentation, samples, compatibility policy, and release validation commands.

## Scope

- choose and tag the final `1.0.0` version;
- run the canonical release gate;
- publish approved NuGet packages;
- verify package availability;
- update release notes with final release details.

## Required Validation

```sh
./eng/check.sh
./eng/release-check.sh 1.0.0
```

## Non-Goals

This milestone should not introduce new public API surface or projection behavior. Any compatibility-affecting change must go back through a readiness milestone before release execution.
