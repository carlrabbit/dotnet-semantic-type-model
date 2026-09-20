# Milestone — M0081 Dynamic Model Sample Catalog & TestData Inspection

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
| Implementation autonomy | high |
| Documentation sync | direct-for-touched-surfaces |
| Focused validation | Tier 1 TestData-focused tests plus focused M0081 filters as useful |
| Repository validation | Tier 2 `check` |
| Integration validation | Tier 3 packed-package sample execution and release-candidate validation |
| Validation locus/platform | local or CI on supported Windows/Linux; no external service/runtime required |
| Consumer/release validation | `release-check 6.1.0-m0081`; no publish |
| Human review | no blocking milestone-specific review; normal repository/PR review remains expected |

## Goal

Turn the 6.1 programmatic-authoring capability into an extensive executable documentation catalog and add a
deterministic human-readable inspection surface for `SemanticTestValue` graphs so consumers can see, for each
representative canonical semantic concept:

```text
how the model is authored programmatically
-> what deterministic Random TestData looks like
-> what TestData looks like when example values are supplied through a Semantic Terminology Profile
```

The resulting samples are code-first documentation in the repository sense: readable consumer source that is
also executed against the currently packed package suite.

## Target State

When M0081 is complete:

1. `samples/programmatic-model-catalog/` is one package-based executable sample project containing individually
   invokable scenarios for the current dynamic/programmatic canonical model surface and representative semantic
   vocabulary.
2. Each generatable scenario visibly constructs its own relevant canonical declarations through
   `TypeSchemaModelAuthoringBuilder`, generates deterministic Random semantic TestData, supplies visible example
   candidates through a validated Semantic Terminology Profile, and renders both value graphs through the
   supported TestData inspection API.
3. Unsupported TestData constructs are represented by explicit diagnostic scenarios rather than fake successful
   example output.
4. `SemanticTypeModel.TestData` exposes deterministic human-readable text inspection for `SemanticTestValue`
   graphs without CLR materialization.
5. The catalog is individually runnable by stable scenario id, listable, and runnable in aggregate.
6. `eng/samples` executes the aggregate catalog against the locally packed aligned suite.
7. Public sample/TestData guidance routes consumers to the catalog and explains that “example data” in the
   catalog is Semantic Terminology Profile guidance, not a second authoring source or embedded faker system.
8. The capability remains additive on the 6.1.0 development line and is validated with package version
   `6.1.0-m0081`.

## Scope

### A. TestData semantic-value inspection

Add the public inspection contract defined by `docs/specs/test-data-inspection.md`.

The inspection surface belongs to `SemanticTypeModel.TestData` and must:

- inspect a `SemanticTestValue` together with the canonical `TypeSchemaModel` that gives its identities meaning;
- require no CLR type or materialization;
- preserve object/array/dictionary/null/scalar/enum structure;
- use deterministic invariant scalar formatting;
- resolve object property identities to canonical property names;
- remain human-readable development/test output rather than a persisted interchange format.

### B. One executable dynamic-model catalog

Create exactly one new executable catalog project:

```text
samples/programmatic-model-catalog/
```

Do not create one project per semantic concept.

The catalog is a package consumer under the existing samples engineering policy:

- it consumes locally packed `SemanticTypeModel.*` packages through the sample package mechanism;
- it must not use `src/*` project references;
- it must not depend on `SemanticTypeModel.DotNet` or `SemanticTypeModel.Generators`;
- it may reference `Abstractions`, `Core`, and `TestData` as required;
- target-specific packages are outside this catalog.

The command contract is:

```text
dotnet run --project samples/programmatic-model-catalog -- list
dotnet run --project samples/programmatic-model-catalog -- <scenario-id>
dotnet run --project samples/programmatic-model-catalog -- all
```

Scenario ids are stable, lower-case, human-readable identifiers. Exact dispatcher and source organization are
implementation-owned.

`eng/samples` must run the catalog in aggregate mode.

### C. Scenario source contract

The detailed documentation is the scenario source itself.

Each scenario should teach one canonical element or one deliberately coupled semantic behavior. Implementation
may group concepts that are inseparable for comprehension, but must not hide unrelated concepts behind a large
shared model.

For every normal generatable scenario, the source must make these steps visible without requiring the reader to
open helper implementation:

```text
canonical TypeDefinition / PropertyDefinition declarations
-> TypeSchemaModelAuthoringBuilder finalization
-> fixed-seed Random semantic generation
-> visible example candidate declaration and profile binding
-> validated terminology profile consumption
-> example-guided semantic generation
-> deterministic inspection output
```

Shared infrastructure may own:

