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
        ScalarTypeDefinition text = new()
        {
            Id = new TypeId("Text"),
            Name = "Text",
            Kind = TypeKind.Scalar,
            ScalarKind = ScalarKind.String,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
        };
        ObjectTypeDefinition child = ReferenceTestObject("Child", Property("Code", "Text", required: true, minLength: 3));
        var alias = new ReferenceTypeDefinition
        {
            Id = new TypeId("RootAlias"),
            Name = "RootAlias",
            Kind = TypeKind.Reference,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            Target = new TypeRef(new TypeId("Root")),
        };
        var array = new ArrayTypeDefinition
        {
            Id = new TypeId("RootArray"),
            Name = "RootArray",
            Kind = TypeKind.Array,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            ItemType = new TypeRef(new TypeId("Root")),
        };
        var dictionary = new DictionaryTypeDefinition
        {
            Id = new TypeId("RootDictionary"),
            Name = "RootDictionary",
            Kind = TypeKind.Dictionary,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            KeyType = new TypeRef(text.Id),
            ValueType = new TypeRef(new TypeId("Root")),
        };
        var union = new UnionTypeDefinition
        {
            Id = new TypeId("RootChoice"),
            Name = "RootChoice",
            Kind = TypeKind.Union,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            Options = [new TypeRef(new TypeId("Root")), new TypeRef(child.Id)],
            Semantics = UnionSemantics.OneOf,
        };
        var node = new ObjectTypeDefinition
        {
            Id = new TypeId("Root"),
            Name = "Root",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            Keys = [],
            Properties =
            [
                Property("Name", "Text", required: true, minLength: 3),
                Property("Next", "Root"),
                Property("Alias", "RootAlias"),
                Property("Items", "RootArray"),
                Property("Mapped", "RootDictionary"),
                Property("Choice", "RootChoice"),
                Property("Other", "Child"),
            ],
        };
        TypeDefinition[] types = [node, text, child, alias, array, dictionary, union];
        TypeSchemaModel model = new()
        {
            Id = new SchemaModelId("Root"),
            Types = types,
            TypesById = types.ToDictionary(static type => type.Id),
            Annotations = new AnnotationBag(),
        };

        JsonElement exported = JsonSchemaExporter.Export(model).Document.RootElement;
        _ = await Assert.That(exported.GetProperty("$defs").TryGetProperty("Root", out _)).IsFalse();
        _ = await Assert.That(exported.GetProperty("$defs").TryGetProperty("RootAlias", out _)).IsTrue();
        var originalSchema = DraftSchema.FromText(exported.GetRawText());
        await AssertInvalid(originalSchema, /*lang=json,strict*/ "{\"Name\":\"root\",\"Next\":{\"Name\":\"x\"}}");
        await AssertInvalid(originalSchema, /*lang=json,strict*/ "{\"Name\":\"root\",\"Alias\":{\"Name\":\"x\"}}");
        await AssertInvalid(originalSchema, /*lang=json,strict*/ "{\"Name\":\"root\",\"Items\":[{\"Name\":\"x\"}]}");
        await AssertInvalid(originalSchema, /*lang=json,strict*/ "{\"Name\":\"root\",\"Mapped\":{\"entry\":{\"Name\":\"x\"}}}");
        await AssertInvalid(originalSchema, /*lang=json,strict*/ "{\"Name\":\"root\",\"Choice\":{\"Name\":\"x\"}}");
        await AssertInvalid(originalSchema, /*lang=json,strict*/ "{\"Name\":\"root\",\"Other\":{\"Code\":\"x\"}}");
        await AssertValid(originalSchema, /*lang=json,strict*/ "{\"Name\":\"root\",\"Next\":{\"Name\":\"child\"},\"Alias\":{\"Name\":\"alias\"},\"Items\":[{\"Name\":\"item\"}],\"Mapped\":{\"entry\":{\"Name\":\"value\"}},\"Choice\":{\"Code\":\"other\"},\"Other\":{\"Code\":\"other\"}}");

        JsonObject originalDocument = JsonNode.Parse(exported.GetRawText())!.AsObject();
        var preservedRoot = (JsonObject)originalDocument.DeepClone();
        _ = preservedRoot.Remove("$id");
        _ = preservedRoot.Remove("$defs");
        var consumerDefinitions = (JsonObject)originalDocument["$defs"]!.DeepClone();
        consumerDefinitions["Node"] = preservedRoot;
        JsonObject consumerRoot = new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject { ["Payload"] = new JsonObject { ["$ref"] = "#/$defs/Node" } },
            ["required"] = new JsonArray("Payload"),
        };
        JsonObject rerooted = new()
        {
            ["$ref"] = "#/$defs/Wrapper",
            ["$defs"] = consumerDefinitions,
        };
        consumerDefinitions["Wrapper"] = consumerRoot;
        RebaseRootReferences(consumerDefinitions, "#/$defs/Node");
        var rerootedSchema = DraftSchema.FromText(rerooted.ToJsonString());
        await AssertValid(rerootedSchema, /*lang=json,strict*/ "{\"Payload\":{\"Name\":\"root\",\"Next\":{\"Name\":\"child\"}}}");
        await AssertInvalid(rerootedSchema, /*lang=json,strict*/ "{\"Payload\":{\"Name\":\"root\",\"Next\":{\"Name\":\"x\"}}}");
    }

    private static ObjectTypeDefinition ReferenceTestObject(string id, params PropertyDefinition[] properties)
    {
        return new ObjectTypeDefinition
        {
            Id = new TypeId(id),
            Name = id,
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            Keys = [],
            Properties = properties,
        };
    }

    private static PropertyDefinition Property(string name, string type, bool required = false, int? minLength = null)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId($"Root.{name}"),
            Name = name,
            Type = new TypeRef(new TypeId(type)),
            Cardinality = new Cardinality { IsRequired = required, AllowsNull = false },
            Constraints = new ConstraintSet { String = minLength is null ? null : new StringConstraints { MinLength = minLength } },
            Annotations = new AnnotationBag(),
        };
    }

    private static void RebaseRootReferences(JsonNode? node, string replacement)
    {
        if (node is JsonObject obj)
        {
            foreach ((var key, JsonNode? value) in obj.ToArray())
            {
                if (key == "$ref" && string.Equals(value?.GetValue<string>(), "#", StringComparison.Ordinal))
                {
                    obj[key] = replacement;
                }
                else
                {
                    RebaseRootReferences(value, replacement);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (JsonNode? item in array)
            {
                RebaseRootReferences(item, replacement);
            }
        }
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
