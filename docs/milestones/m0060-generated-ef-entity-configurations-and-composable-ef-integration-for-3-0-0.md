# M0060: Generated EF Entity Configurations and Composable EF Integration for 3.0.0

## Status

Completed on 2026-08-14.

## Goal

Replace the runtime EF Core `ModelBuilder` cleanup/reconciliation path with compile-time generation of ordinary EF Core `IEntityTypeConfiguration<TEntity>` classes.

Introduce the new NuGet package:

```text
SemanticTypeModel.EFCore.Generators
```

Prepare, but do not publish, the `3.0.0` package set.

## Why This Milestone Exists

The remaining problem is architectural, not EF CLR scanning itself.

The current runtime path participates in a global mutable EF model and then removes or rejects metadata that is not represented by the semantic model being applied. That is not composable when one `DbContext` contains:

```text
semantic model A
semantic model B
manual EF entities
application/framework entities
```

A semantic model must not treat unrelated entities as invalid.

The runtime repair approach also gives consumers a poor hotfix boundary. Consumers should be able to inspect generated EF code and add normal EF configuration in their own source without editing generated files.

## Architectural Decision

New flow:

```text
semantic model assembly
    ↓
compile-time semantic manifest
    ↓
persistence project explicitly selects semantic model(s)
    ↓
SemanticTypeModel.EFCore.Generators
    ↓
EF relational projection
    ↓
generated IEntityTypeConfiguration<TEntity> per semantic Entity
    ↓
generated deterministic Apply<Model>() extension
    ↓
normal EF Core model building
```

Ownership:

```text
semantic model defines intent
STM translates intent into normal EF configuration code
EF Core builds/finalizes the model
the application owns DbContext composition
```

## Core Invariants

### Composability

A generated semantic EF model may configure only CLR entity types it owns.

It must never globally inspect, remove, ignore, reject, or validate unrelated EF entity types in the `DbContext`.

### Extensibility

Every generated entity configuration is inspectable source and exposes application-owned partial hooks:

```text
ConfigureBeforeGenerated
generated STM mapping
ConfigureAfterGenerated
```

`ConfigureAfterGenerated` is the normal consumer override/hotfix point.

### Entity-only configuration

Generate `IEntityTypeConfiguration<TEntity>` only for semantic Entities.

```text
Entity -> IEntityTypeConfiguration<TEntity>
ValueKind -> property mapping inside owner Entity configuration
Enum -> scalar property mapping
Strong scalar / identifier -> scalar property mapping
Nonsemantic base -> inherited property mapping on semantic storage Entity
DTO/interface/repository/framework type -> no EF entity configuration
```

### Direct EF code

Generated source must expose meaningful EF API calls directly.

Prefer:

```csharp
builder.Property(x => x.ImportType)
    .HasConversion<string>();
```

Do not generate wrappers that delegate the whole entity to a runtime descriptor interpreter.

Runtime helpers are limited to converter/comparer/helper primitives.

## Package Structure

### `SemanticTypeModel.EFCore`

Keep as the runtime/contract package for:

```text
EF relational projection model and diagnostics
model-selection attribute/API
converter/comparer/helper primitives used by generated code
inspection/derivation APIs that do not globally mutate ModelBuilder
```

Remove the supported runtime application/cleanup responsibilities:

```text
ApplySemanticTypeModel
ApplySemanticRelationalModel
global convention cleanup/suppression
global exact semantic-entity-set enforcement
```

`DeriveEfRelationalModel` may remain as an inspection/derivation API if still coherent.

### `SemanticTypeModel.EFCore.Generators`

Create:

```text
src/SemanticTypeModel.EFCore.Generators/
```

Package requirements:

