# CI Workflow

## Goal

Validate pull requests and pushes to `main` using canonical checks.

## Constraints

- Must use `./eng/check.sh`.
- Must support repositories with and without Bun tooling.
- Must not publish packages.

## Triggers

- Pull requests.
- Pushes to `main`.
