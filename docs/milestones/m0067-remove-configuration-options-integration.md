# M0067 — Remove Configuration / Options Integration

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-broad |
| Scope size | broad removal across package, acquisition metadata, engineering inventory, samples, tests, and current authority |
| Implementation autonomy | high within the resolved removal and compatibility contract |
| Repository role | capability-provider |
| Maturity | published-maintenance |
| Documentation sync | separate pass |
| Release readiness | separate pass |
| Human review | none |
| Recommended implementation branch | `codex/m0067-remove-configuration-options-integration` |

## Branch Boundary

Start from the latest `main`, then create and switch to `codex/m0067-remove-configuration-options-integration`. Perform all implementation work on that branch; do not commit M0067 implementation directly to `main`.

## Goal

Remove SemanticTypeModel's Microsoft.Extensions.Configuration / Microsoft.Extensions.Options capability completely while preserving the projection-neutral semantic concepts that remain independently useful.

The milestone reduces supported surface rather than replacing the removed integration with a new abstraction.

## Target State

When M0067 is complete:

1. `SemanticTypeModel.Configuration` is no longer a source project, package, solution member, package-inventory entry, package-smoke expectation, sample dependency, test area, or documented current capability;
2. the Configuration package domain model and runtime Options registration APIs no longer exist;
3. Configuration-specific .NET authoring contracts no longer exist, including section/presence, DataAnnotations-validation, startup-validation, and generated Options-registration policy attributes/types;
4. extraction/generation no longer emits or recognizes the `configuration.*` annotation namespace or Configuration/Options policy metadata;
5. no compatibility/tombstone package, obsolete API shim, forwarding type, or automatic replacement registration layer remains;
6. `SemanticTypeRole.Configuration` remains a supported projection-neutral semantic role and continues to extract/generate normally;
7. `SemanticRequiredWhen` remains a supported projection-neutral conditional constraint and retains existing JSON Schema behavior;
8. configuration-shaped fixture/sample types may remain where they still provide useful projection-neutral or JSON Schema coverage, but they carry no STM-owned Options metadata or behavior;
9. current architecture, terminology, specs, capability metadata, engineering policy, and tests no longer present Configuration/Options as an STM projection target;
10. historical release/history records may continue to state that the capability existed in older releases;
11. deferred consumer documentation clearly records the future migration requirement: applications use Microsoft.Extensions.Configuration / Microsoft.Extensions.Options directly after upgrading;
12. the change is treated as a breaking public/package removal for the next major release line; M0067 does not publish a release.

## Scope

### Capability removal

Remove the complete implementation surface whose purpose is STM-owned application Configuration/Options integration, including as applicable:

- the `SemanticTypeModel.Configuration` package and its package references;
- Configuration package domain-model and Options-registration behavior;
- Configuration package tests;
- Configuration-specific runnable samples;
- solution/package inventory and affected-area routing;
- package-smoke package-count/consumer assumptions;
- Configuration-specific capability-catalog entries;
- Configuration-specific public diagnostics and diagnostic expectations that have no remaining producer;
- now-unused Microsoft.Extensions.Configuration/Options dependency versions whose only consumer was the removed capability.

Implementation owns the concrete deletion/edit list and must discover all supporting references in the live repository rather than treating this list as an exhaustive allowlist.

### Remove Configuration-specific authoring policy

Remove public .NET authoring API and extraction behavior whose meaning exists only for the removed Options target, including the current equivalents of:

```text
SemanticConfigurationSectionAttribute
SemanticConfigurationSectionPresence
SemanticValidateDataAnnotationsAttribute
SemanticValidateOnStartAttribute
SemanticGenerateOptionsRegistrationAttribute
configuration.* canonical annotations
```

If the live repository contains additional public symbols or annotations whose only meaning is Configuration/Options registration, they are part of this removal.

Do not reinterpret those symbols as generic core semantics merely to preserve compatibility.

### Preserve projection-neutral semantics

Keep:

```text
SemanticTypeRole.Configuration
SemanticRequiredWhenAttribute / canonical RequiredWhen semantics
```

The `Configuration` role means only that the modeled type describes configurable behavior/settings. It does not imply a Configuration package, section path, binding source, named Options instance, Options validation, startup validation, service registration, or generated helper.

`RequiredWhen` remains a general semantic-validity constraint and continues to project to JSON Schema according to current JSON contracts.

### Repository authority cleanup

M0067 supersedes the old Configuration domain/Options projection authority.

The implementation must ensure current project truth no longer contains active Configuration/Options subsystem contracts. This includes updating/removing, as applicable:

- architecture target-pipeline descriptions;
- Configuration-specific terminology;
- projection-capability target lists/matrices;
- JSON representation attribute-classification material that lists removed Configuration-only attributes;
- diagnostics authority for removed diagnostics;
- package/sample/engineering authority;
- concise architectural history where the removal is useful context.

