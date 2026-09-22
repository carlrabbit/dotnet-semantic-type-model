# Milestone — M0084 Coordinated TestData Generation

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Scope size | medium-large |
| Implementation autonomy | high within resolved coordination semantics |
| Documentation sync | direct-for-touched-surfaces |
| Focused validation | Tier 1 TestData coordination/generation tests |
| Repository validation | Tier 2 `check` |
| Integration validation | Tier 3 packed-package sample and release-candidate validation |
| Validation locus/platform | local or CI on supported Windows/Linux; no external service/runtime required |
| Consumer/release validation | `release-check 6.1.0-m0084`; no publish |
| Human review | none as a milestone completion gate; normal PR review remains expected |

## PR 127 Hardening / Reconciliation Context

M0084 has been implemented on pull request 127 but is **not yet merged**.

Observed implementation head during hardening planning:

```text
PR: 127
branch: feat/m0084-coordinated-testdata
head: cf973a9ed087f444b41d591dcd8b1e1fb30e0db7
```

The branch is not considered complete M0084 implementation until this hardening pass reconciles the live implementation and tests against the already-authoritative M0084 specifications.

This is not a new product milestone and does not reopen settled M0084 semantics.

The hardening pass exists because branch inspection found contract gaps that current focused tests do not exercise.

### Confirmed hardening gaps to resolve

The implementation must correct all of the following against existing project authority:

1. **Scalar/enum source parity**
   - coordinated and profile-supplied enum candidates must participate in the documented source pipeline rather than being bypassed by direct Random enum selection;
   - at minimum cover TestData Profile weighted enum values, derived enum values, sequence enum values, and applicable explicit programmatic enum values;
   - enum candidates must be validated against declared enum values and fail closed when invalid.

2. **Coordinated profile composition**
   - composition must overlay coordinated rule fields according to the profile composition contract rather than replacing the entire coordination object accidentally;
   - supported combinations created across separate profile layers, such as `Derived + Unique`, must survive composition;
   - contradictory combinations introduced only after composition must be rejected;
   - the final composed coordinated rule set and dependency graph must be revalidated;
   - dependency cycles that arise only after profile composition must be rejected before generation.

3. **Inherited effective dependencies**
   - profile validation must use the same effective inherited/composed property semantics as runtime object generation;
   - a valid derived property may depend on an inherited scalar/enum sibling when the declaring effective object contract permits it;
   - an exact-property rule must still reject a dependency that exists only on a more-derived containing type when the target property's declaring effective object does not contain it.

4. **Coordination diagnostics**
   - derived/sequence callback exceptions must surface through the documented `TESTDATA_COORDINATION_*` diagnostic family rather than being serialized as ordinary candidates and later reported as generic custom-candidate failures;
   - null coordinated-producer results must be diagnosed directly according to the coordination contract;
   - canonically invalid coordinated candidates must report coordinated-candidate failure rather than falling through to a lower source;
   - undeclared dependency reads and unavailable required dependencies must surface their documented coordination diagnostic categories.

5. **Dependency context type semantics**
   - `TryGet<T>` returns `false` only for an omitted or explicit-null declared dependency;
   - requesting an incompatible `T` is a programmer/configuration error and must not be reported as ordinary absence.

6. **Focused test coverage**
   - the three current M0084 focused tests are insufficient closure evidence;
   - the hardening acceptance matrix below must be represented by focused automated tests, with existing M0082/M0083 tests retained as regression evidence.

### Hardening constraints

- Do not redesign the Generation Session architecture; the invocation-scoped Root/Batch design remains authoritative.
- Do not broaden M0084 into coherent datasets, relationship inference, arbitrary property paths, persistent/global state, or faker-provider work.
- Prefer correction and consolidation over new public surface.
- If satisfying an existing contract requires a small public API correction, preserve source compatibility where practical because 6.1.0 has not yet been released.
- Do not treat the existing green PR `ci/check` result or the PR description's validation claims as sufficient hardening evidence; validation must be rerun on the hardened head.
- Before final hardening validation, update the PR branch to the current `main` base so the validation result applies to the actual merge candidate.

