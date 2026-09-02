using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.TestModels.ModelA;

namespace SemanticTypeModel.TestData.Tests.Unit;

#pragma warning disable CS1591, CA1707
public sealed class M0079TestDataDxTests
{
    [Test]
    public async Task Facade_generates_typed_values_and_many_is_seed_deterministic()
    {
        TypeSchemaModel model = TestModels.ModelA.Generated.ModelASemanticTypeModel.Create();
        IReadOnlyList<TestDataScenario> first = model.TestData().WithSeed(17).GenerateMany<TestDataScenario>(2);
        IReadOnlyList<TestDataScenario> second = model.TestData().WithSeed(17).GenerateMany<TestDataScenario>(2);

        _ = await Assert.That(first.Count).IsEqualTo(2);
        _ = await Assert.That(first.Select(value => value.Id)).IsEquivalentTo(second.Select(value => value.Id));
    }

    [Test]
    public async Task Facade_materializes_an_existing_semantic_value_without_regeneration()
    {
        TypeSchemaModel model = TestModels.ModelA.Generated.ModelASemanticTypeModel.Create();
        TestDataGenerationResult generated = SemanticTestDataGenerator.Generate(model, new TypeId("global::SemanticTypeModel.TestModels.ModelA.TestDataScenario"), seed: 23);
        TestDataScenario materialized = model.TestData().Materialize<TestDataScenario>(generated.Value!);

        _ = await Assert.That(materialized).IsNotNull();
        _ = await Assert.That(materialized.Items).IsNotNull();
    }

    [Test]
    public async Task Logical_and_property_generators_are_available_on_the_typed_surface()
    {
        TypeSchemaModel model = TestModels.ModelA.Generated.ModelASemanticTypeModel.Create();
        var expected = Guid.Parse("00000000-0000-4000-8000-000000000042");
        TestDataScenario value = model.TestData()
            .WithLogicalTypeGenerator("ScenarioId", _ => expected)
            .Generate<TestDataScenario>();

        _ = await Assert.That(value.Id).IsEqualTo(expected);
    }
}
