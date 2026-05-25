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

JsonSchemaImportResult imported = JsonSchemaImporter.Import(input);
var adapted = LegacyTypeSchemaModelAdapter.Adapt(imported.Model);

SchemaTransformationPipeline pipeline = SchemaTransformationPipeline.Create()
    .Use(new NormalizeNamesTransformation())
    .Use(new NormalizeAnnotationsTransformation())
    .Use(new ValidateModelTransformation());

SchemaPipelineResult transformed = await pipeline.RunAsync(adapted.Model!);
var projectionContext = new SchemaProjectionContext { Target = ProjectionTarget.JsonSchema };
var exporter = new JsonSchemaRuntimeProjection();
JsonSchemaExportResult exported = exporter.Project(transformed.Model, projectionContext);

Console.WriteLine($"import diagnostics: {imported.Diagnostics.Count}");
Console.WriteLine($"transform diagnostics: {transformed.Diagnostics.Count}");
Console.WriteLine($"projection diagnostics: {projectionContext.Diagnostics.Count}");
Console.WriteLine(exported.Document.RootElement.GetRawText());