## Goal

Add deterministic stateful coordination within one TestData generation invocation so exact scalar/enum properties can derive values from sibling properties, generate scoped sequences, share values within Root/Batch scopes, and enforce TestData-only scoped uniqueness without adding canonical relationship semantics or persistent generator state.

M0084 also introduces the Generation Session architecture required to make `GenerateMany` a coherent Batch rather than a series of independent roots.

## Target State

When M0084 is complete:

1. TestData Profiles can configure exact scalar/enum property rules for derived values, sequences, shared values, and scoped uniqueness.
2. `Root` and `Batch` are the only public coordination scopes.
3. Every `Generate`/`GenerateMany` call owns one fresh isolated Generation Session.
4. `GenerateMany` shares Batch state across its roots and resets Root state per root.
5. derived-value dependencies are explicit, same-object, dependency-ordered, and cycle-validated;
6. sequence indexes, shared caches, and uniqueness sets follow the precise semantics in `docs/specs/test-data-coordination.md`;
7. source precedence remains compatible with existing programmatic generators, profile weighted values, terminology, and built-in generation;
8. M0082/M0083 behavior is preserved for profiles without coordinated rules;
9. the existing programmatic-model catalog contains a package-based `coordinated-generation` scenario;
10. public documentation clearly distinguishes invocation-scoped TestData coordination from canonical keys/relationships and later coherent datasets.

## Scope

### A. Generation Session

Implement the Generation Session boundary from `docs/architecture/test-data-generation-pipeline.md` and `docs/decisions/testdata-coordination-state-is-invocation-scoped.md`.

One session exists per public `Generate`/`GenerateMany` invocation. The session owns Batch state and per-root Root state. Do not store mutable coordination state on reusable profiles/facades or globally. `GenerateMany` must share one session across root ordinals.

### B. Public coordination vocabulary

Add the equivalent public capability:

```csharp
TestDataValueScope.Root
TestDataValueScope.Batch

.Property(target).From(dependencies, callback)
.Property(target).Sequence(scope, index => value)
.Property(target).Shared(scope)
.Property(target).Unique(scope)
```

Exact helper/builder type names and overload mechanics are implementation-owned. The semantics and eligibility rules in `docs/specs/test-data-coordination.md` are not implementation-owned.

### C. Derived values and dependency planning

Support explicit sibling dependency graphs within one effective object instance, including declared scalar/enum dependencies only, same-object boundary, inherited effective properties where valid, dependency chains, stable topological execution, cycle/self-dependency validation, undeclared-dependency access prevention, absent/null dependency observation, and canonical validation of derived results.

Do not add arbitrary property paths or cross-object/root reads.

### D. Sequences

Support zero-based per-rule sequences with Root or Batch state.

Indexes advance only when the sequence producer is actually invoked for a present non-null occurrence and is not shadowed by a higher-precedence programmatic generator.

Sequence results remain canonical candidates and fail closed when invalid.

### E. Shared values

Support Root/Batch shared value caching for exact properties.

Only the first present non-null occurrence resolves and stores the value. Later present non-null occurrences reuse it without reinvoking upstream value sources. Presence/null decisions remain occurrence-local and happen before sharing.

### F. Scoped uniqueness

Support Root/Batch uniqueness for present non-null values of one exact property.

Retry declarative sources deterministically where `test-data-coordination.md` declares them retryable. Do not automatically reinvoke imperative programmatic callbacks, derived callbacks, or sequence callbacks to repair collisions. Exhaustion is diagnostic, never silent duplication.

### G. Source integration and rule validation

Implement the resolved precedence:

```text
programmatic property generator
-> programmatic Logical Type generator
-> exact-property coordinated producer
-> property profile weighted values
-> Logical-Type profile weighted values
-> property terminology
-> Logical Type terminology
-> built-in generation
```

