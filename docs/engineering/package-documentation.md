# Package Documentation

## Purpose

Define consumer documentation for the tightly coupled SemanticTypeModel package suite.

## Shared package README

All packable `SemanticTypeModel.*` projects use one source:

```text
public-docs/nuget/SemanticTypeModel.md
```

It is packed as `README.md` in every NuGet package.

Do not maintain package-specific README files while package versions and usage remain suite-coupled.

The shared README must contain:

- the suite version-alignment rule;
- scenario-to-package selection;
- a minimal generated-model flow;
- links to usage, configuration, troubleshooting, diagnostics, and target guides;
- a package-role reference table;
- important suite-wide non-goals/compatibility boundaries.

## Version rule

Every `SemanticTypeModel.*` package used in one consumer application must use the same exact version.
Documentation must not recommend mixed suite versions.

Prefer version-neutral evergreen install snippets. Historical versions belong in release notes and migration
context.

## Public guide standard

Guides are task-oriented. A guide should answer, in the order most useful to a consumer:

```text
Use
Configure
Diagnose
Reference
```

Do not force filler sections. Include conceptual explanation only when it helps a consumer make a usage,
configuration, or diagnostic decision.

### Use

Provide a minimal real consumer path and one realistic composition example where needed.

### Configure

Document actual public options/policies with:

- option/API name;
- default;
- allowed values/supported shapes;
- effect;
- unsupported/error behavior.

Do not require consumers to read implementation specs to discover public configuration.

### Diagnose

For public diagnostics/failures provide:

- what happened;
- likely cause;
- concrete fix;
- related configuration when applicable.

### Reference

Use concise tables for package roles, supported shapes, limitations, and links to authoritative detail.

## Generator completeness rule

Every public source-generator/library configuration option must be discoverable from
`public-docs/configuration.md`.

Every public generator diagnostic must be discoverable from `public-docs/diagnostics.md` or a diagnostic
range page.

## Samples

Do not create per-sample README/Markdown documentation. The executable project is the detailed sample.
Maintain only `public-docs/samples.md` as an index.

## Release boundary

Evergreen guides describe current behavior. Move release-specific corrections, milestone narration, and
upgrade chronology to `public-docs/release-notes.md` or `public-docs/api/compatibility.md`.

## Validation

```sh
./eng/public-docs.sh
```