Do not retain superseded Configuration specs/decisions as archives. Git is history.

### Public documentation synchronization boundary

Consumer-documentation synchronization remains a separate pass according to the repository profile.

M0067 creates a focused `.guide-sync/pending/` hint. Ordinary implementation agents are not required to read that file.

The implementation may make direct public-doc edits only when required for repository validation or to avoid a mechanically broken current tree; broader consumer guidance and release migration wording belong to the deferred synchronization pass.

## Non-Goals

M0067 does not:

- replace `SemanticTypeModel.Configuration` with another STM configuration-binding package;
- create a generic options/configuration abstraction;
- add ASP.NET Core hosting integration;
- add a JSON-Schema-to-Options bridge;
- remove `SemanticTypeRole.Configuration`;
- remove or narrow projection-neutral `SemanticRequiredWhen` semantics;
- redesign JSON Schema validation or System.Text.Json fidelity;
- redesign Dependency Injection support unrelated to the removed package;
- change EF Core, Power BI, or System.Text.Json behavior merely because a modeled type has the `Configuration` role;
- publish packages or perform release-readiness synchronization;
- preserve source/binary compatibility for the removed package or public Configuration-specific authoring API.

## Resolved Architecture, Semantic, and Compatibility Decisions

### The role survives; the integration does not

`SemanticTypeRole.Configuration` is core semantic meaning and remains supported.

Microsoft.Extensions.Configuration / Microsoft.Extensions.Options behavior is application-framework integration and is removed from STM.

The durable rationale is `docs/decisions/configuration-role-does-not-imply-options-integration.md`.

### No compatibility shim

This is deliberate removal, not deprecation.

Do not retain:

```text
empty/tombstone SemanticTypeModel.Configuration package
obsolete AddSemanticOptions APIs
forwarding Configuration domain types
obsolete Configuration-specific attributes
compatibility annotation readers
```

Old released versions remain available through normal package history; the current source tree does not carry retired compatibility scaffolding.

### Major-version boundary

Removing a previously published package and public .NET authoring API is a breaking change.

Treat M0067 as preparation for the next major release line. Validation uses `5.0.0-m0067`; do not publish it.

No claim is made in this milestone about final 5.0 release readiness or release timing.

### Historical truth is not current authority

Historical release notes and useful `docs/HISTORY.md` evolution text may mention the former Configuration capability.

Current specs, decisions, terminology, architecture, package maps, samples, and consumer capability guidance must not continue to present the removed subsystem as supported after the appropriate implementation/documentation synchronization stage.

## Required Project Authority

The disconnected implementation agent starts with this milestone, then reads only the live repository authority needed for implementation:

- `AGENTS.md`;
- `docs/TERMINOLOGY.md`;
- `docs/SPECS.md`;
- `docs/specs/core-semantic-vocabulary.md`;
- `docs/specs/core-conditional-constraint-semantics.md`;
- `docs/specs/type-model-dotnet-attributes.md`;
- `docs/specs/type-model-dotnet-extraction.md`;
- `docs/specs/type-model-projection-capabilities.md`;
- `docs/specs/json-representation-fidelity.md` only where removed authoring attributes/classifications are referenced;
- `docs/ARCHITECTURE.md` and `docs/architecture/code-first-domain-projection-pipeline.md`;
- `docs/DECISIONS.md` and `docs/decisions/configuration-role-does-not-imply-options-integration.md`;
- `docs/ENGINEERING.md` and the command/package/sample engineering documents needed for validation and package inventory;
- live source/tests/samples/engineering code necessary to find the complete removal surface.

The old Configuration domain spec and the two superseded Configuration decisions are intentionally removed by the planning overlay and are not implementation authority.

`.guide-profile.json` is guide-selection metadata and is not required reading for ordinary implementation.

`.guide-sync/` is deferred documentation-sync metadata and is not required reading for ordinary implementation.

## Acceptance Criteria

M0067 is complete only when all applicable outcomes below are true.

### Product/package surface

- No publishable/source project named `SemanticTypeModel.Configuration` remains.
- No package inventory, solution, package-smoke, sample, or test routing expects that package.
- Packed output for `5.0.0-m0067` contains the remaining package suite and does not contain `SemanticTypeModel.Configuration.5.0.0-m0067.nupkg`.
- No production package retains a dependency that exists solely for the removed Configuration/Options capability.

### Public/current authoring surface

- Configuration-specific authoring attributes/types and `AddSemanticOptions`/Configuration domain APIs are absent from current production source.
- Current extraction/generation does not emit or consume `configuration.*` metadata.
- Repository-wide current-source/test/sample searches do not reveal hidden replacement/tombstone behavior.

### Preserved semantics

