# Guide Sync Hint: M0042 Package README and Usage Guide Rewrite

## Status

Pending until M0042 implementation is complete.

## Purpose

Track follow-up documentation synchronization after package README sources and usage guides are rewritten to the documentation standard.

## Areas to Check

```text
docs/engineering/package-documentation.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
README.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/packages.md
public-docs/samples.md
public-docs/nuget/*.md
public-docs/guides/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
public-docs/samples/*.md
eng/public-docs.sh
```

## Package README Required Sections

Each `public-docs/nuget/*.md` file should contain:

```text
What this package does
Install
Use when
Minimal example
Main APIs
Works with
Does not do
More documentation
```

## Usage Guide Required Sections

Each `public-docs/guides/*.md` file should contain:

```text
Goal
Prerequisites
Packages
Minimal path
Full example
How it works
Options and policies
Diagnostics
Common mistakes
Limitations
Related docs
```

## Stale or Risky Terms to Search

```text
Abstractions.Canonical
Canonical.TypeSchemaModel
TypeShape
ObjectShape
PropertyShape
ShapeRef
PublicAPI.Shipped
PublicAPI.Unshipped
public-api.sh
this guide explores
comprehensive framework
JSON Schema import is the canonical authoring path
```

## Validation Hints

After synchronization:

```sh
./eng/public-docs.sh
./eng/samples.sh
```

If snippets or sample commands changed, verify sample command snippets against runnable sample projects.
