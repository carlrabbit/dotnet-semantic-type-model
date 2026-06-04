# Type Model Query and Inspection Specification

## Status

Authoritative behavioral specification.

## Purpose

Define the query and inspection APIs for code-first semantic models.

This specification is authoritative for:

- canonical semantic model query behavior;
- typed query behavior;
- string fallback query behavior;
- diagnostic query behavior;
- deterministic model text inspection;
- deterministic diagnostic text inspection;
- generated model and snapshot compatibility.

## Product Role

Query and inspection support the primary development loop:

```text
Annotate .NET types.
Generate or extract a semantic model.
Query the model in tests or console tools.
Inspect diagnostics and model text.
Adjust annotations or transformation configuration.
Repeat.
```

The APIs must be small, deterministic, and useful for tests. They must not become a general graph query language.

## Package Boundary

| Package | Responsibility |
|---|---|
| `SemanticTypeModel.Abstractions` | Shared result types or interfaces only when required across packages. |
| `SemanticTypeModel.Core` | Query implementations, text inspection formatters, and diagnostic helpers. |
| `SemanticTypeModel.DotNet` | CLR metadata annotations required by typed queries. |
| `SemanticTypeModel.Generators` | Generated models must preserve metadata needed for typed queries. |
| Domain packages | Domain-specific query/inspection extensions only in later milestones. |

The common query and inspection surface must not require a dependency on `SemanticTypeModel.DotNet`.

## Query Principles

- Prefer typed access when CLR metadata is available.
- Always provide string fallback based on canonical identifiers.
- Keep ordering deterministic.
- Keep APIs usable in unit tests.
- Provide safe APIs for library code.
- Provide assertive APIs for tests and console workflows.
- Fail with explicit guidance when typed metadata is unavailable.
- Do not mutate the model.
- Do not require runtime model editing.

## Canonical Identifiers

String fallback query APIs operate on canonical identifiers.

Rules:

- identifiers must match the canonical model identifier stored in the model;
- string lookup is case-sensitive unless an existing model contract says otherwise;
- missing identifiers must be reported clearly;
- assertive APIs must include the missing identifier in the exception message.

## Typed Query Metadata

Typed queries require stable metadata linking model elements to CLR symbols.

Required metadata direction:

```text
TypeShape -> CLR type identity annotation
PropertyShape -> CLR declaring type identity annotation + CLR member name annotation
```

Exact annotation keys are implementation-owned but must be deterministic and documented in the relevant extraction/generator tests.

Typed queries must work for generated code-first models when the consumer project has the CLR types available.

Typed queries over loaded snapshots without CLR metadata must fail with explicit guidance and recommend string fallback.

## Type Queries

Required behavior:

```csharp
model.RequireType<Customer>();
model.TryGetType<Customer>(out var type);
model.RequireType("global::MyApp.Customer");
model.TryGetType("global::MyApp.Customer", out var type);
```

The implementation may expose equivalent names, but it must support:

- lookup by CLR type;
- lookup by canonical string identifier;
- safe lookup;
- assertive lookup;
- deterministic enumeration of all types.

Assertive lookup must throw an exception that includes:

- query kind;
- requested CLR type or identifier;
- available close matches when feasible.

## Property Queries

Required behavior:

```csharp
model.RequireProperty<Customer>(x => x.Email);
model.TryGetProperty<Customer>(x => x.Email, out var property);
model.RequireProperty("global::MyApp.Customer", "Email");
model.TryGetProperty("global::MyApp.Customer", "Email", out var property);
```

Property-expression queries must accept simple property access expressions only.

Unsupported expressions must fail explicitly, for example:

```text
x => x.Email.ToLower()
x => new { x.Email }
x => x.Customer.Email
```

Property lookup by string uses the canonical property/member name as defined by the model.

## Semantic and Annotation Queries

Required behavior:

```csharp
model.Types().WithSemanticType("Entity");
model.Types().WithAnnotation("semantic.type", "Entity");
model.PropertiesOf<Customer>().WithSemantic("Key");
model.Properties().WithAnnotation("efCore.primaryKey");
```

The implementation may expose equivalent names, but must support:

- type filtering by semantic primitive;
- property filtering by semantic primitive;
- annotation-key filtering;
- annotation-key-and-value filtering;
- deterministic result ordering;
- string fallback for unknown/custom metadata.

Core must not depend on domain packages to support domain annotation queries.

## Diagnostic Queries

Diagnostics must remain machine-queryable.

Required behavior:

```csharp
diagnostics.HasErrors();
diagnostics.Errors();
diagnostics.Warnings();
diagnostics.WithCode("STM5008");
diagnostics.ForPath("/types/Customer/properties/Email");
diagnostics.ForType<Customer>();
diagnostics.ForProperty<Customer>(x => x.Email);
diagnostics.ThrowIfErrors();
```

The implementation may expose equivalent names, but must support:

- severity filtering;
- diagnostic code filtering;
- stage filtering when stage exists;
- model path filtering;
- type filtering when typed metadata is available;
- property filtering when typed metadata is available;
- deterministic ordering.

`ThrowIfErrors()` must include deterministic diagnostic text in the exception message.

## Query Result Ordering

All query enumeration must be deterministic.

Default order:

1. model path when available;
2. canonical identifier;
3. declaration/member order when explicitly preserved;
4. stable ordinal fallback.

The implementation must not rely on dictionary iteration order unless the dictionary is explicitly ordered by contract.

## Inspection Principles

