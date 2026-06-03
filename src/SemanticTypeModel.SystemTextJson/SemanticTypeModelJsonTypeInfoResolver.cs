using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Creates conservative System.Text.Json type-info resolvers from semantic model annotations.
/// </summary>
public static class SemanticTypeModelJsonTypeInfoResolver
{
    private const string DotNetMemberNameAnnotation = "dotnet.memberName";

    /// <summary>
    /// Creates a resolver that applies supported System.Text.Json annotations over the default resolver.
    /// </summary>
    public static IJsonTypeInfoResolver Create(TypeSchemaModel model, SystemTextJsonProjectionOptions? options = null)
    {
        return Create(new DefaultJsonTypeInfoResolver(), model, options);
    }

    /// <summary>
    /// Creates a resolver that applies supported System.Text.Json annotations over an existing resolver.
    /// </summary>
    public static IJsonTypeInfoResolver Create(
        IJsonTypeInfoResolver baseResolver,
        TypeSchemaModel model,
        SystemTextJsonProjectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(baseResolver);
        ArgumentNullException.ThrowIfNull(model);

        options ??= new SystemTextJsonProjectionOptions();
        return new SemanticTypeModelResolver(baseResolver, model, options);
    }

    /// <summary>
    /// Wraps an existing resolver with SemanticTypeModel customization.
    /// </summary>
    public static IJsonTypeInfoResolver WithSemanticTypeModelJson(
        this IJsonTypeInfoResolver resolver,
        TypeSchemaModel model,
        Action<SystemTextJsonProjectionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(model);

        var options = new SystemTextJsonProjectionOptions();
        configure?.Invoke(options);
        return Create(resolver, model, options);
    }

    private static void ApplyAnnotations(JsonTypeInfo typeInfo, TypeSchemaModel model, SystemTextJsonProjectionOptions options)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        var typeId = "global::" + typeInfo.Type.FullName;
        if (model.TryGetShape(typeId) is not ObjectShape shape)
        {
            return;
        }

        Dictionary<string, PropertyShape> semanticProperties = BuildPropertyMap(shape);
        var finalNames = new Dictionary<string, JsonPropertyInfo>(StringComparer.Ordinal);
        foreach (JsonPropertyInfo jsonProperty in typeInfo.Properties)
        {
            PropertyShape? property = ResolveProperty(jsonProperty, semanticProperties);
            if (property is not null && TryResolveName(property, options.PropertyNameSource, out var name))
            {
                jsonProperty.Name = name!;
            }

            if (!finalNames.TryAdd(jsonProperty.Name!, jsonProperty))
            {
                throw new InvalidOperationException(
                    $"SemanticTypeModel System.Text.Json customization produced duplicate JSON property name '{jsonProperty.Name}' for type '{typeInfo.Type.FullName}'.");
            }
        }
    }

    private static Dictionary<string, PropertyShape> BuildPropertyMap(ObjectShape shape)
    {
        var properties = new Dictionary<string, PropertyShape>(StringComparer.Ordinal);
        foreach (PropertyShape property in shape.Properties)
        {
            AddIfAbsent(properties, property.Name, property);
            if (TryGetAnnotation(property, DotNetMemberNameAnnotation, out var memberName))
            {
                AddIfAbsent(properties, memberName!, property);
            }

            if (TryGetAnnotation(property, SystemTextJsonAnnotationNames.PropertyName, out var jsonName))
            {
                AddIfAbsent(properties, jsonName!, property);
            }
        }

        return properties;
    }

    private static void AddIfAbsent(Dictionary<string, PropertyShape> properties, string name, PropertyShape property)
    {
        if (!string.IsNullOrWhiteSpace(name) && !properties.ContainsKey(name))
        {
            properties.Add(name, property);
        }
    }

    private static PropertyShape? ResolveProperty(JsonPropertyInfo jsonProperty, Dictionary<string, PropertyShape> properties)
    {
        return jsonProperty.AttributeProvider is MemberInfo member && properties.TryGetValue(member.Name, out PropertyShape? propertyByMember)
            ? propertyByMember
            : properties.TryGetValue(jsonProperty.Name, out PropertyShape? propertyByCurrentName) ? propertyByCurrentName : null;
    }

    private static bool TryResolveName(PropertyShape property, SemanticJsonPropertyNameSource source, out string? name)
    {
        name = source switch
        {
            SemanticJsonPropertyNameSource.ExistingJsonContract => null,
            SemanticJsonPropertyNameSource.SystemTextJsonPropertyNameAnnotation => TryGetAnnotation(property, SystemTextJsonAnnotationNames.PropertyName, out var jsonName) ? jsonName : null,
            SemanticJsonPropertyNameSource.SemanticPropertyName => property.Name,
            _ => throw new InvalidOperationException($"Semantic JSON property name source '{source}' is not supported."),
        };

        return !string.IsNullOrWhiteSpace(name);
    }

    private static bool TryGetAnnotation(PropertyShape property, string key, out string? value)
    {
        SchemaAnnotation? annotation = property.Annotations.FirstOrDefault(annotation => string.Equals(annotation.Key, key, StringComparison.Ordinal));
        value = annotation?.Value;
        return !string.IsNullOrWhiteSpace(value);
    }

    private sealed class SemanticTypeModelResolver(
        IJsonTypeInfoResolver baseResolver,
        TypeSchemaModel model,
        SystemTextJsonProjectionOptions options) : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions optionsFromSerializer)
        {
            JsonTypeInfo? typeInfo = baseResolver.GetTypeInfo(type, optionsFromSerializer);
            if (typeInfo is not null)
            {
                ApplyAnnotations(typeInfo, model, options);
            }

            return typeInfo;
        }
    }
}
