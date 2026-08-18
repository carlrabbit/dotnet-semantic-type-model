# M0065 Documentation Synchronization Hint

## Source milestone

`M0065 — Display Identity and Access Paths`

## Status

Pending after implementation.

## Purpose

Synchronize consumer-facing documentation for the new annotation-only core semantics after implementation is complete.

Ordinary implementation agents must not read this file.

## Required public synchronization

Update current public documentation so consumers can discover and correctly distinguish the concepts.

At minimum inspect and synchronize:

- `public-docs/guides/core-semantics.md`
- `public-docs/guides/projection-capabilities.md`
- `public-docs/nuget/SemanticTypeModel.md`
- `public-docs/diagnostics/stm5xxx.md`

Consumer-facing definitions:

```text
SemanticKey
    -> machine/domain identity

SemanticDisplayIdentity
    -> ordered properties humans can use to recognize an instance

SemanticAccessPath
    -> named ordered properties representing an intended locate/filter route
```

Document the public authoring forms:

```csharp
[SemanticDisplayIdentity(Order = 0)]

[SemanticAccessPath("ByCustomerNumber")]

[SemanticAccessPath("ByDeviceAndTimestamp", Order = 0)]
```

State clearly that M0065 is annotation-only:

- no EF Core index is generated;
- no API query parameter is generated;
- no UI/list/form behavior is generated;
- no Power BI behavior is generated;
- JSON Schema `x-stm` is not extended by M0065;
- no relationship behavior is implied.

Add diagnostics reference entries for:

- `STM5049` — invalid or ambiguous Display Identity definition;
- `STM5050` — invalid or ambiguous Access Path definition.

## Release synchronization

When preparing the first release containing M0065:

- treat the feature as additive minor-version functionality;
- describe Display Identity and Access Path as new core authoring semantics;
- do not claim target-specific index/query/UI behavior;
- do not perform release work merely because this sync hint exists.

## README

A root README change is optional unless the documentation synchronization pass decides the new semantics belong in the repository's minimal first-model example or capability summary.

Do not expand the root README into an attribute catalog.

## Completion

Delete this hint after the affected public documentation is synchronized.

Do not retain it as historical release documentation; Git is history.
