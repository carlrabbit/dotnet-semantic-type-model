# M0055: Opinionated Relational Projection Contract and EF Core Package Reset for 2.5.0

## Status

Implemented; publication remains human-approved.

## Goal

Replace the current EF Core projection/application implementation with a smaller, deterministic, opinionated relational projection.

This is a breaking reset for `2.5.0`.

Backward compatibility is not required.

Do not preserve superseded APIs through aliases, compatibility modes, forwarding members, or `[Obsolete]` attributes. Delete APIs, types, branches, options, diagnostics, tests, and documentation that no longer belong to the approved relational contract.

The semantic model is authoritative. EF Core is only the persistence mechanism.

## Repository Profile

| Field | Value |
|---|---|
| Repository | `carlrabbit/dotnet-semantic-type-model` |
| Profile | `dotnet-library` |
| Role | Product repository and capability provider |
| Current released line | `2.4.x` |
| Target release | `2.5.0` |
| Milestone type | Breaking architectural reset |
| Execution mode | `ai-executed-human-reviewed` |
| Publication | Separate human-approved follow-up |

## Breaking-Change Authorization

The implementer is explicitly authorized to make breaking changes inside `SemanticTypeModel.EFCore` and directly related integration surfaces.

Required policy:

```text
No backward compatibility.
No Obsolete attributes.
No obsolete aliases.
No compatibility enum members.
No forwarding APIs.
No legacy application modes.
No compatibility branches.
No preservation of unused generic abstractions.
Delete everything superseded by the new contract.
```

Do not keep old APIs merely because they were public in `2.4.x`.

If a public type or member no longer represents the approved model, remove it.

## Architectural Contract

```text
Semantic model
  decides included types
  decides semantic roles
  decides inheritance
  decides ownership
  decides semantic links

Relational projection
  maps supported semantic concepts to one fixed relational representation
  emits diagnostics for unsupported combinations
  does not infer domain concepts from EF conventions

EF Core application
  applies the relational model
  does not discover additional semantic shape
```

Invariant:

```text
If a concept is not explicitly represented in the semantic model,
the EF projection must not invent it.
```

## Semantic Model Boundary

Start only from types explicitly annotated with `SemanticType`.

Additionally include:

```text
semantic base types required by annotated semantic inheritance
non-semantic base members inherited by included semantic types
owned value-kind target types reachable through explicit SemanticOwned
supported scalar and enum leaf types
```

Do not traverse or include as semantic types:

```text
implemented interfaces
generic constraints
IEquatable<T>
record-generated infrastructure
repository interfaces
DTOs
request/response types
framework helper types
System.Xml implementation types
System.Text.Json implementation types
collection implementation types
static interface members
method signatures
```

## Supported Semantic Roles

Initial EF roles:

```text
Entity
ValueKind
```

### Entity

```text
independent semantic identity
relational root
may participate in semantic inheritance
cannot be semantically owned
```

### ValueKind

```text
no independent identity
cannot be a DbSet root
cannot become an EF root entity
exists as part of another semantic type
stored as JSON when semantically owned
```

## Approved Relational Projection

| Semantic concept | Relational projection |
|---|---|
| Semantic entity | Table |
| Abstract semantic entity base | TPT base table |
| Derived semantic entity | TPT derived table |
| Scalar property | Column |
| Enum | String column |
| Strong scalar identifier | Underlying scalar column |
| Owned value-kind object | JSON object column |
| Owned value-kind collection | JSON array column |
| Nested owned value-kind object | Nested JSON object |
| Nested owned value-kind collection | Nested JSON array |
| Extension data | JSON object column |
| Entity reference | Identifier scalar only |
| Entity identifier collection | JSON array of identifiers, if explicitly supported |
| Entity object reference | Diagnostic |
| Entity object collection | Diagnostic |
| Owned entity | Diagnostic |
| Non-semantic base | No table; contributes inherited members |
| Arbitrary dictionary | Diagnostic unless explicit supported JSON shape |
| Binary payload | Binary column |
| DTO/interface/repository | Excluded |

## Entity Projection

Each semantic entity maps to a table.

No unannotated type may become an EF entity.

No value-kind type may become an EF entity.

### Inheritance

Use exactly one inheritance strategy:

```text
TPT
```

Example:

```text
Specification
ImportSpecification : Specification
WorkflowSpecification : Specification
```

Result:

```text
Specification table
ImportSpecification table with PK/FK to Specification
WorkflowSpecification table with PK/FK to Specification
```