```text
PackageId = SemanticTypeModel.EFCore.Generators
analyzer/source-generator package
OutputItemType = Analyzer
IncludeBuildOutput = false
generator assembly under analyzers/dotnet/cs
Microsoft.CodeAnalysis.CSharp private to analyzer implementation
README from public-docs/nuget/SemanticTypeModel.EFCore.Generators.md
normal dependency on SemanticTypeModel.EFCore when required by generated source
no Microsoft.EntityFrameworkCore binaries bundled into analyzer assets
```

Follow the existing `SemanticTypeModel.Generators` packaging pattern.

## Shared EF Projection Logic

Do not create a second hand-maintained mapping algorithm.

Refactor pure EF relational derivation/code-generation inputs so runtime inspection and generator output use the same rules without requiring the generator to load the old runtime `ModelBuilder` application implementation.

Internal shared project/source is acceptable.

Do not create another public NuGet package solely for implementation sharing.

## Compile-Time Semantic Model Handoff

The generator normally runs in the persistence project while the semantic CLR model lives in a referenced model/domain project.

The EF generator must not execute referenced consumer assemblies.

Extend `SemanticTypeModel.Generators` to emit a deterministic, versioned compile-time manifest for the generated semantic model.

The manifest must preserve enough information for EF generation, including:

```text
manifest schema version
semantic model id/name
semantic types/properties required by EF
CLR metadata name per relevant type
CLR member identity
declaring/storage lineage
entity/value-kind/enum/scalar/collection shape
ownership
keys
inheritance
```

Requirements:

```text
readable through Roslyn metadata
no Assembly.Load of application binaries
no runtime reflection execution
no invocation of generated Create() from the analyzer
deterministic
versioned
```

## Model Selection API

The persistence project explicitly selects semantic model assemblies through assembly attributes.

Preferred shape:

```csharp
[assembly: GenerateSemanticEfModel(typeof(FinanceModelMarkerType))]
[assembly: GenerateSemanticEfModel(typeof(AccountingModelMarkerType))]
```

The marker type identifies the semantic-model assembly and its generated manifest.

The contract must be:

```text
explicit
strongly typed
visible in source
supports multiple models
does not scan every transitive reference
```

Missing, ambiguous, or unsupported manifests produce generator diagnostics.

The primary supported layout is:

```text
domain/model project
    ↓ project reference
persistence project + EF generator
```

## Generated Configuration Contract

For each semantic Entity:

```csharp
internal partial class ImportSpecificationConfiguration
    : IEntityTypeConfiguration<ImportSpecification>
{
    public void Configure(
        EntityTypeBuilder<ImportSpecification> builder)
    {
        ConfigureBeforeGenerated(builder);
        ConfigureGenerated(builder);
        ConfigureAfterGenerated(builder);
    }

    private static void ConfigureGenerated(
        EntityTypeBuilder<ImportSpecification> builder)
    {
        // Direct generated EF Core calls.
    }

    static partial void ConfigureBeforeGenerated(
        EntityTypeBuilder<ImportSpecification> builder);

    static partial void ConfigureAfterGenerated(
        EntityTypeBuilder<ImportSpecification> builder);
}
```

Do not add fine-grained hooks in M0060.

Do not add a per-entity manual/no-generation mode in M0060.

## Consumer Customization

Application code:

```csharp
internal partial class ImportSpecificationConfiguration
{
    static partial void ConfigureAfterGenerated(
        EntityTypeBuilder<ImportSpecification> builder)
    {
        builder.HasIndex(x => x.DisplayName);

        // Temporary local workaround if needed.
        builder.Property(x => x.ImportType)
            .HasConversion<string>();
    }
}
```

Consumers never edit generated files.

## Generated Registration

Generate one public registration extension per selected semantic model:

```csharp
public static ModelBuilder ApplyFinanceSemanticModel(
    this ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfiguration(
        new SpecificationConfiguration());

    modelBuilder.ApplyConfiguration(
        new ImportSpecificationConfiguration());

    return modelBuilder;
}
```

Ordering:

```text
semantic base entities before derived entities
stable secondary ordering by semantic type id / CLR metadata name
```

