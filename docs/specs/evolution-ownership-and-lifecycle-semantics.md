# Evolution, Ownership, and Lifecycle Semantics Specification

## Status

Authoritative behavioral specification.

## Purpose

Define projection-neutral semantics for model evolution, ownership containment, lifecycle state, temporal validity, and extension-data compatibility.

This specification is authoritative for:

- ownership semantics;
- versioning and revision semantics;
- temporal validity semantics;
- lifecycle state semantics;
- extension-data compatibility-bag semantics;
- target projection defaults for JSON Schema, EF Core, and Power BI;
- System.Text.Json extension-data normalization policy;
- diagnostics and inspection expectations for these semantics.

## Core Principle

M0034 semantics describe model meaning, not active behavior.

Target packages may project that meaning into schema, storage, or analytical metadata. They must not silently generate business behavior such as query filters, workflow transitions, temporal-table configuration, or DAX measures unless the user explicitly selects a target-specific policy.

## Core vs Target-Specific Meaning

| Meaning | Core semantic | Target-specific policy examples |
|---|---|---|
| Contained lifecycle/composition | `Ownership`, `OwnedObject`, `OwnedCollection` | EF Core `OwnsOne` / `OwnsMany`, JSON Schema inline/ref, Power BI flatten/child table |
| Versioned model data | `Versioned`, `Version`, `Revision`, `CurrentVersion` | EF alternate key/index, Power BI revision history, JSON Schema metadata |
| Effective date interval | `TemporalValidity`, `ValidFrom`, `ValidTo` | EF index, Power BI timeline, JSON Schema date-time fields |
| State/status | `LifecycleState` | EF enum storage, Power BI slicer/category, JSON Schema enum/schema metadata |
| Unknown compatibility data | `ExtensionData` | JSON Schema `additionalProperties`, EF JSON/summary storage, Power BI boolean/count summary |

## Canonical Annotation Keys

If the implementation represents these semantics as canonical annotations before or instead of structured primitives, reserve these keys:

```text
schema.ownership
schema.ownership.kind
schema.ownership.collection
schema.ownership.owner
schema.versioned
schema.version
schema.revision
schema.currentVersion
schema.temporalValidity
schema.validFrom
schema.validTo
schema.lifecycleState
schema.extensionData
schema.extensionData.keyType
schema.extensionData.valueType
```

Projection-specific representation details must use target namespaces such as:

```text
jsonSchema.*
efCore.*
powerBi.*
systemTextJson.*
```

## Ownership Semantics

### Ownership

**Kind:** Relationship/property semantic.

**Description:** A relationship or member is lifecycle-contained by an owner and is part of the owner's composition boundary.

**Best used when:** The owned object does not stand independently, is created/removed with the owner, or should be projected as contained data.

**Avoid when:** The target object is independently identified, referenced by multiple owners, or only transported as an envelope payload.

**Projection implications:** JSON Schema may inline or reference owned structure. EF Core may use owned mapping policies. Power BI may flatten simple owned references or project owned collections as child tables when configured.

**Diagnostics / ambiguity notes:** Shared ownership, circular ownership, owned entity roots, and ownership conflicts with independent entity projection are diagnosable.

### OwnedObject

**Kind:** Property semantic.

**Description:** A single object-valued property is owned by the containing type.

**Default projections:**

```text
JSON Schema: structured property schema or $defs reference according to object export policy.
EF Core: OwnsOne with same-table columns by default.
Power BI: flatten simple scalar owned members by default when safe.
```

### OwnedCollection

**Kind:** Property semantic.

**Description:** A collection-valued property contains owned elements whose lifecycle follows the owner.

**Default projections:**

```text
JSON Schema: array of owned item schema.
EF Core: explicit policy required unless a package default is configured.
Power BI: explicit policy required for child table, flattening, summary, or ignore.
```

## Versioning and Revision Semantics

### Versioned

**Kind:** Type semantic.

**Description:** A type participates in model/data evolution where multiple versions or revisions may exist over time.

**Best used when:** Instances carry version/revision fields, evolve over time, or require compatibility handling across revisions.

