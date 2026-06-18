# Public Documentation

## Purpose

Define authoritative consumer-facing documentation surfaces and synchronization rules.

## Authority

This document is authoritative for:
- public documentation surfaces;
- source-of-truth mapping for package README content;
- package documentation standards;
- diagnostics reference mapping;
- sample documentation mapping;
- release notes obligations;
- synchronization requirements between root docs and `public-docs/`.

## Public Documentation Surfaces

- `README.md`
- `public-docs/getting-started.md`
- `public-docs/installation.md`
- `public-docs/concepts.md`
- `public-docs/packages.md`
- `public-docs/samples.md`
- `public-docs/diagnostics.md`
- `public-docs/versioning.md`
- `public-docs/release-notes.md`
- `public-docs/api/public-api.md`
- `public-docs/api/compatibility.md`
- `public-docs/guides/core-semantics.md`
- `public-docs/guides/json-schema.md`
- `public-docs/guides/json-editor-compatibility.md`
- `public-docs/guides/ef-core-projection.md`
- `public-docs/guides/power-bi-projection.md`
- `public-docs/guides/projection-capabilities.md`
- `public-docs/guides/system-text-json.md`
- `public-docs/guides/configuration.md`
- `public-docs/nuget/*.md`
- `public-docs/samples/*.md`
- `public-docs/diagnostics/*.md`

## Documentation Type Standards

`docs/engineering/package-documentation.md` defines the repository standard for:

- NuGet package descriptions;
- NuGet package README sources;
- usage guides;
- compatibility documentation;
- release notes;
- specs.

## Synchronization Rules

- Public behavior and installation guidance must be updated in both `README.md` and `public-docs/` when consumer-facing behavior changes.
- `README.md` remains the repository entry page; `public-docs/` remains the detailed source set.
- Package README sources must follow `docs/engineering/package-documentation.md`.
- Usage guides must follow `docs/engineering/package-documentation.md`.
- Public API and compatibility statements must align with current shipped assemblies and actual repository validation practice.
- Diagnostics references must avoid claiming stable IDs unless stability is explicitly guaranteed.
- Public docs must distinguish semantic meaning from projection-specific representation.

## Package README Source Mapping

NuGet package README sources are mapped per package:

- `SemanticTypeModel.Abstractions` -> `public-docs/nuget/SemanticTypeModel.Abstractions.md`
- `SemanticTypeModel.Core` -> `public-docs/nuget/SemanticTypeModel.Core.md`
- `SemanticTypeModel.JsonSchema` -> `public-docs/nuget/SemanticTypeModel.JsonSchema.md`
- `SemanticTypeModel.DotNet` -> `public-docs/nuget/SemanticTypeModel.DotNet.md`
- `SemanticTypeModel.Generators` -> `public-docs/nuget/SemanticTypeModel.Generators.md`
- `SemanticTypeModel.DependencyInjection` -> `public-docs/nuget/SemanticTypeModel.DependencyInjection.md`
- `SemanticTypeModel.Configuration` -> `public-docs/nuget/SemanticTypeModel.Configuration.md`
- `SemanticTypeModel.Configuration.Generators` -> `public-docs/nuget/SemanticTypeModel.Configuration.Generators.md`
- `SemanticTypeModel.PowerBI` -> `public-docs/nuget/SemanticTypeModel.PowerBI.md`
- `SemanticTypeModel.EFCore` -> `public-docs/nuget/SemanticTypeModel.EFCore.md`
- `SemanticTypeModel.SystemTextJson` -> `public-docs/nuget/SemanticTypeModel.SystemTextJson.md`

Package README-related release validation is performed by `./eng/public-docs.sh`.

## Diagnostics Reference Mapping

- Diagnostics index: `public-docs/diagnostics.md`.
- Detailed diagnostics pages: `public-docs/diagnostics/*.md`.
- Current status for diagnostics compatibility: preview/unstable unless declared stable in `public-docs/api/compatibility.md`.

## Sample Documentation Mapping

- Samples index: `public-docs/samples.md`.
- Per-sample pages under `public-docs/samples/` map to runnable projects under `samples/`.

## Release Notes Obligations

For each release candidate and release:
- update `public-docs/release-notes.md`;
- verify `README.md` and `public-docs/getting-started.md` are current;
- verify per-package README sources;
- verify compatibility and diagnostics pages;
- verify sample documentation links and command snippets.