Do not use `ApplyConfigurationsFromAssembly` as the canonical path.

Application composition:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyFinanceSemanticModel();
    modelBuilder.ApplyAccountingSemanticModel();
    modelBuilder.ApplyConfiguration(new AuditRecordConfiguration());
}
```

## Multi-Model Contract

Multiple semantic models in one `DbContext` are supported.

Applying model A may not remove/reject/configure model B's entities.

Applying model B may not remove/reject/configure model A's entities.

Manual EF entities remain untouched.

If two selected semantic models claim the same CLR Entity type, emit a generator error rather than resolving by ordering.

Suggested diagnostic:

```text
STM_EF_GENERATED_ENTITY_CONFIGURATION_COLLISION
```

## Mapping Policy Retained

The mechanism changes; the semantic EF storage policy does not.

Retain:

```text
Entity -> table
Entity inheritance -> TPT
scalar -> column
enum -> string provider column
strong scalar/id -> underlying scalar
binary -> binary
owned ValueKind object -> JSON column
owned ValueKind collection -> JSON array column
nested ValueKind -> nested JSON
SemanticExtensionData -> JSON object
entity object navigation -> unsupported
entity collection navigation -> unsupported
arbitrary dictionary -> unsupported except extension data
```

Do not introduce `OwnsOne`/`OwnsMany`.

## Generated Source Inspectability

No CLI/tool is required.

Document normal IDE generated-source navigation and the optional compiler setting:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>
    $(BaseIntermediateOutputPath)Generated
  </CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files are not committed.

## Required Tests

Create:

```text
tests/unit/SemanticTypeModel.EFCore.Generators.Tests.Unit/
```

Required generator coverage:

```text
one configuration per Entity
no configuration for ValueKind/enum/nonentity
enum string mapping
JSON ValueKind mapping
strong-id mapping
binary mapping
TPT ordering
inherited nonsemantic-base placement
deterministic generated text
before/after hooks
public Apply<Model>() extension
model-selection handling
missing/ambiguous manifest diagnostic
manifest-version diagnostic
duplicate CLR entity ownership diagnostic
generated name collision diagnostic
```

Compile generated source together with application partial customizations.

### Multi-model provider regression

Use a real `DbContext` with:

```text
semantic model A
semantic model B
one unrelated manually configured EF entity
```

Apply:

```csharp
modelBuilder.ApplyModelA();
modelBuilder.ApplyModelB();
modelBuilder.ApplyConfiguration(new ManualEntityConfiguration());
```

Assert all three sets remain and SQLite finalization/`EnsureCreated`/save/reload succeed.

This is the permanent regression for the old non-composable cleanup behavior.

### Hotfix regression

Use a real `ConfigureAfterGenerated` implementation and prove the final provider-backed EF metadata reflects the application override.

### Package smoke

The packed `SemanticTypeModel.EFCore.Generators` package must be restored and executed as an analyzer from `artifacts/nuget`.

A project-reference-only test is insufficient.

Prefer a two-project smoke fixture:

```text
semantic model project
    SemanticTypeModel.Generators
    emits provider/manifest

persistence consumer
    references model
    SemanticTypeModel.EFCore
    SemanticTypeModel.EFCore.Generators
    SQLite
    generated configurations compile and run
