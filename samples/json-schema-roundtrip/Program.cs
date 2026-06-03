using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Runtime;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Import;
using SemanticTypeModel.JsonSchema.Runtime;

var input = """
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "Customer",
  "type": "object",
  "properties": {
    "id": { "type": "string" },
    "name": { "type": "string" }
  },
  "required": ["id", "name"]
}
""";

// The importer reads a consumer-provided JSON Schema into the canonical semantic model.
JsonSchemaImportResult imported = JsonSchemaImporter.Import(input);
var adapted = LegacyTypeSchemaModelAdapter.Adapt(imported.Model);

// Transformations are deterministic validation/normalization steps before projection.
SchemaTransformationPipeline pipeline = SchemaTransformationPipeline.Create()
    .Use(new NormalizeNamesTransformation())
    .Use(new NormalizeAnnotationsTransformation())
    .Use(new ValidateModelTransformation());

SchemaPipelineResult transformed = await pipeline.RunAsync(adapted.Model!);
var projectionContext = new SchemaProjectionContext { Target = ProjectionTarget.JsonSchema };
var exporter = new JsonSchemaRuntimeProjection();
JsonSchemaExportResult exported = exporter.Project(transformed.Model, projectionContext);

string outputDirectory = Path.Combine("artifacts", "samples", "json-schema-roundtrip");
Directory.CreateDirectory(outputDirectory);
string outputPath = Path.Combine(outputDirectory, "customer.roundtrip.schema.json");
File.WriteAllText(outputPath, exported.Document.RootElement.GetRawText());

Console.WriteLine($"import diagnostics: {imported.Diagnostics.Count}");
Console.WriteLine($"adapter diagnostics: {adapted.Diagnostics.Count}");
Console.WriteLine($"transform diagnostics: {transformed.Diagnostics.Count}");
Console.WriteLine($"projection diagnostics: {projectionContext.Diagnostics.Count}");
Console.WriteLine($"artifacts: {outputPath}");
