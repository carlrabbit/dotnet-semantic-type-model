# M0085 — EF Nested JSON Dictionaries and JSON Schema Contract Clarification (6.1.1 Patch Preparation)

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | Project-configured GPT-5.6 Luna |
| Baseline executor readiness / decision preservation | confirmed |
| Execution ledger | `.execution/M0085.md` (planning-seeded) |
| Scope | focused multi-package correctness + release candidate |
| Implementation autonomy | high, within resolved contracts |
| Documentation synchronization | direct for touched public/authority surfaces |
| Focused / repository validation | Tier 1 affected projects / Tier 2 `eng/check` |
| Consumer/release validation | Tier 3 `eng/release-check 6.1.1` |
| Validation locus | Local Windows or Linux with .NET 10 and SQLite; GitHub manual release-check workflow at candidate commit |
| Environment / infrastructure | Use existing SDK, provider and GitHub workflows; no host-wide provisioning or workflow/infrastructure changes authorized |
| Human review | blocking before milestone acceptance/release authorization |
| Repository role / maturity | capability-provider / published-maintenance; dotnet-library profile (project metadata currently guide 0.7.4; external planning guide 0.9.3) |

## Goal

Correct an EF projection false rejection of ordinary dictionaries nested within JSON-converted structural values; clarify existing JSON Schema root and conditional-presence contracts from issues [#129](https://github.com/carlrabbit/dotnet-semantic-type-model/issues/129) and [#130](https://github.com/carlrabbit/dotnet-semantic-type-model/issues/130); prepare, validate, and submit an aligned **6.1.1** patch release candidate without publishing it.

## Target State

A direct `Dictionary<string,string>` property within a semantic ValueObject already stored as an owned JSON value round-trips through generated EF configuration and SQLite with no dictionary-storage diagnostic. Ordinary dictionary properties are not confused with JSON extension data. The JSON Schema API's existing root/`$defs` contract and `SemanticRequiredWhen` conditional-presence contract are unambiguous and verified. All current packable STM packages form a clean, locally consumable 6.1.1 release candidate. Publication remains a distinct human-approved operation.

## Scope

- EF provider-neutral `DeriveEfRelationalModel` validation and compile-time generator parity for nested dictionary-in-JSON ValueKinds.
- Real annotated CLR fixture, generated manifest/configuration, final EF model, SQLite persistence and change tracking, and packed NuGet consumer boundary.
- Existing JSON Schema `Root`/`Definitions` selection, root exclusion, preserved references, and consumer re-rooting documentation (#129).
- Existing `SemanticRequiredWhen` equality conditional: presence vs nullable value vs variant exclusivity; typed enum/boolean/numeric literals; source-property presence guard; JSON Schema validation and stated System.Text.Json fidelity boundaries (#130).
- Necessary specification, consumer guide, troubleshooting/diagnostic, compatibility and release-note corrections; aligned version/package metadata and Tier 3 non-publishing release candidate checks.
- Resolve stale claims that 6.1.0 is unpublished against independently verified publication-channel evidence (NuGet listed 6.1.0 on 2026-11-16; recheck at release gate).

## Non-goals

- No direct `[SemanticOwned]` dictionary-on-Entity storage extension or new dictionary ownership semantics. Existing unsupported direct shape diagnostics remain in force unless already supported under extension-data semantics.
- No new JSON Schema root-selection public API, `$defs` duplication of the root, or JSON Schema import/reverse modeling.
- No stronger `SemanticRequiredWhen` semantics: it does not imply non-null, exactly-one variant, mutual exclusion, or enforcement by System.Text.Json/EF Core/Power BI.
- No new canonical semantics, new module dependencies, broad rewrites, or changes to established serializer policy.
- No NuGet publication, Git tag, GitHub Release, or CI/workflow/infrastructure modification.
- No requirement to backport to 6.0.0 or add cross-version manifest compatibility.

## Decisions and Constraints

1. A dictionary **nested within an already JSON-converted ValueObject** is JSON content, not an independent relational property. For supported CLR shapes, it requires neither `schema.extensionData` nor `[SemanticOwned]` on the nested dictionary. Preserve ordinary JSON object member naming; extension-data semantics continue to flatten and have distinct canonical meaning.
2. Explicit ownership on the outer structural value determines the EF JSON column; `SemanticOwned` is projection-neutral lifecycle semantics, not an automatic EF `OwnsOne` mapping. Keep entity-role, unsupported nested-entity and other shape safeguards; do not merely suppress all dictionary diagnostics.
3. Nested dictionaries must be supported only where the actual JSON converter can faithfully serialize and deserialize key/value types under the established policy; `Dictionary<string,string>` is mandatory. Unsupported shapes must have a deterministic diagnostic. Do not infer arbitrary serializer compatibility from uninitialized-object serialization probes.
4. EF runtime relational inspection and generated configuration must agree on supported mapping and diagnostics. Required/nullable outer JSON property handling and deep value-comparison behavior remain intact.
5. `RequiredWhen` is conditional **presence**. `if` must require the discriminator source property, then require the target property; a present null may remain valid if target schema permits null; unrelated variants remain permitted. Equality uses normalized typed canonical literals and emitted names. Unsupported operators must report diagnostics rather than vanish.
6. Preserve the sibling-projection boundary: JSON Schema represents semantic validation; System.Text.Json performs serialization, not general semantic validation; EF and Power BI do not implicitly enforce conditional validity. The one-way representation-fidelity claim is bounded by canonical semantic validity and the specified baseline/exclusions; explain rather than weaken the rules.
7. JSON Schema `Root` plus `Definitions` together represent projected types. The selected root is intentionally excluded from `Definitions`. Root is selected by matching model ID, otherwise ordinal first TypeId, subject to explicit envelope policy. Do not expose misleading claims that `Definitions` contains every canonical type; retain resolvable references and deterministic export.
8. A patch release must not expand public APIs or canonical semantics. If a real compatibility or unsupported-shape requirement conflicts with these decisions, stop and escalate rather than silently turning this into minor-version feature work.
9. 6.1.1 is the intended next patch after externally published 6.1.0. Verify **every expected package ID** and the target's absence on NuGet, plus GitHub release/tag truth, immediately before final readiness. Repository text alone is not publication evidence. Never publish automatically.
10. Use exact version alignment across packages, manifests, generated consumers, samples and release documentation. A final candidate is a concrete commit and packed artifact set, not prior green branch tests.

## Required Authority

- `AGENTS.md`, `docs/MILESTONES.md`, `docs/ENGINEERING.md`, `docs/engineering/command-contract.md`
- `docs/specs/ef-core.md`, `docs/engineering/ef-core-testing.md`, `docs/decisions/ef-ownership-uses-target-role-and-storage-policy.md`
- `docs/specs/core-conditional-constraint-semantics.md`, `docs/specs/typed-literals-and-conditional-constraint-semantics.md`, `docs/specs/json-schema-domain-model-and-export.md`
- `docs/specs/json-representation-fidelity.md`, `docs/architecture/code-first-domain-projection-pipeline.md`
- `docs/PUBLIC-DOCS.md`, `docs/engineering/release-readiness.md`, `docs/engineering/packaging.md`, `docs/WORKFLOWS.md`
- Issues #129 and #130 are discovery inputs, not overriding product authority.

## Acceptance Criteria

- **AC-01** — Relational derivation accepts ordinary nested `Dictionary<string,string>` inside an owned JSON ValueObject without `EF_DICTIONARY_STORAGE_NOT_SUPPORTED` and classifies only the outer member as its JSON column.
- **AC-02** — Unsupported direct dictionary-on-Entity storage and unsupported nested JSON shapes retain deterministic, targeted diagnostics; nested ordinary dictionaries do not gain extension-data flattening semantics.
- **AC-03** — Real annotated CLR → generated manifest → EF generator chain compiles for the exact reported nested dictionary shape, preserving nullable/required outer owned JSON properties.
- **AC-04** — Finalized EF model exposes the intended JSON converter/value comparer and property nullability for representative owned JSON dictionaries.
- **AC-05** — SQLite save/reload and update tracking preserve ordinary dictionary entries, empty dictionaries, string escaping, null/empty values as supported, replacement and in-place mutation of the outer JSON value, and nullable outer transitions.
- **AC-06** — JSON Schema export verifies source-presence-guarded equality `if/then/required` with correct typed enum, boolean, numeric comparisons, including condition false/missing, target missing, target null when nullable, and a second populated variant.
- **AC-07** — JSON Schema derivation/export verifies the existing root-selection and `Root`/`Definitions` partition contract; references remain valid for root-as-reference and downstream re-rooting patterns, including envelope policy where applicable.
- **AC-08** — Cross-module tests/documentation affirm canonical semantic ownership, JSON Schema validation vs System.Text.Json output representation, and no implicit conditional-validation enforcement in EF/Power BI; guarantee wording distinguishes invalid semantic values from valid serializer output.
- **DOC-01** — Current specs and JSON Schema/core/EF consumer guides explicitly document dictionary nesting, direct dictionary limitations, conditional presence/non-null/exclusivity differences, root/`$defs` completeness and recommended re-rooting; consumer failure modes and relevant diagnostics are discoverable.
- **DOC-02** — Compatibility/versioning/release notes accurately describe published 6.1.0 and the proposed 6.1.1 fixes, with no stale “6.1.0 unpublished candidate” claims in active consumer/current-state guidance; single NuGet README and samples routing remain consistent.
- **REL-01** — Independently verify publication-channel previous stable, expected package IDs, 6.1.1 availability, and release/tag state; retain dated evidence and any discrepancy resolution before release approval.
- **REL-02** — Exactly aligned 6.1.1 package artifacts contain expected IDs/framework/assets/dependencies/shared README, and current packages succeed in isolated package-smoke consumer without stale/global/project-reference fallback.
- **REL-03** — The complete canonical non-publishing release-check passes for the final candidate commit/artifacts, with samples and public docs; retain independent GitHub manual release-check workflow evidence if available per repository authority.
- **REV-01** — Human reviews the patch-only semantic/compatibility decisions, diagnostic behavior, candidate artifact inventory, notes and release proof; review explicitly approves or rejects milestone acceptance. This does not authorize publishing.

## Acceptance Evidence Topology

| ID | Parent | Distinct required proof path |
|---|---|---|
| EC-03a | AC-03 | Nullable owned JSON outer property generated from real annotated CLR |
| EC-03b | AC-03 | Required owned JSON outer property and manifest/generator parity |
| EC-05a | AC-05 | Insert/reload and content preservation with nonempty/empty dictionaries |
| EC-05b | AC-05 | Mutating/replacing persisted content recognized by value comparer |
| EC-05c | AC-05 | Nullable outer state transitions null → value → changed value → null |
| EC-06a | AC-06 | Equality condition true vs false vs absent discriminator and target presence |
| EC-06b | AC-06 | Typed enum/boolean/numeric equality, nullable target, independent variant |
| EC-07a | AC-07 | Selected root excluded from definitions; root and references resolve |
| EC-07b | AC-07 | Re-rooted consumer document and envelope-root policy keep resolvable references |
| EC-12a | REL-02 | Package ID/version/metadata/README inspection from final packed artifacts |
| EC-12b | REL-02 | Isolated installed-package consumer exercises generated EF path |

## Validation Gates

| ID | Depth | Target / locus | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|
| VAL-01 | Tier 1 | EF projection / generation / final model; local .NET 10 | Platform-native `eng/test-project` or `eng/test-filter` for affected EF tests | AC-01, AC-02, EC-03a, EC-03b, AC-04 | focused passing test report and compiled generated source |
| VAL-02 | Tier 1 / integration | EF Core SQLite provider; local Windows or Linux .NET 10 | Platform-native `eng/test-project` for EF integration test project | EC-05a, EC-05b, EC-05c | real save/reload/change tracking evidence |
| VAL-03 | Tier 1 | JSON Schema + semantic model fixtures; local .NET 10 | Platform-native `eng/test-project` for JSON Schema test project and relevant cross-package tests | EC-06a, EC-06b, EC-07a, EC-07b, AC-08 | exported JSON + Draft 2020-12 validator assertions |
| VAL-04 | Tier 0 | Touched docs; local Windows/Linux | `./eng/public-docs.sh` or `.\eng\public-docs.ps1` | DOC-01, DOC-02 | docs check pass, current links and version check |
| VAL-05 | Tier 2 | Whole repository; local Windows/Linux | `./eng/check.sh` or `.\eng\check.ps1` | AC-01–AC-08, DOC-01, DOC-02 | current repo check pass; not a substitute for behavior-specific evidence |
| VAL-06 | External preflight | NuGet/GitHub publication channels; network-capable locus | Inspect official package versions per expected ID and GitHub releases/tags; record dated links | REL-01 | verified version inventory, no target collision |
| VAL-07 | Tier 3 | Final 6.1.1 packed suite; local .NET 10 | `./eng/package.sh 6.1.1` + `./eng/package-smoke.sh 6.1.1` (or .ps1) | EC-12a, EC-12b | fresh artifacts, inventory and isolated package-smoke pass |
| VAL-08 | Tier 3 | Final candidate commit; local .NET 10 | `./eng/release-check.sh 6.1.1` or `.\eng\release-check.ps1 6.1.1` | REL-03, REL-02, DOC-02 | complete aggregate successful release gate, samples, docs, package checks |
| VAL-09 | Tier 3 independent | Same final candidate SHA; existing GitHub workflow | Manually run existing release-check workflow with exact 6.1.1 input and capture run URL | REL-03 | passing workflow run at candidate SHA; report BLOCKED if unavailable |
| VAL-10 | Human | Candidate artifacts and milestone evidence | Recorded milestone-scoped review decision; no fabricated review script | REV-01 | explicit human approval/waiver decision |

Tier 1 checks should run during implementation as relevant. VAL-05 after affected features stabilize. VAL-06 before final release assertions; VAL-07–VAL-09 only against frozen final candidate inputs, not prematurely during feature implementation. If outputs change, invalidate affected evidence. No unapproved provisioning or workflow changes to bypass missing capabilities: record BLOCKED and escalate. Validation stages are not interchangeable. The existing `release-check` is the non-publishing aggregate; do not interpret child output of a failed aggregate as success.

## Human Review

- Applicability: blocking; reviewer: repository owner/authorized human.
- Review ID: `M0085-RELEASE-REVIEW`; class: semantic compatibility + release readiness.
- Review inputs: all criterion evidence, final commit SHA, package inventory, NuGet/GitHub version preflight, diagnostic/contract comparison, release notes, aggregate gate and independent workflow receipt.
- Decisions: APPROVED or CHANGES_REQUESTED; no silent waiver or implied publication approval. If required evidence is unavailable, report BLOCKED instead of closing the milestone.
- Use a milestone-scoped record in the execution ledger/review evidence. No assumption that an `eng/review-check` launcher exists.

## Documentation Impact

Update only directly affected existing specs, consumer guides, troubleshooting/diagnostics, compatibility and release notes (and shared NuGet README if the public package entry contract changes). Issues #129/#130 may be closed with links to actual completed evidence after implementation; planning does not close them.

## Deferred Documentation Synchronization

None intended: project profile selects direct sync for touched surfaces. If an unexpected broad cleanup is discovered, record a narrowly scoped `.guide-sync/pending/` hint rather than expanding this patch.

## Completion / Closure

Implementation owns work packages, source/test edits, evidence, and execution-ledger progression. Before completion, reread this milestone, verify exact obligation/evidence-case correspondence with `.execution/M0085.md`, and reconcile every row to live repository/validation evidence. Preserve compact final evidence below before deleting the completed milestone/ledger per `docs/ENGINEERING.md`; Git history retains them. Do not mark completion based on green CI alone.

## Completion Evidence

_To be populated only by implementation after all required gates and human approval._

## Escalation Boundary

Return to planning for a new decision about public API expansion, direct-Entity dictionary support, changed canonical meaning, strengthened RequiredWhen semantics, breaking compatibility, alternative validation targets, infrastructure changes, or any version/publication collision. Otherwise implementation mechanics are executor-owned.
