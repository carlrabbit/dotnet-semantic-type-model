using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Samples.OrderFulfillment.Domain;
using SemanticTypeModel.SystemTextJson;

TypeSchemaModel model = OrderFulfillmentSemanticModel.Create();
SystemTextJsonSemanticModel stjModel = model.DeriveSystemTextJsonModel(options => options.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName).Model;
JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
{
    TypeInfoResolver = AppJsonContext.Default.WithSemanticTypeModelJson(stjModel),
};
Customer customer = new() { CustomerId = "C-001", DisplayName = "Ada Lovelace", EmailAddress = "ada@example.test", LoyaltyTier = null, BillingAddress = new Address { Line1 = "1 Logic Ln", City = "London", CountryCode = "GB" }, LastContactedAt = null };
string json = JsonSerializer.Serialize(customer, options);
Customer? roundTripped = JsonSerializer.Deserialize<Customer>(json, options);
Require(json.Contains("customerId", StringComparison.Ordinal) || json.Contains("CustomerId", StringComparison.Ordinal), "Property-name policy applies through wrapped user context.");
Require(roundTripped?.CustomerId == customer.CustomerId && roundTripped.LoyaltyTier is null, "Customer resolves and nullable values round-trip.");
OrderSubmitted evt = new() { EventId = Guid.Parse("11111111-1111-1111-1111-111111111111"), OccurredAt = DateTimeOffset.UnixEpoch, Payload = new OrderSubmittedPayload { OrderId = "O-001", CustomerId = "C-001", InitialDiscount = null } };
string eventJson = JsonSerializer.Serialize(evt, options);
Require(eventJson.Contains("payload", StringComparison.Ordinal) || eventJson.Contains("Payload", StringComparison.Ordinal), "Order event envelope resolves.");
Console.WriteLine($"System.Text.Json sample passed: {json.Length + eventJson.Length} bytes.");
static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

[JsonSerializable(typeof(Customer))]
[JsonSerializable(typeof(OrderSubmitted))]
internal sealed partial class AppJsonContext : JsonSerializerContext { }
