# Guide Sync Hint: M0043 Usage Guide Supported Items and Policy Precision

## Status

Pending until M0043 implementation is complete.

## Purpose

Track follow-up documentation synchronization after usage guides are expanded with concrete options, policies, supported items, diagnostics, and capability matrices.

## Areas to Check

```text
docs/engineering/package-documentation.md
docs/MILESTONES.md
public-docs/guides/configuration.md
public-docs/guides/core-semantics.md
public-docs/guides/json-schema.md
public-docs/guides/json-editor-compatibility.md
public-docs/guides/ef-core-projection.md
public-docs/guides/power-bi-projection.md
public-docs/guides/projection-capabilities.md
public-docs/guides/system-text-json.md
public-docs/packages.md
public-docs/samples.md
public-docs/release-notes.md
public-docs/nuget/*.md
```

## Required Guide Precision Checks

Every usage guide should have:

```text
concrete options/policies table
supported-items table when relevant
one option-changing example when meaningful options exist
diagnostics cause/fix table
guide-specific common mistakes
guide-specific limitations
```

## High-Priority Guide Requirements

### Configuration

Must include the Cold Storage scenario end to end or explicitly mark it as planned if not implemented.

### Projection Capabilities

Must include a matrix across JSON Schema, EF Core, Power BI, System.Text.Json, and Configuration.

### Core Semantics

Must include a vocabulary inventory covering core semantic primitives and projection effects.

## Search Terms

```text
Options and policies\n\n[A-Z][^.]* can describe
Configure projection policy for
Capability handling is target-specific
Configuration annotations can describe
Use UI export options
Abstractions.Canonical
Canonical.TypeSchemaModel
TypeShape
ObjectShape
PropertyShape
ShapeRef
PublicAPI.Shipped
PublicAPI.Unshipped
public-api.sh
```

## Validation Hints

```sh
./eng/public-docs.sh
./eng/samples.sh
```

If `./eng/samples.sh` cannot run, record why and manually verify snippets changed by M0043.
