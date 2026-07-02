# Samples Engineering

## Status

Authoritative engineering policy for public samples.

## Purpose

Define package-based public samples as executable documentation and representative compatibility canaries. Samples do not replace exhaustive tests.

## Validation

```sh
./eng/samples.sh
```

Samples validate locally packed SemanticTypeModel packages. Missing local packages must fail validation.

## Core Rules

- Public samples live under `samples/`.
- `SemanticTypeModel.*` dependencies use `PackageReference`.
- No sample references `src/*`.
- Generator usage occurs through normal MSBuild/NuGet behavior.
- No manual Roslyn driver, source-string compilation, reflection-based generated-provider invocation, external service, secret, database, or network dependency.
- No local README files; docs live under `public-docs/samples/`.
- Samples are deterministic.
- Samples inspect representative output and fail on broken invariants; printing counts alone is insufficient.

## Shared Domain

Code-first samples use:

```text
samples/OrderFulfillment.Domain/
```

The shared domain owns annotated types and generator configuration, contains no projection execution code, is not published, and consumes authoring/generator packages normally.

Projection samples may reference it:

```xml
<ProjectReference Include="../OrderFulfillment.Domain/OrderFulfillment.Domain.csproj" />
```

This does not replace package-based consumption of SemanticTypeModel libraries.

## Overlap

At minimum:

| Concept | Required use |
|---|---|
| Customer | EF Core, JSON Schema/editing, System.Text.Json, Power BI |
| Product | EF Core, Power BI, JSON Schema where useful |
| Order/OrderLine | EF Core, JSON Schema, Power BI |
| Address/value objects | EF Core and JSON Schema |
| Lifecycle enums | Multiple projections |
| Nullable values | EF Core, JSON Schema, Power BI |
| Configuration types | Explicit Configuration registration |
| Event/envelope types | JSON Schema and/or System.Text.Json |

## Selection

Each executable uses the complete generated model but invokes only its target projection. Configuration explicitly selects option types through `AddSemanticOptions<TOptions>` and verifies unrelated types remain unregistered. Do not add exclusion lists for the canonical model.

## Composition

Do not create a universal base class solely for reuse. Prefer composition and value objects. Use inheritance only when meaningful and intentionally demonstrated.

## Canary Assertions

- EF Core: entity, key, relationship, nullable scalar, required scalar.
- JSON Schema: editable Customer contract, required field, optional nullable field, enum, owned object.
- System.Text.Json: resolver wrapping, naming, nullable values, shared Customer/Order type.
- Power BI: facts, dimensions, relationships, nullable analytical field.
- Configuration: explicit registration, required section, nullable value, unrelated options unregistered.

Exhaustive scalar/nullability matrices belong in tests.

## Required Sample Set

| Scenario | Purpose |
|---|---|
| JSON Schema roundtrip | Supported import/export workflow. |
| Code-first JSON Schema | Editable contracts from shared model. |
| Code-first EF Core | Provider-neutral EF metadata from shared model. |
| Code-first Power BI | Facts/dimensions from shared model. |
| System.Text.Json resolver | Resolver customization for shared contracts. |
| Runtime DI | Consumer model/provider composition. |
| Configuration options | Explicit options registration from complete model. |

## Documentation Contract

Review `README.md`, `public-docs/samples.md`, `public-docs/samples/*.md`, affected guides, `eng/samples.sh`, and the command contract. Sample docs state goal, shared-domain slice, packages, run command, expected output, representative assertions, consumer pattern, non-goals, and project path.

## Validation Tiers

| Tier | Use |
|---|---|
| 0 | Docs and policy |
| 1 | Affected sample and focused projection tests |
| 2 | Repository completion gate |
| 3 | Package, smoke, all samples, public docs |
| 4 | Only explicit publication work |

## M0046 shared sample-domain canaries

The Order Fulfillment sample domain is the shared package-based source for code-first projection samples. Executable samples must assert representative invariants instead of printing counts only, while exhaustive nullability matrices remain in unit tests.