Rules:

```text
semantic inheritance drives EF inheritance
CLR inheritance alone does not drive EF inheritance
abstract semantic entity may have a table
non-semantic abstract base never has a table
```

Delete configuration for TPH/TPC or alternative inheritance strategies.

## Scalar Projection

Supported direct scalars:

```text
string
bool
byte
short
int
long
float
double
decimal
Guid
DateOnly
TimeOnly
DateTime
DateTimeOffset
TimeSpan
Uri
byte[]
ReadOnlyMemory<byte> when deterministic provider mapping exists
```

Fixed decisions:

```text
enum -> string
Uri -> string
strong identifier wrapper -> underlying scalar
binary value -> binary column
```

## Ownership Projection

Ownership is semantic lifecycle containment.

It is not EF `OwnsOne` or `OwnsMany`.

### Owned Value-Kind Object

```csharp
[SemanticOwned(Kind = SemanticOwnershipKind.Object)]
public StructuredFileSource? XmlSource { get; init; }
```

Projection:

```text
nullable JSON object column
```

### Owned Value-Kind Collection

```csharp
[SemanticOwned(Kind = SemanticOwnershipKind.Collection)]
public IReadOnlyList<DerivedOrderField> DerivedFields { get; init; }
```

Projection:

```text
JSON array column
```

### Nested Ownership

Nested value-kind objects and collections remain nested inside the containing JSON document.

### Invalid Ownership

A semantic entity cannot be owned.

Diagnostic:

```text
EF_ENTITY_CANNOT_BE_OWNED
```

Delete EF-owned-navigation application paths, `OwnsOne`, `OwnsMany`, owned CLR entity metadata, and related generic configuration machinery.

## Value-Kind Storage Declaration

A non-scalar value-kind property must declare semantic ownership.

Diagnostic:

```text
EF_VALUE_KIND_STORAGE_NOT_DECLARED
```

Do not infer storage from CLR shape or EF conventions.

## Semantic Entity Links

A semantic link does not imply an EF relationship.

Preferred representation:

```text
entity identifier property -> scalar identifier column
```

Do not infer or create:

```text
HasOne
WithMany
WithOne
foreign key constraints
cascade behavior
join tables
navigations
relationship fixup
```

Entity-valued property diagnostic:

```text
EF_ENTITY_REFERENCE_REQUIRES_IDENTIFIER
```

Entity-object collection diagnostic:

```text
EF_ENTITY_COLLECTION_REQUIRES_IDENTIFIER_SHAPE
```

## Extension Data

Persist `SemanticExtensionData` as a JSON object column.

Do not inspect dictionary implementation internals.

Do not ignore extension data by default.

## Non-Semantic Base Classes

A non-semantic base may contribute inherited scalar properties, inherited extension data, technical descriptions, and CLR declaring-member metadata.

It produces no semantic type, table, EF entity, or DbSet.

## EF Convention Boundary

Allowed EF behavior:

```text
register explicit semantic entity CLR types
configure keys
configure columns
configure converters
configure TPT
configure JSON columns
ignore unprojected CLR properties
```

Forbidden EF behavior:

```text
discover entity types through navigations
discover relationships
discover owned entities
infer foreign keys
infer join tables
infer entity collections
promote value kinds to entities
scan implemented interfaces
scan arbitrary CLR graph dependencies
```

Post-application invariant:

```text
Every EF entity type corresponds to an explicitly projected semantic entity.
```

Unexpected EF entity diagnostic:

```text
EF_UNEXPECTED_CONVENTION_ENTITY
```

## New Minimal EF Model

Replace the current generic EF target model with a smaller relational contract.

Suggested shape:

```text
EfRelationalModel
  Entities
  Diagnostics

EfEntity
  SemanticTypeId
  ClrType
  Table
  BaseEntityId
  Key
  ScalarColumns
  JsonColumns
  BinaryColumns

EfScalarColumn
  PropertyId
  MemberName
  ColumnName
  ClrType
  ProviderType
  Converter
  Nullability

EfJsonColumn
  PropertyId
  MemberName
  ColumnName
  JsonShape
    Object
    Array
    ExtensionData
  ValueTypeId
  Nullability

EfEntityReferenceColumn
  PropertyId
  ReferencedEntityId
  IdentifierType
  ColumnName
```

