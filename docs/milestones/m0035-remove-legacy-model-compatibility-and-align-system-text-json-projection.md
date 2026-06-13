# M0035 — Remove Legacy Model Compatibility and Align System.Text.Json Projection

## Status

Planned

## Goal

Remove the remaining old model compatibility surface and stale "hardened" vocabulary, then align `SemanticTypeModel.SystemTextJson` with the current domain projection methodology.

After this milestone, supported runtime and projection behavior must use the current canonical semantic type model surface only. `SemanticTypeModel.SystemTextJson` must derive a System.Text.Json domain semantic model before applying resolver customization, matching the same transformation and projection pattern used by JSON Schema, EF Core, and Power BI.

## Scope

- Remove old model compatibility APIs, adapters, provider paths, query overloads, tests, samples, and public documentation.
- Remove active use of the terms `legacy`, `hardened`, and `hardening` from current source code, active specs, public docs, samples, and package docs.
- Keep historical milestone and decision file names unchanged when they describe past work.
- Replace active vocabulary with `canonical semantic model`, `runtime canonical semantic model`, `domain semantic model`, `transformation`, and `projection`.
- Introduce a System.Text.Json domain semantic model and derivation entry point.
- Move System.Text.Json resolver customization behind the derived domain model.
- Update System.Text.Json samples and tests to use code-first extraction or generation, not manual old model construction.
- Preserve the existing System.Text.Json decision that SemanticTypeModel does not generate `JsonSerializerContext` declarations.

## Non-Goals

- Do not change the external behavior of JSON Schema, EF Core, or Power BI projection except where they directly depend on removed old model compatibility APIs.
- Do not add new serializer features unrelated to the projection realignment.
- Do not generate `JsonSerializerContext` declarations.
- Do not introduce custom serializers.
- Do not keep obsolete APIs as hidden compatibility shims.
- Do not perform broad release documentation synchronization inside this implementation milestone.
- Do not rename historical milestone files only because their title contains stale transition vocabulary.

## Required Authority

Implementation agents should read only the documents listed here plus directly affected source and tests.

- `AGENTS.md`
- `docs/ENGINEERING.md`
- `docs/engineering/command-contract.md`
- `docs/TERMINOLOGY.md`
- `docs/specs/current-canonical-model-surface.md`
- `docs/specs/system-text-json-domain-model-and-resolver-projection.md`
- `docs/specs/system-text-json-contract-integration.md`
- `docs/architecture/code-first-domain-projection-pipeline.md`
- `docs/decisions/remove-system-text-json-context-generation.md`
- `docs/decisions/remove-legacy-model-compatibility-and-hardened-terminology.md`

## Focus Areas

### Focus Area A — Remove old model compatibility surface

#### Goal

Make the current canonical semantic model surface the only supported runtime and projection input surface.

#### Scope

- Remove old model adapter types and provider types.
- Remove DI overloads that accept old model types or old model factories.
- Remove query and inspection overloads for old model types.
- Remove old model conversion helpers from domain semantic models.
- Remove JSON Schema import APIs when their only purpose is old canonical model creation.
- Remove tests that validate old model compatibility.
- Update source references so active packages no longer depend on the old model namespace except where a source file is being deleted in the same focus area.

#### Non-Goals

- Do not keep obsolete APIs with `[Obsolete]` or `[EditorBrowsable(Never)]` as a substitute for removal.
- Do not create a new compatibility adapter.
- Do not migrate old JSON Schema import into a new supported authoring source.

#### Likely Files

- `src/SemanticTypeModel.Abstractions/Model/**`
- `src/SemanticTypeModel.Core/Runtime/LegacyTypeSchemaModelAdapter.cs`
- `src/SemanticTypeModel.DependencyInjection/LegacyDelegateTypeSchemaModelProvider.cs`
- `src/SemanticTypeModel.DependencyInjection/Microsoft/Extensions/DependencyInjection/TypeSchemaModelServiceCollectionExtensions.cs`
- `src/SemanticTypeModel.Core/Query/SemanticModelQueryExtensions.cs`
- `src/SemanticTypeModel.Core/Inspection/SemanticTextExtensions.cs`
- `src/SemanticTypeModel.JsonSchema/Import/**`
- `src/SemanticTypeModel.JsonSchema/JsonSchemaImportOptions.cs`
- `src/SemanticTypeModel.JsonSchema/JsonSchemaImportResult.cs`
- `src/SemanticTypeModel.EFCore/EfModelDefinition.cs`
- `src/SemanticTypeModel.PowerBI/PowerBiDerivation.cs`
- `tests/**`

