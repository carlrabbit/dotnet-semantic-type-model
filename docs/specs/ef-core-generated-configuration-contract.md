# EF Core Generated Configuration Contract

## Manifest

A model assembly's `SemanticTypeModel.Generators` output includes exactly one `SemanticTypeModel.Manifest` assembly-metadata value. Schema version 1 is deterministic base64-encoded JSON containing model identity; semantic type IDs, names, kinds, roles, CLR identities, and CLR base identity; array item identity; and property semantic/member/declaring identities, type identity, required/nullability state, primary-key order, ownership, and extension-data state. Consumers select model assemblies with repeatable `GenerateSemanticEfModel(Type)` assembly attributes. Generators read referenced metadata through Roslyn and never load or execute application assemblies.

## Output

The EF generator emits one internal partial `IEntityTypeConfiguration<TEntity>` per semantic Entity, never per ValueKind, enum, configuration type, or nonentity. `Configure` calls `ConfigureBeforeGenerated`, generated direct EF calls, then `ConfigureAfterGenerated`. One public `Apply<Model>SemanticModel` extension registers semantic bases before derived Entities with ordinal semantic-ID/CLR-name ordering. `ApplyConfigurationsFromAssembly` is not the canonical path.

Generated mapping retains Entity/table, TPT, scalar, enum-string, URI-string, strong-identifier, binary, JSON-owned ValueKind/collection, nested JSON, and extension-data rules. Generated properties apply manifest nullability explicitly with `IsRequired`; nullable URI values use the nullable URI converter. It does not infer navigations, use `OwnsOne`/`OwnsMany`, or offer per-Entity opt-out. Pure normalized storage decisions are linked into runtime relational inspection and generator compilation from the internal shared source; no public sharing package exists.

## Composition

Multiple selected models are supported. Duplicate CLR Entity ownership is an error. A generated model may configure only its manifest-owned CLR Entities. It never enumerates, ignores, removes, rejects, or validates unrelated EF Entities. Manual EF entities remain application-owned.

## Diagnostics

STM5037-STM5046 cover missing, ambiguous, invalid, or unsupported manifests; duplicate ownership; generated configuration/registration names; unresolved CLR types/members; and EF projection errors.
