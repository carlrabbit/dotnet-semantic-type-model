using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Building;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.SystemTextJson;

namespace SemanticTypeModel.Samples.SystemTextJsonBasic;

internal static class Program
{
    public static void Main()
    {
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = AppJsonContext.Default.WithSemanticTypeModelJson(
                CreateModel(),
                semanticOptions => semanticOptions.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName),
        };

        Customer customer = new() { Id = "C-001", DisplayName = "Ada" };
        string json = JsonSerializer.Serialize(customer, options);
        Customer? roundTripped = JsonSerializer.Deserialize<Customer>(json, options);

        Console.WriteLine(json);
        Console.WriteLine(roundTripped?.DisplayName);
        Console.WriteLine(SystemTextJsonAnnotationNames.PropertyName);
    }

    private static TypeSchemaModel CreateModel()
    {
        return new TypeSchemaModelBuilder()
            .AddShape("global::System.String", new ScalarShape { Kind = ScalarKind.String })
            .AddShape("global::SemanticTypeModel.Samples.SystemTextJsonBasic.Customer", new ObjectShape
            {
                Properties =
                [
                    CreateProperty("customerId", "Id", "customer_id"),
                    CreateProperty("displayName", "DisplayName", "display_name"),
                ],
            })
            .SetRoot("global::SemanticTypeModel.Samples.SystemTextJsonBasic.Customer")
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

[JsonSerializable(typeof(Customer))]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}

[SemanticType]
internal sealed class Customer
{
    [SemanticName("customerId")]
    [JsonPropertyName("customer_id")]
    public required string Id { get; init; }

    [SemanticName("displayName")]
    [JsonPropertyName("display_name")]
    public required string DisplayName { get; init; }
}
