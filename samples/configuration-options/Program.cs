using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SemanticTypeModel.Configuration;
using SemanticTypeModel.Samples.OrderFulfillment.Domain;

var settings = new Dictionary<string, string?>
{
    ["orderProcessing:queueName"] = "orders",
    ["orderProcessing:maxConcurrentOrders"] = "8",
    ["orderProcessing:mode"] = "ColdChain",
    ["orderProcessing:coldChainProvider"] = "north-hub",
};
IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
var services = new ServiceCollection();
_ = services.AddSemanticOptions<OrderProcessingOptions>(configuration, OrderFulfillmentSemanticModel.Create());
using ServiceProvider provider = services.BuildServiceProvider();
OrderProcessingOptions selected = provider.GetRequiredService<IOptions<OrderProcessingOptions>>().Value;
Require(selected.QueueName == "orders" && selected.ExpeditedSurcharge is null, "Selected options bind required and nullable values.");
Require(!services.Any(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(ColdStorageOptions))), "Unrelated configuration type remains unregistered.");
Console.WriteLine($"Configuration sample passed: selected {selected.QueueName}; unrelated options unregistered.");
static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
