# M0067 Remove Configuration / Options Integration — Deferred Documentation Sync

## Owning Milestone

`M0067 — Remove Configuration / Options Integration`

## Purpose

Synchronize consumer-facing documentation after the Configuration/Options capability has been removed from the implementation and the milestone has satisfied its completion contract.

This file is deferred documentation-sync metadata. Ordinary implementation agents are not required to read it.

## Required Consumer Truth

Public documentation must make these points discoverable:

1. `SemanticTypeModel.Configuration` is no longer part of the current package suite.
2. `AddSemanticOptions<TOptions>` and the former Configuration domain/runtime registration APIs are no longer supported current APIs.
3. Former Configuration-specific authoring contracts for section binding, section presence, DataAnnotations policy, startup validation, and generated registration helpers are removed.
4. Applications use Microsoft.Extensions.Configuration / Microsoft.Extensions.Options directly when they need application configuration binding/registration.
5. `SemanticTypeRole.Configuration` remains supported as projection-neutral semantic meaning.
6. `SemanticRequiredWhen` remains supported independently and continues to participate in supported JSON Schema conditional validation.
7. The removal is a next-major-version breaking change; do not describe it as a 4.x patch/minor-compatible change.
8. Do not advertise a compatibility/tombstone package or replacement STM Options integration.
9. Historical release notes remain historical truth and should not be rewritten to pretend the package never existed.

## Surfaces to Reconcile

Review/remove/update as applicable:

```text
README.md
public-docs/usage.md
public-docs/configuration.md
public-docs/guides/configuration-options.md
public-docs/guides/projection-capabilities.md
public-docs/samples.md
public-docs/troubleshooting.md
public-docs/api/compatibility.md
public-docs/nuget/SemanticTypeModel.md
public-docs/diagnostics.md
public-docs/diagnostics/*
docs/PUBLIC-DOCS.md
public-docs/release-notes.md    only during the appropriate release synchronization
```

Delete obsolete current Configuration-specific consumer pages rather than retaining them as archives when their information has no current supported use.

Update navigation/validation code that enumerates public documentation surfaces as needed during the documentation-sync pass.

## Migration Guidance

Major-version migration guidance should tell former consumers to:

- remove the `SemanticTypeModel.Configuration` package reference;
- remove Configuration-specific STM authoring attributes;
- replace `AddSemanticOptions<TOptions>` with application-owned Microsoft.Extensions.Options registration/binding/validation as appropriate;
- keep `SemanticTypeRole.Configuration` when the semantic role is still meaningful;
- keep `SemanticRequiredWhen` when the conditional semantic constraint is still required by JSON Schema or other projection-neutral consumers.

Do not prescribe one application-wide Microsoft.Extensions.Options registration recipe; STM no longer owns that application policy.
