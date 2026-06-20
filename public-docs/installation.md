# Installation

Install the packages needed by your scenario. The stable package set does not include a standalone `SemanticTypeModel.JsonEditor` package.

```sh
dotnet add package SemanticTypeModel.JsonSchema --version 2.3.0
dotnet add package SemanticTypeModel.DotNet --version 2.3.0
dotnet add package SemanticTypeModel.SystemTextJson --version 2.3.0
```

For runtime composition, add dependency injection support explicitly:

```sh
dotnet add package SemanticTypeModel.DependencyInjection --version 2.3.0
dotnet add package SemanticTypeModel.Configuration --version 2.3.0
```

For projection targets, add the package that owns the target:

```sh
dotnet add package SemanticTypeModel.EFCore --version 2.3.0
dotnet add package SemanticTypeModel.PowerBI --version 2.3.0
```

JSON Editor compatibility is enabled through `SemanticTypeModel.JsonSchema` export options rather than through a separate package. For package roles and combinations, see [packages.md](packages.md).
