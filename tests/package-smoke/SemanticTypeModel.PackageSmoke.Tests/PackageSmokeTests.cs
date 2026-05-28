using Microsoft.Extensions.DependencyInjection;
using Hardening = SemanticTypeModel.Abstractions.Hardening;
using Legacy = SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Abstractions.Runtime;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.JsonSchema.Import;
using SemanticTypeModel.PowerBI;
using LegacyTypeSchemaModel = SemanticTypeModel.Abstractions.Model.TypeSchemaModel;

namespace SemanticTypeModel.PackageSmoke.Tests;

[SemanticType(Name = "SmokeCustomer")]
internal sealed partial class SmokeCustomer
{
    public string Id { get; set; } = string.Empty;
}

internal sealed class PackageSmokeTests
{
    [Test]
    public async Task PackageSmokeShouldCoverPublicPackageApis()
    {
        var legacyBuilder = new SemanticTypeModel.Core.Building.TypeSchemaModelBuilder()
            .AddShape("Root", new Legacy.ScalarShape { Kind = Legacy.ScalarKind.String })
            .SetRoot("Root");
        LegacyTypeSchemaModel legacyModel = legacyBuilder.Build();

        _ = await Assert.That(legacyModel.RootIdentifier).IsEqualTo("Root");

        JsonSchemaImportResult imported = JsonSchemaImporter.Import("""
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "title": "Customer",
          "type": "object",
          "properties": {
            "id": { "type": "string" }
          }
        }
        """);

        _ = await Assert.That(imported.Model).IsNotNull();
        JsonSchemaExportResult exported = JsonSchemaExporter.Export(imported.Model);
        _ = await Assert.That(exported.Document.RootElement.TryGetProperty("properties", out _)).IsTrue();

        Hardening.TypeSchemaModel hardeningModel = BuildHardeningModel();

        Hardening.SchemaProjectionContext powerBiContext = new() { Target = Hardening.ProjectionTarget.PowerBi };
        TabularModelDefinition powerBiProjection = new PowerBiTabularProjection().Project(hardeningModel, powerBiContext);
        _ = await Assert.That(powerBiProjection).IsNotNull();

        Hardening.SchemaProjectionContext efCoreContext = new() { Target = Hardening.ProjectionTarget.EfCore };
        EfModelDefinition efCoreProjection = new EfCoreModelProjection().Project(hardeningModel, efCoreContext);
        _ = await Assert.That(efCoreProjection).IsNotNull();

        using ServiceProvider provider = new ServiceCollection()
            .AddSemanticTypeModel(hardeningModel)
            .AddSemanticTypeModelJsonSchema()
            .BuildServiceProvider();

        ITypeSchemaModelService modelService = provider.GetRequiredService<ITypeSchemaModelService>();
        TypeSchemaModelResult modelResult = await modelService.GetModelAsync();
        _ = await Assert.That(modelResult.Model).IsNotNull();

        _ = typeof(SemanticTypeAttribute);
        _ = typeof(SemanticDisplayNameAttribute);
        _ = typeof(SemanticFormatAttribute);
        _ = typeof(SemanticStringConstraintsAttribute);
        _ = typeof(SemanticAnnotationAttribute);

        _ = await Assert.That(nameof(SmokeCustomer)).IsEqualTo("SmokeCustomer");
    }

    private static Hardening.TypeSchemaModel BuildHardeningModel()
    {
        Hardening.ScalarTypeDefinition scalar = new()
        {
            Id = new Hardening.TypeId("String"),
            Name = "String",
            Kind = Hardening.TypeKind.Scalar,
            Nullability = Hardening.Nullability.NonNullable,
            Annotations = new Hardening.AnnotationBag(),
            ScalarKind = Hardening.ScalarKind.String,
        };

        System.Collections.Generic.Dictionary<Hardening.TypeId, Hardening.TypeDefinition> typesById = new()
        {
            [scalar.Id] = scalar,
        };

        return new Hardening.TypeSchemaModel
        {
            Id = new Hardening.SchemaModelId("String"),
            Types = [scalar],
            TypesById = typesById,
            Annotations = new Hardening.AnnotationBag(),
        };
    }
}
