using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.SystemTextJson;

Customer customer = new() { Id = "C-001", DisplayName = "Ada" };
string json = JsonSerializer.Serialize(customer, SemanticTypeModel.Generated.SampleJsonContext.Default.Customer);
Customer? roundTripped = JsonSerializer.Deserialize(json, SemanticTypeModel.Generated.SampleJsonContext.Default.Customer);

Console.WriteLine(json);
Console.WriteLine(roundTripped?.DisplayName);
Console.WriteLine(SystemTextJsonAnnotationNames.PropertyName);

[SemanticType]
public sealed class Customer
{
    [SemanticName("Customer ID")]
    [JsonPropertyName("customer_id")]
    public required string Id { get; init; }

    [JsonPropertyName("display_name")]
    public required string DisplayName { get; init; }
}
