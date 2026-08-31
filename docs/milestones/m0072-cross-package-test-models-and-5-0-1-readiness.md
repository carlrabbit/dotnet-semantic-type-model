# M0072 — Cross-Package Test Models and 5.0.1 Release Readiness

## State

```text
Lifecycle: ready
Execution profile: ai-executed-broad
Baseline implementation model: GPT-5.6 Luna
Repository role: capability-provider
Maturity: published-maintenance
Guide system: 0.7.4
Integration target: current main after M0071
Expected planning baseline: d2f58ad4f6e71297b8b9f47c5ff64398a8b9a706
Release target: 5.0.1
Implementation package version: 5.0.1-m0072
Consumer-surface validation: required
Documentation synchronization: direct for touched/release surfaces
Human review: none
Publication: forbidden
```

## Goal

Complete the repository-wide test-fixture consolidation deferred by M0071 and leave the current ten-package mainline formally ready for a 5.0.1 release.

The milestone has two ordered outcomes:

```text
A. shared generated test-model consolidation
-> B. 5.0.1 release-readiness preparation and validation
```

Release-readiness evidence must be produced only after the consolidated test architecture is in its final milestone state.

## Context

M0071 introduced:

```text
tests/fixtures/SemanticTypeModel.TestModels.ModelA
tests/fixtures/SemanticTypeModel.TestModels.ModelB
```

as independent generated annotated model assemblies and intentionally used them only for System.Text.Json work.

The repository still contains older positive fixture systems, including current equivalents of:

```text
SemanticTypeModel.RealWorldFixtures
SemanticTypeModel.EFCoreModelShapes
SemanticTypeModel.EFCore.CompatibilityModel
SemanticTypeModel.EFCore.CompositionModel
```

Some of those build canonical models manually or duplicate source-model shapes.

M0072 generalizes the M0071 fixture architecture across the package suite without turning every unit test into an integration test.

## Release-Line Decision

The release target is **5.0.1**.

This is a deliberate patch-line decision.

M0071 automatic System.Text.Json Entity polymorphism and runtime-composition work is treated as corrective completion of the intended 5.0 System.Text.Json runtime behavior, not as the start of a 5.1 release line.

Implementation must not change the candidate to `5.1.0` solely because M0071 previously used `5.1.0-m0071` as an implementation-validation version.

5.0.1 remains an aligned ten-package suite.

The milestone does not reopen the M0067 Configuration/Options removal or introduce an eleventh package.

## Shared Test-Model Authority

Follow:

```text
docs/engineering/shared-generated-test-models.md
```

as the durable fixture policy.

The desired architecture is:

```text
Annotated CLR Model A
    -> real STM generator
    -> generated provider + manifest
             |
Annotated CLR Model B
    -> real STM generator
    -> generated provider + manifest
             |
             +-------------------------------+
             |               |               |
             v               v               v
        JSON Schema       EF Core          Power BI
             |               |               |
             +------- other package boundaries -------+
```

System.Text.Json already uses this architecture from M0071 and must remain passing.

## Model A Coverage

Expand Model A from its M0071 STJ-focused shape into a dimension-complete positive semantic coverage model.

At minimum it must contain representative valid authoring for all currently supported scalar kinds:

```text
Boolean
String
Integer
Number
Decimal
Date
Time
DateTime
DateTimeOffset
Duration
Guid
Binary
```

and Strong Scalars backed by each currently supported Strong Scalar scalar kind.

Guid-backed Strong Scalar coverage remains explicit and must include representative direct, nullable, and nested/owned use where supported.

Model A also covers representative valid forms of the current semantic vocabulary used across projections, including:

```text
Entity
ValueObject
semantic inheritance

required / optional
nullable / non-nullable

enum / nullable enum

array / collection
dictionary

owned object
owned collection
nested ownership
nullable owned shapes where supported

extension data

string constraints
numeric constraints
collection constraints
format
RequiredWhen

Key
Display Identity
Access Path

lifecycle mutability
user description
technical description
representative ui.* metadata

current envelope / evolution / lifecycle semantics
```

Coverage is dimension-complete, not a Cartesian product.

Do not create synthetic combinations solely to multiply test cases.

## Model B Coverage

Keep Model B smaller and independent.

It must retain enough overlap to prove:

- independent generated provider/manifest identity;
- multiple-model operation;
- absence of global single-model state;
- correct use of CLR/model identity rather than simple names.

Where practical, Model B should deliberately contain simple CLR type names that also occur in Model A under its own namespace/model identity.

At minimum Model B contains:

```text
its own Entity hierarchy
its own Guid-backed Strong Scalar
an owned value shape
enum + nullable member
```

Cross-model inheritance remains out of scope.

## Migration Scope

Migrate positive boundary/integration tests across the repository to use Model A and/or Model B where the test claim includes real code-first authoring, generation, manifest/provider transport, or target projection behavior.

Inspect at least the current test surfaces for:

```text
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
SemanticTypeModel.JsonSchema
SemanticTypeModel.EFCore
SemanticTypeModel.EFCore.Generators
SemanticTypeModel.PowerBI
SemanticTypeModel.DependencyInjection
SemanticTypeModel.SystemTextJson
package smoke / isolated consumer
```

