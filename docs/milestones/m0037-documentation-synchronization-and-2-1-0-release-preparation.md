# M0037: Documentation Synchronization and 2.1.0 Release Preparation

## Status

Completed.

## Completion Note

M0037 synchronized repository indexes and public release-preparation documentation for the 2.1.0 candidate, resolved its pending guide-sync hint, and passed `./eng/check-affected.sh README.md docs public-docs .github/copilot-instructions.md AGENTS.md`, `./eng/public-docs.sh`, and `./eng/release-check.sh 2.1.0`. Publication remains a human-approved follow-up outside this milestone.

## Goal

Prepare the repository documentation and release-readiness surfaces for a potential `2.1.0` release after the recent post-2.0 milestone work.

The milestone combines two execution concerns that should be handled together for this repository state:

```text
documentation synchronization
2.1.0 release preparation planning and validation
```

The milestone must make repository-local documentation consistent with implemented behavior and planned release scope without copying external guide documents, treating copied research guides as authority, or introducing broad methodology files.

## Repository Role and Maturity Assumptions

| Field | Assumption |
|---|---|
| Repository role | Product repository and capability provider for `SemanticTypeModel.*` packages. |
| Inferred profile | `dotnet-library`. |
| Project type | Public .NET library/package set. |
| Maturity mode | Post-2.0 public package set preparing a 2.1.0 release. |
| Capability-provider scope | Validate implemented SemanticTypeModel capabilities, package boundaries, public APIs, public docs, samples, and release commands. |
| Capability-consumer scope | Use samples and package-smoke only as bounded dogfood validation, not as unrelated application development. |

## Execution Mode

```text
documentation-sync + release-readiness
```

The implementation agent may read `.guide-sync/pending/` because this milestone explicitly performs documentation synchronization and release preparation.

Ordinary feature implementation agents remain exempt from reading `.guide-profile.json`, `.guide-sync/`, or the external guide repository.

## Scope

### In Scope

- Synchronize repository documentation indexes after M0033, M0034, M0035, and M0036 planning packages if they are present in the working tree.
- Verify and update milestone status, authority links, direct documentation impact, and deferred documentation notes for M0033 through M0036 according to actual implementation state.
- Update public documentation that is affected by implemented behavior from M0033 through M0036.
- Prepare `2.1.0` release documentation surfaces:
  - root `README.md`;
  - `public-docs/getting-started.md`;
  - `public-docs/installation.md`;
  - `public-docs/packages.md`;
  - `public-docs/api/compatibility.md`;
  - `public-docs/diagnostics.md` and detailed diagnostics pages when diagnostic IDs changed;
  - `public-docs/versioning.md`;
  - `public-docs/release-notes.md`;
  - package README sources under `public-docs/nuget/`;
  - sample index and sample pages under `public-docs/samples/`.
- Verify public docs distinguish supported 2.1.0 behavior from planned or not-yet-implemented behavior.
- Verify old copied setup/engineering guides under `docs/research/` are not treated as operational authority.
- Verify `AGENTS.md` and `.github/copilot-instructions.md` remain concise routing documents and do not duplicate external guide methodology.
- Verify `.guide-profile.json`, if present, remains metadata only and is not required for ordinary implementation agents.
- Verify `.guide-sync/pending/` hints are either resolved, kept as explicit future work, or removed from the ZIP if obsolete.
- Run documentation and release-readiness validation commands appropriate for a release-preparation milestone.

### Conditional Scope

Apply only when the repository state shows the related work exists:

| Condition | Work |
|---|---|
| M0033 implementation exists | Synchronize envelope projection policy documentation, package docs, samples, diagnostics, and release notes. |
| M0034 implementation exists | Synchronize ownership/evolution/lifecycle semantics documentation, package docs, samples, diagnostics, and release notes. |
| M0035 implementation exists | Synchronize current canonical model surface, System.Text.Json domain-model projection docs, public API compatibility notes, and samples. |
| M0036 guide-adoption package exists | Synchronize guide metadata and old copied-guide non-authority notes, but do not require ordinary agents to read guide metadata. |
| Public API changed | Update public API compatibility documentation and run public API validation. |
| Diagnostic IDs changed | Update diagnostics reference pages and run diagnostics/public-doc validation. |
| Samples changed | Update `public-docs/samples.md`, affected sample pages, and run package/sample validation. |

## Non-Goals

M0037 must not:

```text
implement new SemanticTypeModel behavior
modify production source code
modify test source code except through a separate implementation milestone
copy external guide documents into the repository
make external guide documents operational authority
introduce TBPs
introduce issue templates
introduce workflow YAML
introduce non-root README files
perform package publication
perform NuGet publishing
perform website publication unless a repository-local release command already owns it
invent completion status for milestones without verifying implementation state
```

## Focus Areas

### Focus Area 1 — Repository Authority and Index Synchronization

#### Intent

