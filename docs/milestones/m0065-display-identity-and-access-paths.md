# M0065 — Display Identity and Access Paths

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-broad |
| Scope size | medium |
| Implementation autonomy | high within the resolved semantic/public-API contract |
| Repository role | capability-provider |
| Maturity | published-maintenance |
| Documentation sync | separate pass |
| Release readiness | separate pass |
| Human review | none |

## Goal

Add two new projection-neutral, annotation-only code-first semantics:

- **Display Identity** — the single ordered property group that describes how humans should recognize an instance;
- **Access Path** — a named ordered property group that describes an intended way consumers may locate or narrow instances.

Expose both through public `SemanticTypeModel.DotNet` attributes, preserve them deterministically in the canonical semantic model, diagnose invalid/ambiguous declarations, and prove runtime extraction/source generation/package consumption without adding target-specific behavior.

## Target State

When M0065 is complete:

1. consumers can declare `[SemanticDisplayIdentity]` on one or more properties of an object;
2. consumers can declare one or more `[SemanticAccessPath("Name")]` attributes on a property;
3. compile-time and direct .NET extraction produce the canonical reserved annotations defined in `docs/specs/core-semantic-vocabulary.md`;
4. generated canonical providers preserve those annotations deterministically;
5. existing models without the new attributes retain their prior semantics and generated behavior;
6. invalid/ambiguous Display Identity or Access Path groups produce stable STM5xxx diagnostics and do not survive as valid canonical groups;
7. no canonical model contract type is added or changed solely for these concepts;
8. no EF Core, JSON Schema, Power BI, System.Text.Json, Configuration, API, UI, query-engine, or index behavior is added;
9. the EF compile-time semantic manifest remains schema version 1 and does not carry Display Identity or Access Path metadata;
10. the change is additive public API suitable for the next minor release line (4.1.x), not a 4.0.x patch release.

## Scope

### Public .NET authoring attributes

Add these public authoring contracts in `SemanticTypeModel.DotNet`.

#### `SemanticDisplayIdentityAttribute`

Contract:

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticDisplayIdentityAttribute : Attribute
{
    public int Order { get; init; }
}
```

Semantic rules:

- `Order` defaults to `0`;
- `Order` must be non-negative;
- every attributed property belongs to the containing object's single Display Identity;
- component order is ascending numeric `Order`;
- effective order values must be unique for one object;
- order gaps are valid;
- there are no named/multiple Display Identity variants in this milestone.

The exact source-file placement is implementation-owned; the public shape above is part of the compatibility contract.

#### `SemanticAccessPathAttribute`

Contract:

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class SemanticAccessPathAttribute : Attribute
{
    public SemanticAccessPathAttribute(string name);
    public string Name { get; }
    public int Order { get; init; }
}
```

Semantic rules:

- `Name` is required;
- `Name` is case-sensitive using ordinal comparison;
- `Name` is scoped to the containing object;
- `Name` must match `[A-Za-z][A-Za-z0-9_.-]*`;
- `Order` defaults to `0`;
- `Order` must be non-negative;
- members of one named path are ordered by ascending `Order`;
- effective order values must be unique within one named path on one object;
- order gaps are valid;
- one property may participate in multiple differently named Access Paths.

The exact source-file placement is implementation-owned; the public shape above is part of the compatibility contract.

### Canonical annotation encoding

This milestone is annotation-only.

Do not add `DisplayIdentityDefinition`, `AccessPathDefinition`, new properties to `ObjectTypeDefinition`/`PropertyDefinition`, or another strongly typed canonical model surface.

Canonical encoding:

```text
Display Identity member:
    schema.displayIdentity = "<non-negative decimal order>"

Access Path member:
    schema.accessPath.<name> = "<non-negative decimal order>"
```

Examples:

```text
CustomerNumber:
    schema.displayIdentity = "0"
    schema.accessPath.ByCustomerNumber = "0"

Name:
    schema.displayIdentity = "1"

DeviceId:
    schema.accessPath.ByDeviceAndTimestamp = "0"

Timestamp:
    schema.accessPath.ByDeviceAndTimestamp = "1"
```

This encoding is selected because current extracted descriptors use a unique string-to-string annotation dictionary and canonical annotation normalization merges duplicate keys. Different Access Path names therefore use distinct reserved annotation keys.

### Semantic independence

The following semantics may overlap on one property but never imply one another:

```text
SemanticKey
    -> machine/domain identity

Display Identity
    -> human-recognition components

Access Path
    -> intended lookup/filter route
```

No inference is allowed:

