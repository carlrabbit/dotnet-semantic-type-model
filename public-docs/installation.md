# Installation

Install the packages needed by your scenario. The 1.0 stable package set does not include a standalone `SemanticTypeModel.JsonEditor` package.

```sh
dotnet add package SemanticTypeModel.JsonSchema --version 1.0.0
dotnet add package SemanticTypeModel.DotNet --version 1.0.0
dotnet add package SemanticTypeModel.SystemTextJson --version 1.0.0
```

For runtime composition, add dependency injection support explicitly:

```sh
dotnet add package SemanticTypeModel.DependencyInjection --version 1.0.0
```

For projection targets, add the package that owns the target:

```sh
dotnet add package SemanticTypeModel.EFCore --version 1.0.0
dotnet add package SemanticTypeModel.PowerBI --version 1.0.0
```

JSON Editor compatibility is enabled through `SemanticTypeModel.JsonSchema` export options rather than through a separate package. For package roles and combinations, see [packages.md](packages.md).