Inspection output is human-readable diagnostic/development output.

It is not:

- a serialization format;
- a round-trip persistence format;
- a stable wire format;
- a replacement for model snapshots.

Inspection output must be deterministic and useful for tests.

## Text Detail Levels

Required detail levels:

| Level | Purpose |
|---|---|
| `Summary` | Minimal console overview. |
| `Normal` | Default development-loop and test output. |
| `Detailed` | Includes annotations, constraints, source metadata, and optional diagnostics. |

Equivalent names are allowed, but the three-level behavior must be present.

## Model Text Inspection

Required behavior:

```csharp
string text = model.ToSemanticText();

string detailed = model.ToSemanticText(new SemanticTextOptions
{
    Detail = SemanticTextDetail.Detailed,
    IncludeDiagnostics = true,
    IncludeAnnotations = true,
    IncludeConstraints = true,
    IncludeSource = false
});
```

The implementation may expose equivalent APIs, but must support:

- deterministic model summary;
- deterministic type listing;
- deterministic property listing;
- relationship/key display when available;
- constraints display when enabled;
- annotations display when enabled;
- diagnostics display when enabled;
- source metadata display when enabled.

Output must use stable line endings normalized by the implementation or tests.

Example normal output intent:

```text
Model AppSemanticTypeModel
Root: Customer

Types:
  Customer [Entity]
    Key: Id
    Property Id: string required
    Property DisplayName: string required
    Property Email: string optional format=email
    Relationship Orders -> Order many

Diagnostics:
  warning STM5008 /types/Customer/properties/Email Missing semantic display name.
```

## Diagnostic Text Inspection

Required behavior:

```csharp
string text = diagnostics.ToDiagnosticText();
```

Output must include:

- severity;
- code;
- model path when available;
- message;
- related model paths when enabled;
- stage/projection target when detailed output is enabled.

Example:

```text
error STM5012 /types/Customer/properties/Orders Unsupported dictionary key type.
warning STM5008 /types/Customer/properties/Email Missing semantic display name.
```

## Options

Required model text options:

```csharp
public sealed class SemanticTextOptions
{
    public SemanticTextDetail Detail { get; set; }
    public bool IncludeDiagnostics { get; set; }
    public bool IncludeAnnotations { get; set; }
    public bool IncludeConstraints { get; set; }
    public bool IncludeSource { get; set; }
}
```

Required diagnostic text options:

```csharp
public sealed class DiagnosticTextOptions
{
    public SemanticTextDetail Detail { get; set; }
    public bool IncludeRelatedPaths { get; set; }
    public bool IncludeSource { get; set; }
}
```

The final API names may differ, but equivalent behavior is required.

## Snapshot and No-CLR Compatibility

Loaded model snapshots may not have access to CLR types.

Required behavior:

- string identifier queries work without CLR types;
- inspection works without CLR types;
- diagnostics text works without CLR types;
- typed queries fail with explicit guidance when CLR metadata is absent;
- typed query failure must recommend string fallback.

## Errors and Exceptions

Assertive query APIs must throw deterministic exceptions.

Exception messages must include:

- what was requested;
- why resolution failed;
- relevant identifier/type/member;
- string fallback guidance when typed lookup cannot run;
- available candidates when feasible.

Safe query APIs must not throw for normal not-found results.

## Test Requirements

Short-running tests must cover:

- type lookup by CLR type;
- type lookup by string identifier;
- property lookup by expression;
- property lookup by string name;
- unsupported property-expression diagnostics/exceptions;
- semantic primitive filtering;
- annotation filtering;
- diagnostic filtering;
- `ThrowIfErrors()`;
- model text summary output;
- model text detailed output;
- diagnostic text output;
- deterministic ordering;
- generated model query compatibility;
- no-CLR/string fallback behavior.

Snapshot-style tests are recommended for inspection output.


## Public API Surface

M0027 implementation exposes the shared query and inspection helpers from `SemanticTypeModel.Core`.
Consumers opt in with the Core query and inspection namespaces and may use the same string-fallback APIs for generated models and snapshot-like models that do not have CLR metadata available.

Required supported surface:

```csharp
using SemanticTypeModel.Core.Query;
using SemanticTypeModel.Core.Inspection;

var type = model.RequireType<Customer>();
var typeById = model.RequireType("global::MyApp.Customer");
var property = model.RequireProperty<Customer>(x => x.Email);
var propertyByName = model.RequireProperty("global::MyApp.Customer", "Email");
var entities = model.Types().WithSemanticType("Entity");
var annotated = model.Properties().WithAnnotation("efCore.primaryKey");
var constrained = model.Properties().WithConstraint("string.minLength", 5);
var text = model.ToSemanticText(new SemanticTextOptions { Detail = SemanticTextDetail.Detailed });
```

Diagnostic helpers operate on canonical `SchemaDiagnostic` sequences and must preserve deterministic ordering for filtering, assertion, and text inspection:

```csharp
diagnostics.HasErrors();
diagnostics.Errors();
diagnostics.WithCode("STM5008");
diagnostics.ForPath("/types/Customer/properties/Email");
diagnostics.ThrowIfErrors(new DiagnosticTextOptions { IncludeRelatedPaths = true });
```

## Non-Goals

M0027 does not define:

- a LINQ provider;
- a query parser;
- a graph query language;
- domain-specific query DSLs;
- interactive visualization;
- JSON/YAML inspection output;
- round-trip serialization;
- runtime model editing;
- full transformation tracing.