Presence/null sampling precedes source resolution. `Shared`/`Unique` are coordination modifiers around resolved values. Reject contradictory profile rule combinations and revalidate the final graph after profile composition.

### H. Executable documentation

Extend `samples/programmatic-model-catalog/` with one stable scenario:

```text
coordinated-generation
```

The sample must visibly demonstrate several generated roots, same-object derived values, a dependency chain, Batch sequence, Batch shared value, Batch uniqueness, fixed seed, deterministic semantic-value inspection, and assertions that fail on regression.

Do not create another sample project or per-scenario Markdown page.

## Non-goals

M0084 does not add canonical TestData annotations or relationships, default/object/Logical-Type coordinated rules, whole-object/array/dictionary coordinated producers, arbitrary path expressions, parent/child or cross-root dependencies, heterogeneous/multi-root-type dataset orchestration, foreign-key/reference binding, automatic Logical-Type matching as relationships, process/global/thread-static/persistent sequence or shared state, caller-named sessions, profile serialization/persistence, domain faker providers, invalid/adversarial generation, time-series/workflow generation, regex synthesis, or stable 6.1.0 publication.

Coherent datasets remain a later explicit capability.

## Decisions and Constraints

- 6.0.0 remains the released stable baseline; M0084 targets the 6.1.0 development line.
- M0083 is completed implementation history and its durable authority remains current.
- Coordination is TestData runtime policy, not canonical semantics.
- A Generation Session is invocation-scoped and ephemeral.
- `Generate` is one Batch containing one Root.
- `GenerateMany` is one Batch containing all generated roots in ascending root ordinal.
- `Root` and `Batch` are the only coordination scopes.
- Reusing the same configured facade/profile for another call starts completely fresh coordination state.
- Concurrent calls through one facade/profile do not share coordination state.
- Coordinated rules are exact-property rules only and target scalar/enum properties only.
- Derived dependencies are explicit scalar/enum sibling properties in the same effective object instance.
- A callback may read only dependencies it declared.
- The target property's declaring effective object must contain every dependency so an exact-property rule is valid in every containing context where the target can occur.
- Stable topological planning preserves existing effective property order among unrelated nodes.
- Derived/sequence callbacks return non-null candidates; null generation remains owned by existing nullability/null-probability policy.
- Sequence indexes are zero-based and per exact rule + scope.
- Optional omission/null does not consume sequence indexes.
- Higher-precedence programmatic generators shadow coordinated producers; a shadowed sequence does not consume an index.
- Shared applies only to present non-null values and caches the first successfully resolved value in scope.
- Shared does not freeze presence/null decisions.
- Uniqueness applies only to present non-null values.
- Declarative weighted/terminology/built-in sources may be retried deterministically for uniqueness; explicit programmatic/derived/sequence producers are not automatically reinvoked.
- `Derived + Unique` and `Sequence + Unique` are supported.
- `Derived + Sequence`, `Derived + Shared`, `Sequence + Shared`, and `Shared + Unique` are invalid combinations.
- Profile composition revalidates the final coordinated rule set and dependency graph.
- Existing no-coordination M0082/M0083 behavior remains compatible.
- The aligned suite package inventory remains unchanged.
- M0084 validation version is `6.1.0-m0084`.

## Baseline Executor Readiness

Architecture, session ownership/lifecycle, public scopes, exact-property eligibility, dependency locality and graph semantics, dependency-context behavior, sequence indexing, sharing semantics, uniqueness equality/participation/retry boundary, rule-combination validity, source precedence, compatibility/non-goals, documentation/sample obligations, validation topology, and human-review policy are settled.

The baseline executor retains implementation freedom over internal session/state classes, storage structures, stable topological-sort mechanics, concrete builder helper/overload types consistent with the required public capability, internal candidate fingerprint implementation consistent with semantic equality, bounded retry mechanics for retryable uniqueness sources, source files/folders, refactoring sequence, and test decomposition.

