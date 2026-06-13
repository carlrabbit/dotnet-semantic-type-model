using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Core.Inspection;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.SystemTextJson.Tests.Unit;

public sealed class SemanticTypeModelJsonTypeInfoResolverTests
{
    [Test]
    public async Task Derivation_should_create_deterministic_system_text_json_domain_model()
    {
        SemanticDerivationResult<SystemTextJsonSemanticModel> result = CreateCustomerModel().DeriveSystemTextJsonModel(
            projectionOptions => projectionOptions.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName);

        var text = result.Model.ToSemanticText(new SemanticTextOptions { Detail = SemanticTextDetail.Detailed, IncludeDiagnostics = true });

        _ = await Assert.That(result.Model.TypesById.Count).IsEqualTo(1);
        _ = await Assert.That(text).Contains("Property customerId member=Id jsonName=customer_id projectedJsonName=customerId");
        _ = await Assert.That(result.Diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AddSemanticTypeModelJson_should_preserve_existing_contract_by_default()
    {
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = ResolverCustomerContext.Default,
        };

        _ = options.AddSemanticTypeModelJson(CreateCustomerModel());

        var json = JsonSerializer.Serialize(new ResolverCustomer { Id = "C-001", DisplayName = "Ada" }, options);

        _ = await Assert.That(json).Contains("customer_id");
        _ = await Assert.That(json).Contains("display_name");
        _ = await Assert.That(json).DoesNotContain("customerId");
    }

    [Test]
    public async Task Resolver_should_serialize_and_deserialize_using_semantic_property_names()
    {
        SystemTextJsonSemanticModel stjModel = CreateCustomerModel()
            .DeriveSystemTextJsonModel(projectionOptions => projectionOptions.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName)
            .Model;
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = ResolverCustomerContext.Default.WithSemanticTypeModelJson(stjModel),
        };

        var json = JsonSerializer.Serialize(new ResolverCustomer { Id = "C-001", DisplayName = "Ada" }, options);
        ResolverCustomer? roundTripped = JsonSerializer.Deserialize<ResolverCustomer>("""
            { "customerId": "C-002", "displayName": "Grace" }
            """, options);

        _ = await Assert.That(json).Contains("customerId");
        _ = await Assert.That(json).Contains("displayName");
        _ = await Assert.That(json).DoesNotContain("customer_id");
        _ = await Assert.That(roundTripped).IsNotNull();
        _ = await Assert.That(roundTripped!.Id).IsEqualTo("C-002");
        _ = await Assert.That(roundTripped.DisplayName).IsEqualTo("Grace");
    }

    [Test]
    public async Task Resolver_should_use_system_text_json_annotation_names_when_requested()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        _ = options.AddSemanticTypeModelJson(
            CreateCustomerModel(),
            projectionOptions => projectionOptions.PropertyNameSource = SemanticJsonPropertyNameSource.SystemTextJsonPropertyNameAnnotation);

        var json = JsonSerializer.Serialize(new ResolverCustomer { Id = "C-001", DisplayName = "Ada" }, options);

        _ = await Assert.That(json).Contains("customer_id");
        _ = await Assert.That(json).Contains("display_name");
    }

    [Test]
    public async Task Resolver_should_fail_deterministically_for_duplicate_final_json_names()
    {
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = ResolverCustomerContext.Default.WithSemanticTypeModelJson(
                CreateDuplicateNameModel(),
                projectionOptions => projectionOptions.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName),
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            JsonSerializer.Serialize(new ResolverCustomer { Id = "C-001", DisplayName = "Ada" }, options));

        _ = await Assert.That(exception.Message).Contains("duplicate JSON property name 'duplicate'");
    }

    private static TypeSchemaModel CreateCustomerModel()
    {
        ScalarTypeDefinition stringType = Scalar("global::System.String");
        var customer = new ObjectTypeDefinition
        {
            Id = new TypeId("global::SemanticTypeModel.SystemTextJson.Tests.Unit.ResolverCustomer"),
            Name = "ResolverCustomer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            Properties =
            [
                CreateProperty("customerId", "Id", "customer_id"),
                CreateProperty("displayName", "DisplayName", "display_name"),
            ],
            Keys = [],
            Relationships = [],
        };

        return BuildModel(customer.Id.Value, customer, stringType);
    }

    private static TypeSchemaModel CreateDuplicateNameModel()
    {
        ScalarTypeDefinition stringType = Scalar("global::System.String");
        var customer = new ObjectTypeDefinition
        {
            Id = new TypeId("global::SemanticTypeModel.SystemTextJson.Tests.Unit.ResolverCustomer"),
            Name = "ResolverCustomer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            Properties =
            [
                CreateProperty("duplicate", "Id", "customer_id"),
                CreateProperty("duplicate", "DisplayName", "display_name"),
            ],
            Keys = [],
            Relationships = [],
        };

        return BuildModel(customer.Id.Value, customer, stringType);
    }

    private static ScalarTypeDefinition Scalar(string id)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(id),
            Name = id,
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            ScalarKind = ScalarKind.String,
        };
    }

    private static PropertyDefinition CreateProperty(string semanticName, string memberName, string jsonName)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(memberName),
            Name = semanticName,
            Type = new TypeRef(new TypeId("global::System.String")),
            Cardinality = new Cardinality { IsRequired = true },
            Mutability = Mutability.InitOnly,
            Constraints = new ConstraintSet(),
            Annotations = Annotations(
                Annotation("dotnet.memberName", memberName),
                Annotation(SystemTextJsonAnnotationNames.PropertyName, jsonName)),
        };
    }

    private static TypeSchemaModel BuildModel(string rootId, params TypeDefinition[] types)
    {
        return new TypeSchemaModel
        {
            Id = new SchemaModelId(rootId),
            Types = types,
            TypesById = types.ToDictionary(static type => type.Id, static type => type),
            Annotations = new AnnotationBag(),
        };
    }

    private static AnnotationBag Annotations(params Annotation[] annotations)
    {
        return new AnnotationBag { Items = annotations };
    }

    private static Annotation Annotation(string key, object value)
    {
        return new Annotation
        {
            Key = new AnnotationKey(key),
            Value = value,
            Scope = AnnotationScope.Member,
            Source = AnnotationSource.Imported,
        };
    }
}

public sealed class ResolverCustomer
{
    [JsonPropertyName("customer_id")]
    public required string Id { get; init; }

    [JsonPropertyName("display_name")]
    public required string DisplayName { get; init; }
}

[JsonSerializable(typeof(ResolverCustomer))]
internal sealed partial class ResolverCustomerContext : JsonSerializerContext
{
}
