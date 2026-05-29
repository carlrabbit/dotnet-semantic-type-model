using System.Text.Json.Serialization.Metadata;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Creates conservative System.Text.Json type-info resolvers from semantic model annotations.
/// </summary>
public static class SemanticTypeModelJsonTypeInfoResolver
{
    /// <summary>
    /// Creates a resolver that applies supported System.Text.Json annotations when CLR names can be matched safely.
    /// </summary>
    public static IJsonTypeInfoResolver Create(TypeSchemaModel model, SystemTextJsonProjectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        options ??= new SystemTextJsonProjectionOptions();

        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo => ApplyAnnotations(typeInfo, model, options));
        return resolver;
    }

    private static void ApplyAnnotations(JsonTypeInfo typeInfo, TypeSchemaModel model, SystemTextJsonProjectionOptions options)
    {
        var typeId = "global::" + typeInfo.Type.FullName;
        if (model.TryGetShape(typeId) is not ObjectShape shape)
        {
            return;
        }

        var semanticProperties = shape.Properties.ToDictionary(static property => property.Name, static property => property, StringComparer.Ordinal);
        foreach (JsonPropertyInfo jsonProperty in typeInfo.Properties)
        {
            if (!semanticProperties.TryGetValue(jsonProperty.Name, out PropertyShape? property))
            {
                continue;
            }

            var jsonName = property.Annotations.FirstOrDefault(static annotation => annotation.Key == SystemTextJsonAnnotationNames.PropertyName)?.Value;
            if (options.UseJsonPropertyNameAsSerializationName && !string.IsNullOrWhiteSpace(jsonName))
            {
                jsonProperty.Name = jsonName;
            }
        }
    }
}
