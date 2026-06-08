using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Runtime;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.Generated;
using SemanticTypeModel.Samples.CodeFirstEfCore;

// The generated provider is compiled into this assembly by the packaged source generator.
TypeSchemaModel generatedModel = AppSemanticTypeModel.Create();
var adapted = LegacyTypeSchemaModelAdapter.Adapt(generatedModel);
var hardenedModel = adapted.Model ?? throw new InvalidOperationException("Generated model could not be adapted.");

SemanticDerivationResult<EfCoreSemanticModel> derived = hardenedModel.DeriveEfCoreModel(options =>
{
    // Samples keep all output in memory; this configures provider-neutral projection metadata only.
    options.Projection = options.Projection with { ProjectUnannotatedObjectsAsEntities = true };
    _ = options.Envelopes.For<ManagedSpecificationEnvelope>()
        .UseEnvelopeAsEntity()
        .Payload(x => x.Specification)
        .StoreAsSerializedJson("SpecificationJson");
});

var modelBuilder = new ModelBuilder(new ConventionSet());
modelBuilder.ApplyEfCoreSemanticModel(derived.Model, defaultSchema: "sample");

Console.WriteLine($"root: {generatedModel.RootIdentifier}");
Console.WriteLine($"adapter diagnostics: {adapted.Diagnostics.Count}");
Console.WriteLine($"derivation diagnostics: {derived.Diagnostics.Count}");
Console.WriteLine($"modelBuilder entities: {derived.Model.EntityTypes.Count}");
Console.WriteLine($"trace steps: {derived.Trace.Entries.Count}");