- Key -> Display Identity;
- Key -> Access Path;
- Display Identity -> Key;
- Display Identity -> Access Path;
- Access Path -> Key;
- Access Path -> Display Identity;
- `SemanticDisplayName` -> Display Identity;
- `SemanticOrder` -> Display Identity or Access Path.

### Display Identity meaning

Display Identity means:

> the ordered set of properties that a human-facing consumer can use as stable recognition components for an instance.

It does **not** define:

- machine/domain identity;
- uniqueness;
- key semantics;
- formatting;
- concatenation;
- separator text;
- null rendering;
- a display caption;
- UI layout/order;
- a fallback algorithm;
- database behavior.

A future target may use Display Identity to construct selector/list/form/report/API labels, but this milestone adds no target behavior.

### Access Path meaning

Access Path means:

> a named ordered property sequence that represents an intended way consumers may locate or narrow instances of an object.

It does **not** define:

- a physical database index;
- index uniqueness;
- index clustering/included columns/provider options;
- a key or alternate key;
- query frequency or a performance guarantee;
- equality/range/prefix operators;
- whether all members must be provided in one query;
- composite-index prefix semantics;
- API parameter names;
- sort order;
- UI order/visibility;
- importance rank/priority.

A future target may use Access Paths as input to explicit policy, for example:

```text
EF Core -> index candidate
API -> query/filter affordance
list/grid -> filter/search candidate
form/selector -> lookup candidate
reporting -> filtering/slicing candidate
```

Those behaviors require later target-specific contracts and are outside M0065.

### Inheritance/effective property sets

Existing inherited-member extraction rules remain authoritative.

Validation for both semantics is applied to the effective extracted property set of each object:

- inherited attributed properties contribute to the derived object's effective groups;
- a derived object's own members may extend an inherited Access Path when names/orders remain valid;
- duplicate effective Display Identity order makes the derived object's Display Identity invalid;
- duplicate effective order inside one Access Path makes that named path invalid for the derived object;
- invalidity on a derived effective group does not retroactively invalidate the base object's independently valid group.

Do not add new inheritance/override attributes in this milestone.

### Diagnostics

Allocate the next two STM5xxx codes as stable public diagnostics:

```text
STM5049 — invalid or ambiguous Display Identity definition
STM5050 — invalid or ambiguous Access Path definition
```

`STM5049` covers at least:

- negative Display Identity order;
- duplicate effective Display Identity order within one object.

`STM5050` covers at least:

- null/empty/invalid Access Path name;
- negative Access Path order;
- duplicate membership of one property in the same named path;
- duplicate effective order within one named Access Path on one object.

Behavior for invalid groups:

- if any Display Identity declaration makes the effective group invalid, omit the entire Display Identity annotation group for that affected object;
- if one named Access Path is invalid, omit that entire named path for that affected object;
- unrelated valid named Access Paths remain;
- existing STM5xxx generator warning severity policy remains unchanged;
- message wording is non-authoritative; diagnostic codes and invalid-group behavior are authoritative.

Implementation must add the normal stable diagnostic constants/descriptors/reference coverage required by repository engineering rules.

## Non-goals

- strongly typed canonical `DisplayIdentityDefinition` or `AccessPathDefinition` records;
- changes to `TypeSchemaModel`, `ObjectTypeDefinition`, `PropertyDefinition`, or other canonical public model contracts for these concepts;
- key semantics changes;
- key inference changes;
- Display Identity inference;
- Access Path inference;
- multiple/named Display Identity variants;
- Access Path mode/operator metadata such as equality, range, prefix, full-text, or starts-with;
- Access Path priority/importance/estimated selectivity/frequency;
- target-specific database index generation;
- EF Core changes of any kind beyond proving the semantic manifest remains unaffected;
- API endpoint/query-parameter generation;
- UI/list/form rendering behavior;
- Power BI behavior;
- System.Text.Json behavior;
- Configuration/Options behavior;
- JSON Schema `x-stm` vocabulary changes;
- persisted-model schema/version changes solely for these string annotations;
- EF semantic-manifest schema/version changes;
- new query/inspection convenience APIs dedicated to these concepts;
- relationship semantics;
- release preparation or publication.

## Resolved Architecture and Compatibility Decisions

### Projection-neutral core semantics

Both concepts belong to reserved `schema.*` core semantics rather than `ui.*` or `efCore.*`.

Reason:

- Display Identity is useful beyond UI;
- Access Path is useful beyond databases;
- both meanings remain true before a target projection is selected.

### Annotation-only canonical representation

The current generic `AnnotationBag` is sufficient.

