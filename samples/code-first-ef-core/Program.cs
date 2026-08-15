using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.Generated.EFCore;
using SemanticTypeModel.Samples.OrderFulfillment.Domain;

TypeSchemaModel model = OrderFulfillmentSemanticModel.Create();
SemanticDerivationResult<EfRelationalModel> derived = model.DeriveEfRelationalModel();
if (derived.Diagnostics.Count > 0)
{
    throw new InvalidOperationException(string.Join(Environment.NewLine, derived.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
}
var modelBuilder = new ModelBuilder(new ConventionSet());
modelBuilder.ApplyAppSemanticModel();
Require(derived.Model.Entities.Any(e => e.Table == nameof(Customer)), "Customer entity is projected.");
Require(modelBuilder.Model.GetEntityTypes().Any(e => e.GetTableName() == nameof(Customer)), "Customer CLR table is applied.");
Console.WriteLine($"EF Core sample passed: {derived.Model.Entities.Count} entities from {model.Id.Value}.");

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
