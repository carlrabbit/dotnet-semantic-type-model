# Decision: Consumer-Facing Package-Based Samples

## Status

Accepted for M0025.

## Context

SemanticTypeModel has public packages and public documentation. Samples are listed as executable documentation, but several existing samples are development-oriented:

```text
manual source-generator execution
source strings compiled with Roslyn
in-memory assembly emission
reflection over generated providers
direct ProjectReference usage against src/*
```

These patterns are useful for tests and internal development but do not show how consumers use the packages.

The repository also needs samples to catch package-level defects such as missing analyzer assets, missing transitive build assets, incorrect package dependencies, and behavior that only works through source project references.

## Decision

Public samples under `samples/` must be consumer-facing and package-based.

They must consume SemanticTypeModel packages through `PackageReference` and validate against locally prepared package artifacts during repository validation.

Generator samples must use normal MSBuild/NuGet source-generator execution. They must not manually instantiate or run Roslyn generator drivers.

Internal generator harnesses and source-string compilation examples must move to tests or tooling, or be removed from public sample status.

## Rationale

- Public samples should teach supported consumer workflows.
- Package-based samples validate the real NuGet experience.
- Source-project samples can hide missing package assets and dependency errors.
- Manual generator harnesses are valuable tests but misleading public examples.
- Keeping public samples close to user workflows reduces release risk.

## Consequences

- Existing samples using `ProjectReference` to `src/*` must be converted or removed from the public sample set.
- Existing source-string/generator-driver samples must be rewritten as normal consumer projects or moved to tests/tooling.
- `./eng/samples.sh` or an equivalent documented command must validate package-based samples.
- Sample documentation must describe consumer scenarios and package usage.
- Package preparation becomes part of sample validation when sample projects consume local artifacts.
- Public sample changes may require public-docs updates but do not require broad documentation synchronization in every implementation slice.

## Alternatives Considered

### Keep source-project samples

Rejected because source-project samples do not validate the packaged consumer experience.

### Keep manual generator harnesses as public samples

Rejected because they teach unsupported or non-normal usage.

### Move all samples into package smoke tests

Rejected because package smoke tests are validation assets, while samples are public executable documentation.

### Keep both public and internal samples under samples/

Rejected for now because it blurs the public documentation boundary. Internal harnesses should live in tests or tooling unless a later decision creates an explicit internal-samples structure.
