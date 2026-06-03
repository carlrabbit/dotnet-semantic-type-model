using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Building;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.SystemTextJson;

namespace SemanticTypeModel.Samples.SystemTextJsonResolver;

internal static class Program
{
    public static void Main()
    {
        JsonSerializerOptions options = new()
        {
            // AppJsonContext is user-authored; SemanticTypeModel customizes its resolver.
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
        // In consumer code this model can come from the generator or another supported import path.
        return new TypeSchemaModelBuilder()
            .AddShape("global::System.String", new ScalarShape { Kind = ScalarKind.String })
            .AddShape("global::SemanticTypeModel.Samples.SystemTextJsonResolver.Customer", new ObjectShape
            {
                Properties =
                [
                    CreateProperty("customer_id", "Id", "customer_id"),
                    CreateProperty("display_name", "DisplayName", "display_name"),
                ],
            })
            .SetRoot("global::SemanticTypeModel.Samples.SystemTextJsonResolver.Customer")
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
                // The resolver can apply System.Text.Json names from semantic annotations.
                new SchemaAnnotation(SystemTextJsonAnnotationNames.PropertyName, jsonName),
            ],
        };
    }
}

// Consumers own System.Text.Json source-generation contexts.
[JsonSerializable(typeof(Customer))]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}

[SemanticType]
internal sealed class Customer
{
    [SemanticName("customer_id")]
    public required string Id { get; init; }

    [SemanticName("display_name")]
    public required string DisplayName { get; init; }
}
