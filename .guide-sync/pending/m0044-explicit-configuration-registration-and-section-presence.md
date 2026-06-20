# Guide Sync Hint: M0044 Explicit Configuration Registration and Section Presence

## Status

Pending until M0044 implementation is complete.

## Purpose

Track deferred documentation synchronization for explicit per-type options registration, selected-type derivation, required section presence, and generated helper delegation.

## Areas to Check

```text
docs/TERMINOLOGY.md
docs/SPECS.md
docs/DECISIONS.md
docs/MILESTONES.md
docs/specs/configuration-domain-model-and-options-projection.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-compile-time-generator.md
public-docs/guides/configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.md
public-docs/nuget/SemanticTypeModel.Configuration.Generators.md
public-docs/packages.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
README.md
```

## Required Public Documentation Topics

```text
explicit AddSemanticOptions<TOptions> registration
selected-type derivation
multiple selected types from one complete model
proof that unselected types remain unregistered
ConfigurationSectionPresence.Optional
ConfigurationSectionPresence.Required
required section plus ValidateOnStart
named options
call-site section override
generated helper delegation to runtime registration
registration-time versus options-validation failures
```

## Stale or Risky Patterns to Search

```text
AddSemanticConfigurationOptions(configurationModel)
automatically registers every configuration type
exclude configuration types
skip configuration types
empty section is always valid
required section is core semantics
generated helper reimplements validation
```

## Validation Hints

```sh
./eng/public-docs.sh
./eng/samples.sh
./eng/package-smoke.sh 0.0.0-m0044
```
