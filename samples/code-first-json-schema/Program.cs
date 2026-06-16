using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.Generated;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;

TypeSchemaModel canonicalModel = AppSemanticTypeModel.Create();

var jsonSchemaModel = canonicalModel.DeriveJsonSchemaModel(options => _ = options.UseDefaultTransformations());
JsonSchemaExportResult exported = JsonSchemaExporter.Export(jsonSchemaModel.Model);

string outputDirectory = Path.Combine("artifacts", "samples", "code-first-json-schema");
Directory.CreateDirectory(outputDirectory);
string outputPath = Path.Combine(outputDirectory, "customer.schema.json");
File.WriteAllText(outputPath, exported.Document.RootElement.GetRawText());

Console.WriteLine($"root: {canonicalModel.Id.Value}");
Console.WriteLine($"types: {canonicalModel.Types.Count}");
Console.WriteLine($"derivation diagnostics: {jsonSchemaModel.Diagnostics.Count}");
Console.WriteLine($"derivation trace: {string.Join(", ", jsonSchemaModel.Trace.Entries.Select(static entry => entry.TransformationId))}");
Console.WriteLine($"export diagnostics: {exported.Diagnostics.Count}");
Console.WriteLine($"artifacts: {outputPath}");

[SemanticType(Name = "Customer")]
public sealed partial class Customer
{
    [SemanticKey]
    public required string Id { get; init; }

    public required string Name { get; init; }
}
