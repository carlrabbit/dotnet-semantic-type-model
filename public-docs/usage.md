# Using SemanticTypeModel

## Install a scenario

SemanticTypeModel is a tightly coupled package suite. **Use the same exact version for every
`SemanticTypeModel.*` package in one application.**

Start with:

```sh
dotnet add package SemanticTypeModel.DotNet
dotnet add package SemanticTypeModel.Generators
```

Add the target/runtime package you need:

```text
JSON Schema             SemanticTypeModel.JsonSchema
EF Core                 SemanticTypeModel.EFCore + SemanticTypeModel.EFCore.Generators
System.Text.Json        SemanticTypeModel.SystemTextJson
Power BI                SemanticTypeModel.PowerBI
Runtime DI              SemanticTypeModel.DependencyInjection
Semantic test data      SemanticTypeModel.TestData
```

See the [shared package README](nuget/SemanticTypeModel.md) for package roles.

## Define a model

Annotated .NET code is the supported authoring source.

```csharp
using SemanticTypeModel.DotNet;

[SemanticType(SemanticTypeRole.Entity)]
public sealed class Customer
{
    [SemanticKey]
    [SemanticLogicalType("CustomerId")]
    public required Guid Id { get; init; }

    [SemanticDisplayName("Customer name")]
    [SemanticUserDescription("Name shown to users.")]
    public required string Name { get; init; }
}
```

`SemanticLogicalType` is optional property-level semantic metadata over an ordinary scalar. It does not change
its CLR or target representation and is not inferred from CLR wrapper shape.

`SemanticTypeModel.Generators` extracts the configured source set at compile time and emits a deterministic
provider for the canonical `TypeSchemaModel`.

## Create the model

The default generated API is:

```csharp
using SemanticTypeModel.Generated;

TypeSchemaModel model = AppSemanticTypeModel.Create();
```

The default namespace is `SemanticTypeModel.Generated` and the default provider type is
`AppSemanticTypeModel`. Both are configurable; see [Configuration](configuration.md).

## Use a target or runtime capability

### JSON Schema

```csharp
var result = model.DeriveJsonSchemaModel(options => options.UseDefaultTransformations());
result.Diagnostics.ThrowIfErrors();

JsonSchemaExportResult export = JsonSchemaExporter.Export(result.Model);
```

See [JSON Schema](guides/json-schema.md).

### EF Core

The model project emits a semantic manifest. The persistence project selects the model and lets
`SemanticTypeModel.EFCore.Generators` emit normal EF configuration:

```csharp
[assembly: GenerateSemanticEfModel(typeof(AppModelMarker))]

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyAppSemanticModel();
}
```

See [EF Core](guides/ef-core.md).

### System.Text.Json

SemanticTypeModel customizes application-owned serializer options/resolvers; it does not generate a serializer
context. See [System.Text.Json](guides/system-text-json.md).

### Power BI

Derive deterministic local analytical metadata. See [Power BI](guides/power-bi.md).

### Semantic test data

Random deterministic generation needs only the canonical model:

```csharp
Customer customer = model.TestData()
    .WithSizeProfile(TestDataSizeProfile.Moderate)
    .WithSeed(42)
    .Generate<Customer>();
```

`Simple`, `Moderate`, and `Extreme` are size profiles; they do not change validity rules. Optional terminology
profiles can supply synthetic property/Logical-Type candidates, and programmatic scalar generators can supply
application-specific values. Candidates are validated against supported canonical constraints and safety budgets;
invalid explicit custom values fail closed. Built-in regex synthesis is intentionally unsupported.

See [Constraint-aware test data](guides/test-data.md).

## Handle diagnostics

Compile-time generator problems appear as diagnostics such as `STM5xxx`. Runtime derivation/projection APIs
return diagnostic collections. TestData uses descriptive `TESTDATA_*` runtime diagnostics.

Do not ignore diagnostics merely because output was produced.

- Start with [Troubleshooting](troubleshooting.md) when you have a symptom.
- Use [Diagnostics](diagnostics.md) when you have a diagnostic ID.
- Use the target guide for projection-specific failures and limitations.

## Configure generation

Common customizations include:

```csharp
[assembly: SemanticTypeModelGeneratorOptions(
    generatedNamespace: "MyApplication.Semantics",
    providerName: "ApplicationSemanticModel",
    IncludeInternalTypes = true)]
```

Equivalent MSBuild properties are available. See the complete [Configuration reference](configuration.md).

## Inspect generated source

To write compiler-generated files for inspection:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Do not commit generated files.

## Next references

- [Core semantics](guides/core-semantics.md)
- [Configuration](configuration.md)
- [Projection capabilities](guides/projection-capabilities.md)
- [Constraint-aware test data](guides/test-data.md)
- [Diagnostics](diagnostics.md)
- [Compatibility](api/compatibility.md)
- [Executable samples](samples.md)