#### Validation Tier

Tier 1 during implementation using affected package tests, then Tier 2 before completion.

Suggested focused commands:

```sh
./eng/test-project.sh tests/unit/SemanticTypeModel.Core.Tests.Unit/SemanticTypeModel.Core.Tests.Unit.csproj
./eng/test-project.sh tests/unit/SemanticTypeModel.DependencyInjection.Tests.Unit/SemanticTypeModel.DependencyInjection.Tests.Unit.csproj
./eng/test-project.sh tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/SemanticTypeModel.JsonSchema.Tests.Unit.csproj
./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit/SemanticTypeModel.EFCore.Tests.Unit.csproj
./eng/test-project.sh tests/unit/SemanticTypeModel.PowerBI.Tests.Unit/SemanticTypeModel.PowerBI.Tests.Unit.csproj
```

Completion command:

```sh
./eng/check.sh
```

#### Direct Documentation Impact

- Update specs that still describe old model import, adapter, query, runtime, or DI behavior as supported behavior.
- Update package/public docs only where they directly mention removed APIs or supported old model import behavior.

#### Deferred Documentation Impact

- Index normalization in `docs/SPECS.md`, `docs/DECISIONS.md`, and `docs/MILESTONES.md` may be handled by the later documentation synchronization pass.
- Broad release notes and migration notes may be handled by the later documentation synchronization pass unless implementation introduces a public API break note file as direct impact.

#### Acceptance Criteria

- No active source file exposes public or internal supported APIs accepting old model surface types.
- No active source file contains `LegacyTypeSchemaModelAdapter` or an equivalent old model adapter.
- No active DI registration path accepts old model instances or factories.
- No active query or inspection extension overload targets the old model surface.
- No active JSON Schema import API remains as a supported canonical model source.
- Tests no longer construct old model shapes to validate current behavior.
- Historical docs remain untouched unless they are active authority for current behavior.

### Focus Area B — Remove active `hardened` terminology

#### Goal

Remove transition-era vocabulary from current source, active specs, public docs, samples, and package docs.

#### Scope

- Replace `hardened model`, `hardened canonical model`, and `hardened runtime model` with current canonical terminology.
- Replace `hardening` in current active behavior docs with `stabilization`, `validation`, `cleanup`, or a feature-specific term.
- Remove active `legacy` wording where the old compatibility surface is deleted.
- Keep historical milestone/decision filenames and historical descriptions unchanged unless they are actively referenced as current behavior.

#### Non-Goals

- Do not rewrite historical repository records for cosmetic consistency.
- Do not rename historical milestone files.
- Do not alter package IDs, namespaces, or public types unless required by Focus Area A or C.

#### Likely Files

- `docs/SPECS.md`
- `docs/specs/*.md`
- `docs/architecture/*.md`
- `docs/decisions/*.md`
- `public-docs/**/*.md`
- `src/**/*.cs`
- `tests/**/*.cs`
- `samples/**/*.cs`

#### Validation Tier

Tier 1 for affected test projects, then Tier 2 before completion.

#### Direct Documentation Impact

- Update only active specs/docs that describe current behavior with stale transition terminology.

#### Deferred Documentation Impact

- Public documentation style normalization may be handled by the later documentation synchronization pass.

#### Acceptance Criteria

- Current source comments and active docs do not call the current model `hardened`.
- Current docs do not present `legacy` and current surfaces as two supported implementation paths.
- Historical docs can still mention past `hardening` milestones as historical records.

### Focus Area C — Add System.Text.Json domain semantic model and derivation

#### Goal

Align `SemanticTypeModel.SystemTextJson` with the domain projection pipeline.

#### Scope

- Add a package-owned System.Text.Json domain semantic model.
- Add a derivation entry point from the current canonical semantic model to the System.Text.Json domain semantic model.
- Run configured transformations before domain model creation.
- Preserve imported System.Text.Json metadata as projection-specific metadata, not core semantic structure.
- Preserve semantic-name and serialization-name separation.
- Emit diagnostics for ambiguous or unsupported resolver behavior.
- Provide deterministic inspection output for the System.Text.Json domain model and diagnostics.

#### Non-Goals

- Do not generate `JsonSerializerContext` declarations.
- Do not infer custom converter behavior.
- Do not replace user-authored source-generated contexts.
- Do not make System.Text.Json metadata part of the projection-neutral core model.