No stronger model is required to make a project-level decision. The work is medium-large but can be decomposed into bounded profile/API, session, dependency, stateful-rule, validation, sample, and documentation work packages in `.execution/M0084.md`.

## Required Authority

Implementation must read:

- `docs/specs/test-data-generation.md`;
- `docs/specs/test-data-profiles.md`;
- `docs/specs/test-data-coordination.md`;
- `docs/specs/test-data-inspection.md`;
- `docs/architecture/test-data-generation-pipeline.md`;
- `docs/decisions/testdata-profiles-are-runtime-sampling-policy.md`;
- `docs/decisions/testdata-coordination-state-is-invocation-scoped.md`;
- `docs/engineering/command-contract.md`;
- `docs/engineering/release-readiness.md`;
- `docs/engineering/samples.md`;
- `docs/ENGINEERING.md`;
- `docs/PUBLIC-DOCS.md`;
- `public-docs/guides/test-data.md`;
- `public-docs/samples.md`;
- `public-docs/api/compatibility.md`;
- `public-docs/release-notes.md`;
- `docs/TERMINOLOGY.md`.

Implementation may inspect live TestData source/tests and current programmatic catalog source to choose local mechanics.

No external guide repository or planning conversation is required.

## Acceptance Criteria

### Session lifecycle and isolation

- `Generate` creates a fresh Batch/Root session and discards it on completion/failure.
- `GenerateMany` uses one Batch session across all root ordinals and fresh Root state for each root.
- a second generation call through the same configured facade starts fresh sequence/shared/unique state;
- concurrent calls through one facade/profile do not influence one another;
- failed generation leaves no coordination state observable by a later call.

### Public profile capability

- canonical-only consumers can configure `From`, `Sequence`, `Shared`, and `Unique` on exact scalar/enum properties without CLR types;
- Root and Batch are the only supported public scopes;
- coordinated configuration is immutable after profile build;
- profile composition preserves left-to-right overlay semantics and revalidates conflicts/graphs;
- existing M0083 profile APIs remain source-compatible.

### Derived values

- a derived property may read declared scalar/enum siblings regardless of declaration order;
- multi-step dependency chains produce correct results;
- unrelated properties retain stable effective ordering;
- self-dependency and dependency cycles fail at profile build/composition;
- unknown, cross-object, non-scalar/non-enum, and derived-only-containing-type dependency declarations fail profile validation;
- undeclared runtime dependency access fails rather than implicitly widening the graph;
- `IsPresent`/`IsNull`/`TryGet<T>` distinguish omitted and explicit-null dependencies according to the spec;
- `Get<T>` succeeds for compatible present non-null declared dependencies and fails for unavailable ones;
- derived output is validated against the exact target property constraints;
- invalid/null derived output does not fall through to lower sources.

### Sequences

- a Batch sequence over five ordinary roots yields deterministic indexes `0..4`;
- a second identical `GenerateMany` invocation restarts Batch sequence at `0`;
- Root sequence state resets for each root;
- repeated matching nested occurrences within one root consume increasing Root indexes;
- omitted/null target occurrences do not consume indexes;
- a higher-precedence programmatic generator prevents the sequence producer from running/advancing for that occurrence;
- invalid sequence output fails closed;
- independent sequence rules maintain independent counters.

### Shared values

- Batch sharing gives every present non-null matching occurrence the same resolved semantic value across roots;
- Root sharing reuses within one root and obtains independent values for later roots;
- upstream callback/source invocation occurs once per populated shared scope;
- omitted/null occurrences remain occurrence-local and do not populate the cache;
- later generation invocations do not reuse the prior shared value.

### Scoped uniqueness

- Root uniqueness prevents repeated present non-null values within one root and resets for the next root;
- Batch uniqueness prevents repeated present non-null values across all roots in the invocation;
- omitted/null occurrences do not consume uniqueness;
- retryable Random/weighted/terminology sources can choose deterministic alternatives after collision;
- finite candidate-domain exhaustion fails with `TESTDATA_COORDINATION_UNIQUENESS_EXHAUSTED`;
- duplicate explicit programmatic/derived/sequence results fail rather than being silently mutated or repeatedly reinvoked;
- Binary uniqueness compares byte contents;
- uniqueness remains TestData scenario policy and does not alter canonical keys/semantics.

