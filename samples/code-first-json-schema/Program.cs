using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.Generated;

// AppSemanticTypeModel is generated during the normal project build by the
// SemanticTypeModel.Generators package referenced by this consumer project.
TypeSchemaModel model = AppSemanticTypeModel.Create();
JsonSchemaExportResult exported = JsonSchemaExporter.Export(model);

string outputDirectory = Path.Combine("artifacts", "samples", "code-first-json-schema");
Directory.CreateDirectory(outputDirectory);
string outputPath = Path.Combine(outputDirectory, "customer.schema.json");
File.WriteAllText(outputPath, exported.Document.RootElement.GetRawText());

Console.WriteLine($"root: {model.RootIdentifier}");
Console.WriteLine($"shapes: {model.Shapes.Count}");
Console.WriteLine($"artifacts: {outputPath}");
