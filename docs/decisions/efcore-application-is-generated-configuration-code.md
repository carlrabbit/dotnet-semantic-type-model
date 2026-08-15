# EF Core Application Is Generated Configuration Code

Status: Accepted for 3.0.0; supersedes runtime closed-`ModelBuilder` application decisions.

Semantic model assemblies expose deterministic manifest schema version 1 through assembly metadata. Persistence assemblies explicitly select one or more manifests. `SemanticTypeModel.EFCore.Generators` emits ordinary Entity configurations and deterministic registration. Generated code may configure only CLR Entities owned by the selected model and must not inspect or mutate unrelated EF metadata. `ConfigureAfterGenerated` is the normal application override/hotfix boundary.

Runtime `ApplySemanticTypeModel` and `ApplySemanticRelationalModel`, including global convention suppression, exact-set enforcement, removal, and auditing, are removed. Provider-neutral `DeriveEfRelationalModel` remains an inspection API. The application owns `DbContext` composition, migrations, providers, and manual entity configurations.
