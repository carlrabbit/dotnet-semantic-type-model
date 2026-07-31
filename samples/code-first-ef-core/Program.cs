using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.Samples.OrderFulfillment.Domain;

TypeSchemaModel model = OrderFulfillmentSemanticModel.Create();
SemanticDerivationResult<EfCoreSemanticModel> derived = model.DeriveEfCoreModel();
var modelBuilder = new ModelBuilder(new ConventionSet());
_ = modelBuilder.Entity<Customer>();
_ = modelBuilder.Entity<Order>();
_ = modelBuilder.Entity<OrderLine>();
_ = modelBuilder.Entity<ProjectionProbe>();
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
}
Require(probe.Properties.Single(p => p.Name == "RequiredInt").ClrType == typeof(long), "Required value-type control remains non-nullable.");
Require(derived.Model.SourceTypes.Any(type => type.IsRootEntity && type.SourceClrTypeName.Contains(typeof(Customer).FullName!, StringComparison.Ordinal)), "Customer CLR root lineage is preserved.");
Require(efModel.FindEntityType(typeof(Customer)) is not null, "Closed application configures the Customer CLR entity.");
Console.WriteLine($"EF Core sample passed: {derived.Model.EntityTypes.Count} entities from {model.Id.Value}.");

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