Make repository-local authority indexes reflect the current milestone/spec/decision set.

#### Required Authority

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

#### Work

- Update `docs/MILESTONES.md` to include M0033, M0034, M0035, M0036, and M0037 when corresponding milestone documents exist or are added by the current working set.
- Update `docs/SPECS.md` for new authoritative specs created by M0033 through M0035.
- Update `docs/DECISIONS.md` for new decisions created by M0033 through M0035.
- Verify `docs/TERMINOLOGY.md` contains all terms used by synchronized docs.
- Do not list external guide documents as repository authority.

#### Validation

Tier 0:

```sh
./eng/check-affected.sh docs/MILESTONES.md docs/SPECS.md docs/DECISIONS.md docs/TERMINOLOGY.md
```

### Focus Area 2 — Milestone Status and Impact Synchronization

#### Intent

Ensure M0033 through M0036 reflect actual repository state without inventing completion details.

#### Required Authority

```text
docs/milestones/m0033-envelope-projection-policies-and-ef-core-owned-payload-storage.md
docs/milestones/m0034-evolution-ownership-and-lifecycle-semantics.md
docs/milestones/m0035-remove-legacy-model-compatibility-and-align-system-text-json-projection.md
docs/milestones/m0036-adopt-external-agentic-project-guide-system-v0.3.0.md
```

Read each document only if it exists in the working tree.

#### Work

- Update status to `Completed` only when implementation evidence exists and validation status is known.
- Keep status as `Planned` or `In Progress` when source implementation or validation cannot be verified.
- Replace stale direct documentation impact notes with completion notes when documentation was synchronized.
- Preserve deferred documentation notes only when follow-up work truly remains.
- Keep milestone bodies as routers; do not duplicate full spec content.

#### Validation

Tier 0:

```sh
./eng/check-affected.sh docs/milestones
```

### Focus Area 3 — Public Documentation Synchronization

#### Intent

Make consumer-facing documentation describe the actual supported behavior for the 2.1.0 candidate.

#### Required Authority

```text
docs/PUBLIC-DOCS.md
README.md
public-docs/
docs/engineering/public-documentation.md
```

Read affected specs when a public doc describes target-specific behavior:

```text
docs/specs/envelope-projection-policies.md
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/current-canonical-model-surface.md
docs/specs/system-text-json-contract-integration.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
```

#### Work

- Update root `README.md` only for user-first release/package behavior, not internal planning mechanics.
- Update package docs under `public-docs/nuget/` for affected packages.
- Update target guides under `public-docs/guides/` for implemented behavior.
- Update sample docs to match runnable package-based samples.
- Remove or rewrite stale public claims that JSON Schema import is the primary authoring path.
- Mark planned behavior as planned only when it is not implemented.
- Do not expose guide-system implementation mechanics in public docs.

#### Validation

Tier 0:

```sh
./eng/public-docs.sh
```

### Focus Area 4 — 2.1.0 Release Notes, Versioning, and Compatibility Preparation

#### Intent

Prepare release-facing docs for a 2.1.0 candidate without publishing.

#### Required Authority

```text
public-docs/release-notes.md
public-docs/versioning.md
public-docs/api/compatibility.md
docs/engineering/release-readiness.md
docs/engineering/packaging.md
```

#### Work

- Add or update a `2.1.0` section in `public-docs/release-notes.md` using verified implementation facts.
- Document breaking changes only when public API validation confirms them.
- Document known migration notes for removed legacy compatibility surfaces when implemented.
- Ensure version examples across README and public docs consistently target the 2.1.0 candidate only when the release branch/versioning policy expects it.
- Do not claim packages are published until publication happens.

#### Validation

Tier 0:

```sh
./eng/public-docs.sh
```

Tier 3 release candidate validation before declaring release readiness:

```sh
./eng/release-check.sh 2.1.0
```

### Focus Area 5 — Release Candidate Validation and Human Review

#### Intent

Run the repository-local release gate and collect human-review checkpoints before publishing.

#### Required Authority

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/release-readiness.md
```

#### Work

- Run the release-readiness command without publishing:

```sh
./eng/release-check.sh 2.1.0
```

- Capture failures as follow-up work, not as silent exceptions.
- If validation fails because of source/test implementation issues, stop release readiness and document the failing area.
- Require human review for:
  - final release notes wording;
  - breaking-change classification;
  - public API compatibility documentation changes;
  - package version selection;
  - any decision to publish.

## Implementation Constraints

- Target repository documentation must contain project truth only.
- External guide documents may inform this milestone planning but must not be copied into the repository.
- Do not make implementation agents read the external guide repository.
- Treat `docs/research/project-setup-guide-*.md` and `docs/research/engineering-guide-*.md` as legacy/non-authoritative unless a current repository-local authority document explicitly says otherwise.
- Keep public docs consumer-oriented.
- Keep index documents short and navigational.
- Preserve non-root README avoidance.
- Do not introduce TBPs or issue templates.
- Keep provider/consumer distinction clear: this repository validates provider capabilities; samples are bounded dogfood consumers.

## Required Authority Documents

The implementation agent starts here:

```text
docs/milestones/m0037-documentation-synchronization-and-2-1-0-release-preparation.md
```

Always read:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/public-documentation.md
docs/engineering/release-readiness.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
```