```

## Build and Solution Integration

Update `SemanticTypeModel.slnx` with:

```text
src/SemanticTypeModel.EFCore.Generators/SemanticTypeModel.EFCore.Generators.csproj
tests/unit/SemanticTypeModel.EFCore.Generators.Tests.Unit/SemanticTypeModel.EFCore.Generators.Tests.Unit.csproj
```

Add any internal non-packaged shared project used by generation.

Ensure normal:

```sh
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/check.sh
```

covers the new projects.

CI currently delegates to `./eng/check.sh`; keep that single command boundary.

## Canonical Package Inventory

`eng/common.sh` currently defines the package IDs/projects used by packing, smoke, and publishing.

Add:

```text
SemanticTypeModel.EFCore.Generators
src/SemanticTypeModel.EFCore.Generators/SemanticTypeModel.EFCore.Generators.csproj
```

to:

```text
semantic_type_model_package_ids
semantic_type_model_package_projects
```

### Remove duplicated package inventory

`eng/public-docs.sh` currently duplicates package IDs and project paths in Python lists.

Refactor it so package validation consumes the canonical inventory from `eng/common.sh` or a mechanically generated equivalent.

Acceptance rule:

> Adding a package to the canonical inventory must not require maintaining a second independent package-id/project-path list inside `eng/public-docs.sh`.

## NuGet Packing and Publishing

### Pack

`eng/package.sh` already loops the canonical project list.

Verify:

```sh
./eng/package.sh 3.0.0
```

creates:

```text
SemanticTypeModel.EFCore.Generators.3.0.0.nupkg
SemanticTypeModel.EFCore.Generators.3.0.0.snupkg
```

Inspect the `.nupkg`.

Required:

```text
analyzers/dotnet/cs generator assets
README.md
correct package metadata/dependencies
```

Forbidden:

```text
generator implementation in lib/ref
bundled Microsoft.EntityFrameworkCore analyzer assets
missing README
```

### Package smoke

`eng/package-smoke.sh` already derives package count/IDs from `eng/common.sh`.

Update its consumer/tests so the new generator package is actually executed.

Also update `tests/package-smoke/SemanticTypeModel.PackageSmoke.Tests` where package references are explicitly maintained.

### Publish

`eng/publish.sh` already loops canonical package IDs and fails when a package is missing.

Verify the new package is therefore included automatically.

Do not publish in M0060.

### GitHub workflows

Review:

```text
.github/workflows/ci.yml
.github/workflows/pack.yml
.github/workflows/release-check.yml
.github/workflows/publish-nuget.yml
```

Preserve script-driven package inventory.

Do not add a second hardcoded package list to workflow YAML.

Required outcome:

```text
CI builds/tests generator through eng/check.sh
pack workflow produces/uploads new nupkg/snupkg
release-check validates generator package smoke/docs
publish workflow would publish the new package through eng/publish.sh
```

If YAML changes are not needed because the scripts already cover the new canonical inventory, record the no-change rationale in completion notes.

## Documentation

Create:

```text
public-docs/nuget/SemanticTypeModel.EFCore.Generators.md
```

Update at minimum:

```text
README.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
public-docs/packages.md
public-docs/installation.md
public-docs/getting-started.md
public-docs/guides/ef-core-projection.md
public-docs/guides/projection-capabilities.md
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/nuget/SemanticTypeModel.EFCore.Generators.md
public-docs/samples/code-first-ef-core.md
public-docs/api/compatibility.md
public-docs/api/public-api.md
public-docs/release-notes.md
samples/code-first-ef-core/
```

Update `eng/public-docs.sh` required-file validation for the new package README.

Documentation must explain:

```text
runtime cleanup removal
runtime vs generator package roles
persistence-project generation
explicit model selection
generated source shape
source inspection
partial hooks and precedence
multiple semantic models in one DbContext
manual EF entity coexistence
duplicate entity ownership diagnostic
no CLI/tool
```

## Architecture Docs

Create authoritative current docs:

```text
docs/decisions/efcore-application-is-generated-configuration-code.md
docs/specs/ef-core-generated-configuration-contract.md
```

Update current architecture/specs as needed:

```text
docs/architecture/code-first-domain-projection-pipeline.md
docs/specs/opinionated-ef-relational-projection-contract.md
docs/specs/ef-model-shape-test-matrix-and-member-placement.md
```

Mark old runtime application/cleanup authority as superseded where appropriate, including:

```text
docs/specs/ef-core-closed-modelbuilder-application.md
docs/specs/ef-core-clr-convention-suppression.md
docs/specs/ef-core-source-lineage-diagnostics.md
docs/decisions/efcore-semantic-model-is-the-ef-application-contract.md
docs/decisions/ef-source-lineage-is-diagnostic-first-and-application-policy-aware.md
```

Do not rewrite historical milestones.

## Diagnostics

Add stable generator diagnostics for:

```text
selected manifest missing
selected manifest ambiguous
manifest version unsupported
EF projection errors
duplicate CLR entity ownership
generated configuration name collision
generated registration name collision
CLR source type/member cannot be resolved
```

Use repository diagnostic conventions.

## Non-Goals

```text
dotnet tool/materialized generated files
committed generated source
runtime reflection scanning
automatic generation for all transitive models
relationship/navigation inference
OwnsOne/OwnsMany
TPH/TPC
provider-specific JSON querying
per-entity manual mode
fine-grained hooks
backward-compatible runtime ModelBuilder application
global EF model cleanup
```

## Implementation Order

1. Add M0060 decision/spec skeletons.
2. Define semantic compile-time manifest.
3. Extend `SemanticTypeModel.Generators` to emit it.
4. Add model-selection API to `SemanticTypeModel.EFCore`.
5. Add `SemanticTypeModel.EFCore.Generators`.
6. Share/refactor pure relational generation rules.
7. Generate entity configurations and hooks.
8. Generate deterministic per-model registration.
9. Add generator diagnostics/collision checks.
10. Add generator unit/source-compilation tests.
11. Add multi-model + manual-entity provider test.
12. Add `ConfigureAfterGenerated` provider test.
13. Remove runtime ModelBuilder application/cleanup APIs.
14. Update sample.
15. Add projects/package inventory.
16. Refactor public-doc package inventory validation.
17. Update package smoke to execute the packed generator.
18. Update public docs/package README/API compatibility/release notes.
19. Supersede old runtime EF authority docs.
20. Review GitHub workflows.
21. Build/inspect `3.0.0` packages.
22. Run full release validation.
23. Stop before publishing.

## Validation

Focused:

```sh
./eng/test-filter.sh M0060
./eng/test-filter.sh EFCoreGenerator
./eng/test-filter.sh GeneratedConfiguration
./eng/test-filter.sh MultiModel
./eng/test-filter.sh PartialHook

