# Remove Stale Public API Baselines

## Status

Accepted.

## Context

The repository carried text API baseline files without an API analyzer enforcing them. The release helper only checked that the files existed and that unshipped files were empty, so the files could appear authoritative while not proving binary or source compatibility.

## Decision

Remove the stale public API baseline files and remove the stale script-only baseline checker from release validation. Compatibility review remains required, but it is documented through package smoke tests, runnable samples, public documentation, release notes, compatibility documentation, and human review.

## Consequences

- Release validation no longer claims API-baseline enforcement that the repository does not provide.
- Compatibility expectations are reviewed in the documentation and sample surfaces consumers use.
- A future milestone may add a real API compatibility analyzer, but this milestone intentionally does not introduce one.
