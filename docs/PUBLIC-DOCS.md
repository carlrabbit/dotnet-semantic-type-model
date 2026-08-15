# Public Documentation

## Purpose

Define the supported consumer-documentation surface and its synchronization rules.

Public docs are organized around user tasks, not repository implementation history.

## Consumer contract

For every meaningful public capability, documentation must make these answers discoverable:

1. **Use** — how to invoke it in a real consumer project.
2. **Configure** — available options, defaults, allowed values, and customization points.
3. **Diagnose** — expected diagnostics/failures and concrete fixes.

## Entry points

| Surface | Purpose |
|---|---|
| `README.md` | Product landing page and fast routing |
| `public-docs/usage.md` | First complete code-first flow and package selection |
| `public-docs/configuration.md` | Library/source-generator configuration reference |
| `public-docs/troubleshooting.md` | Symptom-oriented failure recovery |
| `public-docs/diagnostics.md` | Diagnostic ranges, stability, and common fixes |
| `public-docs/guides/*.md` | Target-specific use/configuration/diagnostics |
| `public-docs/samples.md` | Compact index into executable `samples/` projects |
| `public-docs/nuget/SemanticTypeModel.md` | Shared README packed into every SemanticTypeModel NuGet package |
| `public-docs/api/compatibility.md` | Current compatibility and migration boundaries |
| `public-docs/versioning.md` | Suite version-alignment policy |
| `public-docs/release-notes.md` | Version-specific chronology and migration notes |

## Guide inventory

- `public-docs/guides/core-semantics.md`
- `public-docs/guides/json-schema.md`
- `public-docs/guides/json-editor-compatibility.md`
- `public-docs/guides/ef-core.md`
- `public-docs/guides/power-bi.md`
- `public-docs/guides/system-text-json.md`
- `public-docs/guides/configuration-options.md`
- `public-docs/guides/projection-capabilities.md`

## One shared NuGet README

Every packable `SemanticTypeModel.*` project must pack:

```text
public-docs/nuget/SemanticTypeModel.md -> README.md
```

Do not create package-specific NuGet README sources while the package suite remains tightly coupled.
Package-specific roles belong in the shared README's package table.

## Version alignment

`SemanticTypeModel.*` packages form one aligned suite.

- Consumer guidance must tell users to use the same exact version for all SemanticTypeModel packages.
- Evergreen docs should avoid hardcoded package versions unless a version is required for the example.
- If active docs contain explicit SemanticTypeModel install/reference versions, they must resolve to one exact suite version.
- Historical versions are allowed in `public-docs/release-notes.md` and compatibility/migration context.
- Do not infer publication truth from source, milestones, or release-preparation text.

## Samples

Detailed sample documentation lives in executable source under `samples/`, not duplicated Markdown pages.
`public-docs/samples.md` is only an index/routing surface.

## Release-history boundary

Evergreen usage/configuration/guides must describe current behavior without milestone or release-candidate
narration. Version-specific corrections and upgrade history belong in release notes or compatibility docs.

## Synchronization

When consumer-visible behavior changes, update the smallest existing public authority that answers the user
task. Do not add another page just because a feature is new.

Run:

```sh
./eng/public-docs.sh
```