### Rule combinations and source precedence

- supported combinations `Derived + Unique` and `Sequence + Unique` behave according to both rules;
- invalid combinations listed in Decisions and Constraints fail profile build/composition;
- precedence is programmatic property -> programmatic Logical Type -> coordinated producer -> property weighted -> Logical-Type weighted -> property terminology -> Logical-Type terminology -> built-in;
- Shared/Unique modify the successful resolved value without changing that precedence;
- presence/null decisions continue to precede source resolution.

### Determinism and compatibility

- same aligned version/model/profile/settings/root/count/seed reproduces the same coordinated semantic graphs;
- roots remain returned in root-ordinal order;
- session state does not reintroduce mutable-RNG entropy-order coupling for unrelated Random values;
- existing M0082 deterministic/diversity tests remain valid;
- existing M0083 profile/sampling tests remain valid;
- generation with no coordinated rules remains behaviorally compatible with M0083.

### Package consumer sample

- `samples/programmatic-model-catalog/` exposes an individually runnable `coordinated-generation` scenario;
- the scenario uses the packed package path, not source project references;
- it visibly demonstrates the Scope H requirements from readable source;
- `eng/samples` executes it in the aggregate catalog;
- no external service/network/database/browser/native runtime is required.

### Documentation and diagnostics

Implementation directly updates, as applicable:

- `README.md` if the repository TestData landing summary needs coordination mention;
- `public-docs/guides/test-data.md`;
- `public-docs/samples.md`;
- `public-docs/nuget/SemanticTypeModel.md`;
- `public-docs/api/compatibility.md`;
- `public-docs/diagnostics.md`;
- `public-docs/release-notes.md` under 6.1.0 development.

Public guidance must explain:

```text
TestData Profile
    = reusable immutable configuration

Generation Session
    = one ephemeral Generate/GenerateMany run

Root scope
    = one top-level root graph

Batch scope
    = all roots in that invocation
```

It must explicitly state that scoped TestData uniqueness and derived-value dependencies do not create canonical relationships, keys, or persistent dataset semantics.

## PR 127 Hardening Acceptance Matrix

These criteria are additional closure evidence for the existing M0084 acceptance criteria. They do not replace the original acceptance sections.

### Enum source resolution

- a weighted enum rule deterministically selects only from the configured legal weighted enum candidates;
- a derived enum rule returns the declared enum value produced by its callback;
- a sequence enum rule returns the declared enum values produced by sequence indexes;
- an applicable programmatic property generator for an enum remains higher precedence than a coordinated producer;
- an invalid explicit/coordinated enum candidate fails closed;
- ordinary enum generation with no supplied source retains existing deterministic Random behavior.

### Profile composition and revalidation

- composing a profile that supplies `Derived` with a later profile that supplies `Unique` yields an effective `Derived + Unique` rule;
- composing profiles preserves unaffected coordinated fields when a later layer does not set them;
- composing a contradictory rule such as `Derived` with later `Shared` fails profile composition/build validation;
- a dependency cycle created only by combining otherwise individually valid profiles fails before generation;
- composed coordination behavior is independent of fluent declaration order except for the documented left-to-right profile layering.

### Inheritance and dependency planning

- a derived property declared on a derived object can consume an inherited scalar/enum sibling when valid under the target declaring effective object contract;
- inherited dependency ordering works regardless of declaration order;
- depending on a property that exists only on a more-derived containing object remains rejected when the target property is declared on a base object;
- runtime dependency planning and profile validation agree on the effective property set.

### Dependency context and diagnostics

