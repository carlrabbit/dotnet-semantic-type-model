using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Runtime;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.Generated;

// The generated provider is compiled into this assembly by the packaged source generator.
TypeSchemaModel generatedModel = AppSemanticTypeModel.Create();
var adapted = LegacyTypeSchemaModelAdapter.Adapt(generatedModel);
var hardenedModel = adapted.Model ?? throw new InvalidOperationException("Generated model could not be adapted.");

var modelBuilder = new ModelBuilder(new ConventionSet());
EfCoreModelBuilderProjectionResult applied = modelBuilder.ApplySemanticTypeModel(
    hardenedModel,
    options =>
    {
        // Samples keep all output in memory; this configures projection metadata only.
        options.ProjectUnannotatedObjectsAsEntities = true;
        options.DefaultSchema = "sample";
    });

Console.WriteLine($"root: {generatedModel.RootIdentifier}");
Console.WriteLine($"adapter diagnostics: {adapted.Diagnostics.Count}");
Console.WriteLine($"modelBuilder entities: {applied.Model.EntityTypes.Count}");
