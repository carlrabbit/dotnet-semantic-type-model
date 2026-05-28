# SemanticTypeModel

SemanticTypeModel is a .NET 10 library set for canonical semantic type models, JSON Schema import/export, transformations, runtime composition, and projection targets such as Power BI-like and EF Core-like metadata.

## Install

Add the package(s) your scenario needs:

```sh
dotnet add package SemanticTypeModel.JsonSchema --version 0.1.0-alpha
dotnet add package SemanticTypeModel.JsonEditor --version 0.1.0-alpha
dotnet add package SemanticTypeModel.DotNet --version 0.1.0-alpha
```

See full package guidance in [public-docs/packages.md](public-docs/packages.md).

## Quick Start

```csharp
using SemanticTypeModel.JsonSchema.Import;
using SemanticTypeModel.JsonSchema.Export;

const string schema = """
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "Customer",
  "type": "object",
  "properties": {
    "id": { "type": "string" }
  },
  "required": ["id"]
}
""";

var imported = JsonSchemaImporter.Import(schema);
var exported = JsonSchemaExporter.Export(imported.Model!);
Console.WriteLine(exported.Document.RootElement.GetRawText());
```

## Package List (0.1.0-alpha)

- `SemanticTypeModel.Abstractions`
- `SemanticTypeModel.Core`
- `SemanticTypeModel.JsonSchema`
- `SemanticTypeModel.DotNet`
- `SemanticTypeModel.Generators`
- `SemanticTypeModel.JsonEditor`
- `SemanticTypeModel.PowerBI`
- `SemanticTypeModel.EFCore`

## Prerelease Notice

`0.1.0-alpha` is the first prerelease. APIs, package split, and known limitations may change before 1.0.

## Samples

Runnable samples live under `samples/` and are documented in [public-docs/samples.md](public-docs/samples.md).

For a code-first end-to-end flow, start with `samples/code-first-authoring`.

Run all samples:

```sh
./eng/samples.sh
```

## Public Docs

- [public-docs/getting-started.md](public-docs/getting-started.md)
- [public-docs/installation.md](public-docs/installation.md)
- [public-docs/concepts.md](public-docs/concepts.md)
- [public-docs/packages.md](public-docs/packages.md)
- [public-docs/api/public-api.md](public-docs/api/public-api.md)
- [public-docs/diagnostics.md](public-docs/diagnostics.md)
- [public-docs/guides/ef-core-projection.md](public-docs/guides/ef-core-projection.md)
- [public-docs/guides/projection-capabilities.md](public-docs/guides/projection-capabilities.md)
- [public-docs/versioning.md](public-docs/versioning.md)
- [public-docs/release-notes.md](public-docs/release-notes.md)

## Contributor Docs

- [docs/TERMINOLOGY.md](docs/TERMINOLOGY.md)
- [docs/GUARDRAILS.md](docs/GUARDRAILS.md)
- [docs/ENGINEERING.md](docs/ENGINEERING.md)
- [docs/MILESTONES.md](docs/MILESTONES.md)
- [docs/PUBLIC-DOCS.md](docs/PUBLIC-DOCS.md)