Read when affected files exist or the focus area requires them:

```text
.github/copilot-instructions.md
.guide-sync/pending/
docs/specs/envelope-projection-policies.md
docs/specs/evolution-ownership-and-lifecycle-semantics.md
docs/specs/current-canonical-model-surface.md
docs/specs/system-text-json-contract-integration.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/type-model-ef-core-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/decisions/envelope-projection-policies-are-target-specific.md
docs/decisions/evolution-semantics-remain-projection-neutral.md
docs/decisions/remove-legacy-model-compatibility-and-hardened-terminology.md
docs/milestones/m0033-envelope-projection-policies-and-ef-core-owned-payload-storage.md
docs/milestones/m0034-evolution-ownership-and-lifecycle-semantics.md
docs/milestones/m0035-remove-legacy-model-compatibility-and-align-system-text-json-projection.md
docs/milestones/m0036-adopt-external-agentic-project-guide-system-v0.3.0.md
README.md
public-docs/
```

Do not read the external guide repository during implementation.

## Files or Areas Likely Affected

```text
README.md
AGENTS.md only if routing currently contradicts M0037 constraints
.github/copilot-instructions.md only if routing currently contradicts M0037 constraints
docs/TERMINOLOGY.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/PUBLIC-DOCS.md
docs/milestones/m0033-*.md
docs/milestones/m0034-*.md
docs/milestones/m0035-*.md
docs/milestones/m0036-*.md
docs/specs/*.md affected by M0033-M0035
public-docs/**/*.md
.guide-sync/pending/*.md
```

## Validation Tiers and Concrete Commands

### Tier 0 — Documentation validation

Use during documentation edits:

```sh
./eng/check-affected.sh README.md docs public-docs .github/copilot-instructions.md AGENTS.md
./eng/public-docs.sh
```

### Tier 2 — Repository check

Use if documentation edits reveal source/test drift or if repository policy requires full validation before release-readiness work:

```sh
./eng/check.sh
```

### Tier 3 — Release readiness

Required before M0037 can be considered release-ready:

```sh
./eng/release-check.sh 2.1.0
```

Do not run Tier 4 publish validation in this milestone unless a human explicitly changes the scope to publication.

## Acceptance Criteria

- `docs/MILESTONES.md` indexes M0037 and any prior milestone documents present in the working tree.
- `docs/SPECS.md` indexes authoritative specs created by M0033 through M0035 when present.
- `docs/DECISIONS.md` indexes decisions created by M0033 through M0035 when present.
- Public documentation reflects supported behavior and does not describe planned behavior as shipped.
- Public docs no longer position JSON Schema import as the primary authoring path.
- System.Text.Json docs distinguish current resolver behavior from M0035 realignment if not yet implemented.
- Release notes contain a fact-based 2.1.0 candidate section or explicitly state that release notes are blocked by missing implementation evidence.
- `AGENTS.md` and `.github/copilot-instructions.md` remain concise and do not require ordinary implementation agents to read external guide docs, `.guide-profile.json`, or `.guide-sync/`.
- `docs/research/` guide copies are not referenced as operational authority.
- No TBPs, issue templates, workflow YAML, source files, test files, generated code, or copied guide documents are introduced.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.1.0` passes before declaring release readiness.
- Human review items are recorded and resolved or explicitly deferred.

## Direct Documentation Impact

This milestone directly owns synchronization for:

```text
repository index documents
milestone status and completion notes
public documentation surfaces
package README sources
sample documentation
compatibility notes
versioning guidance
release notes
pending guide-sync hint resolution
```

## Deferred Documentation Synchronization Hints

Use existing `.guide-sync/pending/` files when present.

This planning package adds:

```text
.guide-sync/pending/m0037-documentation-sync-and-2-1-0-release-preparation.md
```

Preserve deferred hints only when follow-up work truly remains after M0037. Delete or mark resolved hints when M0037 completes them.

## Human Review Requirements

Human review is required before declaring 2.1.0 release readiness for:

- final release notes;
- compatibility and breaking-change classification;
- public API compatibility documentation changes;
- NuGet package version selection;
- sample scope and dogfood boundaries;
- any decision to publish packages;
- any decision to leave known public-doc or release-check failures unresolved.

## Out-of-Scope Guide Migration Work

M0037 does not perform guide-system migration. If M0036 has not been applied, apply or explicitly defer M0036 separately.

M0037 may read `.guide-sync/pending/` because its execution mode is documentation synchronization and release readiness, but it must not make the external guide repository operational authority for the target repository.