./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Generators.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
./eng/test-project.sh tests/integration/SemanticTypeModel.EFCore.Tests.Integration
```

Repository:

```sh
./eng/check.sh
```

Package/docs:

```sh
./eng/package.sh 3.0.0
./eng/package-smoke.sh 3.0.0
./eng/samples.sh
./eng/public-docs.sh
```

Inspect:

```sh
find artifacts/nuget -maxdepth 1 -type f -print | sort
unzip -l artifacts/nuget/SemanticTypeModel.EFCore.Generators.3.0.0.nupkg
```

Full non-publishing release validation:

```sh
./eng/release-check.sh 3.0.0
```

Do not run:

```sh
./eng/publish.sh 3.0.0
```

## Acceptance Criteria

- Generated EF configuration is the supported static CLR EF application path.
- Runtime global cleanup/application APIs are removed.
- New generator package exists and is packed as analyzer assets.
- One configuration is generated per Entity and not for ValueKinds/enums/nonentities.
- Before/After partial hooks compile and `After` can change finalized EF metadata.
- One deterministic public `Apply<Model>()` extension is generated per selected semantic model.
- Two semantic models plus a manual entity coexist in one real `DbContext`.
- No generated model removes/rejects unrelated entities.
- Duplicate CLR entity ownership is a generator error.
- Existing Entity/TPT/scalar/enum/strong-id/binary/JSON ValueKind policies remain.
- Packed generator executes in package smoke.
- New package is in the canonical pack/publish inventory.
- `eng/public-docs.sh` no longer has an independent duplicate package ID/project inventory.
- README/package docs/EF guide/sample/API compatibility/release notes are updated.
- Old runtime EF authority docs are marked superseded.
- `docs/MILESTONES.md` includes M0059 and M0060.
- `./eng/check.sh` passes.
- `./eng/package.sh 3.0.0` passes.
- `./eng/package-smoke.sh 3.0.0` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 3.0.0` passes.
- New `.nupkg` contents are inspected.
- No package is published.

