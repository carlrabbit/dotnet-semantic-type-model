# SemanticTypeModel.DependencyInjection

`SemanticTypeModel.DependencyInjection` provides runtime composition helpers for registering semantic type model providers, transformations, and projections with `Microsoft.Extensions.DependencyInjection`.

```sh
dotnet add package SemanticTypeModel.DependencyInjection --version 1.1.0
```

Use this package when an application needs runtime model providers or projection services. JSON Editor compatibility is provided by `SemanticTypeModel.JsonSchema` through `JsonSchemaUiMode.JsonEditorCompatible`; it is not a standalone package.
