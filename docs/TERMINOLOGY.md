# Terminology

## Rules

- One sentence per term.
- One canonical meaning per term.
- Avoid aliases unless explicitly declared.
- Add new domain terms before broad use.
- Use terminology consistently across documentation, issues, and code.

## Terms

### Task Best Practice
Reusable operational guidance for a class of repository work.

### Specification
Authoritative behavioral description of a system, component, feature, or process.

### Milestone
Controlled implementation phase with explicit scope, deliverables, and exit criteria.

### Guardrail
Project-wide constraint that limits implementation, testing, documentation, or operational behavior.

### Engineering Guide
Stack-specific definition of repository commands, tooling, validation, and optional engineering modules.

### Command Contract
Stable set of repository commands used by humans, CI, and agents.

### Short-Running Test
Test intended to be safe for local development and agent execution.

### Long-Running Test
Test intended only for explicit local execution or GitHub workflow execution.

### Document Authority
Declaration of what a document is allowed to define.

### Document Contract
Declaration of related documents and synchronization obligations.

### Semantic Type Model
A canonical, immutable representation of type shapes, properties, constraints, and annotations used as the authoritative runtime model.

### Canonical Semantic Model
The current projection-neutral semantic type model used by runtime services, transformations, query APIs, inspection APIs, and domain projections.

### Runtime Canonical Semantic Model
The canonical semantic model instance used by runtime APIs after extraction, generation, programmatic finalization, loading, or transformation.

### Programmatic Model Authoring
Explicit runtime construction and validation of the existing canonical semantic model through a Core-owned authoring/finalization API without generating CLR types or introducing an external schema language.

### Dynamic Model
An informal consumer term for a canonical semantic model produced through Programmatic Model Authoring; it is not a separate model type or a mutable finalized model.

### Model Surface
The public .NET contract namespace and type family used to represent canonical semantic models.

### Unified Model Surface
The single supported model surface that all generators, runtime services, transformations, query APIs, inspection APIs, and projection packages consume.

### Legacy Shape Model
The obsolete `TypeShape`-based model graph that predates the current canonical semantic model contracts.

### TypeShape
A legacy shape-graph type name retained only for historical or migration context after the unified model surface removes the old shape model.

### Type Definition
The canonical representation of a type in the unified semantic model surface.

### Source-Generated Model Provider
Generated code that creates a canonical semantic model from annotated .NET code.

### Schema Projection
A derived representation of the canonical type model targeted at a specific output format or system.

### Public Documentation
Consumer-facing documentation that defines supported usage for external adopters.

### Consumer
A user or system integrating SemanticTypeModel packages or outputs.

### Public Documentation Surface
A repository file or folder treated as externally visible consumer documentation.

### Package README
The consumer-facing package documentation source used for NuGet README content.

### Diagnostics Reference
Consumer-facing documentation describing diagnostics, severity, cause, and corrective action.

### Public API Compatibility Documentation
A documented comparison point used to detect breaking changes in consumer-visible API contracts.

### Code Source
Annotated .NET code used as the primary CLR-oriented authoring source for a canonical semantic type model.

### Model Snapshot
A persisted representation of a finalized semantic type model that can be loaded without access to its original authoring source and is not itself a separate authoring language.

### Semantic Primitive
A canonical semantic concept such as entity, value object, key, requiredness, nullability, format, constraint, conditional constraint, envelope, ownership, lifecycle state, extension data, or annotation.

### Core Semantic Vocabulary
The authoritative set of projection-neutral semantic primitives and usage rules available to supported canonical-model authors.

### Conditional Constraint
A projection-neutral constraint whose applicability depends on another modeled value or a simple modeled condition.

### Required When
A conditional constraint stating that a property must be present when another property equals a supported literal value.

### Envelope
A wrapper type whose primary semantic role is to carry, manage, version, transport, persist, authorize, audit, or otherwise contextualize a distinguished payload.

### Envelope Payload
The distinguished property inside an envelope that carries the semantic value being transported, managed, persisted, cached, or contextualized.

### Envelope Metadata
A property on an envelope that describes the envelope lifecycle, context, transport, audit, revision, status, or management state rather than the payload domain state.

### Projection-Specific Metadata
Metadata that describes representation for one projection target, such as JSON Schema, EF Core, Power BI, or System.Text.Json, rather than projection-neutral domain meaning.

### Logical Type
An explicit property-level name attached to an ordinary scalar for semantic labeling without changing its scalar representation or introducing a canonical type node.

### Semantic Terminology Profile
A versioned external TestData sidecar containing synthetic scalar candidate values bound to model-local Logical Types or canonical properties without authoring or mutating the canonical semantic model.

### TestData Profile
An immutable model-bound runtime TestData configuration that defines how legal canonical values are sampled for a named test-data scenario without adding canonical semantics.

### Sampling Policy
A TestData-only rule controlling selection among values already legal under the canonical model, including presence, null frequency, weighted candidates, boundary strategy, and collection sizing.

