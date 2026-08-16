# Compatibility

This page describes current consumer compatibility boundaries. Version-by-version chronology belongs in
[Release notes](../release-notes.md).

## Package suite

All `SemanticTypeModel.*` packages used together must use the same exact version. Mixed suite versions are
unsupported, including generator/analyzer packages.

The compile-time semantic manifest is ephemeral internal build transport. A consuming generator must use the
same exact SemanticTypeModel suite version as the manifest producer; cross-version manifest consumption is not
supported.

## Canonical model authoring

Annotated .NET code is the supported public authoring source for canonical semantic models. Generated providers
return the current `SemanticTypeModel.Abstractions.Model.TypeSchemaModel` surface.

The old `Canonical` namespace/legacy shape graph is not a supported current model surface. JSON Schema import
has been removed and is not a supported canonical authoring path.

## Lifecycle mutability

Canonical lifecycle mutability is optional.

```text
SemanticMutability:
  Mutable
  Immutable
```

Object/type and property declarations are nullable. No declaration means mutability is not part of the semantic
contract. A property declaration overrides its containing object's declaration in either direction, including a
mutable property in an immutable object.

Old canonical mutability states that described CLR access shape (`InitOnly`, `ReadOnly`, `WriteOnly`) are not
part of the current semantic mutability contract. CLR setter, `init`, record, and `readonly` shape do not infer
lifecycle mutability.

Code-first declarations use `[SemanticMutable]` and `[SemanticImmutable]`.

## Relationships

SemanticTypeModel no longer exposes a general canonical relationship model.

Removed current paths include the former relationship definition/cardinality/delete-behavior model,
`SemanticRelationshipAttribute`, and relationship inference.

Object references, collections, keys, ownership, and aggregate-root semantics remain supported concepts but do
not implicitly create a general semantic relationship.

Applications and target projections configure target-specific relationships through their native APIs/policies.

## JSON Schema

JSON Schema is an export/projection target, not a canonical authoring source.

The JSON Schema projection can preserve selected STM-only semantics in one optional `x-stm` object. The initial
extension vocabulary is:

```text
role
aggregateRoot
mutability
technicalDescription
keys
unit
ui
```

Standard JSON Schema remains authoritative for semantics it represents natively. `x-stm` is not a serialized
`TypeSchemaModel` and has no compatibility-negotiation/version protocol.

`UserDescription` maps to standard JSON Schema `description`. `TechnicalDescription` remains independent and can
be preserved as `x-stm.technicalDescription`.

`ui.*` annotations are open JSON-compatible pass-through metadata under `x-stm.ui`; JSON Editor compatibility
modes, widget inference, and a closed editor vocabulary are not supported current APIs.

## Public API validation

The repository validates compatibility through automated repository tests, package smoke tests, runnable
samples, public-documentation validation, compatibility documentation, and release validation. It does not rely
on committed text API baseline files as the sole compatibility gate.

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

`SemanticTypeModel.Configuration.Generators` has been removed; there is no compatibility/tombstone package.

## EF Core

The supported static application path is generated configuration through
`SemanticTypeModel.EFCore.Generators`:

- the model assembly emits an ephemeral internal semantic manifest containing its producer suite version;
- the persistence assembly explicitly selects model(s), and its EF generator must use the same exact suite
  version as the manifest producer;
- the generator emits ordinary `IEntityTypeConfiguration<TEntity>` implementations and a deterministic apply
  extension;
- generated configuration owns only semantic CLR Entities selected from that model;
- unrelated/manual application entities remain application-owned;
- application relationship configuration remains application/EF-owned.

The retired runtime global `ModelBuilder` cleanup/application path is not the current application contract.
There is no compatibility bridge that reintroduces broad relationship inference, `OwnsOne`/`OwnsMany`, or
alternative inheritance modes into the current generated contract.

## 4.0.0 migration boundary

4.0.0 is prepared as a major compatibility boundary for the accumulated breaking changes described above.

Consumers moving to 4.0.0 should, where applicable:

1. align every SemanticTypeModel package/analyzer to exactly `4.0.0`;
2. migrate EF application to `SemanticTypeModel.EFCore.Generators` and explicit selected-model configuration;
3. remove references to `SemanticTypeModel.Configuration.Generators`;
4. remove JSON Schema import usage;
5. replace relationship attributes/inference with target-owned relationship configuration;
6. replace assumptions about CLR access mutability with explicit optional `[SemanticMutable]` /
   `[SemanticImmutable]` semantics when lifecycle mutability matters;
7. replace JSON Editor compatibility options with JSON Schema `x-stm`/open `ui.*` metadata where needed.

Do not infer the previously published package version from repository release-candidate notes. Publication truth
must be verified from the actual package/release channel during release work.

## Migration history

For exact version-specific additions/removals and upgrade notes, use [Release notes](../release-notes.md).