**Avoid when:** A property is merely a display label or package/software version unrelated to the instance semantics.

**Projection implications:** JSON Schema preserves semantic metadata. EF Core maps version/revision fields as scalar columns and may derive optional alternate key/index metadata by explicit policy. Power BI exposes revision/version fields as reporting columns.

### Version

**Kind:** Property semantic.

**Description:** A version identifier, usually semantic, textual, numeric, or externally meaningful.

**Default projections:**

```text
JSON Schema: normal property schema plus semantic version metadata.
EF Core: scalar column according to type mapping.
Power BI: categorical or numeric reporting column.
```

### Revision

**Kind:** Property semantic.

**Description:** A monotonic or otherwise ordered revision identifier for a versioned object or envelope.

**Default projections:**

```text
JSON Schema: normal property schema plus revision metadata.
EF Core: required scalar column when required in canonical model; no primary-key change by default.
Power BI: revision history column.
```

EF Core must not silently replace primary keys with revision/composite keys. Alternate key/index derivation requires explicit policy or safe configured default.

### CurrentVersion

**Kind:** Property semantic.

**Description:** A marker indicating that an instance represents the current or active revision/version.

**Default projections:**

```text
JSON Schema: boolean/status property schema plus semantic metadata.
EF Core: scalar column; no global query filter by default.
Power BI: filter/slicer candidate.
```

## Temporal Validity Semantics

### TemporalValidity

**Kind:** Type or property-group semantic.

**Description:** A type or object is valid/effective for a time interval.

**Best used when:** The model needs effective-dated behavior, validity windows, or historical reporting.

**Avoid when:** The timestamps are merely audit timestamps such as created/modified time.

**Projection implications:** JSON Schema exports temporal fields and semantic metadata. EF Core maps scalar date/time columns and optional indexes by policy. Power BI exposes timeline/effective-date metadata.

### ValidFrom

**Kind:** Property semantic.

**Description:** The start of an interval for which the object or value is valid.

### ValidTo

**Kind:** Property semantic.

**Description:** The optional end of an interval for which the object or value is valid.

**Default rules:**

- `ValidFrom` must use a temporal-compatible type.
- `ValidTo` must use a temporal-compatible nullable or optional type unless closed intervals are explicitly required.
- `ValidTo` without `ValidFrom` is diagnosable.
- `ValidFrom <= ValidTo` is semantic meaning but not automatically enforced by all targets.

## Lifecycle State Semantics

### LifecycleState

**Kind:** Property semantic.

**Description:** A property represents the lifecycle/status state of an entity, envelope, specification, workflow, document, operation, or value.

**Best used when:** Consumers need to distinguish states such as draft, active, retired, failed, approved, published, or archived.

**Avoid when:** The value is merely a display category, arbitrary tag, or target-specific UI grouping.

**Projection implications:** JSON Schema exports enum/scalar state schema. EF Core maps a scalar/enum column according to enum policy. Power BI treats the value as categorical/slicer-suitable metadata.

Lifecycle state must not generate workflow transition rules by default.

## Extension Data Semantics

### ExtensionData

**Kind:** Property semantic.

**Description:** A property captures unknown, unmodeled, forward-compatible, or externally supplied members for compatibility and round-tripping across model revisions.

**Best used when:** Newer payload versions may contain fields unknown to the current model and those fields should be preserved or summarized.

**Avoid when:** The property is normal dynamic domain data, target-specific annotations, or a modeled dictionary whose keys have domain meaning.

**Projection implications:** JSON Schema controls openness/additional members. EF Core ignores or stores/summarizes the bag by policy. Power BI ignores or summarizes the bag by policy.

**Required distinction:** Extension data is instance data. It is not model annotation metadata.

### Accepted Shapes

The default accepted shape is dictionary-like:

```text
Dictionary<string, JsonElement>
IDictionary<string, JsonElement>
Dictionary<string, object>
IReadOnlyDictionary<string, object>
```

Other shapes require explicit configuration or diagnostics.