Do not preserve general relationship graphs, `OwnsOne`/`OwnsMany` metadata, source-lineage graphs over every CLR type, shared-type entity application, generic unsupported-shape fallback models, multiple application modes, convention augmentation, or provider-neutral dynamic shared-type projection.

## Required Deletions

Audit and remove, where no longer required:

```text
EfCoreApplicationMode
ClosedClrModel / SharedTypeModel mode branching
SharedTypeProjection compatibility aliases
ClrConventionAugmentation compatibility aliases
ApplyEfCoreSemanticModelAsSharedTypes
shared-type Dictionary<string, object> entity application
EfCoreSourceLineage as a broad CLR graph model
owned navigation mappings
OwnsOne / OwnsMany application code
ValueObjectProjectionMode alternatives
inheritance strategy alternatives
generic relationship projection
automatic navigation and relationship configuration
legacy diagnostics tied to removed concepts
legacy tests that assert removed behavior
legacy docs and samples teaching removed behavior
```

If a small piece of an old type remains useful, extract the useful concept into the new model rather than retaining the old abstraction.

## Public API Policy

Design a small new public API.

Preferred flow:

```csharp
SemanticDerivationResult<EfRelationalModel> result =
    semanticModel.DeriveEfRelationalModel(options => { });

modelBuilder.ApplySemanticRelationalModel(result.Model);
```

A convenience method may exist:

```csharp
modelBuilder.ApplySemanticTypeModel(model, options => { });
```

but it must delegate to the same derivation and application pipeline.

Do not expose multiple competing application modes.

Do not add obsolete compatibility methods.

## Diagnostic Contract

Minimum required diagnostics:

```text
EF_ENTITY_CANNOT_BE_OWNED
EF_VALUE_KIND_STORAGE_NOT_DECLARED
EF_ENTITY_REFERENCE_REQUIRES_IDENTIFIER
EF_ENTITY_COLLECTION_REQUIRES_IDENTIFIER_SHAPE
EF_DICTIONARY_STORAGE_NOT_SUPPORTED
EF_UNSUPPORTED_SCALAR_TYPE
EF_ENTITY_KEY_REQUIRED
EF_STRONG_ID_SHAPE_NOT_SUPPORTED
EF_SEMANTIC_BASE_INHERITANCE_INVALID
EF_UNEXPECTED_CONVENTION_ENTITY
EF_JSON_VALUE_TYPE_NOT_SERIALIZABLE
EF_DUPLICATE_TABLE_NAME
EF_DUPLICATE_COLUMN_NAME
```

No raw infrastructure exceptions such as sequence failures, `Single()` failures, DbSet misclassification, or relationship-discovery exceptions.

## Real-Life Fixture Acceptance

Use the anonymized fixtures introduced by M0054.

### Specification Fixture

Required relational result:

```text
Specification table
ImportSpecification TPT table
WorkflowSpecification TPT table
```

Required JSON columns:

```text
DeliveryContract
Schedule
Polling
CsvSource
XmlSource
PrimaryApiSource
SecondaryApiSource
PostProcessing
DerivedProperties array
ExtensionData
```

Required negative assertions:

```text
no CsvSourceSpecification table
no CsvSourceSpecification DbSet requirement
no value-kind entity
no IEquatable lineage
no marker-interface lineage
no EF navigation inference
```

### Run-State Fixture

Required behavior:

```text
explicit semantic entities become tables
owned value state becomes JSON
record-struct identifiers become scalar columns
binary payload becomes binary column
DTOs and repositories are excluded
```

## Real EF Validation

Mandatory test layers:

```text
unit derivation tests
real ModelBuilder tests using CLR DbContext
SQLite in-memory EnsureCreated tests
SQLite insert/load tests for supported shapes
```

Do not consider projection DTO tests sufficient.

## Documentation

Rewrite EF documentation around the new contract.

Remove documentation for deleted APIs and modes.

## Release Preparation

Prepare `2.5.0`.

```sh
./eng/check.sh
./eng/package.sh 2.5.0
./eng/package-smoke.sh 2.5.0
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 2.5.0
```

Do not publish packages.

## Non-Goals

```text
backward compatibility with 2.4.x EF APIs
Obsolete-based migration period
TPH
TPC
OwnsOne
OwnsMany
relational navigation inference
foreign-key inference
many-to-many
shared-type entity models
keyless entities
generic relational dictionaries
provider-specific JSON querying
automatic migrations
cascade semantics
general-purpose EF model abstraction
```

