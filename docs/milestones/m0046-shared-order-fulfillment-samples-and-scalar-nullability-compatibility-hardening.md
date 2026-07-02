# M0046: Shared Order Fulfillment Samples and Scalar Nullability Compatibility Hardening

## Status

Planned.

## Goal

Replace isolated code-first sample models with one shared Order Fulfillment semantic model, fix EF Core nullable value-type projection, and establish systematic scalar/nullability compatibility coverage across extraction, projections, runtime adapters, and package-based samples.

The milestone must demonstrate:

```text
one solution-wide semantic model
  -> several target-specific projections
  -> each consumer uses only the relevant portion

configuration registration
  -> explicitly selects option types
  -> unrelated configuration contracts remain unregistered
```

Customer must overlap across EF Core, JSON Schema/editing, System.Text.Json, and Power BI. Order and OrderLine must overlap across at least EF Core, JSON Schema, and Power BI.

## Repository Role and Maturity Assumptions

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Role | Product repository and capability provider |
| Profile | `dotnet-library` |
| Maturity | Published post-2.3.0 package set |
| Execution mode | `ai-executed-broad` |
| Provider scope | Projection correctness, tests, packages, diagnostics, and public sample behavior |
| Consumer/dogfood scope | Package-based samples consuming a shared domain through normal public APIs |

## Execution Mode

`ai-executed-broad`.

The architecture and package boundaries are already normalized, the required sample theme and overlap are explicit, the nullability invariant is concrete, and validation is strong. Human review remains required for sample clarity, public compatibility, and release-note wording.

## Scope

- Create `samples/OrderFulfillment.Domain/` as the shared annotated model project.
- Rework all code-first projection samples to consume the shared generated model.
- Keep `SemanticTypeModel.*` dependencies package-based.
- Allow projection samples to project-reference only the shared sample-domain project.
- Create or normalize a Configuration executable sample.
- Fix EF Core nullable scalar and enum value-type projection.
- Audit JSON Schema, System.Text.Json, Power BI, Configuration, extraction, and generator behavior for the same nullability gap.
- Add layered extraction, projection, and runtime/application tests.
- Make samples deterministic compatibility canaries with representative assertions.
- Update sample, projection, compatibility, and release-note documentation.

## Non-Goals

- No sample-domain NuGet package.
- No universal entity base class introduced only for code reuse.
- No single executable that demonstrates every projection.
- No exhaustive test matrix embedded in public samples.
- No external services, databases, secrets, or network dependency.
- No provider-specific EF database behavior.
- No patch release or publication in this milestone.
- No broad unrelated cleanup, copied guides, TBPs, issue templates, workflow docs, or non-root READMEs.

## Focus Areas

### 1. Shared Order Fulfillment Domain

Create a shared model containing at least:

```text
Customer
Product
Order
OrderLine
Warehouse
Shipment
Address
Money or equivalent monetary semantics
OrderStatus
ShipmentStatus
OrderSubmitted or equivalent event/envelope
OrderProcessingOptions
ColdStorageOptions
NotificationOptions
ProjectionProbe
```

Use current supported semantics where appropriate: entities, value objects, keys, relationships, requiredness, nullability, ownership, envelopes, lifecycle state, configuration roles/sections, analytical fact/dimension metadata, descriptions, formats, constraints, and conditional requiredness.

Do not use a universal CLR base class as the main sharing mechanism. Prefer composition. Inheritance is allowed only when meaningful and intentionally tested.

### 2. Required Cross-Sample Overlap

| Shared concept | Required use |
|---|---|
| Customer | EF Core, JSON Schema/editing, System.Text.Json, Power BI |
| Product | EF Core, Power BI, and JSON Schema where useful |
| Order | EF Core, JSON Schema, Power BI, event/serialization scenario |
| OrderLine | EF Core, JSON Schema, Power BI |
| Address/value object | EF Core and JSON Schema |
| Lifecycle enums | Multiple projections |
| Nullable monetary/temporal values | EF Core, JSON Schema, Power BI |
| Configuration types | Present in complete model; explicitly selected only by Configuration consumer |
| Event/envelope types | JSON Schema and/or System.Text.Json |

Customer editing must be a concrete JSON Schema scenario, not merely a schema export count.

### 3. Selection from a Complete Model

All code-first samples create the same shared semantic model, for example:

```csharp
TypeSchemaModel model = OrderFulfillmentSemanticModel.Create();
```

Each sample invokes only the relevant projection or adapter. Do not invent include/exclude APIs for the canonical model.

The Configuration sample must:

