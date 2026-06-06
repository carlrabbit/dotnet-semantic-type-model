# Decision: Power BI Integration Stops at Semantic Model Derivation and Local Metadata Projection

## Status

Accepted for M0031.

## Context

`SemanticTypeModel.PowerBI` is a domain package in the code-first SemanticTypeModel architecture.

The package should create useful analytical metadata from code-derived semantic models while avoiding scope expansion into Power BI deployment tooling, service orchestration, PBIX generation, or full Tabular Object Model automation.

Power BI and Analysis Services Tabular contain a large surface area: service publishing, workspaces, authentication, datasets, XMLA, refresh, partitions, roles, calculation groups, perspectives, translations, TOM scripting, PBIX files, and deployment pipelines. SemanticTypeModel should only own the semantic metadata derivation portion.

## Decision

`SemanticTypeModel.PowerBI` derives a `PowerBiSemanticModel` from the canonical semantic model and emits deterministic local metadata output.

The Power BI package owns:

```text
Power BI domain semantic model derivation
local deterministic metadata export
tables/columns/relationships
explicit measures
explicit calculated tables
display folders
hidden/visible flags
data categories
summarization hints
format strings
sort-by-column metadata
basic explicit hierarchies where modeled
diagnostics and inspection for supported analytical metadata
```

The Power BI package does not own:

```text
Power BI Service publishing
workspace management
dataset deployment
authentication
gateway configuration
refresh scheduling
incremental refresh configuration
PBIX generation
Power BI REST API orchestration
Fabric integration
deployment pipelines
XMLA endpoint operations
query execution
credentials/secrets handling
full TOM parity
```

## Rationale

- SemanticTypeModel is a semantic metadata framework, not a Power BI deployment framework.
- Local deterministic metadata output is testable, reviewable, and suitable for source control.
- Users own DAX, calculated tables, deployment, refresh, credentials, workspaces, and operational Power BI lifecycle.
- Supporting measures and calculated tables makes the projection useful without turning the package into a DAX authoring framework.
- Avoiding full TOM parity prevents scope creep into Tabular Editor / TOM automation replacement territory.

## Consequences

- Power BI support should be broad enough for common analytical metadata, including explicit measures and calculated tables.
- DAX expressions are preserved, not parsed or validated.
- Users can extend derivation through options, model configuration hooks, custom transformations, and custom annotations.
- Tests must not require Power BI Desktop, Power BI Service, XMLA, credentials, a workspace, network access, or PBIX files.
- Public docs and package README content must not imply publishing, deployment, PBIX generation, refresh scheduling, XMLA operations, or full TOM parity.
- Future Power BI work must stay within semantic model derivation and deterministic local metadata output unless a later accepted decision changes the boundary.

## Alternatives Considered

### Minimal Tables/Columns Only

Rejected because Power BI without measures and common analytical metadata is not useful enough for real consumers.

### Full TOM Parity

Rejected because it would make the package a TOM/Tabular Editor replacement and absorb operational/deployment concerns outside the semantic model.

### Power BI Service Publishing

Rejected because publishing, authentication, workspace management, refresh, and gateway concerns belong to user deployment tooling.

### DAX Authoring Framework

Rejected for M0031. The package preserves explicit DAX expressions and metadata, but does not parse, validate, generate, or optimize DAX.