- `SemanticTypeRole.Configuration` still extracts/generates as the `Configuration` role without requiring the removed package.
- A Configuration-role modeled type can still participate in generic/canonical and JSON Schema scenarios supported by existing contracts.
- `SemanticRequiredWhen` behavior remains intact and existing JSON Schema conditional behavior continues to pass.

### Current project truth

- Active architecture/spec/decision/terminology/capability/engineering authority no longer treats Configuration/Options as an STM target integration.
- Superseded Configuration spec/decision files are absent rather than archived.
- Historical release/history material is not rewritten to pretend the capability never existed.
- Any current docs that must remain temporarily stale for the separate documentation-sync pass are covered by the M0067 guide-sync hint and do not block repository validation.

### Completion audit

After required validation succeeds, perform a milestone completion audit rather than stopping at green tests.

The audit must explicitly check:

1. every Target State item;
2. package/source/test/sample removal;
3. public authoring/API removal;
4. preserved `Configuration` role and `RequiredWhen` behavior;
5. current-authority cleanup;
6. package inventory and packed artifacts;
7. absence of tombstone/compatibility scaffolding;
8. required deferred documentation-sync metadata;
9. branch/worktree cleanliness and intended changes only.

If an unsatisfied item is agent-resolvable, continue implementation. Terminate only as `COMPLETE`, `AWAITING HUMAN REVIEW`, or `BLOCKED`.

## Validation

Development/validation for this milestone uses Windows PowerShell launchers for the canonical `eng` command interface.

### Tier 1 — focused validation

Run focused tests for the areas materially changed by the implementation, including at minimum the current equivalents of:

```powershell
.\eng\test-project.ps1 tests\unit\SemanticTypeModel.DotNet.Tests.Unit\SemanticTypeModel.DotNet.Tests.Unit.csproj
.\eng\test-project.ps1 tests\unit\SemanticTypeModel.JsonSchema.Tests.Unit\SemanticTypeModel.JsonSchema.Tests.Unit.csproj
.\eng\test-project.ps1 tests\unit\SemanticTypeModel.Engineering.Tests.Unit\SemanticTypeModel.Engineering.Tests.Unit.csproj
```

If live dependencies/changes show another affected test project, include it; the list above is a minimum, not an edit allowlist.

### Tier 2 — repository completion check

```powershell
.\eng\check.ps1
```

### Tier 3 — package and sample boundary

```powershell
.\eng\package.ps1 5.0.0-m0067
.\eng\package-smoke.ps1 5.0.0-m0067
.\eng\samples.ps1
```

Do not publish.

If implementation makes public-documentation edits required for mechanical repository consistency, also run:

```powershell
.\eng\public-docs.ps1
```

Validation success is necessary evidence but does not replace the completion audit.

## Human Review

Applicability: none.

The removal contract and preserved semantics are machine-verifiable. No subjective UI, generated visual output, or policy judgment remains for milestone completion.

The repository's default `ai-executed-human-reviewed` implementation mode does not create a separate human completion gate when this milestone explicitly declares none.

## Constrained Execution

No milestone-specific resumable/sharded suite is required.

Tier 1 commands provide the normal constrained inner loop. Tier 2 and Tier 3 are aggregate completion evidence and must succeed as whole commands in a capable environment. Partial child-command success is not aggregate success.

Package smoke is capability-provider validation of the packed remaining suite. No external capability-consumer product validation is required by M0067.

## Direct Documentation Impact

The planning package directly:

- adopts guide-system 0.7.1 execution-closure metadata;
- makes M0067 the active ready milestone;
- replaces the old Configuration projection rationale with the decision that the core role survives without Options integration;
- removes the old Configuration subsystem spec from the current reading map.

Implementation owns the supporting current-authority cleanup required by the acceptance criteria.

## Deferred Documentation Synchronization

Use `.guide-sync/pending/m0067-remove-configuration-options-integration.md` in the later documentation-sync pass.

That pass must remove current consumer guidance for the package/API and add major-version migration guidance without rewriting historical release truth.

## Escalation Boundary

Return to planning only if implementation discovers a material issue such as:

- some supposedly Configuration-specific API/annotation is required by another supported non-Options capability and cannot be removed without changing that capability contract;
- preserving `SemanticTypeRole.Configuration` would require keeping an STM-owned Options behavior path;
- removing the package would require a compatibility/tombstone mechanism despite the explicit no-shim decision;
- package removal reveals a persisted/public protocol whose compatibility cannot be resolved by ordinary source/API removal;
- the next-major compatibility classification would have to change;
- required validation cannot be made meaningful without changing repository-wide testing policy.

Do not escalate ordinary deletion/refactoring mechanics, broken references, test updates, package-count updates, documentation cleanup, or failures that can be fixed within this resolved contract.