```csharp
services.AddSemanticOptions<OrderProcessingOptions>(configuration, model);
```

and verify that at least one other configuration type in the complete model remains unregistered.

### 4. EF Core Nullable Value-Type Correction

Required invariant:

```text
if an EF property is nullable
and its projected CLR type is a non-nullable value type
then its effective CLR type must be Nullable<T>.
```

The EF domain model and applied EF metadata must agree:

```text
EfPropertyDefinition.ClrType == IProperty.ClrType
EfPropertyDefinition.IsNullable == IProperty.IsNullable
```

Required cases:

```text
int?
long?
decimal?
bool?
DateTime?
DateTimeOffset?
Guid?
nullable enum
required non-nullable value-type control
required string control
optional string control
```

Review nullable foreign keys, alternate/generated keys, converter source/provider types, flattened value-object members, and nullable envelope payload scalars. Unsupported combinations must be diagnosed rather than fail late or silently coerce.

### 5. Layered Nullability Tests

Add three layers:

1. Extraction/generator tests: CLR declarations such as `int?` become correct scalar references plus cardinality/nullability.
2. Domain-projection tests: each target preserves or intentionally maps nullability.
3. Runtime/application tests: inspect real EF `IProperty`, JSON Schema output, System.Text.Json resolver behavior, and Configuration options behavior.

Use a reusable test fixture for the scalar/nullability matrix. Production code must not depend on the fixture.

Each audited projection must receive one of:

```text
passing proof
fix plus passing proof
explicit diagnostic and documented limitation
```

### 6. Samples as Compatibility Canaries

Samples must inspect representative output and fail deterministically. Printing counts alone is insufficient.

Required assertions:

**EF Core**
- Customer entity, key, relationship;
- supported owned/value-object behavior;
- nullable scalar has `Nullable<T>` and nullable EF metadata;
- required control property remains non-nullable.

**JSON Schema**
- editable Customer schema;
- required fields;
- optional nullable field distinguishes absence from null;
- enum and owned Address/object shape.

**System.Text.Json**
- overlapping Customer or Order event type resolves;
- property-name policy applies;
- nullable values remain valid;
- user resolver/context is wrapped, not replaced.

**Power BI**
- Order/OrderLine facts;
- Customer/Product dimensions;
- relationships;
- nullable numeric/date metadata without false unsupported diagnostics.

**Configuration**
- selected type registered;
- unrelated type unregistered;
- required-section behavior;
- optional nullable scalar binding;
- `RequiredWhen` behavior;
- generated helper delegates to runtime behavior when demonstrated.

### 7. Package-Based Sample Boundary

- All `SemanticTypeModel.*` dependencies remain `PackageReference`.
- No sample references `src/*`.
- Projection executables may reference `samples/OrderFulfillment.Domain`.
- The shared domain consumes authoring/generator packages normally.
- `./eng/samples.sh` restores/builds/runs against locally packed artifacts.
- Missing local packages must fail sample validation.

### 8. Documentation

Update:

```text
README.md
docs/engineering/samples.md
docs/specs/type-model-ef-core-projection.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/guides/system-text-json.md
public-docs/guides/power-bi-projection.md
public-docs/guides/configuration.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

Document the shared model, overlap, target selection, explicit Configuration registration, canary-vs-test distinction, and the released 2.3.0 EF nullable-value-type defect.

## Implementation Constraints

- Keep scalar identity, presence, requiredness, and nullability distinct.
- Keep canonical semantics projection-neutral.
- Keep target-specific CLR/metadata mapping in target packages.
- Do not silently drop unsupported properties.
- Keep projection execution code outside the shared domain.
- Keep target-specific annotations intentional and limited.
- Keep sample output deterministic.
- Use canonical `eng/` scripts.
- Do not weaken tests or sample assertions.
- Do not publish packages.

## Required Authority Documents

### Always Read

```text
AGENTS.md
README.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/samples.md
docs/engineering/packaging.md
public-docs/samples.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

### Sample Architecture

```text
docs/decisions/consumer-facing-package-based-samples.md
docs/decisions/shared-order-fulfillment-sample-domain.md
docs/milestones/m0025-consumer-facing-samples-and-package-based-sample-validation.md
samples/*/*.csproj
samples/*/*.cs
eng/samples.sh
public-docs/samples/*.md
```

### Semantics and Projections

```text
docs/specs/type-model-dotnet-extraction.md
docs/specs/type-model-compile-time-generator.md
docs/specs/core-semantic-vocabulary.md
docs/specs/type-model-transformation-and-domain-derivation.md
docs/specs/type-model-ef-core-projection.md
docs/specs/json-schema-domain-model-and-export.md
docs/specs/system-text-json-domain-model-and-resolver-projection.md
docs/specs/type-model-powerbi-tom-projection.md
docs/specs/configuration-domain-model-and-options-projection.md
```