Do not mechanically rewrite tests whose purpose is genuinely isolated canonical/domain behavior.

System.Text.Json tests already migrated by M0071 should be adjusted only when Model A/B expansion or shared helper cleanup requires it.

## Synthetic-Test Boundary

Hand-built `TypeSchemaModel` inputs remain valid when the test deliberately exercises:

- invalid canonical state;
- isolated transformations;
- pathological or impossible-to-author graphs;
- target-domain behavior where authoring/generator transport is intentionally not part of the claim;
- small direct unit behavior.

Inline Roslyn source remains valid for extraction/generator diagnostics that need malformed or specially constructed compilation input.

Do not replace such tests merely to maximize fixture reuse.

## Multiple-Model Acceptance

Multiple independently generated models are a first-class package requirement.

Where a package naturally composes models into shared state, final tests must exercise Model A and Model B together.

At minimum:

### EF Core

Prove composition of both generated semantic models in one application EF model/DbContext context while preserving an unrelated manually configured application entity.

Do not infer new relationships or change current EF storage policy while migrating the fixtures.

### System.Text.Json

Retain M0071 coverage proving both models compose on one `JsonSerializerOptions`.

### Other projections

For targets that project one model at a time, process Model A and Model B in the same test process and prove model-local results and absence of cross-model/global-state leakage.

No package may assume there is only one generated semantic provider or manifest in the process/application.

## Fixture Cleanup

After scenarios are migrated and evidence is established, remove obsolete positive fixture systems that Model A/Model B fully replace.

The expected cleanup candidates are:

```text
tests/fixtures/SemanticTypeModel.RealWorldFixtures
tests/fixtures/SemanticTypeModel.EFCoreModelShapes
tests/fixtures/SemanticTypeModel.EFCore.CompatibilityModel
tests/fixtures/SemanticTypeModel.EFCore.CompositionModel
```

Do not preserve an obsolete fixture project as a tombstone.

If a candidate contains a scenario that cannot correctly move to Model A/Model B under the resolved shared-fixture contract, retain only the irreducible target-specific boundary and document why. Do not keep duplicate positive model graphs for convenience.

Remove obsolete hand-built positive `FixtureModels` / `ModelShapeModels` style builders once their positive scenarios are covered by generated fixtures.

Git retains history.

## Test Organization

The fixture projects contain model data and generated semantic providers, not target-specific assertion helpers.

Target-specific:

```text
DbContext types
EF manual-entity configuration
JSON Schema expected documents
Power BI expected metadata
serializer options
database setup
assertion helpers
```

remain in their respective test projects unless a genuinely target-neutral helper already has a suitable home.

Do not create a general test-framework abstraction merely to hide ordinary test setup.

## Product-Code Boundary

M0072 is primarily test architecture plus release readiness.

Do not intentionally change public product semantics.

If fixture migration exposes a real product defect:

- add the smallest regression proving it through the appropriate real boundary;
- fix it when the fix is unambiguously within current specifications and compatibility policy;
- update direct docs when consumer behavior actually changes.

Return to planning only when the defect requires a new material semantic, architecture, compatibility, scope, or release decision.

Do not weaken tests to preserve a defect.

## 5.0.1 Documentation

Prepare release-facing documentation for 5.0.1 after the final product/test outcome is known.

At minimum inspect/update current equivalents of:

```text
public-docs/release-notes.md
public-docs/api/compatibility.md
public-docs/nuget/SemanticTypeModel.md
README.md
public-docs/guides/system-text-json.md
public-docs/versioning.md
```

The 5.0.1 release notes must distinguish:

### Consumer-visible runtime correction from M0071

- ordinary `JsonSerializerOptions` remains the primary complete STM System.Text.Json runtime path;
- modeled semantic Entity inheritance receives automatic `$type` polymorphism when the application has not supplied an explicit STJ contract;
- existing explicit application STJ polymorphism wins;
- Strong Scalars and multiple STM models compose through runtime options;
- Minimal API global JSON configuration uses `ConfigureHttpJsonOptions`;
- automatic polymorphic/discriminator output remains outside the JSON Schema/System.Text.Json fidelity baseline.

### Test/infrastructure changes from M0072

Describe only as engineering/reliability improvements where useful.

Do not present shared test-model consolidation as a new consumer API.

Do not claim publication before actual publication occurs.

## Publication-Channel Preflight

Before declaring 5.0.1 release readiness:

1. verify actual publication-channel state;
2. verify the complete expected 5.0.0 ten-package predecessor suite is published;
3. verify 5.0.1 is not already published for any expected package ID;
4. verify repository documentation assumptions agree with publication truth.

If required publication truth cannot be established, final release readiness is incomplete.

Do not guess from repository tags, source versions, or earlier planning messages.

## Package Inventory

The expected 5.0.1 suite is exactly:

```text
SemanticTypeModel.Abstractions
SemanticTypeModel.Core
SemanticTypeModel.JsonSchema
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
SemanticTypeModel.DependencyInjection
SemanticTypeModel.PowerBI
SemanticTypeModel.EFCore
SemanticTypeModel.EFCore.Generators
SemanticTypeModel.SystemTextJson
```

