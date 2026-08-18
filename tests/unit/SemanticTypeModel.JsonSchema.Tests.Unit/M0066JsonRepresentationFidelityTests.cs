using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using DraftSchema = Json.Schema.JsonSchema;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Semantics;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.SystemTextJson;

[assembly: SemanticTypeModelGeneratorOptions("SemanticTypeModel.JsonSchema.Tests.Unit.Generated", "M0066SemanticTypeModel")]

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707
#pragma warning disable CA1869
#pragma warning disable IDE0028

public sealed class M0066JsonRepresentationFidelityTests
{
    [Test]
    public async Task Generated_annotated_model_output_should_validate_against_derived_schema()
    {
        TypeSchemaModel model = Generated.M0066SemanticTypeModel.Create();
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = M0066JsonContext.Default.WithSemanticTypeModelJson(
                model,
                projectionOptions => projectionOptions.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName),
        };
        options.Converters.Add(new JsonStringEnumConverter());

        string json = JsonSerializer.Serialize(new M0066Customer
        {
            CustomerNumber = "C-001",
            Status = M0066Status.Active,
            Tags = ["vip"],
            ExtensionData = new Dictionary<string, JsonElement>(),
        }, options);
        DraftSchema schema = DraftSchema.FromText(JsonSchemaExporter.Export(model).Document.RootElement.GetRawText());
        using JsonDocument instance = JsonDocument.Parse(json);
        EvaluationResults validation = schema.Evaluate(instance.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Flag });

        _ = await Assert.That(validation.IsValid).IsTrue();
    }

    [Test]
    public async Task Export_should_preserve_structured_semantics_and_typed_extension_data()
    {
        var text = Scalar("Text", ScalarKind.String);
        var extensionValue = Scalar("ExtensionValue", ScalarKind.String);
        var extension = new DictionaryTypeDefinition
        {
            Id = new TypeId("Extensions"),
            Name = "Extensions",
            Kind = TypeKind.Dictionary,
            Nullability = Nullability.NonNullable,
            KeyType = new TypeRef(text.Id),
            ValueType = new TypeRef(extensionValue.Id),
            Annotations = new AnnotationBag(),
        };
        var customer = new ObjectTypeDefinition
        {
            Id = new TypeId("Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotations((CoreSemanticAnnotationKeys.Envelope, true), (CoreSemanticAnnotationKeys.EnvelopePurpose, "transport")),
            Properties =
            [
                Property("Id", text.Id, true, Annotations((CoreSemanticAnnotationKeys.DisplayIdentity, "0"), ($"{CoreSemanticAnnotationKeys.AccessPathPrefix}ById", "0"))),
                Property("Payload", text.Id, true, Annotations((CoreSemanticAnnotationKeys.EnvelopePayload, true), (CoreSemanticAnnotationKeys.OwnershipKind, "object"))),
                Property("ExtensionData", extension.Id, false, Annotations((CoreSemanticAnnotationKeys.ExtensionData, true))),
            ],
            Keys = [],
        };
        TypeDefinition[] types = [customer, extension, text, extensionValue];
        var model = new TypeSchemaModel
        {
            Id = new SchemaModelId(customer.Id.Value),
            Types = types,
            TypesById = types.ToDictionary(static type => type.Id),
            Annotations = new AnnotationBag(),
        };

        JsonElement root = JsonSchemaExporter.Export(model).Document.RootElement;

        _ = await Assert.That(root.GetProperty("x-stm").GetProperty("displayIdentity")[0].GetString()).IsEqualTo("Id");
        _ = await Assert.That(root.GetProperty("x-stm").GetProperty("accessPaths").GetProperty("ById")[0].GetString()).IsEqualTo("Id");
        _ = await Assert.That(root.GetProperty("x-stm").GetProperty("envelope").GetProperty("payload").GetString()).IsEqualTo("Payload");
        _ = await Assert.That(root.GetProperty("properties").GetProperty("Payload").GetProperty("x-stm").GetProperty("ownership").GetString()).IsEqualTo("object");
        _ = await Assert.That(root.TryGetProperty("ExtensionData", out _)).IsFalse();
        _ = await Assert.That(root.GetProperty("additionalProperties").GetProperty("$ref").GetString()).IsEqualTo("#/$defs/ExtensionValue");
    }

    private static ScalarTypeDefinition Scalar(string id, ScalarKind kind) => new()
    {
        Id = new TypeId(id),
        Name = id,
        Kind = TypeKind.Scalar,
        Nullability = Nullability.NonNullable,
        ScalarKind = kind,
        Annotations = new AnnotationBag(),
    };

    private static PropertyDefinition Property(string name, TypeId type, bool required, AnnotationBag annotations) => new()
    {
        Id = new PropertyId($"Customer.{name}"),
        Name = name,
        Type = new TypeRef(type),
        Cardinality = new Cardinality { IsRequired = required },
        Constraints = new ConstraintSet(),
        Annotations = annotations,
    };

    private static AnnotationBag Annotations(params (string Key, object Value)[] values) => new()
    {
        Items = [.. values.Select(static item => new Annotation { Key = new AnnotationKey(item.Key), Value = item.Value, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared })],
    };
}

[SemanticType]
public sealed class M0066Customer
{
    [SemanticDisplayIdentity(Order = 0)]
    [SemanticAccessPath("ByCustomerNumber", Order = 0)]
    public string CustomerNumber { get; set; } = string.Empty;

    public M0066Status Status { get; set; }

    public List<string> Tags { get; set; } = [];

    [SemanticExtensionData]
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public enum M0066Status
{
    Inactive,
    Active,
}

[JsonSerializable(typeof(M0066Customer))]
internal sealed partial class M0066JsonContext : JsonSerializerContext
{
}
