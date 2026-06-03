using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Building;

namespace SemanticTypeModel.SystemTextJson.Tests.Unit;

public sealed class SemanticTypeModelJsonTypeInfoResolverTests
{
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
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = ResolverCustomerContext.Default.WithSemanticTypeModelJson(
                CreateCustomerModel(),
                projectionOptions => projectionOptions.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName),
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
        return new TypeSchemaModelBuilder()
            .AddShape("global::System.String", new ScalarShape { Kind = ScalarKind.String })
            .AddShape("global::SemanticTypeModel.SystemTextJson.Tests.Unit.ResolverCustomer", new ObjectShape
            {
                Properties =
                [
                    CreateProperty("customerId", "Id", "customer_id"),
                    CreateProperty("displayName", "DisplayName", "display_name"),
                ],
            })
            .SetRoot("global::SemanticTypeModel.SystemTextJson.Tests.Unit.ResolverCustomer")
            .Build();
    }

    private static TypeSchemaModel CreateDuplicateNameModel()
    {
        return new TypeSchemaModelBuilder()
            .AddShape("global::System.String", new ScalarShape { Kind = ScalarKind.String })
            .AddShape("global::SemanticTypeModel.SystemTextJson.Tests.Unit.ResolverCustomer", new ObjectShape
            {
                Properties =
                [
                    CreateProperty("duplicate", "Id", "customer_id"),
                    CreateProperty("duplicate", "DisplayName", "display_name"),
                ],
            })
            .SetRoot("global::SemanticTypeModel.SystemTextJson.Tests.Unit.ResolverCustomer")
            .Build();
    }

    private static PropertyShape CreateProperty(string semanticName, string memberName, string jsonName)
    {
        return new PropertyShape
        {
            Name = semanticName,
            IsRequired = true,
            Type = ShapeRef.FromIdentifier("global::System.String"),
            Annotations =
            [
                new SchemaAnnotation("dotnet.memberName", memberName),
                new SchemaAnnotation(SystemTextJsonAnnotationNames.PropertyName, jsonName),
            ],
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
