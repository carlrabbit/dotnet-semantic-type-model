# M0031: Power BI Domain Semantic Model and Local Metadata Projection

## Status

Implemented for 2.0.0.

## Completed Outcomes

- Power BI integration is bounded to `PowerBiSemanticModel` derivation and deterministic local metadata output.
- The durable Power BI boundary decision is recorded in `docs/decisions/power-bi-integration-stops-at-local-metadata-projection.md`.
- `docs/specs/type-model-powerbi-tom-projection.md` is the authoritative behavior spec for tables, columns, relationships, measures, calculated tables, analytical metadata, extension points, diagnostics, inspection, and local export.
- Public Power BI docs and package README source describe supported 2.0.0 behavior without implying Power BI Service publishing, PBIX generation, workspace management, authentication, refresh scheduling, XMLA operations, or full TOM parity.

## Documentation Synchronization

Direct documentation impact for this milestone has been synchronized into the relevant index, public guide, package README source, and release-note files for 2.0.0. Any future behavior changes must update the authoritative specs first and then synchronize the public documentation surfaces listed in `docs/PUBLIC-DOCS.md`.
