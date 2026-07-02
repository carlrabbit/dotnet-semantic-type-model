using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.Samples.OrderFulfillment.Domain;

TypeSchemaModel model = OrderFulfillmentSemanticModel.Create();
var jsonSchemaModel = model.DeriveJsonSchemaModel(options => _ = options.UseDefaultTransformations());
JsonSchemaExportResult exported = JsonSchemaExporter.Export(jsonSchemaModel.Model);
string json = exported.Document.RootElement.GetRawText();
using JsonDocument doc = JsonDocument.Parse(json);
string text = doc.RootElement.GetRawText();
Require(text.Contains("Customer", StringComparison.Ordinal), "Customer schema is present for editing.");
Require(text.Contains("DisplayName", StringComparison.Ordinal) || text.Contains("displayName", StringComparison.Ordinal), "Customer required display field is present.");
Require(text.Contains("BillingAddress", StringComparison.Ordinal) || text.Contains("billingAddress", StringComparison.Ordinal), "Owned Address object shape is present.");
Require(text.Contains("OrderLine", StringComparison.Ordinal), "OrderLine schema is present.");
Require(text.Contains("null", StringComparison.Ordinal), "Nullable properties are represented.");
string outputDirectory = Path.Combine("artifacts", "samples", "code-first-json-schema");
Directory.CreateDirectory(outputDirectory);
string outputPath = Path.Combine(outputDirectory, "order-fulfillment.schema.json");
File.WriteAllText(outputPath, json);
Console.WriteLine($"JSON Schema sample passed: {model.Types.Count} canonical types; artifact {outputPath}.");
static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
