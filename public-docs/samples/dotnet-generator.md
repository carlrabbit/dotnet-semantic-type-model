# Sample - .NET Generator to JSON Schema

Project: `samples/dotnet-generator-to-json-schema`

Demonstrates:

- annotated C# type discovery;
- projection-neutral semantic attributes such as role, display metadata, format, constraints, enum-value metadata, and custom annotations;
- compile-time source generator output (`SemanticTypeModel.Generated.AppSemanticTypeModel`);
- generated provider `Create()` usage;
- JSON Schema export.

Run:

```sh
dotnet run --project samples/dotnet-generator-to-json-schema/dotnet-generator-to-json-schema.csproj
```