M0065 intentionally does not expand the strongly typed canonical model surface. This keeps the addition additive and allows future experience to determine whether either concept deserves a structured first-class model type.

### Unique-key Access Path encoding

Use:

```text
schema.accessPath.<name> = "<order>"
```

rather than repeated `schema.accessPath` entries.

Current annotation normalization is key-unique and merges duplicate keys, while `.NET` extracted descriptors use a `string -> string` dictionary. The name in the annotation key gives one property deterministic membership in multiple paths without changing those infrastructure contracts.

### No target behavior

Existing targets must behave exactly as before when the new annotations are present.

In particular:

- EF Core generator/model projection must not emit indexes or other configuration from Access Paths;
- the internal EF semantic manifest must not grow these fields and remains version `1`;
- JSON Schema must not add Display Identity or Access Path to `x-stm` in this milestone;
- open `ui.*` behavior remains unchanged.

### Additive compatibility

This is an additive public authoring API.

No existing public member is removed or changed.

Existing code without the new attributes must retain prior generated canonical-model semantics.

Because this is a new capability, a future release containing M0065 should be a minor release (4.1.0 or later compatible 4.1.x line), not 4.0.2.

Release work is separate.

## Required Project Authority

Implementation reads:

- `AGENTS.md`
- `docs/TERMINOLOGY.md`
- `docs/SPECS.md`
- `docs/specs/core-semantic-vocabulary.md`
- `docs/specs/current-canonical-model-surface.md`
- `docs/specs/code-first-semantic-model-architecture.md`
- `docs/specs/type-model-dotnet-attributes.md`
- `docs/specs/type-model-dotnet-extraction.md`
- `docs/specs/type-model-compile-time-generator.md`
- `docs/ENGINEERING.md`
- `docs/engineering/dotnet.md`
- `docs/engineering/command-contract.md`
- `docs/engineering/packaging.md`
- this milestone

Implementation should inspect the live `.DotNet`, generator, canonical-model, diagnostics, tests, and package-smoke source required to choose concrete mechanics.

Ordinary implementation must **not** read:

- the external guide repository;
- the planning conversation;
- `.guide-profile.json`;
- `.guide-sync/`;
- old copied guides or research;
- historical milestones unless a concrete source behavior cannot be understood from current authority/live code.

No current decision record needs to be reopened.

## Acceptance Criteria

### Public API

- `SemanticDisplayIdentityAttribute` exists with the exact public contract resolved above;
- `SemanticAccessPathAttribute` exists with the exact public contract resolved above;
- both are property-only;
- AttributeUsage multiplicity matches the resolved contract;
- XML documentation explains semantic meaning and explicitly avoids target-specific promises.

### Extraction

For valid declarations:

- single-member Display Identity extracts;
- composite Display Identity extracts with deterministic order;
- single-member Access Path extracts;
- composite Access Path extracts with deterministic order;
- one property can participate in multiple Access Paths;
- a property may simultaneously carry Key, Display Identity, and Access Path semantics;
- order gaps remain valid;
- valid inherited members participate according to existing effective-property extraction.

Canonical member annotations exactly match:

```text
schema.displayIdentity
schema.accessPath.<name>
```

with invariant non-negative decimal-string values.

### Invalid/ambiguous declarations

Tests prove:

- negative Display Identity order -> `STM5049`;
- duplicate effective Display Identity order -> `STM5049`;
- invalid Access Path name -> `STM5050`;
- negative Access Path order -> `STM5050`;
- duplicate same-path membership on one property -> `STM5050`;
- duplicate effective order in one named path -> `STM5050`;
- invalid Display Identity group is omitted as a whole;
- only the invalid Access Path is omitted when other named paths are valid;
- inheritance-induced duplicate effective order is diagnosed deterministically.

### Source generation

- generated canonical provider contains the same valid annotations as direct extraction;
- generated source compiles under repository nullable/warning policy;
- generated output remains deterministic;
- a model with no new attributes does not acquire new annotations or semantic behavior;
- the generated EF semantic manifest remains schema version `1` and omits Display Identity/Access Path metadata.

### Canonical model compatibility

- no new canonical contract field/type is required for these semantics;
- existing generic annotation cloning/normalization/query/inspection paths preserve valid annotation keys/values;
- reserved annotation normalization does not merge distinct Access Path names;
- unrelated existing core semantic tests remain green.

### Target isolation

Automated regression coverage establishes that the presence of the new annotations alone does not change:

- EF relational derivation/generation;
- JSON Schema output or `x-stm` vocabulary;
- Power BI projection;
- System.Text.Json behavior;
- Configuration behavior.

