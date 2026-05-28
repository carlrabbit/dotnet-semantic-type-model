# M0015: EF Core Projection Hardening

## Status

Active implementation milestone.

## Goal

Make the EF Core projection useful for real code-first model-building scenarios by projecting a canonical `TypeSchemaModel` into EF Core model configuration through a configurable `ModelBuilder` API.

This milestone builds on the existing canonical type model, .NET type extraction, semantic annotations, and EF Core projection prototype. It hardens the EF Core projection into a practical, documented, package-ready capability.

The preferred consuming API shape is configurable:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplySemanticTypeModel(
        AppSemanticTypeModel.Create(),
        options =>
        {
            options.DefaultSchema = "app";
            options.UseTableNaming(SemanticEfNamingPolicy.SnakeCase);
        });
}
```

The projection must remain downstream of the canonical semantic model:

```text
C# attributes / conventions / runtime schema input
        ↓
canonical TypeSchemaModel
        ↓
EF Core projection
        ↓
ModelBuilder configuration
```

The EF Core projection must not become a parallel type model.

---

## Scope

This milestone covers the EF Core package surface, projection API, model-building behavior, configuration options, diagnostics, tests, documentation, and package-smoke scenarios required to make EF Core projection usable from consumers.

Required package focus:

```text
SemanticTypeModel.EFCore
```

Related package dependencies may include:

```text
SemanticTypeModel.Abstractions
SemanticTypeModel.Core
SemanticTypeModel.DotNet
SemanticTypeModel.Generators
```

The milestone must make EF Core projection consume the canonical model and annotation system. It must not duplicate semantic type discovery or source-generator responsibilities.

---

## Non-Goals

- Reworking the canonical type model.
- Reworking the semantic annotation model beyond what is required to consume it.
- Creating a runtime `DbContext` generator.
- Creating migrations automatically.
- Connecting to a real database as part of the projection baseline.
- Supporting every EF Core provider-specific feature.
- Inferring rich relational models from arbitrary JSON Schema without explicit semantics.
- Adding Power BI, JSON Editor, or JSON Schema projection features.
- Making EF Core projection the canonical representation of the model.

---

## Required Reading

- `docs/TERMINOLOGY.md`
- `docs/SPECS.md`
- `docs/MILESTONES.md`
- `docs/ENGINEERING.md`
- `docs/GUARDRAILS.md`
- `docs/PUBLIC-DOCS.md`
- `docs/guardrails/testing.md`
- `docs/guardrails/implementation.md`
- `docs/guardrails/languages/dotnet.md`
- `docs/engineering/command-contract.md`
- `docs/engineering/packaging.md`
- `docs/tbps/feature-implementation.md`
- `docs/tbps/public-documentation-update.md`
- `docs/specs/type-model-ef-core-projection.md`
- `docs/specs/type-model-annotations.md`
- `docs/milestones/m0014-semantic-type-annotation-usability.md`
- `docs/research/project-setup-guide-v5.md`
- `docs/research/engineering-guide-v4.md`

---

## Dependencies

This milestone assumes these capabilities exist or are being finalized:

- Canonical semantic type model hardening.
- Diagnostics with stable codes and model paths.
- Transformation pipeline support.
- .NET type-system extraction.
- Compile-time generator baseline.
- Semantic type annotation usability from M0014.
- Initial EF Core projection prototype from M0008.
- Prerelease packaging and release-readiness expectations from M0013.

If M0014 is incomplete, this milestone may proceed only against the stable subset of projection-neutral annotations and must avoid inventing conflicting EF-specific semantics.

---

## Design Principles

### Canonical Model First

The EF Core projection must consume `TypeSchemaModel` or an equivalent canonical semantic model representation. It must not inspect arbitrary CLR types directly except through already-defined .NET extraction/generator APIs.

### Configurable but Predictable

The API must expose useful options for naming, schema, unsupported shapes, owned-type behavior, relationship behavior, enum storage, and provider-specific extension points. Defaults must be deterministic.

### Provider-Neutral Baseline

The default projection must be provider-neutral. SQL Server, PostgreSQL, SQLite, or other provider-specific behavior may be supported through explicit extension points or namespaced annotations, but provider-specific behavior must not be required for the baseline.

### Diagnostics over Silent Skips

Unsupported or ambiguous model shapes must produce diagnostics with stable codes and model paths. The projection must not silently ignore public model elements unless configured to do so and documented.

### EF Core as Projection, Not Authority

EF Core configuration must be a projection of canonical semantic information. EF Core annotations may refine projection behavior, but they must not define general semantic truth.

---

## Public API Requirements

Add or harden a public API in `SemanticTypeModel.EFCore` similar to:

```csharp
public static class SemanticTypeModelEfCoreExtensions
{
    public static SemanticEfCoreProjectionResult ApplySemanticTypeModel(
        this ModelBuilder modelBuilder,
        TypeSchemaModel model,
        Action<SemanticEfCoreProjectionOptions>? configure = null);
}
```

The exact type names may differ if the repository already has established naming conventions, but the API must preserve the following shape:

- extension method on `ModelBuilder`;
- accepts the canonical semantic model;
- accepts an optional configuration callback;
- returns a projection result with diagnostics and useful metadata;
- does not require database connection or provider service resolution;
- does not publish partial or hidden failures silently.

Suggested supporting types:

```text
SemanticEfCoreProjectionOptions
SemanticEfCoreProjectionResult
SemanticEfCoreNamingPolicy
SemanticEfCoreUnsupportedShapePolicy
SemanticEfCoreOwnedTypePolicy
SemanticEfCoreEnumStoragePolicy
SemanticEfCoreRelationshipPolicy
SemanticEfCoreProviderOptions
SemanticEfCoreDiagnosticCodes
```

The API must also support non-extension projection for tests and tools, for example:

```csharp
public interface ISemanticEfCoreProjection
{
    SemanticEfCoreProjectionResult Apply(
        ModelBuilder modelBuilder,
        TypeSchemaModel model,
        SemanticEfCoreProjectionOptions options);
}
```

or an equivalent service-friendly abstraction.

---

## Configurable API Shape

The options object should support at least these configuration areas:

```csharp
public sealed class SemanticEfCoreProjectionOptions
{
    public string? DefaultSchema { get; set; }

