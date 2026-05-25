# SemanticTypeModel Package README Source

SemanticTypeModel provides canonical semantic model contracts, import/export tooling, and projection/runtime integration for .NET 10.

## Install

```sh
dotnet add package SemanticTypeModel.JsonSchema
```

## Quick Example

```csharp
using SemanticTypeModel.JsonSchema.Import;
using SemanticTypeModel.JsonSchema.Export;

var imported = JsonSchemaImporter.Import("""{"type":"string"}""");
var exported = JsonSchemaExporter.Export(imported.Model);
Console.WriteLine(exported.Document.RootElement.GetRawText());
```

For full docs and samples, see repository `README.md` and `public-docs/`.