### Implementation and Tests

```text
src/SemanticTypeModel.DotNet/
src/SemanticTypeModel.Generators/
src/SemanticTypeModel.EFCore/
src/SemanticTypeModel.JsonSchema/
src/SemanticTypeModel.SystemTextJson/
src/SemanticTypeModel.PowerBI/
src/SemanticTypeModel.Configuration/
tests/unit/SemanticTypeModel.*.Tests.Unit/
```

Ordinary implementation agents do not need to read `.guide-profile.json` or `.guide-sync/`.

## Files or Areas Likely Affected

```text
samples/OrderFulfillment.Domain/
samples/code-first-json-schema/
samples/code-first-ef-core/
samples/code-first-powerbi/
samples/system-text-json-resolver/
samples/runtime-di/
samples/configuration-options/
src/SemanticTypeModel.EFCore/
other projection source only when audit proves a defect
tests/unit/SemanticTypeModel.*.Tests.Unit/
eng/samples.sh
docs/engineering/samples.md
docs/specs/type-model-ef-core-projection.md
other projection specs only when behavior changes
public-docs/samples.md
public-docs/samples/*.md
public-docs/guides/*.md
public-docs/api/compatibility.md
public-docs/release-notes.md
README.md
.guide-sync/pending/
```

## Validation Tiers and Concrete Commands

### Tier 1

Confirm actual project paths before running:

```sh
./eng/test-filter.sh Nullable
./eng/test-filter.sh Nullability
./eng/test-filter.sh EFCore
./eng/test-project.sh tests/unit/SemanticTypeModel.EFCore.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.DotNet.Tests.Unit
./eng/test-project.sh tests/unit/SemanticTypeModel.Generators.Tests.Unit
```

Run other projection test projects when the audit adds or changes tests.

### Tier 2

```sh
./eng/check.sh
```

### Tier 3

```sh
./eng/package.sh 0.0.0-m0046
./eng/package-smoke.sh 0.0.0-m0046
./eng/samples.sh
./eng/public-docs.sh
```

Do not publish.

## Acceptance Criteria

- Shared `samples/OrderFulfillment.Domain` exists.
- All code-first samples consume the same generated model.
- Customer is used by EF Core, JSON Schema/editing, System.Text.Json, and Power BI.
- Order/OrderLine overlap across EF Core, JSON Schema, and Power BI.
- Configuration types coexist in the full model without automatic registration.
- Selected Configuration registration and unrelated non-registration are verified.
- No artificial universal base class is introduced.
- Original EF nullable value-type bug has a regression test.
- Nullable integral, decimal, Boolean, date/time, GUID, and enum cases are covered.
- EF domain and applied metadata agree on CLR type/nullability.
- Extraction/generator tests prove nullable CLR declarations reach the canonical model.
- JSON Schema distinguishes optional presence from nullable values.
- System.Text.Json, Power BI, and Configuration nullable behavior is proven or explicitly limited.
- Samples assert representative invariants and fail on regressions.
- Samples retain package-based `SemanticTypeModel.*` consumption.
- `./eng/check.sh` passes.
- package, smoke, samples, and public-doc validation pass for `0.0.0-m0046`.

## Direct Documentation Impact

```text
docs/engineering/samples.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/decisions/shared-order-fulfillment-sample-domain.md
docs/specs/type-model-ef-core-projection.md
README.md
public-docs/samples.md
public-docs/samples/*.md
public-docs/guides/ef-core-projection.md
public-docs/guides/json-schema.md
public-docs/guides/system-text-json.md
public-docs/guides/power-bi-projection.md
public-docs/guides/configuration.md
public-docs/api/compatibility.md
public-docs/release-notes.md
```

Update other projection specs only when their authoritative behavior changes.

## Deferred Documentation Synchronization Hints

Created:

```text
.guide-sync/pending/m0046-shared-samples-and-nullability-hardening.md
```

It tracks patch-release synchronization after M0046. Ordinary implementation agents are not required to read it.

## Human Review Requirements

Human review is required for domain realism, overlap sufficiency, Customer editing clarity, EF compatibility implications, nullable enum/converter behavior, unsupported key/relationship diagnostics, sample readability, Configuration selection behavior, and patch-release wording.

## Out-of-Scope Guide Migration Work

M0046 is not a guide migration. Do not read, copy, or modify external guide documents and do not reference them as target-repository authority.