    public SemanticEfCoreNamingPolicy TableNamingPolicy { get; set; }

    public SemanticEfCoreNamingPolicy ColumnNamingPolicy { get; set; }

    public SemanticEfCoreUnsupportedShapePolicy UnsupportedShapePolicy { get; set; }

    public SemanticEfCoreOwnedTypePolicy OwnedTypePolicy { get; set; }

    public SemanticEfCoreEnumStoragePolicy EnumStoragePolicy { get; set; }

    public SemanticEfCoreRelationshipPolicy RelationshipPolicy { get; set; }

    public bool ConfigureIndexes { get; set; }

    public bool ConfigureAlternateKeys { get; set; }

    public bool ConfigureConcurrencyTokens { get; set; }

    public bool ConfigureValueGeneration { get; set; }
}
```

A fluent configuration surface may be added for usability:

```csharp
modelBuilder.ApplySemanticTypeModel(
    model,
    options => options
        .UseDefaultSchema("app")
        .UseTableNaming(SemanticEfNamingPolicy.SnakeCase)
        .UseColumnNaming(SemanticEfNamingPolicy.SnakeCase)
        .TreatUnsupportedShapesAsDiagnostics());
```

Do not require fluent style if object-initializer configuration is simpler and consistent with repository style.

---

## Mapping Requirements

### Entity Types

Object types with entity semantics must map to EF Core entity types.

Required behavior:

- include explicitly selected entity types;
- respect semantic entity/value-object roles;
- apply table name and schema;
- apply configured table naming policy;
- detect duplicate table names after normalization;
- report diagnostics for entity types that cannot be mapped.

### Value Objects and Owned Types

Object types with value-object semantics must map according to configured owned-type policy.

Required behavior:

- support owned/value-object properties;
- support nested value object only when explicitly supported by the policy;
- reject or diagnose unsupported nested object graphs;
- avoid treating value objects as independent root entities unless explicitly configured.

### Scalar Properties

Scalar semantic properties must map to EF Core scalar properties.

Required behavior:

- map string, bool, integer, number, decimal, date/time, GUID, binary, and enum-compatible scalar kinds;
- preserve required versus optional semantics;
- preserve nullable value semantics separately from property presence where possible;
- apply max length, precision, scale, Unicode, fixed-length, and format hints when available;
- apply column names through annotation or naming policy;
- detect duplicate column names after normalization.

### Keys

Canonical key definitions must map to EF Core keys.

Required behavior:

- primary keys;
- alternate keys when configured;
- natural/surrogate key metadata if represented by canonical model;
- generated key behavior;
- composite keys;
- diagnostics for missing primary keys unless explicitly configured as keyless;
- diagnostics for key references to missing properties.

### Indexes

Index annotations or canonical index metadata must map to EF Core indexes.

Required behavior:

- single-property indexes;
- composite indexes;
- unique indexes;
- configured index names where supported;
- diagnostics for duplicate or invalid index definitions.

### Relationships

Canonical relationship definitions must map to EF Core relationships.

Required behavior:

- one-to-one;
- one-to-many;
- many-to-one;
- many-to-many only if deliberately supported;
- principal/dependent side selection;
- foreign key property mapping;
- navigation-name hints if available;
- delete behavior mapping;
- diagnostics for ambiguous principal/dependent side;
- diagnostics for relationship references to missing types or properties;
- diagnostics for unsupported relationship cardinality.

### Enums

Enum definitions and enum scalar properties must map according to configured enum storage policy.

Required behavior:

- string storage;
- numeric storage;
- annotation-based override;
- diagnostics for unsupported enum storage metadata;
- deterministic mapping of enum names and values.

### Unsupported Shapes

The projection must handle unsupported shapes explicitly.

Required behavior:

- arrays must be unsupported by default unless a configured conversion/JSON-column policy exists;
- dictionaries must be unsupported by default unless a configured conversion/JSON-column policy exists;
- unions must be unsupported by default unless a configured discriminator/inheritance policy exists;
- recursive object graphs must be diagnosed if they cannot be represented safely;
- JSON-column, converter, or provider-specific strategies must be explicit and documented if implemented.

---

## Annotation Consumption

The EF Core projection must consume projection-neutral semantic annotations from M0014 and EF-specific override annotations from `SemanticTypeModel.EFCore`.

Projection-neutral examples:

```text
semantic role
semantic key
semantic relationship
semantic requiredness
semantic display/name/description
semantic scalar constraints
semantic enum metadata
```

EF-specific examples:

```text
efCore.tableName
efCore.schema
efCore.columnName
efCore.maxLength
efCore.precision
efCore.scale
efCore.unicode
efCore.fixedLength
efCore.valueGenerated
efCore.concurrencyToken
efCore.owned
efCore.keyless
efCore.index
efCore.uniqueIndex
efCore.deleteBehavior
efCore.conversion
efCore.provider.*
```

Rules:

- EF-specific annotations refine EF projection only.
- EF-specific annotations must not alter canonical semantic meaning.
- Conflicting annotations must produce diagnostics.
- Provider-specific annotations must be namespaced and optional.
- Attribute-based annotation sources and runtime annotation sources must produce equivalent projection behavior.

---

## Diagnostics Requirements

Add stable diagnostics for EF Core projection failures and warnings.

Suggested diagnostic categories:

```text
STMEF0001 missing primary key
STMEF0002 duplicate table name
STMEF0003 duplicate column name
STMEF0004 unsupported scalar type
STMEF0005 unsupported collection shape
STMEF0006 unsupported dictionary shape
STMEF0007 unsupported union shape
STMEF0008 relationship target type missing
STMEF0009 relationship property missing
STMEF0010 ambiguous relationship side
STMEF0011 invalid delete behavior
STMEF0012 conflicting entity/value-object annotations
STMEF0013 keyless entity also defines key
STMEF0014 invalid precision/scale
STMEF0015 invalid index definition
STMEF0016 provider-specific annotation unsupported by baseline projection
```

The final codes may follow the repository’s existing diagnostic code scheme, but they must be stable, documented, and tested.

Every diagnostic must include:

- stable code;
- severity;
- message;
- model path;
- relevant type/member name where available;
- public documentation entry if diagnostic is public.

---

## Testing Requirements

Use short-running tests by default. Do not require a live database for baseline projection tests.

Required test groups:

### Projection API Tests

- configurable `ModelBuilder.ApplySemanticTypeModel(...)` extension exists;
- options callback is applied;
- result includes diagnostics;
- projection can be used without opening a database connection.

### Entity Mapping Tests

- simple entity with generated primary key;
- entity with required and optional properties;
- entity with table and schema override;
- duplicate table name diagnostic.

### Property Mapping Tests

- string max length;
- decimal precision and scale;
- nullable value type;
- required reference type;
- enum string/numeric storage;
- duplicate column name diagnostic.

### Key and Index Tests

- primary key;
- composite key;
- alternate key;
- natural key metadata if supported;
- unique index;
- invalid index diagnostic.

### Relationship Tests

- one-to-many;
- many-to-one;
- one-to-one if supported;
- missing target diagnostic;
- ambiguous principal/dependent diagnostic;
- delete behavior mapping.

### Owned Type Tests

- entity with owned value object;
- nested owned value object according to policy;
- unsupported nested object diagnostic.

### Unsupported Shape Tests

- array property diagnostic;
- dictionary property diagnostic;
- union property diagnostic;
- unsupported recursive graph diagnostic if applicable.

### Attribute Integration Tests

- semantic annotations from M0014 produce expected EF projection;
- EF-specific annotations override EF projection behavior;
- conflicting annotations produce diagnostics.

### Package Smoke Tests

Ensure `SemanticTypeModel.EFCore` can be consumed from a packed package and a clean consumer project can call the configurable EF Core projection API.

---

## Documentation Requirements

Create or update:

```text
docs/specs/ef-core-projection.md
docs/engineering/packaging.md
public-docs/packages.md
public-docs/guides/ef-core-projection.md
public-docs/api/
public-docs/diagnostics.md
public-docs/diagnostics/
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/release-notes.md
```

Documentation must explain:

- how EF Core projection fits after the canonical semantic model;
- how to call the configurable API;
- what is provider-neutral;
- what is explicitly unsupported;
- how diagnostics are reported;
- how annotations influence EF Core projection;
- how value objects and owned types are handled;
- how relationships are configured;
- how to keep provider-specific behavior explicit.

Do not create README files outside the repository root.

---

## Public Documentation Impact

This milestone affects public package behavior and must update public documentation.

Affected surfaces:

```text
README.md
public-docs/packages.md
public-docs/guides/ef-core-projection.md
public-docs/api/
public-docs/diagnostics.md
public-docs/diagnostics/
public-docs/nuget/SemanticTypeModel.EFCore.md
public-docs/release-notes.md
```

If diagnostics are public, each new diagnostic must have a public diagnostic reference entry or be included in an indexed diagnostics reference document.

---

## Engineering and Release Readiness

Ensure the milestone remains compatible with prerelease packaging from M0013.

Required validation commands:

```sh
./eng/check.sh
./eng/release-check.sh 0.1.0-alpha
```

If `release-check` is not appropriate during implementation, run the smallest relevant validation set and document the exact reason in the completion report.

The EF Core package smoke test must consume packed packages, not project references.

---

## Acceptance Criteria

- `SemanticTypeModel.EFCore` exposes a configurable `ModelBuilder` projection API.
- The API accepts the canonical semantic model and an options callback.
- The projection returns a result containing diagnostics and useful projection metadata.
- Entity, property, key, index, enum, relationship, and owned-type mappings are implemented for the supported baseline.
- Unsupported arrays, dictionaries, unions, ambiguous relationships, missing keys, duplicate names, and conflicting annotations produce stable diagnostics.
- Projection-neutral annotations from M0014 influence EF projection correctly.
- EF-specific annotations refine EF projection only and do not redefine canonical semantics.
- Provider-neutral defaults are deterministic.
- Provider-specific behavior is explicit, optional, and namespaced.
- Tests cover the required mapping, diagnostics, and package-smoke scenarios.
- Public documentation explains the EF Core projection and its limitations.
- NuGet package documentation for `SemanticTypeModel.EFCore` is current.
- No non-root README files are introduced.
- `./eng/check.sh` succeeds.
- Release readiness remains compatible with `./eng/release-check.sh 0.1.0-alpha`.

---

## Completion Report

When closing this milestone, report:

- final public API shape;
- supported EF Core mappings;
- unsupported shapes and diagnostics;
- provider-specific behavior, if any;
- tests added or updated;
- package-smoke validation result;
- public docs updated;
- validation commands run;
- any follow-up issues required.
