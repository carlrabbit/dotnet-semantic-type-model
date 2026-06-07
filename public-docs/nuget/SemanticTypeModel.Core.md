# SemanticTypeModel.Core

`SemanticTypeModel.Core` contains canonical model contracts, core semantic vocabulary support, transformation infrastructure, diagnostics, and inspection helpers.

```sh
dotnet add package SemanticTypeModel.Core --version 2.0.0
```

## Core semantics

2.0.0 defines a projection-neutral core semantic vocabulary. Use core semantics for domain meaning that should be available to JSON Schema, EF Core, Power BI, System.Text.Json, diagnostics, queries, and inspection.

Envelope semantics are part of the core vocabulary:

- `Envelope` marks a wrapper boundary.
- `EnvelopePayload` marks the distinguished payload.
- `EnvelopeMetadata` marks lifecycle/context metadata on the wrapper.

See `public-docs/guides/core-semantics.md`.