Key type must be string-like by default.

### System.Text.Json Normalization

When the System.Text.Json integration is active or configured, `[JsonExtensionData]` may normalize to `ExtensionData`.

If `[JsonExtensionData]` is found and normalization is disabled, the system must either preserve target-specific STJ metadata or emit an informational diagnostic according to existing STJ policy.

## Projection Defaults

### JSON Schema

| Semantic | Default behavior |
|---|---|
| OwnedObject | structured property schema or `$defs` reference according to object policy |
| OwnedCollection | array of owned item schema |
| Version / Revision | normal property schema plus semantic metadata |
| CurrentVersion | normal property schema plus semantic metadata |
| TemporalValidity | temporal fields with `date-time` format where applicable |
| LifecycleState | enum/scalar schema plus semantic metadata |
| ExtensionData | use `additionalProperties` / `unevaluatedProperties` policy; do not expose bag as normal property by default |

JSON Schema must not claim to enforce cross-property interval constraints unless explicit validation metadata exists.

### EF Core

| Semantic | Default behavior |
|---|---|
| OwnedObject | `OwnsOne` same-table columns |
| OwnedCollection | explicit policy required unless configured package default exists |
| Version / Revision | scalar columns; no primary-key replacement by default |
| CurrentVersion | scalar column; no global query filter by default |
| TemporalValidity | scalar columns; no temporal tables and no query filters by default |
| LifecycleState | enum/scalar column according to enum policy |
| ExtensionData | ignored by default; opt-in serialized JSON/hash/count/boolean summary |

### Power BI

| Semantic | Default behavior |
|---|---|
| OwnedObject | flatten simple scalar owned members when safe |
| OwnedCollection | explicit policy required for child table, summary, or ignore |
| Version / Revision | reporting columns |
| CurrentVersion | filter/slicer candidate |
| TemporalValidity | timeline/effective-date columns |
| LifecycleState | categorical column / slicer candidate |
| ExtensionData | ignored by default; opt-in `HasExtensionData`, count, hash, or known-key summary |

Power BI must not flatten arbitrary extension-data keys by default.

## Policy Hooks

Implementations may expose fluent policies equivalent to:

```csharp
options.Ownership.For<Customer>()
    .OwnsOne(x => x.Address)
    .StoreAsOwnedColumns(prefix: "Address");

options.Ownership.For<Order>()
    .OwnsMany(x => x.Lines)
    .StoreAsOwnedJson(columnName: "Lines");

options.Versioning.For<WorkflowSpecification>()
    .UseRevision(x => x.Revision)
    .UseCurrentMarker(x => x.IsCurrent);

options.TemporalValidity.For<PriceListEntry>()
    .UseInterval(x => x.ValidFrom, x => x.ValidTo);

options.ExtensionData.For<WorkflowSpecification>()
    .StoreAsSerializedJson(columnName: "ExtensionDataJson");

options.ExtensionData.For<WorkflowSpecification>()
    .ProjectSummary(summary => summary.HasAny("HasExtensionData"));
```

Specific API shape may differ, but the same policy concepts must be represented.

## Diagnostics

Required diagnostic classes include:

```text
unknown M0034 semantic alias
ownership cycle
shared ownership conflict
owned object also projected as independent root without explicit policy
owned collection has no target policy where required
revision/version member has unsupported type
versioned type has no version/revision member in strict mode
current-version marker is not boolean/status-compatible
temporal validity has ValidTo without ValidFrom
validity endpoint uses non-temporal type
lifecycle state member has unsupported type
multiple extension-data bags on one type
extension-data bag has non-string key type without explicit policy
extension-data bag flattening requested without stable key set
extension-data requested as summary but no summary computation policy exists
JsonExtensionData found but core normalization is disabled
projection target cannot represent selected policy
```

Diagnostics must include model path and projection target where available.

## Inspection

Inspection output must include deterministic sections for:

```text
Ownership
Versioning
TemporalValidity
LifecycleState
ExtensionData
Projection policies
Diagnostics
```

Inspection output must be stable enough for snapshot testing.
