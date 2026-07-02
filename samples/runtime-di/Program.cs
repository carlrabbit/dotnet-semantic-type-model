using Microsoft.Extensions.DependencyInjection;
using SemanticTypeModel.Abstractions.Runtime;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.Samples.OrderFulfillment.Domain;

using ServiceProvider serviceProvider = new ServiceCollection()
    .AddSemanticTypeModel(OrderFulfillmentSemanticModel.Create)
    .AddSemanticTypeModelTransformation<ValidateModelTransformation>()
    .AddSemanticTypeModelJsonSchema()
    .BuildServiceProvider();

ITypeSchemaModelService modelService = serviceProvider.GetRequiredService<ITypeSchemaModelService>();
TypeSchemaModelResult modelResult = await modelService.GetModelAsync();
SchemaProjectionResult<JsonSchemaExportResult> projection = await serviceProvider.GetRequiredService<ITypeSchemaProjectionService<JsonSchemaExportResult>>().ProjectAsync();
string json = projection.Projection!.Document.RootElement.GetRawText();
Require(modelResult.Model!.Types.Any(t => t.Name == "Customer"), "Runtime DI exposes shared Customer model.");
Require(json.Contains("Customer", StringComparison.Ordinal), "Runtime JSON Schema projection uses shared model.");
Console.WriteLine($"Runtime DI sample passed: {modelResult.Model.Types.Count} model types and {projection.Diagnostics.Count} projection diagnostics.");
static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
