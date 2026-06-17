# Guide Sync Hint — M0039 Documentation Sync and 2.2.0 Release Preparation

## Status

Consumed by M0039; retained as history with completion notes.

## Purpose

Carry deferred documentation-synchronization and release-readiness reminders for M0039 without requiring ordinary implementation agents to read `.guide-sync/`.

## Applies To

```text
M0039: Documentation Synchronization and 2.2.0 Release Preparation
```

## Hints

### Verify M0038 before documenting release behavior

- Confirm the implemented model-surface unification before writing consumer-facing 2.2.0 claims.
- Treat any remaining `SemanticTypeModel.Abstractions.Canonical`, old shape-graph, adapter, or hand-built sample usage as a release-readiness blocker unless explicitly documented as internal-only transitional residue.

### Synchronize internal indexes

Check and update when needed:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/TERMINOLOGY.md
```

### Synchronize public docs

Check and update when needed:

```text
README.md
public-docs/getting-started.md
public-docs/concepts.md
public-docs/packages.md
public-docs/guides/core-semantics.md
public-docs/guides/json-schema.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/system-text-json.md
public-docs/guides/projection-capabilities.md
public-docs/nuget/*.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
public-docs/versioning.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
```

### Search terms to eliminate or justify

Review active source-facing and public documentation for stale terms:

```text
SemanticTypeModel.Abstractions.Canonical
Abstractions.Canonical
Canonical.TypeSchemaModel
TypeShape
ObjectShape
PropertyShape
ShapeRef
SchemaAnnotation
old shape graph
legacy model
hardened model
hand-built canonical model
```

Historical milestone titles may retain historical wording. Active consumer docs and current specs should not present obsolete surfaces as supported usage.

### Release readiness

Before completing M0039, run or record blockers for:

```sh
./eng/public-docs.sh
./eng/package.sh 2.2.0
./eng/package-smoke.sh 2.2.0
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.2.0
```

Do not publish packages, create tags, or create GitHub releases as part of this hint.

## Completion Notes

M0039 unpacked the milestone package, removed the source zip, synchronized 2.2.0 public documentation/package README sources, and recorded release-review items in the milestone. Retain this hint as historical synchronization context unless the repository later defines an archive convention for resolved guide-sync hints.
