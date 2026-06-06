using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.Generated;

// AppSemanticTypeModel is generated during the normal project build by the
// SemanticTypeModel.Generators package referenced by this consumer project.
TypeSchemaModel canonicalModel = AppSemanticTypeModel.Create();

// JSON Schema is a code-first projection: derive the package-owned JSON Schema
// domain semantic model, inspect diagnostics/trace, and then export Draft 2020-12.
var jsonSchemaModel = canonicalModel.DeriveJsonSchemaModel(options => options.UseDefaultTransformations());
JsonSchemaExportResult exported = JsonSchemaExporter.Export(jsonSchemaModel.Model);

string outputDirectory = Path.Combine("artifacts", "samples", "code-first-json-schema");
Directory.CreateDirectory(outputDirectory);
string outputPath = Path.Combine(outputDirectory, "customer.schema.json");
File.WriteAllText(outputPath, exported.Document.RootElement.GetRawText());

Console.WriteLine($"root: {canonicalModel.RootIdentifier}");
Console.WriteLine($"shapes: {canonicalModel.Shapes.Count}");
Console.WriteLine($"derivation diagnostics: {jsonSchemaModel.Diagnostics.Count}");
Console.WriteLine($"derivation trace: {string.Join(", ", jsonSchemaModel.Trace.Entries.Select(static entry => entry.TransformationId))}");
Console.WriteLine($"export diagnostics: {exported.Diagnostics.Count}");
Console.WriteLine($"artifacts: {outputPath}");
