# M0068 Strong Scalar / 4.1 — Deferred Documentation Sync

## Owning Milestone

`M0068 — Strong Scalar for 4.1`

## Purpose

Synchronize consumer-facing documentation after Strong Scalar implementation is complete and 4.1 release readiness is being prepared.

This is deferred documentation-sync metadata. Ordinary implementation agents do not read it.

## Required Consumer Truth

Public documentation must explain:

1. 4.1 adds explicit nominal Strong Scalar semantics through `[SemanticStrongScalar]`.
2. Strong Scalar is not an Identifier/Key role; key semantics remain property/group semantics.
3. The supported 4.1 CLR shape is a non-generic readonly struct/record struct with one `Value` property and a matching public constructor.
4. Strong Scalar is explicit only; arbitrary one-property wrappers are not reclassified automatically.
5. JSON Schema represents Strong Scalar using the underlying scalar shape and optionally `x-stm.strongScalar`.
6. STM-configured System.Text.Json serializes Strong Scalar as the underlying scalar value, not `{ "Value": ... }`.
7. EF maps Strong Scalar directly to the underlying provider scalar and supports Strong Scalar leaves inside STM-owned JSON.
8. The `SpecificationVersionId(Guid Value)` nested-owned-JSON failure from 4.0.1 is fixed by first-class Strong Scalar semantics.
9. Existing unannotated direct EF strong-wrapper behavior remains compatible.
10. Configuration/Options Strong Scalar binding is not a 4.1 capability and must not be advertised.
11. 4.1 retains the pre-M0067 eleven-package suite, including `SemanticTypeModel.Configuration`.
12. M0067 Configuration removal belongs to the later major-version line, not 4.1.

## Surfaces to Reconcile

Review/update as applicable:

```text
README.md
public-docs/getting-started.md
public-docs/concepts.md
public-docs/configuration.md
public-docs/guides/core-semantics.md
public-docs/guides/json-schema-projection.md
public-docs/guides/system-text-json.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/projection-capabilities.md
public-docs/diagnostics.md
public-docs/diagnostics/stm5xxx.md
public-docs/api/compatibility.md
public-docs/release-notes.md
public-docs/nuget/SemanticTypeModel.md
public-docs/samples.md
docs/PUBLIC-DOCS.md
```

Use the repository's actual current filenames when guide names differ.

## 4.1 Release Narrative

The eventual 4.1 release documentation should combine the unreleased post-4.0.1 additive work on the 4.1 line, including M0065/M0066 and Strong Scalar, without including M0067 Configuration removal.

Do not claim publication until the actual package channel confirms it.

Do not describe Strong Scalar as an EF-only workaround; the EF regression exposed a missing canonical nominal-scalar concept.
