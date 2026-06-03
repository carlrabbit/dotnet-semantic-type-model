using SemanticTypeModel.Abstractions.Model;
using Hardening = SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Runtime;
using SemanticTypeModel.PowerBI;
using SemanticTypeModel.Generated;

// Power BI projection consumes the same generated semantic model as other projections.
TypeSchemaModel generatedModel = AppSemanticTypeModel.Create();
var adapted = LegacyTypeSchemaModelAdapter.Adapt(generatedModel);
var hardenedModel = adapted.Model ?? throw new InvalidOperationException("Generated model could not be adapted.");

var projection = new PowerBiModelProjection(new PowerBiProjectionOptions { ProjectUnannotatedObjectsAsTables = true });
var context = new Hardening.SchemaProjectionContext { Target = Hardening.ProjectionTarget.PowerBi };
PowerBiProjectionModel powerBiModel = projection.Project(hardenedModel, context);

Console.WriteLine($"root: {generatedModel.RootIdentifier}");
Console.WriteLine($"adapter diagnostics: {adapted.Diagnostics.Count}");
Console.WriteLine($"tables: {powerBiModel.Tables.Count}");
Console.WriteLine($"diagnostics: {powerBiModel.Diagnostics.Count}");
