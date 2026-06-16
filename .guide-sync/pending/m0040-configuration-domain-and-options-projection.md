# Guide Sync Hint: M0040 Configuration Domain and Options Projection

## Status

Pending until M0040 implementation is complete.

## Purpose

Track deferred documentation synchronization for the Configuration domain model, options registration projection, and core conditional constraints.

## Trigger

Run after M0040 implementation changes are merged or ready for release review.

## Areas to Check

```text
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/specs/core-semantic-vocabulary.md
docs/specs/core-conditional-constraint-semantics.md
docs/specs/configuration-domain-model-and-options-projection.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-compile-time-generator.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/system-text-json-contract-integration.md
docs/specs/diagnostics.md
docs/PUBLIC-DOCS.md
README.md
public-docs/**
public-docs/nuget/*.md
public-docs/samples/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

## Public Documentation Topics

Verify that public docs explain:

- Configuration package installation;
- how to mark an options type as configuration;
- how to specify a configuration section;
- how generated service-registration helpers are used;
- how `RequiredWhen` maps to Options validation;
- how JSON Schema handles `RequiredWhen`;
- what EF Core, Power BI, and System.Text.Json do by default with conditional constraints;
- what the Configuration package explicitly does not own.

## Stale or Risky Terms to Search

```text
configuration-only required when
options validation in core
ValidateOnStart as core semantic
appsettings generation as primary feature
configuration provider loading
secret management generated
arbitrary validation expression
```

## Validation Hints

Use documentation and release-readiness validation after synchronization:

```sh
./eng/public-docs.sh
./eng/samples.sh
./eng/public-api.sh
```

Run the full release candidate gate only if the synchronization pass is part of release readiness.
