using System.Text.Json;
using System.Text.Json.Nodes;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.TestData.Tests.Unit;

#pragma warning disable CS1591, CA1707
public sealed class TestDataCorrectiveTests
{
    [Test]
    public async Task Uri_and_uri_reference_candidates_use_distinct_validation_contracts()
    {
        TypeSchemaModel model = ScalarModel("uri", "uri");
        SemanticTerminologyProfile profile = SemanticTerminologyProfileJson.Create(model) with
        {
            Properties = [SemanticTerminologyProfileJson.Create(model).Properties[0] with { Values = [JsonDocument.Parse("\"relative/path\"").RootElement.Clone()] }],
        };
        TerminologyProfileResult<SemanticTerminologyProfile> invalidUri = SemanticTerminologyProfileJson.Import(model, JsonSerializer.Serialize(profile));
        _ = await Assert.That(invalidUri.Succeeded).IsFalse();

        TypeSchemaModel referenceModel = ScalarModel("uri-reference", "uri-reference");
        SemanticTerminologyProfile referenceProfile = SemanticTerminologyProfileJson.Create(referenceModel) with
        {
            Properties = [SemanticTerminologyProfileJson.Create(referenceModel).Properties[0] with { Values = [JsonDocument.Parse("\"relative/path\"").RootElement.Clone()] }],
        };
        TerminologyProfileResult<SemanticTerminologyProfile> validReference = SemanticTerminologyProfileJson.Import(referenceModel, JsonSerializer.Serialize(referenceProfile));
        _ = await Assert.That(validReference.Succeeded).IsTrue();
    }

    [Test]
    public async Task Materialization_supports_date_time_guid_uri_character_binary_and_json_forms()
    {
        TypeSchemaModel model = ScalarModel("scalar", null);
        SemanticTestDataFacade facade = model.TestData();
        _ = await Assert.That(facade.Materialize<DateOnly>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Date, new DateOnly(2024, 1, 2)))).IsEqualTo(new DateOnly(2024, 1, 2));
        _ = await Assert.That(facade.Materialize<TimeOnly>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Time, new TimeOnly(3, 4)))).IsEqualTo(new TimeOnly(3, 4));
        _ = await Assert.That(facade.Materialize<DateTime>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.DateTime, new DateTime(2024, 1, 2)))).IsEqualTo(new DateTime(2024, 1, 2));
        _ = await Assert.That(facade.Materialize<DateTimeOffset>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.DateTimeOffset, new DateTimeOffset(2024, 1, 2, 3, 4, 0, TimeSpan.Zero)))).IsEqualTo(new DateTimeOffset(2024, 1, 2, 3, 4, 0, TimeSpan.Zero));
        _ = await Assert.That(facade.Materialize<TimeSpan>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Duration, TimeSpan.FromMinutes(3)))).IsEqualTo(TimeSpan.FromMinutes(3));
        var guid = Guid.NewGuid();
        _ = await Assert.That(facade.Materialize<Guid>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Guid, guid))).IsEqualTo(guid);
        _ = await Assert.That(facade.Materialize<Guid?>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Guid, guid))).IsEqualTo(guid);
        _ = await Assert.That(facade.Materialize<int?>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Integer, 12m))).IsEqualTo(12);
        _ = await Assert.That(facade.Materialize<Uri>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.String, "https://example.com"))).IsEqualTo(new Uri("https://example.com"));
        _ = await Assert.That(facade.Materialize<char>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.String, "x"))).IsEqualTo('x');
        byte[] bytes = [1, 2, 3];
        _ = await Assert.That(facade.Materialize<byte[]>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Binary, bytes))).IsEquivalentTo(bytes);
        _ = await Assert.That(facade.Materialize<ReadOnlyMemory<byte>>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Binary, bytes)).ToArray()).IsEquivalentTo(bytes);
        JsonElement element = JsonDocument.Parse("{\"ok\":true}").RootElement.Clone();
        _ = await Assert.That(facade.Materialize<JsonElement>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Json, element)).GetProperty("ok").GetBoolean()).IsTrue();
        _ = await Assert.That(facade.Materialize<JsonDocument>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Json, element)).RootElement.GetProperty("ok").GetBoolean()).IsTrue();
        _ = await Assert.That(facade.Materialize<JsonNode>(new ScalarTestValue(new TypeId("Scalar"), ScalarKind.Json, element))!["ok"]!.GetValue<bool>()).IsTrue();
    }

    [Test]
    public async Task Raw_profiles_are_revalidated_before_facade_consumption()
    {
        TypeSchemaModel model = TestModels.ModelA.Generated.ModelASemanticTypeModel.Create();
        SemanticTerminologyProfile invalid = SemanticTerminologyProfileJson.Create(model) with { ModelId = "other-model" };
        TestDataGenerationException exception = Assert.Throws<TestDataGenerationException>(() => model.TestData().WithTerminology(invalid));
        _ = await Assert.That(exception.Diagnostics.Any(d => d.Code == "TESTDATA_PROFILE_MODEL_MISMATCH")).IsTrue();
    }

    [Test]
    public async Task Invalid_property_and_logical_generators_fail_closed()
    {
        TypeSchemaModel model = TestModels.ModelA.Generated.ModelASemanticTypeModel.Create();
        TestDataGenerationException propertyException = Assert.Throws<TestDataGenerationException>(() => model.TestData().WithPropertyGenerator<TestModels.ModelA.TestDataScenario>(scenario => scenario.Id, _ => "not-a-guid").Generate<TestModels.ModelA.TestDataScenario>());
        TestDataGenerationException logicalException = Assert.Throws<TestDataGenerationException>(() => model.TestData().WithLogicalTypeGenerator("ScenarioId", _ => "not-a-guid").Generate<TestModels.ModelA.TestDataScenario>());
        _ = await Assert.That(propertyException.Diagnostics.Any(d => d.Code == "TESTDATA_CUSTOM_CANDIDATE_INVALID")).IsTrue();
        _ = await Assert.That(logicalException.Diagnostics.Any(d => d.Code == "TESTDATA_CUSTOM_CANDIDATE_INVALID")).IsTrue();
    }

    [Test]
    public async Task GenerateMany_wraps_materialization_failures_in_the_TestData_exception_contract()
    {
        TypeSchemaModel model = ScalarModel("global::SemanticTypeModel.TestData.Tests.Unit.MaterializationFailureRoot", null);
        TestDataGenerationException exception = Assert.Throws<TestDataGenerationException>(() => model.TestData().GenerateMany<MaterializationFailureRoot>(1));

        _ = await Assert.That(exception.Diagnostics.Any(d => d.Code == "TESTDATA_MATERIALIZATION_FAILED")).IsTrue();
    }

    private static TypeSchemaModel ScalarModel(string id, string? format)
    {
        ScalarTypeDefinition scalar = new() { Id = new("Scalar"), Name = "Scalar", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String, Format = format };
        PropertyDefinition property = new() { Id = new("Value"), Name = "Value", Type = new(scalar.Id), Cardinality = new Cardinality { IsRequired = true }, Constraints = new(), Annotations = new() };
        ObjectTypeDefinition owner = new() { Id = new(id), Name = id, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = [property], Keys = [] };
        return new TypeSchemaModel { Id = new(id), Types = [owner, scalar], TypesById = new Dictionary<TypeId, TypeDefinition> { [owner.Id] = owner, [scalar.Id] = scalar }, Annotations = new() };
    }
}

public sealed class MaterializationFailureRoot
{
}
