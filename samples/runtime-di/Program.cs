using Microsoft.Extensions.DependencyInjection;
using SemanticTypeModel.Abstractions.Runtime;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Import;

const string schema = """
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "Order",
  "type": "object",
  "properties": {
    "orderId": { "type": "string" }
  },
  "required": ["orderId"]
}
""";

// Consumers register model creation, transformations, and projections in their DI container.
using ServiceProvider serviceProvider = new ServiceCollection()
    .AddSemanticTypeModel(() => JsonSchemaImporter.Import(schema).Model)
    .AddSemanticTypeModelTransformation<ValidateModelTransformation>()
    .AddSemanticTypeModelJsonSchema()
    .BuildServiceProvider();

ITypeSchemaModelService modelService = serviceProvider.GetRequiredService<ITypeSchemaModelService>();
TypeSchemaModelResult modelResult = await modelService.GetModelAsync();
SchemaProjectionResult<JsonSchemaExportResult> projection = await serviceProvider
    .GetRequiredService<ITypeSchemaProjectionService<JsonSchemaExportResult>>()
    .ProjectAsync();

Console.WriteLine($"runtime diagnostics: {modelResult.Diagnostics.Count}");
Console.WriteLine($"projection diagnostics: {projection.Diagnostics.Count}");
Console.WriteLine(projection.Projection!.Document.RootElement.GetRawText());
