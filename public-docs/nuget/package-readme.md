# SemanticTypeModel Package README Source

SemanticTypeModel provides unified canonical semantic model contracts, code-first model generation, and projection/runtime integration for .NET 10.

## Install

```sh
dotnet add package SemanticTypeModel.Generators
dotnet add package SemanticTypeModel.JsonSchema
```

## Quick Example

```csharp
using SemanticTypeModel.DotNet;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;

[SemanticType]
public sealed partial class Customer
{
    public required string Id { get; init; }
}

var model = AppSemanticTypeModel.Create();
var jsonSchemaModel = model.DeriveJsonSchemaModel();
var exported = JsonSchemaExporter.Export(jsonSchemaModel.Model);
Console.WriteLine(exported.Document.RootElement.GetRawText());
```

For full docs and samples, see repository `README.md` and `public-docs/`.
