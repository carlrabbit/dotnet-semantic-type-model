# SemanticTypeModel.EFCore

## What this package does

`SemanticTypeModel.EFCore` maps the explicit semantic model to one opinionated CLR-backed relational representation.

## Install

```sh
dotnet add package SemanticTypeModel.EFCore --version 2.5.1
```

## Minimal example

```csharp
using SemanticTypeModel.EFCore;

var result = AppSemanticTypeModel.Create().DeriveEfRelationalModel();
result.Diagnostics.ThrowIfErrors();
modelBuilder.ApplySemanticRelationalModel(result.Model);
```

`ApplySemanticTypeModel` is the convenience path that derives and applies the same model.

## Fixed contract

- semantic entities are tables and semantic entity inheritance is TPT;
- scalars are columns, enums are strings, and strong identifiers use their underlying scalar;
- explicitly owned ValueKind objects and collections are JSON columns;
- semantic extension data is a JSON object column;
- entity objects, undeclared ValueKind storage, and arbitrary dictionaries produce diagnostics;
- EF relationships, navigations, shared-type entities, `OwnsOne`, and `OwnsMany` are not projected.
- the package suppresses EF convention discovery for semantic ValueKinds and other non-entities, so the final CLR entity set exactly equals the semantic Entity CLR set;
- JSON-owned objects and collections remain converted properties rather than owned or keyless EF entities, without consumer `ModelBuilder.Ignore(...)` calls.

The application still selects its EF provider and owns migrations and database operations. The 2.5 line intentionally removes the 2.4.x EF API rather than retaining compatibility aliases.

## More documentation

- [Compatibility](../api/compatibility.md)
- [EF Core projection guide](../guides/ef-core-projection.md)
