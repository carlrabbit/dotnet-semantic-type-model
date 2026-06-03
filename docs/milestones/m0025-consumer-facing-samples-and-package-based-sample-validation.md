# M0025: Consumer-Facing Samples and Package-Based Sample Validation

## Status

Planned.

## Maturity Mode

Public package quality correction.

The repository has shipped public packages and public documentation. Samples are consumer-visible executable documentation and must validate the published package experience, not only source-tree development behavior.

This milestone changes sample architecture, sample engineering policy, package-based validation, and directly affected public sample documentation.

## Task Mode

Milestone implementation routing.

This milestone is not a broad documentation synchronization pass. It defines focused implementation slices for replacing development-oriented samples with consumer-facing package-based samples. It does not introduce TBPs, issue templates, implementation source patches, workflow YAML changes, or generated code in the planning package.

## Goal

Make repository samples useful to consumers.

The corrected sample contract is:

```text
Public samples under samples/ demonstrate how a consumer uses SemanticTypeModel packages.

They must not be internal source-generator harnesses, source-string compiler tests, or source-tree integration tests.

They must consume locally prepared NuGet packages and validate the package experience.
```

## Problem Statement

Current samples are runnable, but several are not public consumer samples.

Observed issues include:

- source-generator samples manually invoke Roslyn generator APIs over source strings;
- samples reference `src/*` projects directly instead of consuming packages;
- code-first samples use in-memory compilation and reflection instead of normal generated provider usage;
- samples lack sufficient explanatory code comments for public learning;
- sample documentation is thin and does not distinguish consumer workflows from internal development harnesses;
- `./eng/samples.sh` runs samples against source projects rather than prepared package artifacts.

The repository needs public samples that exercise the same package and build behavior consumers will use.

## Required Authority