- `IsPresent` distinguishes omission from explicit null;
- `IsNull` reports explicit null only;
- `TryGet<T>` returns false for omitted and explicit-null declared dependencies;
- `TryGet<T>` with incompatible `T` fails as a programmer/configuration error rather than returning false;
- `Get<T>` on an omitted/null declared dependency produces `TESTDATA_COORDINATION_DEPENDENCY_UNAVAILABLE`;
- reading an undeclared dependency produces `TESTDATA_COORDINATION_DEPENDENCY_UNDECLARED`;
- an exception from a derived or sequence callback produces `TESTDATA_COORDINATION_CALLBACK_FAILED`;
- a null or canonically invalid derived/sequence result produces `TESTDATA_COORDINATION_CANDIDATE_INVALID`;
- coordinated producer failure does not fall through to weighted, terminology, or Random sources.

### Sequence state

- Batch sequence indexes are deterministic and continue across root ordinals in one `GenerateMany`;
- a second invocation restarts Batch sequence state at zero;
- Root sequence state resets for each root;
- repeated matching nested occurrences within one root consume increasing Root indexes in deterministic visitation order;
- an omitted target occurrence does not consume a sequence index;
- a sampled-null target occurrence does not consume a sequence index;
- a higher-precedence programmatic generator shadows the sequence without consuming an index;
- separate exact-property sequence rules have independent counters;
- sequence callback output remains canonically validated.

### Shared state

- Batch sharing resolves the first present non-null value once and reuses it across eligible roots;
- Root sharing reuses within one root and resets for the next root;
- a higher-precedence programmatic source under `Shared` is invoked once for each populated shared scope, not once per occurrence;
- omitted/null occurrences neither create nor consume the shared cache;
- a later public generation invocation never reuses a previous session's shared value.

### Scoped uniqueness

- Root uniqueness prevents duplicate present non-null values inside one root and resets for the next root;
- Batch uniqueness prevents duplicates across roots in one invocation;
- omitted/null occurrences do not occupy uniqueness state;
- built-in Random, profile weighted values, and terminology candidates exercise deterministic retry behavior after collisions;
- finite candidate-domain exhaustion reports `TESTDATA_COORDINATION_UNIQUENESS_EXHAUSTED` without an unbounded loop;
- duplicate explicit programmatic, derived, or sequence results fail without hidden reinvocation;
- `Derived + Unique` and `Sequence + Unique` work;
- Binary uniqueness uses byte-content equality.

### Session isolation and failure behavior

- two concurrent generation invocations using the same immutable facade/profile do not share counters, shared caches, or uniqueness sets;
- a failed `GenerateMany` does not expose a partial successful result;
- a failure in one invocation does not affect a later invocation using the same facade/profile;
- returned roots remain in ascending RootOrdinal order.

### Regression evidence

- all pre-existing M0082 focused tests pass unchanged;
- all pre-existing M0083 focused tests pass unchanged;
- the existing `coordinated-generation` package-consumer scenario passes after hardening;
- no-profile and no-coordination generation behavior remains compatible.

### PR integration readiness

Before the hardening pass is declared complete:

- PR 127's branch contains the current `main` base tip or an equivalent conflict-free update;
- GitHub reports the updated PR mergeable;
- the PR `ci/check` workflow succeeds on the final hardened head;
- the complete M0084 validation commands are rerun on that head;
- the final M0084 candidate gate is `release-check 6.1.0-m0084`;
- stable `release-check 6.1.0`, publication-channel preflight, tagging, and publishing remain separate post-merge release work and are not claimed by this hardening pass.

## Validation

### Tier 1 — focused TestData validation

Run:

```sh
./eng/test-project.sh tests/unit/SemanticTypeModel.TestData.Tests.Unit/SemanticTypeModel.TestData.Tests.Unit.csproj
./eng/test-filter.sh M0084
```

Use the matching PowerShell launchers on Windows.

Expected evidence covers every acceptance group and retained M0082/M0083 focused behavior.

### Tier 2 — repository validation

Run:

```sh
./eng/check.sh
```

or the matching PowerShell launcher.

