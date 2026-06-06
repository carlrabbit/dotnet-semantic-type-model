using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Runtime;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.Generated;
using SemanticTypeModel.PowerBI;
using SemanticTypeModel.Samples.CodeFirstPowerBi;

// Power BI derivation consumes the same generated semantic model as other projections and emits only local metadata.
TypeSchemaModel generatedModel = AppSemanticTypeModel.Create();
var adapted = LegacyTypeSchemaModelAdapter.Adapt(generatedModel);
var hardenedModel = adapted.Model ?? throw new InvalidOperationException("Generated model could not be adapted.");

SemanticDerivationResult<PowerBiSemanticModel> result = hardenedModel.DerivePowerBiModel(options =>
{
    _ = options.UseDefaultTransformations();
    options.Projection.ProjectUnannotatedObjectsAsTables = true;
    options.Measures.Add<SalesRecord>("Total Sales", "SUM(SalesRecord[Amount])", measure =>
    {
        measure.FormatString = "$#,0.00";
        measure.DisplayFolder = "Sales";
    });
    options.CalculatedTables.Add("Positive Sales", "FILTER(SalesRecord, SalesRecord[Amount] > 0)");
});

Console.WriteLine($"root: {generatedModel.RootIdentifier}");
Console.WriteLine($"adapter diagnostics: {adapted.Diagnostics.Count}");
Console.WriteLine($"tables: {result.Model.Tables.Count}");
Console.WriteLine($"calculated tables: {result.Model.CalculatedTables.Count}");
Console.WriteLine($"diagnostics: {result.Diagnostics.Count}");
Console.WriteLine(PowerBiLocalMetadataExporter.Inspect(result.Model));
