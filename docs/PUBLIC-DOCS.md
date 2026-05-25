# Public Documentation

## Purpose

Define authoritative consumer-facing documentation surfaces and synchronization rules.

## Authority

This document is authoritative for:
- public documentation surfaces;
- source-of-truth mapping for package README content;
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
- `public-docs/nuget/package-readme.md`
- `public-docs/samples/*.md`
- `public-docs/diagnostics/*.md`

## Synchronization Rules

- Public behavior and installation guidance must be updated in both `README.md` and `public-docs/` when consumer-facing behavior changes.
- `README.md` remains the repository entry page; `public-docs/` remains the detailed source set.
- Public API and compatibility statements must align with current shipped assemblies.
- Diagnostics references must avoid claiming stable IDs unless stability is explicitly guaranteed.

## Package README Source Mapping

- NuGet package README source: `public-docs/nuget/package-readme.md`.
- Package README-related release validation is performed by `./eng/public-docs.sh`.

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
- verify package README source;
- verify API/compatibility and diagnostics pages;
- verify sample documentation links and command snippets.

## Document Contract

When public documentation policy changes, review and update:
- `README.md`
- `docs/ENGINEERING.md`
- `docs/engineering/public-documentation.md`
- `docs/workflows/public-docs.md`
- `eng/public-docs.sh`
