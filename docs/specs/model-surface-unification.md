# Model Surface Unification

## Status

Authoritative for M0038 model-surface unification.

## Purpose

Define the single supported public model surface after collapsing the `SemanticTypeModel.Abstractions.Model` / `SemanticTypeModel.Abstractions.Canonical` split.

## Core Contract

`SemanticTypeModel.Abstractions.Model` is the sole public namespace for canonical semantic model contracts.

The model surface must contain the contract family currently represented by the canonical semantic model, including:

```text
TypeSchemaModel
SchemaModelId
TypeId
PropertyId
RelationshipId
AnnotationKey
AnnotationBag
Annotation
TypeDefinition
ObjectTypeDefinition
PropertyDefinition
KeyDefinition
RelationshipDefinition
ConstraintSet
ScalarTypeDefinition
EnumTypeDefinition
ArrayTypeDefinition
DictionaryTypeDefinition
UnionTypeDefinition
IntersectionTypeDefinition
ReferenceTypeDefinition
TypeRef
PropertyRef
```

The old shape graph is not part of the supported model surface after M0038:

```text
TypeShape
ObjectShape
PropertyShape
ScalarShape
EnumShape
ArrayShape
DictionaryShape
UnionShape
ShapeRef
SchemaAnnotation
```

## Invariants

- There is one canonical semantic model surface for source-generated models, runtime services, transformations, query, inspection, domain derivation, and projection packages.
- Generated providers return `SemanticTypeModel.Abstractions.Model.TypeSchemaModel`.
- Domain derivation entry points accept `SemanticTypeModel.Abstractions.Model.TypeSchemaModel` directly.
- Projection packages must not require consumers to convert source-generated models before use.
- `SemanticTypeModel.Abstractions.Model` must not appear in shipped public source or public API baselines after migration.
- Old shape-graph contracts must not remain as active public compatibility contracts.
- Internal test fixtures may construct model instances directly, but public samples must use the code-first generator path.
- Public documentation must describe annotated .NET code plus generated provider output as the supported authoring path.

## Source Generator Contract

The source generator emits a deterministic provider with a `Create()` method returning the unified model type:

```csharp
public static SemanticTypeModel.Abstractions.Model.TypeSchemaModel Create();
```

Generated source must not reference:

```text
SemanticTypeModel.Abstractions.Model
old TypeShape/ObjectShape/PropertyShape shape graph contracts
old-shape TypeSchemaModelBuilder unless rewritten to build the unified model
```

## Transformation and Domain Derivation Contract

Transformation and projection APIs must use the unified model surface:

```text
SemanticTypeModel.Core transformation APIs
SemanticTypeModel.Core validation APIs
SemanticTypeModel.Core query APIs
SemanticTypeModel.Core inspection APIs
SemanticTypeModel.JsonSchema derivation APIs
SemanticTypeModel.EFCore derivation APIs
SemanticTypeModel.PowerBI derivation APIs
SemanticTypeModel.SystemTextJson derivation/resolver APIs
SemanticTypeModel.DependencyInjection provider/projection APIs
```

APIs that currently accept the old shape graph must be removed or replaced with equivalent unified-model APIs.

## Sample Contract

Public samples under `samples/` must demonstrate supported consumer workflows.

Code-first projection samples must:

- define annotated C# model classes;
- consume the generated provider produced by `SemanticTypeModel.Generators`;
- pass the generated `Model.TypeSchemaModel` directly to projection APIs;
- avoid hand-building model contracts in public sample code.

Test-only model factories are permitted only when they are clearly internal test fixtures and not public consumer examples.

## Compatibility Contract

M0038 is an intentional public surface cleanup for the 2.2.0 line.

Breaking changes must be reflected in:

```text
public API baselines
public-docs/api/compatibility.md
public-docs/release-notes.md
package README sources
sample documentation
```

The compatibility policy must not claim old-shape or `Canonical` namespace support after the migration is complete.

## Diagnostics and Failure Semantics

- Unsupported post-migration model states must fail deterministically.
- Any retained reference to removed model namespaces in production source must be treated as a defect.
- Generated source that cannot construct the unified model must fail generator tests or package smoke validation.
- Projection entry points must reject null model input with deterministic argument validation.
- Domain derivation diagnostics must remain target-owned and must not hide model-surface errors.

## Validation Expectations

The milestone implementing this spec must validate:

```text
source generator output compiles against unified Model contracts
all projection packages accept generated model output directly
public samples no longer hand-build canonical models
old Model shape graph is absent from shipped source
Canonical namespace is absent from shipped source and public API baselines
public API baselines are updated intentionally
public docs and package README sources match the new usage path
```