## Acceptance Criteria

### Breaking Reset

- No obsolete compatibility APIs remain.
- No `[Obsolete]` attributes are introduced for removed EF APIs.
- No compatibility enum aliases remain.
- No legacy application-mode branches remain.
- Superseded types and code are deleted.
- Public docs describe only the new contract.

### Semantic Boundary

- Only annotated semantic types become semantic roots.
- Required semantic bases are included.
- Non-semantic bases contribute inherited members without becoming entities.
- Interfaces, record infrastructure, DTOs, repositories, and framework helpers are excluded.

### Relational Projection

- Entities map to tables.
- Semantic inheritance maps to TPT.
- Scalars map to columns.
- Enums map to strings.
- Strong IDs map to underlying scalar columns.
- Owned value objects map to JSON object columns.
- Owned value collections map to JSON array columns.
- Extension data maps to JSON object columns.
- Entity links require identifiers.
- Owned entities are rejected.

### EF Application

- EF does not discover additional semantic entities.
- No value-kind type is treated as a DbSet/root entity.
- No EF relationships are inferred.
- No `OwnsOne` or `OwnsMany` is used.
- Unexpected EF entities are rejected deterministically.

### Real Fixtures

- Specification / ImportSpecification / WorkflowSpecification produce three TPT tables.
- CsvSource-like value kinds produce JSON columns, not tables.
- Derived property collections produce JSON arrays.
- Both real-life fixtures build through ModelBuilder.
- SQLite `EnsureCreated` succeeds.
- Supported minimal instances can be inserted and loaded.

### Release

- `./eng/check.sh` passes.
- `./eng/package.sh 2.5.0` passes.
- `./eng/package-smoke.sh 2.5.0` passes.
- `./eng/samples.sh` passes.
- `./eng/public-docs.sh` passes.
- `./eng/release-check.sh 2.5.0` passes.
- No package is published inside the milestone.

## Human Review Requirements

Human review is required for final public API names, deletion inventory, new EF relational model names, JSON provider strategy, TPT application, extension-data persistence, diagnostic codes, fixture mappings, SQLite results, 2.5.0 package inventory, release notes, and publication approval.

## Completion Audit

### Iteration 1 fixed gaps

- Replaced the legacy EF projection, lineage, shared-type, ownership-navigation, relationship, inheritance-option, and application-mode surfaces with the minimal relational model.
- Added fixed TPT, scalar, enum, strong-ID, binary, JSON ownership, and extension-data mapping with deterministic diagnostics.
- Replaced legacy EF tests with derivation, real ModelBuilder, and SQLite create/insert/load acceptance tests.

### Iteration 2 fixed gaps

- Rewrote the package guide, usage guide, sample, compatibility statement, and release notes for the breaking 2.5.0 surface.
- Updated package smoke coverage and removed remaining current guidance for compatibility aliases and legacy modes.
- Verified package and release-candidate commands and recorded the package inventory before handoff.

Publication, tagging, and GitHub release creation remain intentionally outside this milestone and require human approval.

### Human-review follow-up gaps fixed

- Replaced the two-table approximation with the mandatory `Specification`, `ImportSpecification`, and `WorkflowSpecification` TPT fixture and exact owned JSON-column inventory.
- Added deterministic application diagnostics for unexpected convention entities, semantic-base and duplicate-column derivation validation, core key transformations, and exact CLR identity resolution.
- Added explicit `ReadOnlyMemory<byte>` conversion plus SQLite round trips for strong identifiers, binary columns, owned JSON collections, TPT entities, and extension data across both mandatory fixtures.

### Final semantic-authority audit

- Semantic base mapping now comes from canonical `dotnet.baseType` metadata and CLR inheritance is used only to validate agreement before TPT application.
- Nested JSON ValueKind graphs are validated recursively for declared ownership, entity containment, and serializer support.
- Strong-identifier shape failures and missing entity keys are derivation diagnostics, and application checks all errors before changing `ModelBuilder`.

### Diagnostic-contract completion audit

- Entity-object arrays are classified separately from undeclared ValueKind arrays and produce `EF_ENTITY_COLLECTION_REQUIRES_IDENTIFIER_SHAPE`.
- Nested JSON validation rejects entity arrays, arbitrary dictionaries, and unsupported object leaves before EF application.
- The focused diagnostic suite exercises every required diagnostic and proves invalid derivations leave `ModelBuilder` unchanged without surfacing infrastructure exceptions.
