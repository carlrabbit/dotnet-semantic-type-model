using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.Generated;
using SemanticTypeModel.PowerBI;

TypeSchemaModel model = AppSemanticTypeModel.Create();
var result = model.DerivePowerBiModel(options =>
{
    _ = options.UseDefaultTransformations();
    options.Projection.ProjectUnannotatedObjectsAsTables = true;
});

Console.WriteLine($"root: {model.Id.Value}");
Console.WriteLine($"tables: {result.Model.Tables.Count}");
Console.WriteLine($"calculated tables: {result.Model.CalculatedTables.Count}");
Console.WriteLine($"diagnostics: {result.Diagnostics.Count}");
Console.WriteLine(PowerBiLocalMetadataExporter.Inspect(result.Model));

[SemanticType(Name = "SalesRecord")]
public sealed partial class SalesRecord
{
    [SemanticKey]
    public required string Id { get; init; }

    public decimal Amount { get; init; }
}
