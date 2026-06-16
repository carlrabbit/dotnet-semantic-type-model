using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.Generated;

TypeSchemaModel model = AppSemanticTypeModel.Create();
SemanticDerivationResult<EfCoreSemanticModel> derived = model.DeriveEfCoreModel(options => options.Projection = options.Projection with { ProjectUnannotatedObjectsAsEntities = true });

var modelBuilder = new ModelBuilder(new ConventionSet());
modelBuilder.ApplyEfCoreSemanticModel(derived.Model, defaultSchema: "sample");

Console.WriteLine($"root: {model.Id.Value}");
Console.WriteLine($"derivation diagnostics: {derived.Diagnostics.Count}");
Console.WriteLine($"modelBuilder entities: {derived.Model.EntityTypes.Count}");
Console.WriteLine($"trace steps: {derived.Trace.Entries.Count}");

[SemanticType(Name = "Customer")]
public sealed partial class Customer
{
    [SemanticKey]
    public required string Id { get; init; }

    public required string Name { get; init; }
}
