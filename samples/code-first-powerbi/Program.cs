using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.PowerBI;
using SemanticTypeModel.Samples.OrderFulfillment.Domain;

TypeSchemaModel model = OrderFulfillmentSemanticModel.Create();
var result = model.DerivePowerBiModel(options => _ = options.UseDefaultTransformations());
Require(result.Model.Tables.Any(t => t.Name == "Customer"), "Customer dimension is projected.");
Require(result.Model.Tables.Any(t => t.Name == "Product"), "Product dimension is projected.");
Require(result.Model.Tables.Any(t => t.Name == "Order"), "Order fact table is projected.");
Require(result.Model.Tables.Any(t => t.Name == "OrderLine"), "OrderLine fact table is projected.");
Require(!result.Diagnostics.Any(d => d.Severity.ToString() == "Error"), "No Power BI errors are produced.");
Console.WriteLine($"Power BI sample passed: {result.Model.Tables.Count} tables from {model.Id.Value}.");
Console.WriteLine(PowerBiLocalMetadataExporter.Inspect(result.Model));
static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