All must use exactly version `5.0.1`.

The following must not exist:

```text
SemanticTypeModel.Configuration.5.0.1.nupkg
```

or any other removed Configuration/Options package.

## Non-Goals

M0072 does not:

- add new semantic concepts;
- redesign System.Text.Json polymorphism;
- add System.Text.Json source-generation coverage;
- add JSON Schema polymorphism;
- change EF ownership/storage/relationship semantics;
- add cross-model inheritance;
- create combinatorial fixture generation;
- force every unit test to use Model A/B;
- retain obsolete fixture projects for compatibility;
- reintroduce Configuration/Options integration;
- publish packages;
- create a `5.0.1` tag;
- create a GitHub Release.

## Validation

### Focused implementation validation

Use repository `test-project` / `test-filter` launchers for affected projects during migration.

At minimum, final focused evidence must cover every test project materially migrated to Model A/B.

Do not rely solely on the aggregate check to prove that a specific migrated target still exercises the intended boundary.

### Repository gate

Run:

```powershell
.\eng\format.ps1
.\eng\check.ps1
.\eng\public-docs.ps1
.\eng\samples.ps1
```

Inspect formatter changes.

### Implementation package / consumer-surface gate

Before final stable-version validation, the executor may use:

```powershell
.\eng\package.ps1 5.0.1-m0072
.\eng\package-smoke.ps1 5.0.1-m0072
```

for implementation iteration.

Package smoke must consume the packages produced by the current pack operation through the intended local NuGet mechanism and must not accidentally use project references or stale/global packages.

### Final local release gate

After implementation, cleanup, documentation, and publication-channel preflight are complete, run:

```powershell
.\eng\release-check.ps1 5.0.1
```

This final aggregate invocation is the local 5.0.1 release-readiness authority.

Inspect `artifacts/nuget/` from that run and prove:

- exactly ten expected package IDs;
- every package version is exactly 5.0.1;
- expected package metadata/assets/readme are present;
- no removed Configuration package exists;
- package smoke consumed the current 5.0.1 artifacts.

### CI release gate

Run the repository manual `release-check` workflow against the final M0072 candidate commit with:

```text
version = 5.0.1
```

Require aggregate workflow success.

If CI cannot be invoked from the implementation environment but the branch/commit is otherwise ready, use the milestone terminal semantics based on whether this is an unavailable external capability versus an expected post-merge action. Do not fabricate CI success.

## Consumer-Surface Acceptance

Guide-system 0.7.4 applies.

The final packed 5.0.1 suite must be consumed from the artifacts produced by the current build through the isolated/local NuGet package mechanism.

Representative package acceptance must retain/prove at least:

- generated semantic model consumption;
- manifest/version alignment;
- representative Strong Scalar behavior;
- System.Text.Json automatic Entity polymorphism from M0071;
- representative projection behavior affected by fixture consolidation.

Detailed behavior remains in lower-level tests.

## Human Review

Applicability: none.

All milestone acceptance is deterministically testable through repository state, automated tests, package artifacts, public documentation, publication-channel verification, and CI release evidence.

## Completion

Before `COMPLETE`:

1. freshly reread this milestone;
2. reconcile every applicable obligation against `.execution/m0072.md`;
3. verify ledger claims against live repository state and concrete evidence;
4. continue resolving all agent-resolvable gaps;
5. perform the completion audit;
6. remove the completed execution ledger and milestone file according to repository lifecycle policy after durable authority is synchronized.

Audit at least:

- Model A dimension coverage;
- Model B independent composition coverage;
- migrated positive boundary tests;
- retained synthetic-test exceptions are purposeful;
- multiple-model behavior across applicable packages;
- obsolete fixture/builder cleanup;
- no unintended product-semantic change;
- 5.0.1 versioning decision preserved;
- release documentation synchronized;
- publication-channel preflight;
- exact ten-package inventory;
- local `release-check 5.0.1`;
- packed consumer-surface evidence;
- required CI evidence;
- no publication/tag/GitHub Release action.

Terminate only as:

```text
COMPLETE
AWAITING HUMAN REVIEW
BLOCKED
```

## Escalation Boundary

Return to planning only if implementation discovers that:

- a current supported positive package scenario cannot be expressed by the two projection-neutral generated fixture assemblies without target-specific semantic-model dependencies;
- preserving multiple-model support requires defining cross-model inheritance or another new semantic concept;
- deleting an old fixture project would remove a distinct required assembly/process boundary that cannot be represented by Model A/Model B plus target-local infrastructure;
- a migration-exposed product defect requires a new semantic/architecture/compatibility decision;
- 5.0.1 cannot satisfy the repository's stable-version compatibility policy without revisiting the explicit patch-line decision;
- the ten-package suite cannot produce valid 5.0.1 consumer artifacts without a new package/topology decision;
- required publication-channel state contradicts the planned 5.0.1 predecessor assumptions.

Do not escalate ordinary fixture type placement, test helper placement, project-reference cleanup, expected-output updates, test migration order, package-smoke edits, release-note wording, or resolvable validation failures.
