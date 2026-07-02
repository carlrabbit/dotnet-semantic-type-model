using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.Samples.OrderFulfillment.Domain;

TypeSchemaModel model = OrderFulfillmentSemanticModel.Create();
SemanticDerivationResult<EfCoreSemanticModel> derived = model.DeriveEfCoreModel();
var modelBuilder = new ModelBuilder(new ConventionSet());
modelBuilder.ApplyEfCoreSemanticModel(derived.Model, defaultSchema: "fulfillment");
var efModel = modelBuilder.Model;

Require(derived.Model.EntityTypes.Any(e => e.Name == "Customer"), "Customer entity is projected.");
Require(derived.Model.EntityTypes.Any(e => e.Name == "Order"), "Order entity is projected.");
Require(derived.Model.EntityTypes.Any(e => e.Name == "OrderLine"), "OrderLine entity is projected.");
var orderLine = derived.Model.EntityTypes.Single(e => e.Name == "OrderLine");
Require(orderLine.Keys.Any(k => k.PropertyNames.Contains("OrderId") && k.PropertyNames.Contains("LineNumber")), "OrderLine composite key is projected.");
var probe = derived.Model.EntityTypes.Single(e => e.Name == "ProjectionProbe");
foreach (var name in new[] { "OptionalInt", "OptionalLong", "OptionalDecimal", "OptionalBool", "OptionalDateTime", "OptionalDateTimeOffset", "OptionalGuid" })
{
    var property = probe.Properties.Single(p => p.Name == name);
    Require(property.IsNullable && Nullable.GetUnderlyingType(property.ClrType) is not null, $"{name} uses Nullable<T> in EF domain metadata.");
    var runtimeProperty = efModel.FindEntityType("ProjectionProbe")!.FindProperty(name)!;
    Require(runtimeProperty.IsNullable && runtimeProperty.ClrType == property.ClrType, $"{name} runtime EF metadata matches projection metadata.");
}
Require(probe.Properties.Single(p => p.Name == "RequiredInt").ClrType == typeof(long), "Required value-type control remains non-nullable.");
Require(efModel.FindEntityType("Customer")!.FindProperty("BillingAddress_Line1") is not null, "Customer owned Address is flattened.");
Console.WriteLine($"EF Core sample passed: {derived.Model.EntityTypes.Count} entities from {model.Id.Value}.");

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
