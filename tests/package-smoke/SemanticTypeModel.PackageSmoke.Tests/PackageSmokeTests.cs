using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using Model = SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Abstractions.Runtime;
using SemanticTypeModel.Core.Authoring;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.PowerBI;
using SemanticTypeModel.SystemTextJson;
using SemanticTypeModel.TestData;

[assembly: SemanticTypeModelGeneratorOptions("SemanticTypeModel.PackageSmoke.Tests.Generated", "PackageSmokeSemanticTypeModel", IncludeInternalTypes = true)]

namespace SemanticTypeModel.PackageSmoke.Tests;

[SemanticType(Name = "SmokeCustomer")]
internal sealed partial class SmokeCustomer
{
    [SemanticDisplayIdentity, SemanticAccessPath("ById")]
    public string Id { get; set; } = string.Empty;

    public Guid ScalarId { get; set; }

}

[SemanticType(SemanticTypeRole.Entity)]
internal abstract class SmokeEntity
{
    public Guid Id { get; set; }
}

[SemanticType(SemanticTypeRole.Entity)]
internal sealed class SmokeSpecialEntity : SmokeEntity
{
    public Guid SpecialId { get; set; }
}

internal sealed class PackageSmokeTests
{
    [Test]
    public async Task PackageSmokeShouldCoverPublicPackageApis()
    {
        Model.TypeSchemaModel canonicalModel = BuildCanonicalModel();
        SemanticTestValue dynamicValue = canonicalModel.TestData().Generate(new Model.TypeId("SmokeRoot"));
        _ = await Assert.That(dynamicValue).IsTypeOf<ObjectTestValue>();
        JsonSchemaExportResult exported = JsonSchemaExporter.Export(canonicalModel.DeriveJsonSchemaModel().Model);
        _ = await Assert.That(exported.Document.RootElement.GetRawText()).Contains("string");


        Model.SchemaProjectionContext powerBiContext = new() { Target = Model.ProjectionTarget.PowerBi };
        PowerBiProjectionModel powerBiProjection = new PowerBiModelProjection().Project(canonicalModel, powerBiContext);
        _ = await Assert.That(powerBiProjection).IsNotNull();

        EfRelationalModel efCoreProjection = canonicalModel.DeriveEfRelationalModel().Model;
        _ = await Assert.That(efCoreProjection).IsNotNull();

        using ServiceProvider provider = new ServiceCollection()
            .AddSemanticTypeModel(canonicalModel)
            .AddSemanticTypeModelJsonSchema()
            .BuildServiceProvider();

        ITypeSchemaModelService modelService = provider.GetRequiredService<ITypeSchemaModelService>();
        TypeSchemaModelResult modelResult = await modelService.GetModelAsync();
        _ = await Assert.That(modelResult.Model).IsNotNull();

        _ = typeof(SemanticTypeAttribute);
        _ = typeof(SemanticDisplayIdentityAttribute);
        _ = typeof(SemanticAccessPathAttribute);
        _ = typeof(SemanticDisplayNameAttribute);
        _ = typeof(SemanticFormatAttribute);
        _ = typeof(SemanticStringConstraintsAttribute);
        _ = typeof(SemanticAnnotationAttribute);
        Model.TypeSchemaModel generatedSmokeModel = Generated.PackageSmokeSemanticTypeModel.Create();
        JsonSerializerOptions jsonOptions = new();
        _ = jsonOptions.AddSemanticTypeModelJson(
            generatedSmokeModel,
            projectionOptions => projectionOptions.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName);
        var smokeJson = JsonSerializer.Serialize(
            new SmokeCustomer { Id = "C-001", ScalarId = Guid.Parse("00000000-0000-0000-0000-000000000001") },
            jsonOptions);
        SmokeCustomer? smokeCustomer = JsonSerializer.Deserialize<SmokeCustomer>("""
            { "Id": "C-002" }
            """, jsonOptions);

        Json.Schema.JsonSchema smokeSchema = Json.Schema.JsonSchema.FromText(JsonSchemaExporter.Export(generatedSmokeModel).Document.RootElement.GetRawText());
        using var smokeDocument = JsonDocument.Parse(smokeJson);
        EvaluationResults smokeValidation = smokeSchema.Evaluate(smokeDocument.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Flag });
        _ = smokeValidation;

        _ = SystemTextJsonAnnotationNames.PropertyName;
        _ = await Assert.That(smokeJson).Contains("Id");
        _ = await Assert.That(smokeCustomer?.Id).IsEqualTo("C-002");
        _ = await Assert.That(nameof(SmokeCustomer)).IsEqualTo("SmokeCustomer");
        SmokeSpecialEntity entity = new() { Id = Guid.Empty, SpecialId = Guid.Parse("22222222-2222-2222-2222-222222222222") };
        var entityJson = JsonSerializer.Serialize<SmokeEntity>(entity, jsonOptions);
        SmokeEntity? entityRoundTrip = JsonSerializer.Deserialize<SmokeEntity>(entityJson, jsonOptions);
        _ = await Assert.That(entityJson).Contains("\"$type\":\"SmokeSpecialEntity\"");
        _ = await Assert.That(entityJson).Contains("22222222-2222-2222-2222-222222222222");
        _ = await Assert.That(entityRoundTrip).IsTypeOf<SmokeSpecialEntity>();
    }

    private static Model.TypeSchemaModel BuildCanonicalModel()
    {
        Model.ScalarTypeDefinition scalar = new()
        {
            Id = new Model.TypeId("String"),
            Name = "String",
            Kind = Model.TypeKind.Scalar,
            Nullability = Model.Nullability.NonNullable,
            Annotations = new Model.AnnotationBag(),
            ScalarKind = Model.ScalarKind.String,
        };

        Model.ObjectTypeDefinition root = new()
        {
            Id = new Model.TypeId("SmokeRoot"),
            Name = "SmokeRoot",
            Kind = Model.TypeKind.Object,
            Nullability = Model.Nullability.NonNullable,
            Annotations = new Model.AnnotationBag(),
            Keys = [],
            Properties = [new Model.PropertyDefinition
            {
                Id = new Model.PropertyId("Value"),
                Name = "Value",
                Type = new Model.TypeRef(scalar.Id),
                Cardinality = new Model.Cardinality { IsRequired = true },
                Constraints = new Model.ConstraintSet(),
                Annotations = new Model.AnnotationBag(),
            }],
        };

        return new TypeSchemaModelAuthoringBuilder(new Model.SchemaModelId("String"))
            .AddType(root)
            .AddType(scalar)
            .Build().Model!;
    }
}

[JsonSerializable(typeof(SmokeCustomer))]
internal sealed partial class PackageSmokeJsonContext : JsonSerializerContext
{
}