- command dispatch;
- headings/output framing;
- profile JSON plumbing;
- repetitive assertion helpers;
- exit codes.

Shared helpers must not hide the semantic declaration being taught, the candidate values being supplied, the
property/Logical-Type scope of those candidates, or the call that performs Random/example-guided generation.

Do not create per-scenario Markdown or README files. `public-docs/samples.md` remains the only Markdown routing
surface for samples.

### D. Scenario output contract

Normal scenario output contains three obvious sections:

```text
Model
Random
Example-guided
```

`Model` uses the existing canonical model text inspection surface or an equally direct existing inspection path.
`Random` and `Example-guided` use the M0081 TestData inspection API.

The catalog uses fixed seeds. Output must be deterministic for the same package version and scenario.

For expected-failure scenarios, replace the unavailable value section with deterministic diagnostic text. A
scenario must never fabricate output for semantics that the TestData specification declares unsupported.

Scenarios include internal assertions sufficient to make a wrong or unexpectedly changed outcome return a
non-zero process result. Console output is documentation; it is not the sole correctness check.

No generated output files or copied expected-output Markdown pages are required.

### E. Minimum catalog coverage

The catalog is extensive by semantic coverage, not by duplicated boilerplate.

#### Type and value shapes

Provide positive individually invokable scenarios covering every currently supported built-in scalar kind:

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
Json
```

Each scalar-kind scenario uses an object/property use site so property-specific example candidates can be
demonstrated.

Provide positive scenarios for:

```text
Enum
Object / nested object
Array
Dictionary
Reference
Any
```

Provide explicit diagnostic scenarios for the current TestData unsupported/no-value cases:

```text
Unknown scalar
Never
Union
Intersection
```

These scenarios document current behavior; M0081 does not add generation support for them.

#### Cardinality, constraints, and formats

Provide dedicated scenarios covering at least:

- required;
- optional;
- nullable;
- required + nullable as distinct from optional;
- string minimum/maximum length;
- numeric minimum/maximum including an exclusive bound;
- `multipleOf`;
- collection minimum/maximum items;
- unique items;
- representative predefined scalar format;
- regex pattern behavior where Random generation fails and a valid terminology example succeeds;
- representative unsatisfiable/invalid generation diagnostic.

#### Projection-neutral semantic vocabulary

Provide dedicated scenarios sufficient to make the following current semantic concepts directly discoverable
from source:

- every non-`Unspecified` `EntityRole`: `Entity`, `ValueObject`, `Dimension`, `Fact`, `Lookup`, `Event`,
  `Configuration`, and `Form`;
- aggregate-root metadata;
- keys, including a composite example and at least one non-primary key kind;
- Logical Type;
- Display Identity;
- Access Path;
- semantic mutability at object/property level;
- display name plus user/technical description metadata;
- ownership, including owned object and owned collection;
- envelope, payload, and envelope metadata;
- version/revision/current-version semantics;
- temporal validity, valid-from/valid-to, and lifecycle state;
- extension data;
- `RequiredWhen`;
- computed-member metadata where represented by the current canonical model.

A scenario for metadata that does not affect TestData value synthesis must not imply otherwise. It may use the
same scalar leaf with Random and example-guided values to show that the metadata is preserved in the model while
generation remains governed by the ordinary value contract.

Projection-specific annotation catalogs are outside M0081.

#### Terminology/example-data behavior

Provide dedicated scenarios for the value-source behavior most likely to be misunderstood:

```text
property-specific example candidate
Logical Type example candidate
property candidate wins over Logical Type candidate
runtime-ineligible property candidate falls through to Logical Type candidate
no eligible terminology candidate falls through to Random generation
pattern-constrained Random failure -> valid terminology success
```

“Example data” in catalog labels/docs always maps to `SemanticTerminologyProfile` candidate values. Programmatic
property/Logical-Type generator callbacks are a separate TestData capability and must not be presented as the
meaning of example data in this catalog.

A separate optional advanced scenario may demonstrate a callback generator, but it does not satisfy any
example-guided acceptance criterion.

## Non-goals

M0081 does not add:

- new canonical semantic primitives;
- changes to programmatic authoring semantics;
- a second dynamic model hierarchy;
- CLR type generation or TestData materialization for dynamic models;
- new TestData generation support for `Unknown`, `Never`, `Union`, or `Intersection`;
- regex synthesis;
- new faker/domain datasets;
- AI/runtime model dependencies;
- a JSON/YAML TestData serialization or persistence protocol;
- a parser/round-trip contract for inspection text;
- target-specific Power BI, JSON Schema, EF Core, or System.Text.Json sample catalogs;
- one `.csproj` per semantic concept;
- per-scenario Markdown pages or README files;
- stable 6.1.0 publication.

## Decisions and Constraints

- M0081 targets the existing 6.1.0 development line after completed M0080.
- The stable baseline remains released 6.0.0.
- Programmatic model construction uses the existing canonical definitions and
  `SemanticTypeModel.Core.Authoring.TypeSchemaModelAuthoringBuilder`; samples must not reintroduce a convenience
  DSL that hides those contracts.
- The catalog is intentionally repetitive where repetition makes each scenario readable in isolation.
  Deduplication is subordinate to code-as-documentation clarity.
- One catalog project is preferred over many projects because package restore/build infrastructure is not part of
  the concept being taught.
- Scenario ids are the individual execution boundary; source files are the detailed documentation boundary.
- The catalog uses semantic TestData (`SemanticTestValue`) and does not require CLR model types.
- Example-guided output uses Semantic Terminology Profiles and current precedence/validation rules.
- The new inspection surface is deterministic human-readable output. It must not be described as JSON, a
  snapshot format, or a stable wire protocol.
- Dictionary inspection preserves semantic key/value structure and must not coerce arbitrary keys into JSON
  object property names.
- Inspection must not mutate the canonical model or generated value graph.
- Existing code-first samples remain; this catalog complements them rather than replacing them.
- All `SemanticTypeModel.*` packages used together remain exact-version aligned.
- No external services, credentials, network calls, database, browser, or native runtime are required by the
  catalog.

## Baseline Executor Readiness

Architecture, semantics, scope, compatibility, sample topology, example-data meaning, inspection behavior,
coverage expectations, documentation policy, and validation topology are settled.

The baseline executor retains implementation freedom over:

- concrete scenario class/interface names;
- dispatcher mechanics;
- source-file naming below the required project;
- helper implementation that respects the visibility rules above;
- inspection formatter internals and punctuation;
- exact assertion/test decomposition;
- refactoring required to integrate the project with existing engineering runners.

No stronger model is required to make a project-level decision.

Execution volume is expected to be larger than the conceptual complexity. The implementation-owned
`.execution/M0081.md` ledger must therefore map catalog coverage, public API work, docs, and validation into
bounded work packages and evidence.

## Required Authority

Implementation must read and follow:

- `docs/specs/programmatic-model-authoring.md`
- `docs/specs/test-data-generation.md`
- `docs/specs/test-data-inspection.md`
- `docs/specs/type-model-core.md`
- `docs/specs/core-semantic-vocabulary.md`
- `docs/specs/type-model-query-and-inspection.md`
- `docs/engineering/samples.md`
- `docs/engineering/command-contract.md`
- `docs/ENGINEERING.md`
- `docs/PUBLIC-DOCS.md`
- `public-docs/guides/test-data.md`
- `public-docs/samples.md`
- `docs/TERMINOLOGY.md`

The existing canonical model and TestData source/tests may be inspected as needed to derive implementation
mechanics.

No external guide file is required during implementation.

## Acceptance Criteria

### TestData inspection

- A public inspection extension exists in `SemanticTypeModel.TestData.Inspection` with the required contract from
  `docs/specs/test-data-inspection.md`.
- Inspection works for scalar, enum, object, array, dictionary, and null value nodes.
- Object inspection resolves canonical property names without CLR metadata.
- Scalar rendering is deterministic and invariant for representative string, numeric, temporal, Guid, binary,
  JSON, boolean, and null values.
- Object output is independent of dictionary enumeration order.
- Arrays preserve semantic item order and dictionaries preserve entry structure without JSON-key coercion.
- Repeated inspection of the same model/value on supported Windows/Linux semantics produces identical normalized
  text.
- Inspection is documented as non-serialization/non-persistence output.

### Catalog

- Exactly one new `samples/programmatic-model-catalog/` executable project provides the M0081 catalog.
- `list`, one scenario id, and `all` are supported command modes.
- Every applicable coverage item in the Scope section is represented by a discoverable scenario.
- Every normal scenario visibly demonstrates programmatic authoring, Random semantic TestData, terminology
  candidate input, example-guided generation, and inspection.
- Expected unsupported cases emit and assert the documented deterministic diagnostic instead of pretending to
  generate a value.
- At least one scenario proves property terminology precedence over Logical Type terminology.
- At least one scenario proves an ineligible property candidate falls through to an eligible Logical Type
  candidate.
- The pattern scenario proves built-in Random generation remains unsupported while a valid terminology candidate
  succeeds.
- Metadata-only semantics do not claim generation behavior they do not own.
- The catalog contains no CLR-domain model merely to support TestData output.
- The catalog contains no per-scenario Markdown documentation.

### Package-consumer boundary

- The catalog consumes the current locally packed suite, not source project references.
- `eng/samples` executes `all` for the catalog and fails if any scenario fails.
- The current packed `SemanticTypeModel.TestData` package exposes and successfully executes the inspection API
  through the sample consumer.
- Sample validation does not accidentally resolve stale/global STM packages.

### Documentation and compatibility

Implementation directly updates, as applicable:

- `public-docs/samples.md` to route to the new catalog and show the list/individual/all invocation pattern;
- `public-docs/guides/test-data.md` with TestData inspection and the terminology/example-data distinction;
- `public-docs/nuget/SemanticTypeModel.md` for the new public inspection capability;
- `public-docs/release-notes.md` under the 6.1.0 development line;
- `docs/SPECS.md` only if the live spec index differs from the planning overlay;
- API compatibility evidence/baselines required by the repository for the new public surface.

No documentation may present the inspection text as a persisted interchange format.

## Validation

### Tier 1 — focused implementation validation

Authoritative target: TestData unit behavior and focused M0081 scenarios.

Use the platform-equivalent forms of:

```sh
./eng/test-project.sh tests/unit/SemanticTypeModel.TestData.Tests.Unit/SemanticTypeModel.TestData.Tests.Unit.csproj
./eng/test-filter.sh M0081
```

Use whichever focused selector matches the implemented test naming while preserving equivalent coverage.

Expected evidence:

- TestData inspection tests pass;
- representative catalog-specific support tests pass;
- deterministic rendering and mismatch/error behavior are covered.

### Tier 2 — repository completion gate

Run:

```sh
./eng/check.sh
```

or the PowerShell equivalent.

Expected evidence: restore/build/short tests/format verification succeed.

### Tier 3 — packed consumer, docs, samples, and 6.1 release-candidate evidence

Use validation version:

```text
6.1.0-m0081
```

Run the canonical commands in an order consistent with repository engineering policy, including:

```sh
./eng/package.sh 6.1.0-m0081
./eng/package-smoke.sh 6.1.0-m0081
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 6.1.0-m0081
```

`release-check` is the aggregate release-readiness evidence required by the repository profile; successful child
commands are not a substitute for the aggregate gate.

The packed-package sample run is the authoritative consumer evidence for the new catalog/inspection usage. It
must resolve the current build's package artifacts through the existing local sample NuGet mechanism.

### Validation locus

Portable validation may run locally or in CI on a supported Windows or Linux environment.

No external runtime/service target is authoritative for M0081.

If the current agent environment cannot execute a required repository command because the execution harness
lacks a normal build capability, report the missing evidence. Do not replace packed-package validation with
project-reference execution.

No resumable shard contract is required by planning.

## Human Review

Applicability: none as a blocking milestone-specific completion gate.

The catalog's readability requirement is made concrete through isolation, visible authoring/candidate/generation
steps, stable scenario ids, source-as-documentation rules, deterministic output, and executable assertions.

Ordinary human PR review remains appropriate for documentation quality but is not a separate milestone terminal
state or `.review/` artifact requirement.

## Documentation Policy

This milestone uses the repository profile's `direct-for-touched-surfaces` policy.

Implementation updates the directly affected specification/public documentation and sample index as part of
M0081. Do not create `.guide-sync/pending/` hints for work already required by this milestone.

Do not create per-sample Markdown pages.

## Completion Expectations

The implementation agent owns milestone closure:

```text
execution decomposition
-> create/reconcile .execution/M0081.md
-> implement bounded catalog/API work packages
-> focused validation and evidence
-> repository/package/sample/public-doc validation
-> freshly reread this milestone
-> reconcile milestone <-> ledger <-> live repository/evidence
-> completion audit
```

Before `COMPLETE`, verify every catalog coverage obligation against the live scenario registry/source. A checked
ledger row or successful `all` invocation is not by itself proof that required semantic coverage exists.

After durable authority/public docs are synchronized and the milestone is complete, apply the repository's normal
completed-milestone cleanup policy.

## Escalation Boundary

Implementation owns concrete files, types, dispatcher mechanics, helper structure, formatter punctuation, tests,
and work-package sequencing within this contract.

Return M0081 to planning if implementation requires a new decision that would materially change:

- the one-project catalog topology;
- the meaning of example data;
- the public TestData inspection contract;
- inspection compatibility/persistence semantics;
- supported/unsupported TestData generation behavior;
- required semantic coverage;
- package boundaries;
- validation target/locus;
- release-line compatibility.

Do not silently broaden M0081 into new generation semantics, a persisted TestData format, a target-specific sample
suite, or a fluent canonical-model DSL.
