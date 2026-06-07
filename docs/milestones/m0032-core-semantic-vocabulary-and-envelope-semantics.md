# M0032: Core Semantic Vocabulary and Envelope Semantics

## Status

Implemented for 2.0.0.

## Completed Outcomes

- `docs/specs/core-semantic-vocabulary.md` is the authoritative vocabulary reference for projection-neutral core semantics.
- Envelope semantics are defined as `Envelope`, `EnvelopePayload`, and `EnvelopeMetadata`, with projection-root policy concepts for envelope-as-root, payload-as-root, embedded, referenced, serialized, and opaque payloads.
- `docs/specs/type-model-dotnet-attributes.md` includes the envelope attribute vocabulary for code-first authoring.
- Public docs explain when to use core semantics instead of target-specific metadata.

## Documentation Synchronization

Direct documentation impact for this milestone has been synchronized into the relevant index, public guide, package README source, and release-note files for 2.0.0. Any future behavior changes must update the authoritative specs first and then synchronize the public documentation surfaces listed in `docs/PUBLIC-DOCS.md`.
