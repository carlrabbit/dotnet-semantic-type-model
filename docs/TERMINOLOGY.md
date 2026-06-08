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

### TypeShape
The canonical representation of a type in the semantic type model.

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

### Public API Baseline
A documented comparison point used to detect breaking changes in consumer-visible API contracts.

### Code Source
Annotated .NET code used as the supported authoring source for a canonical semantic type model.

### Model Snapshot
A persisted representation of a code-generated semantic type model that can be loaded without access to the original codebase.

### Semantic Primitive
A canonical semantic concept such as entity, value object, key, relationship, requiredness, nullability, format, constraint, envelope, ownership, lifecycle state, extension data, or annotation.

### Core Semantic Vocabulary
The authoritative set of projection-neutral semantic primitives and usage rules available to code-first authors.

### Envelope
A wrapper type whose primary semantic role is to carry, manage, version, transport, persist, authorize, audit, or otherwise contextualize a distinguished payload.

### Envelope Payload
The distinguished property inside an envelope that carries the semantic value being transported, managed, persisted, cached, or contextualized.

### Envelope Metadata
A property on an envelope that describes the envelope lifecycle, context, transport, audit, revision, status, or management state rather than the payload domain state.

### Projection-Specific Metadata
Metadata that describes representation for one projection target, such as JSON Schema, EF Core, Power BI, or System.Text.Json, rather than projection-neutral domain meaning.

### Envelope Projection Root
The type selected by target policy as the root projection for an envelope scenario; it may be the envelope wrapper or the envelope payload.

### Ownership
Lifecycle containment in which an object or collection is part of an owner's composition boundary and does not stand independently by default.

### Owned Object
A single object-valued member whose lifecycle follows the containing owner.

### Owned Collection
A collection-valued member whose element lifecycle follows the containing owner.

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
A package-owned semantic model derived from the canonical semantic type model for a specific domain such as JSON Schema, EF Core, Power BI, or System.Text.Json.

### Transformation
A deterministic operation that derives, normalizes, validates, or enriches semantic model information while emitting diagnostics when needed.

### Query Surface
The API surface used to locate and navigate semantic model elements by CLR type, property expression, or canonical string identifier.

### Inspection Surface
The API surface used to produce deterministic human-readable summaries of models, diagnostics, transformations, and domain semantic models.