### Effective Sampling Policy
The deterministic Sampling Policy resolved for one generation occurrence from property, Logical Type, containing-object, profile-default, and built-in defaults.

### Coordinated TestData Generation
TestData generation in which exact-property rules coordinate otherwise legal values through same-object dependencies or invocation-scoped sequence, sharing, and uniqueness state.

### Generation Session
The ephemeral state container created for one `Generate` or `GenerateMany` invocation and discarded when that invocation completes.

### Root Scope
A TestData coordination scope whose state is shared across the full value graph of one top-level generated root and resets for the next root.

### Batch Scope
A TestData coordination scope whose state is shared across all top-level roots produced by one `Generate` or `GenerateMany` invocation.

### Derived TestData Value
A scalar or enum TestData value produced from explicitly declared scalar/enum sibling dependencies in the same effective object instance.

### Coordinated Producer
An exact-property TestData Profile rule, such as a derived-value or sequence rule, that supplies a value before weighted, terminology, or built-in generation sources.

### Scoped TestData Uniqueness
A TestData-only requirement that present non-null values for one exact property do not repeat within a declared Root or Batch generation scope.

### Random Test-Data Generation
Deterministic constraint-aware TestData generation that derives occurrence-diverse values from the canonical semantic model, size profile, seed, and generation coordinate without terminology guidance.

### Profile-Guided Test-Data Generation
TestData generation in which an optional TestData Profile controls legal sampling and optional Semantic Terminology Profile candidates still provide semantic vocabulary before Random fallback.

### CLR Test-Data Materialization
Conversion of a successful semantic TestData value graph into a supported CLR object graph without changing the semantic generation result or treating CLR wrapper shape as semantic meaning.

### Envelope Projection Root
The type selected by target policy as the root projection for an envelope scenario; it may be the envelope wrapper or the envelope payload.

### Ownership
Lifecycle containment in which an object or collection is part of an owner's composition boundary and does not stand independently by default.

### Owned Object
A single object-valued member whose lifecycle follows the containing owner.

### Owned Collection
A collection-valued member whose element lifecycle follows the owner.

### Versioned
A semantic marker indicating that a type or instance participates in version or revision evolution over time.

### Version
A semantic identifier for a version of a type, instance, payload, or contract.

### Revision
An ordered or otherwise comparable instance-level version marker used to distinguish revisions of the same semantic object.

### Current Version
A marker indicating that an instance represents the current or active version or revision.

### Temporal Validity
A semantic interval describing when a type, instance, relationship, or value is valid or effective.

### Valid From
The temporal start endpoint of a validity interval.

### Valid To
The optional temporal end endpoint of a validity interval.

### Lifecycle State
A semantic state/status value describing the lifecycle phase of an entity, envelope, specification, workflow, document, operation, or value.

### Extension Data
Instance-level unknown, unmodeled, forward-compatible, or externally supplied data preserved for compatibility across model revisions.

### Domain Semantic Model
A package-owned semantic model derived from the canonical semantic type model for a specific supported domain such as JSON Schema, EF Core, Power BI, or System.Text.Json.

### System.Text.Json Domain Model
The System.Text.Json package-owned domain semantic model that describes resolver customization decisions derived from the canonical semantic model.

### Resolver Customization Model
A deterministic model of supported System.Text.Json resolver changes, such as property-name selection, ignored members, required markers, and extension-data handling.

### Transformation
A deterministic operation that derives, normalizes, validates, or enriches semantic model information while emitting diagnostics when needed.

### Query Surface
The API surface used to locate and navigate semantic model elements by CLR type, property expression, or canonical string identifier.

### Inspection Surface
The API surface used to produce deterministic human-readable summaries of models, diagnostics, transformations, and domain semantic models.

### Guide Profile
Repository-local metadata describing the external guide-system version, selected profile, repository role, maturity mode, and execution mode used for migration planning.

### Guide Sync Hint
Deferred documentation-synchronization metadata stored under `.guide-sync/` for planning and documentation agents, not ordinary implementation agents.

### Guide Migration
A repository-local migration that adopts external guide-system conventions while keeping operational project authority inside the target repository.

### Semantic Mutability
Optional lifecycle mutability intent expressed only as `Mutable` or `Immutable`.

### Declared Mutability
The nullable mutability value explicitly authored on an object type or property.

### Effective Mutability
A property's declared mutability, otherwise its containing object's declared mutability, otherwise unspecified.

### STM JSON Schema Extension
The optional `x-stm` JSON Schema object that preserves the approved subset of STM-only semantics.

### UI Annotation
An open JSON-compatible canonical annotation whose key begins with `ui.`.

### Display Identity
The single ordered property group that identifies how a human should recognize an instance without defining machine identity, uniqueness, or display formatting.

### Access Path
A named ordered property group that expresses an intended way consumers may locate or narrow instances without defining query operators, uniqueness, or target-specific implementation.

### JSON Representation Fidelity
The one-way compatibility contract under which JSON successfully emitted by a supported SemanticTypeModel System.Text.Json configuration conforms to the JSON Schema derived from the same canonical semantic model.