Expected evidence: repository restore/build/short tests/format checks succeed.

### Tier 3 — packed consumer and release candidate

Use:

```text
6.1.0-m0084
```

Run:

```sh
./eng/package.sh 6.1.0-m0084
./eng/package-smoke.sh 6.1.0-m0084
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 6.1.0-m0084
```

Use platform-equivalent PowerShell commands on Windows.

The final aggregate `release-check 6.1.0-m0084` is required release-candidate evidence. Individually successful child commands are not a substitute.

The packed programmatic-model catalog is representative consumer evidence for M0084.

No publication, tag, or GitHub Release is part of M0084.

## Validation Locus and Constrained Execution

All authoritative M0084 targets are portable package/repository consumers.

Execution locus:

```text
local or ordinary CI
```

Supported platforms:

```text
Windows
Linux
```

No credentials, external service, database, browser, native subsystem, clock source, or network access is required by product validation.

The canonical commands are expected to fit the ordinary execution model; no resumable shard contract is required.

If an executor cannot run a required canonical command in its harness, it must report the missing evidence rather than substituting a weaker target.

## Human Review

Applicability: none as a blocking milestone-specific gate.

The coordination contract is machine-observable through deterministic behavior, profile validation, focused tests, packed-package sample evidence, public documentation validation, and aggregate release-candidate validation.

Normal human PR review remains appropriate but does not create an `AWAITING HUMAN REVIEW` completion requirement.

No `.review/` artifact is created by planning.

## Documentation Policy

Use:

```text
direct-for-touched-surfaces
```

M0084 implementation directly synchronizes affected TestData public guidance, diagnostics, compatibility, release notes, and sample routing.

No `.guide-sync/pending/` hint is required.

## Research Boundary

No durable research artifact is required.

The material questions are internal architecture/semantics and were resolved from live post-M0083 source/tests, current TestData profile/generation/architecture authority, and current repository engineering/packaging policy.

No external product behavior, expensive experiment, volatile vendor contract, or historical reconstruction is needed.

All implementation-affecting conclusions are promoted into ordinary project authority in this planning package.

## Hardening Execution State

Because the previous implementation pass performed completed-milestone cleanup, applying this overlay intentionally re-opens M0084 on the PR branch.

Before production hardening edits, create or reconcile:

```text
.execution/M0084.md
```

The ledger must include each item in the PR 127 Hardening Acceptance Matrix as explicit evidence-bearing obligations.

Do not preserve a prior `COMPLETE` claim merely because the original implementation pass reached that state. The live branch and hardening evidence control.

## Completion Expectations

Before production edits implementation creates/reconciles:

```text
.execution/M0084.md
```

The ledger must map every acceptance, architecture, compatibility, documentation, sample, and validation obligation to bounded work/evidence.

Before `COMPLETE`:

1. freshly reread this milestone;
2. reconcile milestone <-> ledger <-> live repository/evidence;
3. verify session isolation and both scopes;
4. verify every coordinated rule and supported/invalid combination;
5. verify source precedence and retained M0082/M0083 behavior;
6. verify packed-package `coordinated-generation` evidence;
7. verify direct documentation/diagnostics synchronization;
8. run the final aggregate release-candidate gate;
9. perform the completion audit;
10. apply normal completed-milestone cleanup.

Passing tests alone does not establish milestone completion.

## Escalation Boundary

Return to planning if implementation discovers a material need to change invocation-scoped session ownership, Root/Batch scope semantics, coordinated-rule target eligibility, same-object dependency boundary, dependency-context semantics, sequence consumption/index semantics, Shared presence/null/cache semantics, uniqueness participation/equality/retry boundary, allowed/forbidden rule combinations, source precedence, profile-composition validation, canonical-versus-TestData relationship boundary, dataset non-goal, compatibility promises, or validation target/locus.

Implementation owns internal classes/data structures, topological-sort mechanics, bounded retry implementation, source-file organization, refactoring order, test decomposition, and other local mechanics that preserve this contract.
