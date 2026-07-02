# Guide Sync Hint: M0047 Audience-Specific Descriptions

## Status

Pending until M0047 implementation is complete.

## Purpose

Track deferred comprehensive documentation synchronization and 2.4.0 release preparation after the breaking audience-specific description implementation.

This file is synchronization metadata, not behavioral authority, and is not required reading for ordinary implementation agents.

## Areas to Reconcile

```text
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
all description-related specs
public-docs/guides/*.md
public-docs/nuget/*.md
public-docs/api/public-api.md
public-docs/api/compatibility.md
public-docs/diagnostics.md
public-docs/diagnostics/*.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/release-notes.md
```

## Required Synchronization Topics

```text
TechnicalDescription
UserDescription
removal of generic Description
removal of SemanticDescriptionAttribute
new technical/user description attributes
XML summary as automatic technical fallback
removal of IncludeXmlDocumentation
replacement of RequireXmlDocumentation with RequireTechnicalDescription
JSON Schema user-description default
optional JSON Schema technical extension
EF Core table/column comments
Power BI user-description default
Configuration audience-specific output
query and inspection output
Order Fulfillment cross-projection example
breaking migration to 2.4.0
```

## Release Hint

Plan a later documentation-sync and release-readiness milestone for `2.4.0`.

It should package and validate the complete affected package set, run shared samples, public-doc checks, package smoke, compatibility review, and the repository release gate. It must stop before publication unless explicitly authorized.

## Stale Pattern Search

```text
SemanticDescriptionAttribute
SemanticDescription(
IncludeXmlDocumentation
SemanticTypeModelIncludeXmlDocumentation
RequireXmlDocumentation
XML summary maps directly to schema.description
generic canonical Description
technical text used as user-facing fallback
```

## Validation Hints

```sh
./eng/check.sh
./eng/package.sh 0.0.0-m0047
./eng/package-smoke.sh 0.0.0-m0047
./eng/samples.sh
./eng/public-docs.sh
```