This does not require one new test in every target if existing target implementations provably ignore unknown `schema.*` annotations and Tier 2 covers them. Add focused target tests only where live implementation inspection reveals a risk of generic annotation pass-through changing output.

### Diagnostics

- `STM5049` and `STM5050` are stable unique IDs;
- generator/runtime extraction diagnostics use them for the resolved cases;
- diagnostic stability/uniqueness tests pass;
- implementation does not repurpose another existing diagnostic code.

### Packed consumer

Packed-package smoke must include a minimal consumer using both new attributes through the packed `SemanticTypeModel.DotNet` and `SemanticTypeModel.Generators` packages and prove the generated canonical model contains expected annotations.

The packed consumer need not exercise any target projection for these annotations.

## Validation

### Tier 1 — focused

Run the affected .NET extraction and generator projects:

```sh
./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit/SemanticTypeModel.DotNet.Tests.Unit.csproj
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit/SemanticTypeModel.Generators.Tests.Unit.csproj
./eng/test-project.sh tests/unit/SemanticTypeModel.Core.Tests.Unit/SemanticTypeModel.Core.Tests.Unit.csproj
```

Useful focused filters during iteration:

```sh
./eng/test-filter.sh DisplayIdentity
./eng/test-filter.sh AccessPath
./eng/test-filter.sh STM5049
./eng/test-filter.sh STM5050
```

Exact test class/file organization is implementation-owned.

### Tier 2 — completion gate

Required:

```sh
./eng/check.sh
```

### Tier 3 — packed authoring/generator boundary

Use a local non-publishing minor-candidate version:

```sh
./eng/package.sh 4.1.0-m0065
./eng/package-smoke.sh 4.1.0-m0065
```

Both commands must complete successfully.

Do not publish.

## Validation Execution Mode

| Validation | Mode |
|---|---|
| Tier 1 focused projects | direct |
| Tier 2 repository check | direct aggregate |
| Tier 3 package/package-smoke | direct aggregate |
| Human review | not applicable |

No partial child output counts as aggregate success.

## Constrained Runtime Handling

The required suites are not currently defined as resumable/sharded validation.

If an implementation environment cannot complete `./eng/check.sh` or package smoke:

1. continue with focused Tier 1 validation for implementation diagnosis;
2. do not mark the milestone complete from partial output;
3. run the unchanged aggregate command in a capable environment or CI;
4. require complete successful aggregate evidence before `done`;
5. report the successful command/evidence in the implementation completion summary.

Do not add resumable-validation infrastructure as part of M0065.

## Direct Documentation Impact

Planning has already updated project authority for the resolved semantics:

- `docs/TERMINOLOGY.md`;
- `docs/specs/core-semantic-vocabulary.md`;
- `docs/specs/type-model-dotnet-attributes.md`;
- `docs/MILESTONES.md`.

Implementation must keep those documents aligned if live implementation reveals a mechanical wording correction, but must not reopen their semantic decisions.

No architecture or decision record is required because package boundaries and canonical-model architecture remain unchanged.

## Deferred Documentation Synchronization

Consumer-facing documentation synchronization is deferred to:

```text
.guide-sync/pending/m0065-display-identity-and-access-paths.md
```

Ordinary implementation does not read that file.

## Human Review

Applicability: `none`.

Reason:

- the public API and semantic contracts are fully resolved in planning;
- extraction, canonical annotation shape, diagnostics, generator parity, isolation, determinism, and packed consumption are objectively testable;
- no visual/UI artifact or subjective output is produced.

No `.review/` request/record is required.

## Escalation Boundary

Implementation owns:

- exact source file placement;
- extractor helper/refactoring mechanics;
- internal grouping data structures;
- test class/file structure;
- diagnostic descriptor implementation mechanics consistent with existing repository policy;
- package-smoke fixture mechanics;
- local implementation sequence.

Return M0065 to planning if implementation discovers that completion requires a material decision about:

- adding strongly typed canonical Display Identity or Access Path contracts;
- changing the canonical annotation encoding;
- changing Access Path name grammar/case semantics;
- supporting multiple/named Display Identities;
- adding Access Path operators/modes/priority;
- inferring either concept from keys, names, CLR shape, or another semantic;
- changing key semantics;
- adding target-specific projection behavior;
- changing JSON Schema `x-stm`;
- changing EF manifest version/content;
- changing persisted-model compatibility/versioning;
- changing public attribute shape;
- changing diagnostic severity policy;
- treating the change as breaking or patch-level rather than additive minor-version functionality.

If none of those conditions occurs, implementation must finish the milestone without reopening settled planning.
