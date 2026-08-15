# Compatibility

## Public API Compatibility

- Public API compatibility is reviewed through package smoke tests, runnable samples, public documentation, release notes, compatibility documentation, and human review.
- Breaking changes require explicit milestone and release documentation.
- The repository does not currently maintain text API baseline files as release gates.
- `SemanticTypeModel.SystemTextJson` 1.1.0 removed generated `JsonSerializerContext` support as a documented compatibility correction because the 1.0 design depended on unsupported source-generator chaining.
- 2.2.0 release-preparation documentation records M0038 as the model-surface cleanup boundary; removed `Canonical` namespace and old shape-graph APIs must not be presented as supported current usage.
- 2.4.0 release-preparation documentation records Configuration as explicit per-options-type registration; model-wide Configuration application registration is obsolete pending human compatibility review.

## 2.2.0 Model Surface Compatibility

- `SemanticTypeModel.Abstractions.Model` is the supported public model surface for canonical semantic model contracts.
- `SemanticTypeModel.Abstractions.Canonical` is removed from shipped source and compatibility documentation.
- The old `TypeShape` / `ObjectShape` / `PropertyShape` / `ShapeRef` shape graph is removed rather than retained as a compatibility shim.
- Source-generated providers return `SemanticTypeModel.Abstractions.Model.TypeSchemaModel` directly for projection packages.
- JSON Schema import remains compatibility-oriented and is not the supported canonical authoring path; use annotated .NET code plus generated providers for public samples and package guidance.

## Diagnostics Compatibility

- Diagnostics are currently preview/unstable unless explicitly declared stable in release notes.

## Runtime Compatibility

- Preserve canonical command and package naming constraints:
  - solution `SemanticTypeModel.slnx`
  - root namespace `SemanticTypeModel`
  - package prefix `SemanticTypeModel.*`

## 2.4.0 Configuration Registration Compatibility

- Configuration application registration is explicit per options type through `AddSemanticOptions<TOptions>`.
- Complete-model Configuration derivation remains available for inspection and tooling, but complete-model application registration is obsolete.
- Generated Configuration helpers are optional convenience APIs and must delegate to the runtime adapter.
- Human review is required before removing the obsolete model-wide registration API in a future compatibility boundary.

- `ConfigurationSectionPresence.Optional` is the compatibility default; `Required` adds provider-independent effective-data validation under the selected section.
- Required-section, DataAnnotations, and `RequiredWhen` deployed-value failures are options-validation failures; invalid or ambiguous model metadata fails during registration.

### M0046 EF Core nullable value-type compatibility

EF Core projection preserves canonical nullability for nullable value-type scalars by using `Nullable<T>` for the projected property CLR type and for the applied EF Core model-builder property type. The regression coverage includes nullable integer, long, decimal, Boolean, date/time, GUID, numeric enum storage, and required non-nullable control values.

## 2.4.0 Audience-Specific Description Compatibility

- `UserDescription` and `TechnicalDescription` replace the former general description concept in the public model contracts.
- `SemanticUserDescriptionAttribute` supplies user-facing text for projections such as JSON Schema `description` and Power BI descriptions.
- `SemanticTechnicalDescriptionAttribute` supplies technical text; XML `<summary>` is an automatic technical-description fallback when no explicit technical description exists.
- EF Core maps technical descriptions to table and column comments.
- JSON Schema technical descriptions are emitted only through the opt-in technical-description extension name.
- `SemanticDescriptionAttribute`, the generic canonical `Description`, XML include switches, and XML requirement switches are removed; use `RequireTechnicalDescription` for technical-description enforcement.
- Existing general description text must be classified manually because user-facing versus technical audience intent cannot be inferred safely.

## EF Core 2.5.0 breaking reset

The EF package intentionally has no source-compatibility bridge from 2.4.x. `EfRelationalModel` and `DeriveEfRelationalModel` are the inspection surface. Generated EF configuration is the 3.0 application surface. Application modes, shared-type entities, broad CLR source lineage, EF-owned navigations, relationship inference, and alternative inheritance or ValueKind storage policies were removed.

Owned ValueKinds and extension data use JSON columns; entity inheritance is TPT; semantic links must use identifier scalar properties.

## EF Core 3.0 breaking application change

The supported static CLR application path changes from runtime global `ApplySemanticTypeModel`/`ApplySemanticRelationalModel` cleanup to `SemanticTypeModel.EFCore.Generators`. Persistence projects explicitly select model manifests and call generated registration extensions. Generated configurations own only their semantic CLR Entities, so other selected models and manual entities compose normally.
