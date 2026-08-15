# Compatibility

This page describes current consumer compatibility boundaries. Version-by-version chronology belongs in
[Release notes](../release-notes.md).

## Package suite

All `SemanticTypeModel.*` packages used together must use the same exact version. Mixed suite versions are
unsupported, including generator/analyzer packages.

## Canonical model authoring

Annotated .NET code is the supported public authoring source for canonical semantic models. Generated providers
return the current `SemanticTypeModel.Abstractions.Model.TypeSchemaModel` surface.

The old `Canonical` namespace/legacy shape graph is not a supported current model surface. JSON Schema import,
where retained for compatibility/tooling, is not the recommended canonical authoring path.

## Public API review

The repository reviews compatibility through package smoke tests, runnable samples, public documentation,
release notes, compatibility documentation, and human review. It does not currently use committed text API
baseline files as the sole compatibility gate.

## Diagnostics

Diagnostic IDs and stability are documented in [Diagnostics](../diagnostics.md) and their range pages. Do not
depend on exact diagnostic message text as a compatibility contract.

## Audience-specific descriptions

`UserDescription` and `TechnicalDescription` are separate contracts. User-facing targets do not silently fall
back to technical text, and technical targets do not silently substitute user text. Existing generic-description
content from older APIs requires intentional classification when migrating.

## System.Text.Json

SemanticTypeModel does not generate `JsonSerializerContext`. Applications own contexts/resolvers and may wrap
them with SemanticTypeModel resolver customization. Removed generated-context switches are not a supported
current path.

## Configuration / Options

Application registration is explicit per options type through `AddSemanticOptions<TOptions>`. A complete
semantic model may contain multiple Configuration types without registering all of them automatically.

## EF Core

The supported static application path is generated configuration through
`SemanticTypeModel.EFCore.Generators`:

- the model assembly emits a semantic manifest;
- the persistence assembly explicitly selects model(s);
- the generator emits ordinary `IEntityTypeConfiguration<TEntity>` implementations and a deterministic apply
  extension;
- generated configuration owns only semantic CLR Entities selected from that model;
- unrelated/manual application entities remain application-owned.

The retired runtime global `ModelBuilder` cleanup/application path is not the current application contract.
There is no compatibility bridge that reintroduces broad relationship inference, `OwnsOne`/`OwnsMany`, or
alternative inheritance modes into the current generated contract.

## Migration history

For exact version-specific additions/removals and upgrade notes, use [Release notes](../release-notes.md).