## Human Review

Review:

```text
model-selection API
manifest contract/version
runtime/generator package boundary
generated names/namespaces
partial hook contract
direct generated EF code
multi-model behavior
runtime API removals
new nupkg contents
build/pack/publish integration
public API diff
3.0 compatibility docs/release notes
publication approval
```

## Completion Notes

- Model selection is the repeatable assembly-level `GenerateSemanticEfModel(typeof(Marker))` contract.
- Manifest schema version 1 is deterministic base64-encoded JSON assembly metadata. It records model identity, semantic type kind/role, CLR/base identities, properties and CLR declaring/member identities, nullability, key order, ownership, extension data, arrays, enums, scalars, and collections. Roslyn reads it without executing the model assembly.
- `SemanticTypeModel.EFCore.Generators` is an analyzer-only package under `analyzers/dotnet/cs`, depends normally on `SemanticTypeModel.EFCore`, and bundles no EF binaries.
- Runtime `ApplySemanticTypeModel`, `ApplySemanticRelationalModel`, `EfRelationalApplicationResult`, global ignore/remove/audit behavior, and their obsolete tests were removed. `DeriveEfRelationalModel` remains for inspection.
- Each semantic Entity receives an internal partial configuration with direct EF calls, before/after hooks, and deterministic public model registration. ValueKinds, enums, and nonentities receive no configuration.
- Generator `M0060GeneratedConfigurationTests` compile the updated output and prove deterministic text, Entity-only output, enum strings, JSON ValueKinds, URI and nullable-URI strings, strong identifiers, both binary forms, inherited-member placement, explicit nullability, TPT ordering, both partial hooks, and base-first registration. The diagnostic matrix covers missing, ambiguous, malformed, and unsupported manifests; duplicate ownership; generated-name collisions; unresolved types and members; and projection failures.
- Integration `M0060GeneratedConfigurationTests` use two independently compiled semantic-model assemblies plus manual EF entities declared in the persistence project and in each selected model assembly. SQLite finalization, `EnsureCreated`, save, and reload retain both models, their TPT hierarchy, URI conversion, and every manual entity. Exact finalized-model assertions prove semantic nonentities and unselected POCOs remain absent; single-model contexts prove each generated registration leaves an unrelated entity from the other assembly intact. Real `ConfigureBeforeGenerated` and `ConfigureAfterGenerated` implementations respectively add an annotation and a unique finalized-model index.
- Generator diagnostics are STM5037-STM5046 and are reserved in `DotNetExtractionDiagnosticIds`, static descriptors, stability coverage, and public diagnostics documentation.
- The source and dedicated test projects are in `SemanticTypeModel.slnx`; model fixtures build transitively through integration tests.
- `eng/common.sh` is the canonical inventory. `eng/public-docs.sh` imports it rather than maintaining another package list.
- Package smoke creates a model project and persistence project, restores the packed analyzers from `artifacts/nuget`, executes both generators, calls the generated extension, and verifies its Entity.
- CI, pack, release-check, and publish workflow YAML required no changes: each delegates to canonical `eng/` scripts, which consume the updated inventory. No YAML package list was introduced.
- Focused tests, Tier 2, package, package smoke, samples, public docs, and the full 3.0.0 release check passed. The generator nupkg contains README plus only its DLL/PDB analyzer assets and has a normal EFCore package dependency.
- Public API/compatibility documentation records the intentional 3.0 runtime application removal and generated replacement.
- Remaining risk is provider-specific JSON query behavior, explicitly outside the provider-neutral contract.
- Publication status: prepared and validated; not published.
