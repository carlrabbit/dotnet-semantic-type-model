using System.Text.Json;
using System.Text.Json.Nodes;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.TestData.Tests.Unit;

#pragma warning disable CS1591, CA1707
public sealed class M0078TerminologyProfileTests
{
    [Test]
    public async Task Export_import_and_profile_guided_generation_preserve_model_and_use_candidates()
    {
        TypeSchemaModel model = Model();
        var exported = SemanticTerminologyProfileJson.Export(model);
        JsonObject json = JsonNode.Parse(exported)!.AsObject();
        json["logicalTypes"]![0]!["values"] = new JsonArray("CUST-001");

        TerminologyProfileResult<SemanticTerminologyProfile> imported = SemanticTerminologyProfileJson.Import(model, json.ToJsonString());
        _ = await Assert.That(imported.Succeeded).IsTrue();
        TestDataGenerationResult result = SemanticTestDataGenerator.Generate(model, new TypeId("Customer"), TestDataSizeProfile.Simple, 42, imported.Profile);
        _ = await Assert.That(result.Succeeded).IsTrue();
        var value = (ScalarTestValue)((ObjectTestValue)result.Value!).Properties[new PropertyId("Id")];
        _ = await Assert.That(value.Value).IsEqualTo("CUST-001");
    }

    [Test]
    public async Task Import_rejects_wrong_model_and_invalid_candidate_without_mutating_model()
    {
        TypeSchemaModel model = Model();
        var exported = SemanticTerminologyProfileJson.Export(model).Replace("\"modelId\": \"M0078\"", "\"modelId\": \"Other\"", StringComparison.Ordinal);
        JsonObject json = JsonNode.Parse(exported)!.AsObject();
        json["logicalTypes"]![0]!["values"] = new JsonArray(12);
        TerminologyProfileResult<SemanticTerminologyProfile> imported = SemanticTerminologyProfileJson.Import(model, json.ToJsonString());
        _ = await Assert.That(imported.Succeeded).IsFalse();
        _ = await Assert.That(imported.Diagnostics.Any(d => d.Code == "TESTDATA_PROFILE_MODEL_MISMATCH")).IsTrue();
        _ = await Assert.That(imported.Diagnostics.Any(d => d.Code == "TESTDATA_PROFILE_CANDIDATE_INVALID")).IsTrue();
    }

    [Test]
    public async Task Candidate_order_is_normalized_and_pattern_candidate_is_validated()
    {
        TypeSchemaModel model = Model(pattern: "^CUST-[0-9]+$");
        SemanticTerminologyProfile profile = SemanticTerminologyProfileJson.Create(model) with
        {
            LogicalTypes = [SemanticTerminologyProfileJson.Create(model).LogicalTypes[0] with { Values = [JsonDocument.Parse("\"CUST-002\"").RootElement.Clone(), JsonDocument.Parse("\"CUST-001\"").RootElement.Clone(), JsonDocument.Parse("\"CUST-001\"").RootElement.Clone()] }],
        };
        TerminologyProfileResult<SemanticTerminologyProfile> imported = SemanticTerminologyProfileJson.Import(model, JsonSerializer.Serialize(profile));
        _ = await Assert.That(imported.Succeeded).IsTrue();
        _ = await Assert.That(imported.Profile!.LogicalTypes[0].Values.Select(v => v.GetString()!)).IsEquivalentTo(["CUST-001", "CUST-002"]);
    }

    [Test]
    public async Task Code_first_generated_model_can_export_enrich_import_and_generate_profile_guided_data()
    {
        TypeSchemaModel model = TestModels.ModelA.Generated.ModelASemanticTypeModel.Create();
        SemanticTerminologyProfile exported = SemanticTerminologyProfileJson.Create(model);
        SemanticTerminologyProfile enriched = exported with
        {
            LogicalTypes = [exported.LogicalTypes.Single(entry => entry.Name == "ScenarioId") with { Values = [JsonDocument.Parse("\"00000000-0000-4000-8000-000000000042\"").RootElement.Clone()] }],
        };
        TerminologyProfileResult<SemanticTerminologyProfile> imported = SemanticTerminologyProfileJson.Import(model, JsonSerializer.Serialize(enriched));
        _ = await Assert.That(imported.Succeeded).IsTrue();
        TestDataGenerationResult result = SemanticTestDataGenerator.Generate(model, new TypeId("global::SemanticTypeModel.TestModels.ModelA.TestDataScenario"), TestDataSizeProfile.Simple, 0, imported.Profile);
        _ = await Assert.That(result.Succeeded).IsTrue();
    }

    private static TypeSchemaModel Model(string? pattern = null)
    {
        ScalarTypeDefinition text = new() { Id = new("Text"), Name = "Text", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        PropertyDefinition property = new() { Id = new("Id"), Name = "Id", Type = new(text.Id), Cardinality = new Cardinality { IsRequired = true }, Constraints = new ConstraintSet { String = pattern is null ? null : new StringConstraints { Pattern = pattern } }, Annotations = new AnnotationBag { Items = [new Annotation { Key = new(Core.Semantics.CoreSemanticAnnotationKeys.LogicalType), Value = "CustomerId", Scope = AnnotationScope.Member, Source = AnnotationSource.Declared }] } };
        ObjectTypeDefinition owner = new() { Id = new("Customer"), Name = "Customer", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = [property], Keys = [] };
        return new TypeSchemaModel { Id = new("M0078"), Types = [owner, text], TypesById = new Dictionary<TypeId, TypeDefinition> { [owner.Id] = owner, [text.Id] = text }, Annotations = new() };
    }
}