#### Likely Files

- `src/SemanticTypeModel.SystemTextJson/**`
- `src/SemanticTypeModel.Core/Transformation/**`
- `src/SemanticTypeModel.Core/Inspection/**`
- `src/SemanticTypeModel.DotNet/**`
- `tests/unit/SemanticTypeModel.SystemTextJson.Tests.Unit/**`
- `docs/specs/system-text-json-contract-integration.md`
- `docs/specs/system-text-json-domain-model-and-resolver-projection.md`

#### Validation Tier

Tier 1 during implementation, then Tier 2 before completion.

Suggested focused command:

```sh
./eng/test-project.sh tests/unit/SemanticTypeModel.SystemTextJson.Tests.Unit/SemanticTypeModel.SystemTextJson.Tests.Unit.csproj
```

Completion command:

```sh
./eng/check.sh
```

#### Direct Documentation Impact

- Update System.Text.Json specs if implementation choices refine the domain model contract.
- Update diagnostics docs if new public diagnostics are assigned.

#### Deferred Documentation Impact

- NuGet package README and public guide synchronization may be handled later unless implementation introduces a direct usage change that would make current package docs incorrect.

#### Acceptance Criteria

- System.Text.Json resolver customization can be driven by a derived System.Text.Json domain semantic model.
- Convenience APIs may accept the current canonical semantic model, but must derive the System.Text.Json domain model internally before resolver behavior is applied.
- Resolver customization no longer reads old model shapes or scattered annotations as its primary behavior model.
- Tests validate derivation, diagnostics, inspection, and resolver behavior.

### Focus Area D — Replace System.Text.Json samples and tests

#### Goal

Make System.Text.Json samples demonstrate the current code-first and domain projection workflow.

#### Scope

- Replace sample code that manually constructs old model shapes.
- Use code-first extraction or generated providers as the source of the canonical semantic model.
- Demonstrate user-authored `JsonSerializerContext` plus SemanticTypeModel resolver composition.
- Demonstrate default contract preservation and explicit semantic-name-as-JSON-name override.
- Demonstrate extension data metadata where M0034 behavior is present.
- Keep samples deterministic and package-based.

#### Non-Goals

- Do not introduce sample-only unsupported APIs.
- Do not demonstrate JSON Schema import as a model source.
- Do not demonstrate generated `JsonSerializerContext` output from SemanticTypeModel.

#### Likely Files

- `samples/system-text-json-resolver/**`
- `public-docs/samples/system-text-json-resolver.md`
- `public-docs/samples.md`
- `tests/unit/SemanticTypeModel.SystemTextJson.Tests.Unit/**`
- `tests/package-smoke/**`

#### Validation Tier

Tier 1 for System.Text.Json tests during implementation, then Tier 2 before completion. Sample execution is deferred unless package/sample behavior is directly changed in the implementation focus area and local package preparation is available.

Suggested focused commands:

```sh
./eng/test-project.sh tests/unit/SemanticTypeModel.SystemTextJson.Tests.Unit/SemanticTypeModel.SystemTextJson.Tests.Unit.csproj
```

If sample validation is part of the implementation work:

```sh
./eng/package.sh 0.0.0-local
./eng/samples.sh
```

#### Direct Documentation Impact

- Update the System.Text.Json sample page when sample code changes.

#### Deferred Documentation Impact

- Broader public sample index and package README normalization may be handled by documentation synchronization unless they are directly contradicted by the changed sample.

#### Acceptance Criteria

- System.Text.Json sample no longer constructs old model shapes manually.
- Sample uses the current code-first canonical model flow.
- Sample demonstrates user-owned context composition and explicit resolver behavior.
- Sample documentation matches the sample code.

## Integration Notes

The milestone intentionally combines old model removal with System.Text.Json realignment because the current System.Text.Json package still consumes the old model surface and manual shape construction pattern. Removing the old surface without realigning System.Text.Json would either break the package or force another compatibility layer.

Implementation agents should complete Focus Area A before or together with Focus Area C. Focus Area C must not introduce replacement compatibility APIs to preserve old behavior removed by Focus Area A.

## Completion Notes

To complete the milestone:

- all focus-area acceptance criteria must be satisfied;
- Tier 2 validation must pass through `./eng/check.sh` unless an environment limitation is documented;
- direct documentation impact must be updated;
- deferred documentation impact must be listed for the documentation synchronization pass;
- no release validation is required because this is not a release milestone.
