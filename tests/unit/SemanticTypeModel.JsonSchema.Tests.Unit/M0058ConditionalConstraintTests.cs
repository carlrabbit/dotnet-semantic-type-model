using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Domain;
using SemanticTypeModel.JsonSchema.Export;
using DraftSchema = Json.Schema.JsonSchema;
using Intake = SemanticTypeModel.TestModels.ModelA.Intake;
using ModelAGenerated = SemanticTypeModel.TestModels.ModelA.Generated;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names use underscores for readability.")]
public sealed class M0058ConditionalConstraintTests
{
    [Test]
    public async Task Root_is_excluded_from_definitions_and_references_resolve_for_export_and_consumer_rerooting()
    {
        var node = new ObjectTypeDefinition
        {
            Id = new TypeId("Node"),
            Name = "Node",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            Keys = [],
            Properties =
            [
                new PropertyDefinition
                {
                    Id = new PropertyId("Node.Next"), Name = "Next", Type = new TypeRef(new TypeId("Node")),
                    Cardinality = new Cardinality { IsRequired = false, AllowsNull = false },
                    Constraints = new ConstraintSet(), Annotations = new AnnotationBag(),
                },
            ],
        };
        TypeSchemaModel model = new()
        {
            Id = new SchemaModelId("Node"),
            Types = [node],
            TypesById = new Dictionary<TypeId, TypeDefinition> { [node.Id] = node },
            Annotations = new AnnotationBag(),
        };

        JsonElement exported = JsonSchemaExporter.Export(model).Document.RootElement;
        _ = await Assert.That(exported.TryGetProperty("$defs", out JsonElement definitions) && definitions.EnumerateObject().Any()).IsFalse();
        _ = await Assert.That(exported.GetProperty("properties").GetProperty("Next").GetProperty("$ref").GetString()).IsEqualTo("#");
        var originalSchema = DraftSchema.FromText(exported.GetRawText());
        await AssertValid(originalSchema, /*lang=json,strict*/ "{\"Next\":{\"Next\":{}}}");

        JsonObject preservedRoot = JsonNode.Parse(exported.GetRawText())!.AsObject();
        _ = preservedRoot.Remove("$id");
        preservedRoot["properties"]!["Next"]!["$ref"] = "#/$defs/Node";
        JsonObject consumerRoot = new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject { ["Payload"] = new JsonObject { ["$ref"] = "#/$defs/Node" } },
            ["required"] = new JsonArray("Payload"),
        };
        JsonObject rerooted = new()
        {
            ["$ref"] = "#/$defs/Wrapper",
            ["$defs"] = new JsonObject { ["Node"] = preservedRoot, ["Wrapper"] = consumerRoot },
        };
        await AssertValid(DraftSchema.FromText(rerooted.ToJsonString()), /*lang=json,strict*/ "{\"Payload\":{\"next\":{}}}");
    }

    [Test]
    public async Task RequiredWhen_uses_typed_equality_and_requires_source_presence_without_implying_exclusivity()
    {
        TypeSchemaModel model = Generated.M0066SemanticTypeModel.Create();
        JsonObject document = JsonNode.Parse(JsonSchemaExporter.Export(model).Document.RootElement.GetRawText())!.AsObject();
        _ = document.Remove("$id");
        ObjectTypeDefinition contract = model.Types.OfType<ObjectTypeDefinition>().Single(type => type.Name == nameof(M0066Customer));
        TypeDefinition selectedRoot = model.TryGetType(new TypeId(model.Id.Value))
            ?? model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal).First();
        JsonObject validationDocument = selectedRoot.Id == contract.Id
            ? document
            : new JsonObject { ["$ref"] = $"#/$defs/{contract.Id.Value}", ["$defs"] = document["$defs"]!.DeepClone() };
        var schema = DraftSchema.FromText(validationDocument.ToJsonString());

        await AssertValid(schema, /*lang=json,strict*/ "{\"Mode\":\"Other\"}");
        await AssertValid(schema, /*lang=json,strict*/ "{\"Mode\":\"Other\",\"CsvPath\":\"other\"}");
        await AssertValid(schema, /*lang=json,strict*/ "{\"Mode\":\"Csv\",\"CsvPath\":null}");
        await AssertValid(schema, /*lang=json,strict*/ "{\"Enabled\":false}");
        await AssertValid(schema, /*lang=json,strict*/ "{\"Count\":41}");
        await AssertInvalid(schema, /*lang=json,strict*/ "{\"Mode\":\"Csv\"}");
        await AssertInvalid(schema, /*lang=json,strict*/ "{\"Enabled\":true}");
        await AssertInvalid(schema, /*lang=json,strict*/ "{\"Count\":42}");
    }

    [Test]
    public async Task M0058_import_conditions_emit_deterministic_enum_constants()
    {
        TypeSchemaModel model = ModelAGenerated.ModelASemanticTypeModel.Create();
        ObjectTypeDefinition import = model.Types.OfType<ObjectTypeDefinition>().Single(type => type.Name == nameof(Intake.ImportSpecification));

        ConditionalConstraint[] constraints = [.. import.Properties.SelectMany(property => property.Constraints.Conditional)];
        _ = await Assert.That(constraints.All(constraint => constraint.Literal.Kind == SemanticLiteralKind.EnumMember)).IsTrue();
        _ = await Assert.That(constraints.Select(constraint => constraint.Literal.EnumMemberName!)).IsEquivalentTo(["CsvFile", "XmlFile", "WebService1", "WebService2"]);
        _ = await Assert.That(model.GetType(constraints[0].SourceTypeId)).IsTypeOf<EnumTypeDefinition>();

        JsonSchemaExportResult export = JsonSchemaExporter.Export(model.DeriveJsonSchemaModel().Model);
        JsonElement allOf = export.Document.RootElement.GetProperty("$defs").GetProperty(import.Id.Value).GetProperty("allOf");
        _ = await Assert.That(allOf.GetArrayLength()).IsEqualTo(4);
        _ = await Assert.That(allOf.EnumerateArray().Select(item => item.GetProperty("if").GetProperty("properties").GetProperty("ImportType").GetProperty("const").GetString()!)).IsEquivalentTo(["CsvFile", "XmlFile", "WebService1", "WebService2"]);
    }

    [Test]
    public async Task ConditionalConstraint_unsupported_operator_emits_diagnostic_instead_of_being_dropped()
    {
        TypeSchemaModel source = ModelAGenerated.ModelASemanticTypeModel.Create();
        ObjectTypeDefinition original = source.Types.OfType<ObjectTypeDefinition>().Single(type => type.Name == nameof(Intake.ImportSpecification));
        PropertyDefinition csv = original.Properties.Single(property => property.Name == nameof(Intake.ImportSpecification.CsvSource));
        PropertyDefinition changedCsv = csv with { Constraints = csv.Constraints with { Conditional = [csv.Constraints.Conditional.Single() with { Operator = ConditionalConstraintOperator.NotEquals }] } };
        ObjectTypeDefinition changed = original with { Properties = [.. original.Properties.Select(property => property.Id == csv.Id ? changedCsv : property)] };
        TypeDefinition[] types = [.. source.Types.Select(type => type.Id == original.Id ? changed : type)];
        TypeSchemaModel model = new() { Id = source.Id, Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = source.Annotations };

        SemanticDerivationResult<JsonSchemaSemanticModel> result = model.DeriveJsonSchemaModel();

        _ = await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == "JSONSCHEMA_CONDITIONAL_OPERATOR_UNSUPPORTED")).IsTrue();
    }

    private static async Task AssertValid(DraftSchema schema, string json)
    {
        using var instance = JsonDocument.Parse(json);
        EvaluationResults result = schema.Evaluate(instance.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Flag });
        _ = await Assert.That(result.IsValid).IsTrue();
    }

    private static async Task AssertInvalid(DraftSchema schema, string json)
    {
        using var instance = JsonDocument.Parse(json);
        EvaluationResults result = schema.Evaluate(instance.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Flag });
        _ = await Assert.That(result.IsValid).IsFalse();
    }
}

#pragma warning restore CS1591
