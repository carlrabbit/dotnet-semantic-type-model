# Current Canonical Model Surface Spec

## Status

Authoritative behavioral specification.

## Purpose

Define the supported canonical model surface after removal of old compatibility APIs and transition-era terminology.

This spec is authoritative for:

- supported canonical model inputs to runtime, transformation, query, inspection, and projection APIs;
- removal of old model compatibility adapters;
- terminology for current model surfaces;
- active documentation and source vocabulary rules;
- validation expectations for removing old compatibility behavior.

## Supported Model Surface

The supported runtime model surface is the current canonical semantic type model produced from code-first extraction, compile-time generation, or a persisted snapshot of a code-generated model.

Supported model sources:

- annotated .NET code extracted by `SemanticTypeModel.DotNet`;
- compile-time generated model providers from `SemanticTypeModel.Generators`;
- persisted snapshots that preserve a code-generated canonical model;
- programmatic construction of the current canonical model surface when used by tests or advanced consumers.

Unsupported model sources:

- old model shape APIs used as a canonical authoring surface;
- JSON Schema import as a canonical authoring source;
- adapter-based conversion from old model shapes into the current runtime surface;
- public APIs that accept obsolete model shapes for compatibility.

## API Rules

Runtime, transformation, query, inspection, and projection APIs must accept the current canonical semantic model surface or a package-owned domain semantic model.

APIs must not expose overloads whose primary purpose is compatibility with old model shapes.

Convenience APIs may accept higher-level current inputs, such as generated providers or extraction results, but they must normalize into the current canonical model surface before transformation or projection.

## Removal Rules

Remove these categories when present:

- old model adapter classes;
- old model provider wrappers;
- DI registration overloads for old model instances or factories;
- query and inspection overloads for old model shapes;
- domain model conversion helpers to or from old model definitions;
- JSON Schema import APIs when used to create canonical models;
- tests and samples that manually construct old model shapes to exercise current behavior.

Do not replace removed compatibility APIs with hidden wrappers, internal aliases, or new adapters.

## JSON Schema Import

JSON Schema export remains a supported projection target.

JSON Schema import is not a supported canonical model creation path.

If implementation keeps parsing utilities for test fixtures, those utilities must not be packaged or documented as consumer APIs.

## Historical Documentation

Historical milestone files, decision records, and release notes may retain older terms when they describe past work.

Active authority and public documentation must not present old model surfaces as supported current behavior.

## Terminology Rules

Use these terms for current behavior:

| Use | Meaning |
|---|---|
| `canonical semantic model` | Projection-neutral model used by current runtime, transformation, query, and projection APIs. |
| `runtime canonical semantic model` | Canonical model instance used by runtime services. |
| `domain semantic model` | Package-owned projection model derived from the canonical semantic model. |
| `transformation` | Deterministic derivation, normalization, validation, or enrichment step. |
| `projection` | Target-specific derivation from canonical semantics into a domain model or output. |

Do not use these terms for current behavior:

| Avoid | Replacement |
|---|---|
| `hardened model` | `canonical semantic model` |
| `hardened runtime model` | `runtime canonical semantic model` |
| `hardening` | `stabilization`, `validation`, `cleanup`, or a feature-specific term |
| `legacy model` | Remove the concept or describe only as historical behavior |
| `legacy adapter` | Remove the concept |

## Diagnostics

When old model compatibility removal invalidates user code, diagnostics or compile-time failures should point users to supported code-first extraction, generated providers, or persisted snapshots.

Diagnostics must not imply that old model compatibility can be re-enabled.

## Validation Expectations

Implementation must validate:

- no active package source exposes old model compatibility APIs;
- no active sample constructs old model shapes as the main model source;
- no active package documentation describes JSON Schema import as a supported canonical model source;
- current query and inspection tests use the current canonical model surface;
- domain projection tests use domain semantic models or current canonical models.

## Invariants

- The current canonical semantic model is the only supported runtime and projection input model surface.
- JSON Schema remains an export/projection target, not a canonical authoring source.
- Domain packages own domain semantic models; they must not rely on old model shapes as their primary behavior model.
- Historical documentation may preserve historical terminology; active authority and public docs must use current terminology.
