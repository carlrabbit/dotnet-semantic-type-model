# M0025: Consumer-Facing Samples and Package-Based Sample Validation

## Status

Implemented.

## Maturity Mode

Public package quality correction.

## Task Mode

Documentation-synchronized completed milestone.

## Goal

Make repository samples useful to consumers.

The completed sample contract is:

```text
Public samples under samples/ demonstrate how a consumer uses SemanticTypeModel packages.

They are not internal source-generator harnesses, source-string compiler tests, or source-tree integration tests.

They consume locally prepared NuGet packages and validate the package experience.
```

## Authoritative Documents

- `docs/engineering/samples.md`
- `docs/decisions/consumer-facing-package-based-samples.md`
- `docs/ENGINEERING.md`
- `docs/engineering/command-contract.md`
- affected scenario specs under `docs/specs/`

## Completed Outcomes

- Public sample policy now requires consumer-facing package-based samples.
- Public samples use `PackageReference` instead of `ProjectReference` to `src/*`.
- Source-generator public samples use normal MSBuild/NuGet generator execution.
- Manual Roslyn `CSharpGeneratorDriver`, source-string compilation, in-memory assembly emission, and reflection-based generated-provider invocation are removed from the public sample model.
- Public samples are validated against locally prepared package artifacts.
- Public sample documentation lists only consumer-facing sample scenarios.
- The System.Text.Json sample follows the M0024 resolver-centered contract with a user-authored `JsonSerializerContext`.
- Sample docs describe scenario goals, packages used, run commands, expected output, demonstrated consumer pattern, and non-goals.

## Public Sample Set

The synchronized public sample set is:

| Sample | Purpose |
|---|---|
| `samples/json-schema-roundtrip` | Import, transform, validate, and export JSON Schema. |
| `samples/code-first-json-schema` | Annotated C# model, packaged generator, generated provider, and JSON Schema export. |
| `samples/code-first-ef-core` | Annotated C# model, packaged generator, generated provider, and EF Core projection metadata. |
| `samples/code-first-powerbi` | Annotated C# model, packaged generator, generated provider, and Power BI projection metadata. |
| `samples/system-text-json-resolver` | User-authored `JsonSerializerContext` customized by SemanticTypeModel resolver metadata. |
| `samples/runtime-di` | Dependency-injection registration and projection usage. |

## Direct Documentation Impact Status

Resolved by the M0025 implementation and documentation synchronization pass:

```text
docs/engineering/samples.md
public-docs/samples.md
public-docs/samples/*.md for affected public samples
sample code comments
```

## Deferred Documentation Impact Status

Resolved or intentionally not required:

```text
README.md sample section
docs/MILESTONES.md
docs/DECISIONS.md
public-docs/getting-started.md
public-docs/guides/*.md where directly affected
public-docs/release-notes.md
```

No follow-up documentation work remains for M0025 unless later code changes alter sample names, sample commands, or package validation behavior.

## Validation Guidance

Sample implementation work should complete with Tier 2 validation:

```sh
./eng/check.sh
```

Package-based sample validation requires local package preparation before running samples:

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

If package layout or package smoke behavior changes, use the Tier 3 commands from `docs/engineering/command-contract.md`.

## Non-Goals Preserved

- No non-root README files.
- No TBPs or issue templates.
- No workflow YAML changes as part of this documentation synchronization pass.
- No internal generator harnesses presented as public samples.
