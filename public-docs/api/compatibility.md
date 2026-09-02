# Compatibility

This page describes current consumer compatibility boundaries. Version-by-version chronology belongs in
[Release notes](../release-notes.md).

## Package suite

All `SemanticTypeModel.*` packages used together must use the same exact version. Mixed suite versions are
unsupported, including generator/analyzer packages.

The compile-time semantic manifest is ephemeral internal build transport. A consuming generator must use the
same exact SemanticTypeModel suite version as the manifest producer; cross-version manifest consumption is not
supported. The current manifest schema is v3; it carries only the current canonical type/property contract and is
not a persisted interchange format. There is no cross-version negotiation.

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
logicalType
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

Plain `JsonSerializerOptions.AddSemanticTypeModelJson(...)` is the complete runtime integration path and does
not require `JsonSerializerContext`. It supports automatic semantic Entity polymorphism;
explicit application-owned polymorphism contracts remain authoritative. Register models before first serializer
use because normal System.Text.Json options freeze after metadata materialization.

## Configuration role and Options boundary

`SemanticTypeRole.Configuration` remains projection-neutral semantic meaning, and `SemanticRequiredWhen`
remains independently supported. STM does not own application configuration binding, Options registration,
named options, or startup validation.

`SemanticTypeModel.Configuration` and its former authoring/runtime integration were removed in the 5.0 major
boundary. There is no compatibility or tombstone package. Applications that need configuration use
Microsoft.Extensions.Configuration and Microsoft.Extensions.Options directly.

## Display Identity and Access Path

`SemanticDisplayIdentity` and `SemanticAccessPath` are projection-neutral annotation semantics. They do not
imply EF indexes, API query parameters, UI behavior, Power BI behavior, or relationships.

## CLR wrapper and Logical Type boundary

SemanticTypeModel assigns no special semantic or target-specific meaning to CLR single-value wrapper shape.
A `Value` property plus matching constructor does not make a CLR type an STM scalar, identifier, automatically
EF-convertible value, or primitive serialization contract. Strong Scalar canonical semantics,
`[SemanticStrongScalar]`, `SemanticLiteralKind.StrongIdentifier`, and EF Core's automatic wrapper conversion are
removed in the 6.0 boundary.

Projection-neutral scalar identity is expressed explicitly on an ordinary scalar property with
`[SemanticLogicalType("Name")]`. Logical Type is model-local, case-sensitive metadata; same-name uses in one
model must point to the same scalar type. It does not create a canonical type node or change CLR, JSON Schema,
System.Text.Json, EF Core, LINQ, Power BI, or built-in TestData representation.

The supported JSON fidelity claim is bounded and one-way: STM-configured System.Text.Json output validates
against the derived JSON Schema. Bidirectional serializer/schema equivalence and representation-changing
custom contracts are outside the guarantee.

The fidelity baseline follows native System.Text.Json scalar representation. JSON Schema leaves `Time`,
offset-ambiguous `DateTime`, and `Duration` without inferred standard formats, describes Binary as a Base64
string with `contentEncoding: base64`, and treats Json as unconstrained JSON. `System.Uri` defaults to
`uri-reference`; explicit member format metadata can request the stronger `uri` validation constraint.

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
There is no compatibility bridge that reintroduces broad relationship inference, `OwnsOne`/`OwnsMany`,
alternative inheritance modes, or automatic single-value-wrapper conversion into the current generated contract.

## TestData

`SemanticTypeModel.TestData` is the eleventh aligned suite package. It consumes the canonical model as a runtime
capability and does not mutate canonical semantics or introduce a target projection dependency.

Random generation is deterministic and constraint-aware. Optional Semantic Terminology Profiles are model-bound
versioned sidecars whose synthetic candidates are validated against current supported semantics before use.
Profile-guided precedence is property terminology, then Logical Type terminology, then built-in Random fallback.
Programmatic property and Logical-Type scalar generators take precedence over terminology and fail closed when an
explicit supplied value is invalid. Built-in regex synthesis and arbitrary custom-constraint interpretation are
not compatibility promises.

Typed materialization supports the documented public CLR scalar/object/collection/dictionary shapes without
private-member mutation, uninitialized-object construction, or wrapper inference. Expected generation and
materialization failures use TestData diagnostics/exception boundaries rather than leaking target-package
behavior.

## 6.0 release boundary

`6.0.0` is the intended next stable version and current release candidate after the breaking semantic changes
above. The aligned suite contains exactly eleven packages, including `SemanticTypeModel.TestData`.

Consumers upgrading from 5.x must:

1. use exactly `6.0.0` for every `SemanticTypeModel.*` package, generator, and analyzer used together;
2. remove Strong Scalar/StrongIdentifier usage and any assumptions of automatic CLR wrapper scalar conversion;
3. use explicit property-level Logical Type metadata where representation-neutral scalar identity is needed;
4. rebuild model producers and consuming generators together because manifests require exact suite-version
   alignment;
5. keep strongly typed-ID conversions and other target-specific wrapper behavior in application/target-native
   configuration.

The release is not publication truth until the package channel confirms `6.0.0` publication.

## 5.0 release boundary

5.0.0 is the next major boundary after 4.0.1. Consumers moving from 4.0.x must remove the
`SemanticTypeModel.Configuration` package and former STM Configuration/Options authoring/runtime APIs, then
use application-owned Microsoft.Extensions.Configuration/Options registration as needed. Keep
`SemanticTypeRole.Configuration` and `SemanticRequiredWhen` when their projection-neutral meanings remain
relevant. Use the aligned ten-package 5.0.0 suite; do not mix 4.0.x and 5.0.0 packages.

## 5.0.1 release history

5.0.1 is a patch-line release candidate for the aligned ten-package suite. It carries the corrective
System.Text.Json runtime behavior described in the release notes: ordinary `JsonSerializerOptions` is the
primary path, semantic Entity inheritance can receive automatic `$type` polymorphism when no explicit
application contract is supplied, explicit application contracts win, and multiple semantic models compose
through runtime options. Automatic polymorphism remains outside the JSON
Schema/System.TextJson fidelity baseline. Publication truth must be verified from the package channel;
repository source does not establish publication.

## 4.0 release boundary

4.0.0 established the major compatibility boundary for the accumulated breaking changes described above.
4.0.1 is a patch-compatible EF Core nullability correction and does not introduce a new public API, semantic
nullability contract, relationship model, or JSON storage policy.

Consumers moving from pre-4.0 releases should, where applicable:

1. align every SemanticTypeModel package/analyzer to exactly the same 4.0.x version;
2. migrate EF application to `SemanticTypeModel.EFCore.Generators` and explicit selected-model configuration;
3. remove references to the former Configuration/Options integration;
4. remove JSON Schema import usage;
5. replace relationship attributes/inference with target-owned relationship configuration;
6. replace assumptions about CLR access mutability with explicit optional `[SemanticMutable]` /
   `[SemanticImmutable]` semantics when lifecycle mutability matters;
7. replace JSON Editor compatibility options with JSON Schema `x-stm`/open `ui.*` metadata where needed.

Consumers already on 4.0.0 can move to 4.0.1 without application configuration changes. Update every
`SemanticTypeModel.*` runtime, projection, generator, and analyzer package together so the suite remains exactly
version-aligned.

## Migration history

For exact version-specific additions/removals and upgrade notes, use [Release notes](../release-notes.md).
