# M0069 Forward-Port Strong Scalar to Main — Deferred Documentation Sync

## Owning Milestone

`M0069 — Forward-Port Strong Scalar to Main`

## Purpose

Synchronize consumer-facing next-major documentation after Strong Scalar has been reconciled onto post-M0067 main.

This file is deferred documentation-sync metadata. Ordinary implementation agents are not required to read it.

## Required Consumer Truth

Current/future mainline consumer documentation must eventually make these points discoverable:

1. Strong Scalar is a first-class projection-neutral nominal scalar semantic authored explicitly with `[SemanticStrongScalar]`.
2. Supported Strong Scalars use the underlying scalar representation in JSON Schema, STM-configured System.Text.Json, EF scalar storage/owned JSON, and supported scalar-consuming projections.
3. Strong Scalar does not itself mean Identifier, Key, Entity, or ownership.
4. The `SpecificationVersionId(Guid Value)` nested-owned-JSON case is supported without object-shaped `{ "Value": ... }` JSON.
5. Automatic one-property-wrapper inference is not supported.
6. The post-M0067 mainline does not include `SemanticTypeModel.Configuration`; documentation must not resurrect Configuration/Options examples while explaining Strong Scalar.
7. Do not describe next-major main as the 4.1 package topology. The 4.1 maintenance line and next-major line have intentionally different package sets.
8. Do not claim publication/version availability until verified independently during release synchronization.

## Surfaces to Reconcile

Review/update as applicable during the documentation-sync pass:

```text
README.md
public-docs/usage.md
public-docs/guides/projection-capabilities.md
public-docs/guides/json-schema.md or current JSON Schema guide
public-docs/guides/system-text-json.md or current System.Text.Json guide
public-docs/guides/ef-core.md or current EF Core guide
public-docs/diagnostics.md
public-docs/diagnostics/*
public-docs/nuget/SemanticTypeModel.md
public-docs/api/compatibility.md
public-docs/release-notes.md       only during appropriate release synchronization
docs/PUBLIC-DOCS.md
```

Use actual current filenames/authority discovered during the sync; the list is a routing hint, not an edit allowlist.

## Cross-Line Note

The 4.1 maintenance line may document Strong Scalar together with the eleven-package suite that still contains Configuration.

Post-M0067 main must document Strong Scalar together with the ten-package next-major suite and Configuration removal.

Do not copy maintenance-line package or migration text wholesale between those lines.
