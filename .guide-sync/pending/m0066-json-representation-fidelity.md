# M0066 JSON Representation Fidelity — Deferred Documentation Sync

## Owning Milestone

`M0066 — JSON Representation Fidelity`

## Purpose

Synchronize consumer-facing JSON Schema and System.Text.Json guidance after M0066 implementation has satisfied the ready milestone.

This file is deferred documentation-sync metadata. Ordinary implementation agents are not required to read it.

## Surfaces to Reconcile

Review and update as needed:

```text
README.md
public-docs/guides/json-schema.md
public-docs/guides/system-text-json.md
public-docs/guides/projection-capabilities.md
public-docs/nuget/SemanticTypeModel.md
public-docs/samples.md
public-docs/diagnostics.md
public-docs/diagnostics/*
public-docs/release-notes.md   only during the appropriate release synchronization
```

Do not update release-status language unless publication truth is independently verified.

## Required Consumer Truth

Public guidance should make these points discoverable:

1. JSON Schema and System.Text.Json are sibling projections of the same canonical model.
2. The supported fidelity claim is one-way:
   - supported STJ output validates against the derived schema;
   - bidirectional serializer/schema equivalence is not promised.
3. Existing `ExistingJsonContract` remains the STJ default and has no blanket schema-fidelity guarantee.
4. The fidelity example/configuration uses canonical semantic property names plus compatible string-enum serialization and otherwise bounded serializer settings.
5. Semantic validation constraints are represented by JSON Schema; STJ is not a general validation framework.
6. JSON Schema `x-stm` now preserves the implemented approved semantic vocabulary, including Display Identity, Access Paths, lifecycle/evolution/ownership/envelope/extension-data/enum-value metadata.
7. `IncludeSemanticAnnotations=false` removes `x-stm` while preserving native JSON Schema semantics.
8. Semantic extension data maps to native additional-member behavior; the bag is not a normal schema property.
9. Representation-changing custom converters, representation-changing number handling, reference-preservation wire shapes, unmatched noncanonical members, and explicit STJ polymorphism are outside the initial tandem guarantee.
10. Explicit STJ polymorphism/discriminator fidelity remains unsupported until a structured contract exists.
11. Do not claim SemanticTypeModel ships a production JSON Schema validator unless a later milestone explicitly adds one.
12. Do not claim JSON Editor-specific behavior.

## Projection Capability Matrix

Update the public matrix to distinguish:

```text
native JSON Schema behavior
x-stm semantic preservation
STJ wire behavior
JSON Schema validation responsibility
unsupported/diagnosed fidelity cases
```

Add rows for Display Identity and Access Path if useful for consumer clarity.

Do not invent EF/API/UI behavior from Access Paths or Display Identity.

## Diagnostics

If implementation adds or changes public diagnostic codes, synchronize the appropriate diagnostic reference with:

- code;
- severity;
- cause;
- corrective action;
- whether the issue invalidates the JSON representation fidelity guarantee.

## Samples

If a tandem sample is added or an existing sample is extended, route it through `public-docs/samples.md`; do not create a per-sample Markdown page.

The sample should demonstrate serialization plus schema derivation and may demonstrate validation, but any validator used only for demonstration/testing must not be described as a SemanticTypeModel runtime dependency.
