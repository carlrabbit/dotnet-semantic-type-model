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
    public async Task Random_predefined_formats_remain_valid_under_the_simple_profile()
    {
        string[] formats = ["email", "uri", "uri-reference", "hostname", "ipv4", "ipv6", "date", "time", "date-time", "duration", "uuid"];
        foreach (var format in formats)
        {
            TypeSchemaModel model = ScalarModel("formatted", format);
            var root = (ObjectTestValue)SemanticTestDataGenerator.Generate(model, new TypeId("formatted"), TestDataSizeProfile.Simple, 17).Value!;
            var value = (string)((ScalarTestValue)root.Properties[new PropertyId("Value")]).Value!;
            SemanticTerminologyProfile profile = SemanticTerminologyProfileJson.Create(model) with
            {
                Properties = [SemanticTerminologyProfileJson.Create(model).Properties[0] with { Values = [JsonSerializer.SerializeToElement(value)] }],
            };
            TerminologyProfileResult<SemanticTerminologyProfile> validation = SemanticTerminologyProfileJson.Import(model, JsonSerializer.Serialize(profile));
            _ = await Assert.That(validation.Succeeded).IsTrue();
        }
    }

    [Test]
    public async Task Invalid_predefined_format_candidates_are_rejected()
    {
        var invalid = new Dictionary<string, string>
        {
            ["email"] = "@",
            ["uri"] = "relative/path",
            ["uri-reference"] = "http://[invalid",
            ["hostname"] = "-invalid.example",
            ["ipv4"] = "999.1.1.1",
            ["ipv6"] = "not-an-ipv6-address",
            ["date"] = "2024-1-2",
            ["time"] = "12:00",
            ["date-time"] = "2024-01-02 12:00:00",
            ["duration"] = "1 minute",
            ["uuid"] = "not-a-uuid",
        };
        foreach ((var format, var value) in invalid)
        {
            TypeSchemaModel model = ScalarModel("invalid-" + format, format);
            SemanticTerminologyProfile template = SemanticTerminologyProfileJson.Create(model);
            SemanticTerminologyProfile profile = template with { Properties = [template.Properties[0] with { Values = [JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(value))] }] };
            TerminologyProfileResult<SemanticTerminologyProfile> result = SemanticTerminologyProfileJson.Import(model, JsonSerializer.Serialize(profile));
            _ = await Assert.That(result.Succeeded).IsFalse();
            _ = await Assert.That(result.Diagnostics.Any(d => d.Code == "TESTDATA_PROFILE_CANDIDATE_INVALID")).IsTrue();
        }
    }

    [Test]
    public async Task Low_level_terminology_overload_revalidates_raw_profiles()
    {
        TypeSchemaModel model = ScalarModel("low-level", "email");
        SemanticTerminologyProfile invalid = SemanticTerminologyProfileJson.Create(model) with { FormatVersion = 99 };
        TestDataGenerationException exception = Assert.Throws<TestDataGenerationException>(() => SemanticTestDataGenerator.Generate(model, new TypeId("low-level"), TestDataSizeProfile.Simple, 0, invalid));
        _ = await Assert.That(exception.Diagnostics.Any(d => d.Code == "TESTDATA_PROFILE_VERSION_UNSUPPORTED")).IsTrue();
    }

    [Test]
    public async Task Semantic_minimums_and_supplied_candidates_respect_safety_budgets()
    {
        TypeSchemaModel minimumModel = BudgetModel(100, null);
        TestDataGenerationException minimum = Assert.Throws<TestDataGenerationException>(() => minimumModel.TestData().WithBudgets(new TestDataBudgets { MaxStringLength = 50 }).Generate<BudgetRoot>());
        _ = await Assert.That(minimum.Diagnostics.Any(d => d.Code == "TESTDATA_SIZE_BUDGET_EXHAUSTED")).IsTrue();

        TypeSchemaModel candidateModel = BudgetModel(null, null);
        SemanticTerminologyProfile template = SemanticTerminologyProfileJson.Create(candidateModel);
        SemanticTerminologyProfile terminology = template with { Properties = [template.Properties[0] with { Values = [JsonSerializer.SerializeToElement(new string('x', 100))] }] };
        BudgetRoot generated = candidateModel.TestData().WithBudgets(new TestDataBudgets { MaxStringLength = 10 }).WithTerminology(terminology).Generate<BudgetRoot>();
        _ = await Assert.That(generated.Value.Length).IsLessThanOrEqualTo(10);

        TestDataGenerationException custom = Assert.Throws<TestDataGenerationException>(() => candidateModel.TestData().WithBudgets(new TestDataBudgets { MaxStringLength = 10 }).WithPropertyGenerator<BudgetRoot>(root => root.Value, _ => new string('x', 100)).Generate<BudgetRoot>());
        _ = await Assert.That(custom.Diagnostics.Any(d => d.Code == "TESTDATA_CUSTOM_CANDIDATE_INVALID")).IsTrue();

        TypeSchemaModel binaryModel = BinaryBudgetModel();
        var binaryCandidate = new byte[100];
        SemanticTerminologyProfile binaryTemplate = SemanticTerminologyProfileJson.Create(binaryModel);
        SemanticTerminologyProfile binaryTerminology = binaryTemplate with { Properties = [binaryTemplate.Properties[0] with { Values = [JsonSerializer.SerializeToElement(Convert.ToBase64String(binaryCandidate))] }] };
        BinaryBudgetRoot binaryGenerated = binaryModel.TestData().WithBudgets(new TestDataBudgets { MaxBinaryLength = 10 }).WithTerminology(binaryTerminology).Generate<BinaryBudgetRoot>();
        _ = await Assert.That(binaryGenerated.Value.Length).IsLessThanOrEqualTo(10);

        TypeSchemaModel logicalModel = LogicalBudgetModel();
        TestDataGenerationException logicalCustom = Assert.Throws<TestDataGenerationException>(() => logicalModel.TestData().WithBudgets(new TestDataBudgets { MaxStringLength = 10 }).WithLogicalTypeGenerator("BudgetCode", _ => new string('x', 100)).Generate<LogicalBudgetRoot>());
        _ = await Assert.That(logicalCustom.Diagnostics.Any(d => d.Code == "TESTDATA_CUSTOM_CANDIDATE_INVALID")).IsTrue();
    }

    [Test]
    public async Task Terminology_falls_through_from_ineligible_property_to_logical_type_in_order()
    {
        TypeSchemaModel model = LogicalBudgetModel();
        SemanticTerminologyProfile template = SemanticTerminologyProfileJson.Create(model);
        TerminologyPropertyEntry property = template.Properties[0];
        TerminologyLogicalTypeEntry logical = template.LogicalTypes[0];

        SemanticTerminologyProfile bothSources = template with
        {
            Properties = [property with { Values = [JsonSerializer.SerializeToElement("property")] }],
            LogicalTypes = [logical with { Values = [JsonSerializer.SerializeToElement("logical")] }],
        };
        LogicalBudgetRoot propertyWins = model.TestData().WithBudgets(new TestDataBudgets { MaxStringLength = 20 }).WithTerminology(bothSources).Generate<LogicalBudgetRoot>();
        _ = await Assert.That(propertyWins.Value).IsEqualTo("property");

        SemanticTerminologyProfile propertyTooLong = bothSources with
        {
            Properties = [property with { Values = [JsonSerializer.SerializeToElement(new string('x', 30))] }],
        };
        LogicalBudgetRoot logicalWins = model.TestData().WithBudgets(new TestDataBudgets { MaxStringLength = 10 }).WithTerminology(propertyTooLong).Generate<LogicalBudgetRoot>();
        _ = await Assert.That(logicalWins.Value).IsEqualTo("logical");

        SemanticTerminologyProfile bothTooLong = bothSources with
        {
            Properties = [property with { Values = [JsonSerializer.SerializeToElement(new string('x', 30))] }],
            LogicalTypes = [logical with { Values = [JsonSerializer.SerializeToElement(new string('y', 30))] }],
        };
        LogicalBudgetRoot randomFallback = model.TestData().WithBudgets(new TestDataBudgets { MaxStringLength = 10 }).WithTerminology(bothTooLong).Generate<LogicalBudgetRoot>();
        _ = await Assert.That(randomFallback.Value).IsEqualTo("t");
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

    private static TypeSchemaModel ScalarModel(string id, string? format, ConstraintSet? constraints = null)
    {
        ScalarTypeDefinition scalar = new() { Id = new("Scalar"), Name = "Scalar", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String, Format = format };
        PropertyDefinition property = new() { Id = new("Value"), Name = "Value", Type = new(scalar.Id), Cardinality = new Cardinality { IsRequired = true }, Constraints = constraints ?? new(), Annotations = new() };
        ObjectTypeDefinition owner = new() { Id = new(id), Name = id, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = [property], Keys = [] };
        return new TypeSchemaModel { Id = new(id), Types = [owner, scalar], TypesById = new Dictionary<TypeId, TypeDefinition> { [owner.Id] = owner, [scalar.Id] = scalar }, Annotations = new() };
    }

    private static TypeSchemaModel BudgetModel(int? minimum, int? maximum)
    {
        return ScalarModel("global::SemanticTypeModel.TestData.Tests.Unit.BudgetRoot", null, new ConstraintSet { String = new StringConstraints { MinLength = minimum, MaxLength = maximum } });
    }

    private static TypeSchemaModel BinaryBudgetModel()
    {
        ScalarTypeDefinition scalar = new() { Id = new("Binary"), Name = "Binary", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.Binary };
        PropertyDefinition property = new() { Id = new("Value"), Name = "Value", Type = new(scalar.Id), Cardinality = new Cardinality { IsRequired = true }, Constraints = new(), Annotations = new() };
        ObjectTypeDefinition owner = new() { Id = new("global::SemanticTypeModel.TestData.Tests.Unit.BinaryBudgetRoot"), Name = "BinaryBudgetRoot", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = [property], Keys = [] };
        return new TypeSchemaModel { Id = new("binary-budget"), Types = [owner, scalar], TypesById = new Dictionary<TypeId, TypeDefinition> { [owner.Id] = owner, [scalar.Id] = scalar }, Annotations = new() };
    }

    private static TypeSchemaModel LogicalBudgetModel()
    {
        ScalarTypeDefinition scalar = new() { Id = new("LogicalScalar"), Name = "LogicalScalar", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        PropertyDefinition property = new() { Id = new("Value"), Name = "Value", Type = new(scalar.Id), Cardinality = new Cardinality { IsRequired = true }, Constraints = new(), Annotations = new() { Items = [new Annotation { Key = new("schema.logicalType"), Value = "BudgetCode", Scope = AnnotationScope.Member, Source = AnnotationSource.Declared }] } };
        ObjectTypeDefinition owner = new() { Id = new("global::SemanticTypeModel.TestData.Tests.Unit.LogicalBudgetRoot"), Name = "LogicalBudgetRoot", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = [property], Keys = [] };
        return new TypeSchemaModel { Id = new("logical-budget"), Types = [owner, scalar], TypesById = new Dictionary<TypeId, TypeDefinition> { [owner.Id] = owner, [scalar.Id] = scalar }, Annotations = new() };
    }
}

public sealed class MaterializationFailureRoot
{
}

public sealed class BudgetRoot
{
    public string Value { get; set; } = string.Empty;
}

public sealed class BinaryBudgetRoot
{
    public byte[] Value { get; set; } = [];
}

public sealed class LogicalBudgetRoot
{
    public string Value { get; set; } = string.Empty;
}