Read these documents before implementing any focus area:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/samples.md
docs/decisions/consumer-facing-package-based-samples.md
```

Read these only when the selected focus area touches the relevant scenario:

```text
docs/SPECS.md
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/specs/type-model-json-schema-mapping.md
docs/specs/system-text-json-contract-integration.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/PUBLIC-DOCS.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/guides/*.md
public-docs/nuget/*.md
docs/engineering/packaging.md
docs/engineering/release-readiness.md
```

Do not treat `docs/research/` guide copies as operational authority.

## Scope

### In Scope

- Define and apply consumer-facing sample rules.
- Convert public samples from `ProjectReference` to package-based consumption.
- Ensure samples restore from locally prepared packages in `artifacts/nuget` plus public feeds for third-party dependencies.
- Replace manual Roslyn source-generator harness samples with normal consumer projects.
- Keep generator harness behavior in tests or internal tooling, not public samples.
- Add or update sample documentation under `public-docs/samples/`.
- Add explanatory comments in sample code where they teach consumer usage.
- Update `./eng/samples.sh` or equivalent sample command behavior to validate package-based samples.
- Ensure samples exercise the packaged generator, package build assets, package README assumptions, and transitive dependencies.
- Align System.Text.Json samples with the M0024 resolver-centered contract.
- Keep samples deterministic and short-running.

### Out of Scope

- Broad README rewrite.
- Broad public documentation synchronization unrelated to samples.
- Release publication.
- Workflow YAML changes unless later implementation determines sample validation cannot be wired otherwise.
- New TBPs.
- New issue templates.
- Network-dependent examples.
- Secrets, cloud publishing, Power BI service calls, database server dependencies, or EF migrations against a real database.
- Replacing unit tests or package smoke tests with samples.

## Sample Classification

Implementation must classify every existing sample into one of these outcomes:

| Outcome | Meaning |
|---|---|
| Keep public sample | It demonstrates normal consumer package usage. |
| Rewrite public sample | The scenario is valid, but the implementation is source-tree or harness-oriented. |
| Move to tests/tooling | The content is useful for development validation but not a consumer sample. |
| Remove from public sample set | It duplicates another sample or teaches an unsupported usage pattern. |

Do not leave a sample in `samples/` if it remains an internal development harness.

## Required Public Sample Contract

A public sample under `samples/` must satisfy all of these rules:

- It is a standalone consumer project.
- It uses `PackageReference` for SemanticTypeModel packages.
- It does not use `ProjectReference` to `src/*`.
- It does not manually instantiate or run `SemanticTypeModelSourceGenerator`.
- It does not compile C# source strings to demonstrate normal source-generator usage.
- If it uses the source generator, the generator runs through normal MSBuild/NuGet package usage.
- It includes normal user source files for domain models, configuration, and program code.
- It explains important steps with concise code comments.
- It is deterministic and short-running.
- It writes artifacts only under deterministic sample artifact paths when needed.
- It has matching public sample documentation.
- It can be restored and run against locally prepared packages.

## Required Sample Set

Implement or correct this minimum public sample set.

| Sample | Purpose |
|---|---|
| `samples/json-schema-roundtrip` | Import JSON Schema, transform/validate where appropriate, export JSON Schema. |
| `samples/code-first-json-schema` | Annotated C# domain model, packaged source generator, generated provider, JSON Schema export. |
| `samples/code-first-ef-core` | Annotated C# domain model, packaged source generator, generated provider, EF Core projection. |
| `samples/code-first-powerbi` | Annotated C# domain model, packaged source generator, generated provider, Power BI projection metadata. |
| `samples/system-text-json-resolver` | User-authored `JsonSerializerContext`, generated semantic model or explicit model, SemanticTypeModel resolver customization. |
| `samples/runtime-di` | Consumer-style DI registration and projection usage. |

The implementation may keep existing directory names if they remain clear and public-doc links are updated. It may rename samples when that improves consumer clarity.

## Existing Sample Corrections

Implementation agents must inspect at least these existing samples:

```text
samples/code-first-authoring
samples/dotnet-generator-to-json-schema
samples/runtime-di-usage
samples/json-schema-roundtrip
samples/powerbi-projection
samples/ef-core-projection
samples/system-text-json-basic
```

Known required corrections:

- `samples/dotnet-generator-to-json-schema` must stop being a Roslyn generator-driver harness if it remains public.
- `samples/code-first-authoring` must stop compiling a source string and invoking generated providers through reflection.
- `samples/system-text-json-basic` must not rely on SemanticTypeModel-generated `JsonSerializerContext` support and should demonstrate the M0024 resolver-centered contract.
- All public sample projects must stop referencing `../../src/*` projects directly.

## Package-Based Sample Restore

Samples must consume local package artifacts.

The implementation must choose a deterministic restore strategy, such as:

```text
Directory.Build.props / NuGet.config under samples/
sample-specific RestoreAdditionalProjectSources
eng/samples.sh preparing PackageSmokeSource / package source properties
```

The chosen strategy must ensure:

- SemanticTypeModel packages come from locally prepared `artifacts/nuget` packages during validation;
- public feeds remain available for third-party packages;
- sample projects fail if required SemanticTypeModel packages are not present;
- sample validation exercises package contents and build assets, including analyzer/source-generator packaging.

Do not require consumers to use the repository source tree to run public samples.

## Source Generator Sample Requirements

A consumer-facing generator sample must use normal project compilation.

Required pattern:

```text
Domain model files are part of the sample project.
SemanticTypeModel.Generators is referenced as a package.
The generated provider is consumed directly from generated source.
No Roslyn GeneratorDriver is created by sample code.
No source string is compiled by sample code.
No generated provider is loaded through reflection.
```

The sample must show the consumer-facing generated provider shape, for example:

```csharp
TypeSchemaModel model = AppSemanticTypeModel.Create();
```

The exact namespace/type depends on the configured generator options.

## Code Comments

Samples should be self-explanatory without becoming prose-heavy.

Use code comments to explain:

- why a package is referenced;
- what property configuration enables;
- what a semantic annotation changes;
- where generated provider code comes from;
- what projection output represents;
- why a resolver wraps a user-authored `JsonSerializerContext`;
- where output artifacts are written.

Avoid comments that restate syntax.

## Public Documentation Requirements

Every public sample must have a matching page under `public-docs/samples/`.

Each page must include:

- scenario goal;
- packages used;
- how the sample is run;
- expected output;
- what consumer pattern it demonstrates;
- what it deliberately does not demonstrate;
- link or path to the sample project.

`public-docs/samples.md` must list only public consumer samples.

Do not document internal generator harnesses as public samples.

## Focus Areas

### Focus Area 1 — Sample Policy and Command Contract

#### Intent

Update sample engineering policy and sample validation behavior so public samples are package-based consumer examples.

#### Required Authority

```text
docs/engineering/samples.md
docs/engineering/command-contract.md
docs/engineering/packaging.md
docs/decisions/consumer-facing-package-based-samples.md
```

#### Validation

- Tier 0 for documentation-only changes.
- Tier 1 for changed sample command behavior.
- Tier 2 before completing implementation if command or sample projects changed.

#### Direct Documentation Impact

```text
docs/engineering/samples.md
public-docs/samples.md when sample public set changes
```

#### Deferred Documentation Impact

```text
README.md sample pointers
docs/ENGINEERING.md index wording if needed
```

### Focus Area 2 — Rewrite Generator-Oriented Samples as Consumer Samples

#### Intent

Replace generator-driver/source-string samples with normal source-generator consumption through packages.

#### Required Authority

```text
docs/specs/type-model-compile-time-generator.md
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-dotnet-attributes.md
docs/engineering/samples.md
```

#### Validation

- Tier 1:
  - affected sample run;
  - generator-focused sample validation;
  - package-based sample restore/build.
- Tier 2 before completing the focus area.

#### Direct Documentation Impact

```text
public-docs/samples/dotnet-generator.md or replacement page
public-docs/samples/code-first.md or replacement page
```

#### Deferred Documentation Impact

```text
public-docs/getting-started.md if the generator quick path is referenced there
public-docs/guides/* if guide examples use the old harness pattern
```

### Focus Area 3 — Package-Based Sample Projects

#### Intent

Convert public samples from source-project references to package references and ensure they restore from locally prepared package artifacts.

#### Required Authority

```text
docs/engineering/samples.md
docs/engineering/packaging.md
docs/engineering/command-contract.md
```

#### Validation

- Tier 1:
  - `./eng/package.sh <version>` when package artifacts are needed;
  - focused sample validation using the prepared package source.
- Tier 2:
  - `./eng/check.sh`.
- Tier 3 only if package layout, package contents, or package smoke behavior changes:
  - `./eng/package-smoke.sh <version>`.

This milestone is not release publication work; do not run Tier 4.

#### Direct Documentation Impact

```text
docs/engineering/samples.md
public-docs/samples.md
affected public-docs/samples/*.md
```

#### Deferred Documentation Impact

```text
public-docs/release-notes.md
README.md
```

### Focus Area 4 — Scenario Coverage and Sample Documentation

#### Intent

Ensure the public sample set covers the main consumer scenarios and each sample has useful documentation.

#### Required Authority

```text
docs/engineering/samples.md
docs/PUBLIC-DOCS.md
public-docs/samples.md
```

#### Validation

- Tier 0:
  - public docs validation for sample docs.
- Tier 1:
  - affected sample run.
- Tier 2 before completing implementation if sample projects changed.

#### Direct Documentation Impact

```text
public-docs/samples.md
public-docs/samples/*.md
sample code comments
```

#### Deferred Documentation Impact

```text
public-docs/guides/*.md cross-links
README.md sample section
```

### Focus Area 5 — System.Text.Json Sample Correction

#### Intent

Align the System.Text.Json sample with M0024.

The sample must demonstrate:

- user-authored `JsonSerializerContext` when STJ source generation is shown;
- `SemanticTypeModel.SystemTextJson` resolver customization;
- optional semantic-property-name-as-JSON-name behavior;
- package-based consumer usage.

#### Required Authority

```text
docs/specs/system-text-json-contract-integration.md
docs/decisions/remove-system-text-json-context-generation.md
docs/engineering/samples.md
```

#### Validation

- Tier 1:
  - affected System.Text.Json sample;
  - affected System.Text.Json unit or package smoke tests if the sample exposes behavior gaps.
- Tier 2 before completing implementation if code changed.

#### Direct Documentation Impact

```text
public-docs/samples/system-text-json*.md
public-docs/guides/system-text-json.md only if sample guidance changes supported usage wording
```

#### Deferred Documentation Impact

```text
public-docs/nuget/SemanticTypeModel.SystemTextJson.md if package README examples need a broader pass
```

## Validation Plan

Use validation tiers from `docs/ENGINEERING.md` and `docs/engineering/command-contract.md`.

### Tier 0

Use for documentation-only sample docs and sample policy changes.

Expected command when public docs change:

```sh
./eng/public-docs.sh
```

### Tier 1

Use during sample implementation for affected samples and package-based restore validation.

Expected focused validations include:

```sh
./eng/samples.sh
./eng/check-affected.sh samples/<sample>
```

If the implementation introduces a versioned sample validation command, use that documented command instead.

### Tier 2

Before completing implementation work, run:

```sh
./eng/check.sh
```

unless an explicit environment limitation prevents it.

### Tier 3

Run only when package layout, package contents, package smoke tests, or package-based sample validation requires package artifacts:

```sh
./eng/package.sh <version>
./eng/package-smoke.sh <version>
```

Do not publish packages as part of this milestone.

## Acceptance Criteria

The milestone is complete when:

- Public samples represent consumer workflows.
- Public samples consume SemanticTypeModel through package references, not direct source project references.
- Public samples restore from locally prepared package artifacts during validation.
- Public samples do not manually invoke Roslyn source-generator APIs.
- Public samples do not compile source strings to demonstrate normal source-generator usage.
- Source-generator samples use normal MSBuild/NuGet generator execution.
- Internal generator harness code is moved to tests/tooling or removed from public sample status.
- `./eng/samples.sh` or an equivalent documented command validates package-based public samples.
- Each public sample has matching public documentation under `public-docs/samples/`.
- Sample docs explain scenario goal, packages used, run command, expected output, and non-goals.
- Sample code includes useful consumer-facing comments.
- System.Text.Json sample reflects the M0024 resolver-centered contract.
- Sample engineering policy documents the package-based public-sample requirement.
- Tier 2 validation passes, or any inability to run it is explicitly reported with the exact lower-tier validation performed.
- Tier 3 package validation is run when package layout or package-based sample validation changes require it.
- No TBPs, issue templates, implementation source patches, workflow YAML, generated code, or broad documentation synchronization edits are introduced by this planning package itself.

## Direct Documentation Impact

Implementation should directly update:

```text
docs/engineering/samples.md
public-docs/samples.md
public-docs/samples/*.md for affected public samples
sample code comments
```

Update additional public guide/package README files only when they directly describe a changed sample behavior.

## Deferred Documentation Impact

Leave explicit notes for a later synchronization pass covering:

```text
README.md sample section
docs/MILESTONES.md index entry for M0025
docs/DECISIONS.md index entry for the new decision record
docs/ENGINEERING.md if the samples policy summary should mention package-based validation
public-docs/getting-started.md if it references old sample names or old generator usage
public-docs/guides/*.md if they link old sample names or old source-string generator patterns
public-docs/release-notes.md for the sample validation correction
```

Do not perform broad documentation synchronization as part of narrow sample implementation slices unless the documentation surface is directly affected by that slice.
