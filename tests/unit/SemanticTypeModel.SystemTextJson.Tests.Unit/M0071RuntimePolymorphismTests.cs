using System.Text.Json;
using ModelA = SemanticTypeModel.TestModels.ModelA;
using ModelAGenerated = SemanticTypeModel.TestModels.ModelA.Generated;
using ModelB = SemanticTypeModel.TestModels.ModelB;
using ModelBGenerated = SemanticTypeModel.TestModels.ModelB.Generated;

namespace SemanticTypeModel.SystemTextJson.Tests.Unit;

public sealed class M0071RuntimePolymorphismTests
{
    [Test]
    public async Task Plain_options_round_trip_entity_and_guid_strong_scalar()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _ = options.AddSemanticTypeModelJson(ModelAGenerated.ModelASemanticTypeModel.Create());

        var value = new ModelA.SpecialEntity { Id = Guid.NewGuid(), SpecialId = new ModelA.SpecialId(Guid.Parse("11111111-1111-1111-1111-111111111111")) };
        var json = JsonSerializer.Serialize<ModelA.BaseEntity>(value, options);
        ModelA.BaseEntity? roundTripped = JsonSerializer.Deserialize<ModelA.BaseEntity>(json, options);

        _ = await Assert.That(json).Contains("\"$type\":\"SpecialEntity\"");
        _ = await Assert.That(json).Contains("11111111-1111-1111-1111-111111111111");
        _ = await Assert.That(json).DoesNotContain("\"Value\"");
        _ = await Assert.That(roundTripped).IsTypeOf<ModelA.SpecialEntity>();
        _ = await Assert.That(((ModelA.SpecialEntity)roundTripped!).SpecialId.Value).IsEqualTo(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Test]
    public async Task Multiple_independent_models_compose_in_either_registration_order()
    {
        JsonSerializerOptions first = new();
        _ = first.AddSemanticTypeModelJson(ModelAGenerated.ModelASemanticTypeModel.Create());
        _ = first.AddSemanticTypeModelJson(ModelBGenerated.ModelBSemanticTypeModel.Create());
        JsonSerializerOptions second = CreateOptions(ModelBGenerated.ModelBSemanticTypeModel.Create(), ModelAGenerated.ModelASemanticTypeModel.Create());

        var a = JsonSerializer.Serialize<ModelA.BaseEntity>(new ModelA.SpecialEntity { Id = Guid.Empty, SpecialId = new(Guid.Empty) }, first);
        var b = JsonSerializer.Serialize<ModelB.BaseEntity>(new ModelB.SpecialEntity { Id = "B", OtherId = new(Guid.Empty) }, second);

        _ = await Assert.That(a).Contains("SpecialEntity");
        _ = await Assert.That(b).Contains("SpecialEntity");
        _ = await Assert.That(a).DoesNotContain("OtherId");
        _ = await Assert.That(b).DoesNotContain("SpecialId");
    }

    private static JsonSerializerOptions CreateOptions(
        Abstractions.Model.TypeSchemaModel firstModel,
        Abstractions.Model.TypeSchemaModel secondModel)
    {
        var options = new JsonSerializerOptions();
        _ = options.AddSemanticTypeModelJson(firstModel);
        _ = options.AddSemanticTypeModelJson(secondModel);
        return options;
    }
}
