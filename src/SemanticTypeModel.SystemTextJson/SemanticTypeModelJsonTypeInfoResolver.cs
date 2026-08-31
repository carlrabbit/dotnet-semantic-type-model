using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Creates conservative System.Text.Json type-info resolvers from a System.Text.Json domain semantic model.
/// </summary>
public static class SemanticTypeModelJsonTypeInfoResolver
{
    /// <summary>
    /// Creates a resolver that applies supported System.Text.Json projection metadata over the default resolver.
    /// </summary>
    public static IJsonTypeInfoResolver Create(SystemTextJsonSemanticModel model)
    {
        return Create(new DefaultJsonTypeInfoResolver(), model);
    }

    /// <summary>
    /// Creates a resolver that derives System.Text.Json projection metadata from the canonical semantic model and applies it over the default resolver.
    /// </summary>
    public static IJsonTypeInfoResolver Create(TypeSchemaModel model, SystemTextJsonProjectionOptions? options = null)
    {
        return Create(new DefaultJsonTypeInfoResolver(), model, options);
    }

    /// <summary>
    /// Creates a resolver that applies supported System.Text.Json projection metadata over an existing resolver.
    /// </summary>
    public static IJsonTypeInfoResolver Create(IJsonTypeInfoResolver baseResolver, SystemTextJsonSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(baseResolver);
        ArgumentNullException.ThrowIfNull(model);

        return baseResolver is SemanticTypeModelResolver existing
            ? existing.Add(model)
            : new SemanticTypeModelResolver(baseResolver, [model]);
    }

    /// <summary>
    /// Creates a resolver that derives System.Text.Json projection metadata from the canonical semantic model and applies it over an existing resolver.
    /// </summary>
    public static IJsonTypeInfoResolver Create(
        IJsonTypeInfoResolver baseResolver,
        TypeSchemaModel model,
        SystemTextJsonProjectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(baseResolver);
        ArgumentNullException.ThrowIfNull(model);

        SystemTextJsonSemanticModel stjModel = options is null
            ? model.DeriveSystemTextJsonModel().Model
            : model.DeriveSystemTextJsonModel(configure => CopyOptions(options, configure)).Model;
        return Create(baseResolver, stjModel);
    }

    /// <summary>
    /// Wraps an existing resolver with SemanticTypeModel System.Text.Json projection customization.
    /// </summary>
    public static IJsonTypeInfoResolver WithSemanticTypeModelJson(
        this IJsonTypeInfoResolver resolver,
        SystemTextJsonSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(model);

        return Create(resolver, model);
    }

    /// <summary>
    /// Wraps an existing resolver by deriving System.Text.Json projection metadata from the canonical semantic model.
    /// </summary>
    public static IJsonTypeInfoResolver WithSemanticTypeModelJson(
        this IJsonTypeInfoResolver resolver,
        TypeSchemaModel model,
        Action<SystemTextJsonProjectionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(model);

        SystemTextJsonSemanticModel stjModel = model.DeriveSystemTextJsonModel(configure).Model;
        return Create(resolver, stjModel);
    }

    private static void ApplyProjection(JsonTypeInfo typeInfo, IReadOnlyList<SystemTextJsonSemanticModel> models)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        var typeId = new TypeId("global::" + typeInfo.Type.FullName);
        SystemTextJsonTypeDefinition? type = models.Select(model => model.TryGetType(typeId)).FirstOrDefault(static type => type is not null);
        if (type is null)
        {
            return;
        }

        Dictionary<string, SystemTextJsonPropertyDefinition> properties = BuildPropertyMap(type);
        var finalNames = new Dictionary<string, JsonPropertyInfo>(StringComparer.Ordinal);
        foreach (JsonPropertyInfo jsonProperty in typeInfo.Properties)
        {
            SystemTextJsonPropertyDefinition? property = ResolveProperty(jsonProperty, properties);
            if (property is not null && !property.IsExtensionData && !string.IsNullOrWhiteSpace(property.ProjectedJsonName))
            {
                jsonProperty.Name = property.ProjectedJsonName;
            }

            if (!finalNames.TryAdd(jsonProperty.Name, jsonProperty))
            {
                throw new InvalidOperationException(
                    $"SemanticTypeModel System.Text.Json customization produced duplicate JSON property name '{jsonProperty.Name}' for type '{typeInfo.Type.FullName}'.");
            }
        }

        ApplyEntityPolymorphism(typeInfo, typeId, type, models);
    }

    private static void ApplyEntityPolymorphism(
        JsonTypeInfo typeInfo,
        TypeId typeId,
        SystemTextJsonTypeDefinition type,
        IReadOnlyList<SystemTextJsonSemanticModel> models)
    {
        if (!type.IsEntity || typeInfo.PolymorphismOptions is not null)
        {
            return;
        }

        List<(Type ClrType, string Name)> descendants = [];
        foreach (SystemTextJsonSemanticModel model in models)
        {
            foreach (SystemTextJsonTypeDefinition candidate in model.TypesById.Values.Where(static candidate => candidate.IsEntity))
            {
                Type? candidateType = Resolve(candidate.Id) ?? throw new InvalidOperationException($"STJ009: Unable to resolve CLR type identity for semantic Entity '{candidate.Id.Value}'.");
                if (candidateType != typeInfo.Type && typeInfo.Type.IsAssignableFrom(candidateType) && !candidateType.IsAbstract)
                {
                    descendants.Add((candidateType, candidate.Name));
                }
            }
        }

        if (descendants.Count == 0)
        {
            return;
        }

        var discriminatorValues = new HashSet<string>(StringComparer.Ordinal);
        var polymorphism = new JsonPolymorphismOptions { TypeDiscriminatorPropertyName = "$type" };
        foreach ((Type clrType, var name) in descendants.OrderBy(static item => item.Name, StringComparer.Ordinal))
        {
            if (!discriminatorValues.Add(name))
            {
                throw new InvalidOperationException($"STJ009: Duplicate semantic Entity discriminator '{name}' under '{typeId.Value}'.");
            }

            polymorphism.DerivedTypes.Add(new JsonDerivedType(clrType, name));
        }

        typeInfo.PolymorphismOptions = polymorphism;
    }

    private static Type? Resolve(TypeId id)
    {
        var name = id.Value.StartsWith("global::", StringComparison.Ordinal) ? id.Value[8..] : id.Value;
        return Type.GetType(name) ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name, false)).FirstOrDefault(static type => type is not null);
    }

    private static Dictionary<string, SystemTextJsonPropertyDefinition> BuildPropertyMap(SystemTextJsonTypeDefinition type)
    {
        var properties = new Dictionary<string, SystemTextJsonPropertyDefinition>(StringComparer.Ordinal);
        foreach (SystemTextJsonPropertyDefinition property in type.Properties)
        {
            AddIfAbsent(properties, property.SemanticName, property);
            if (!string.IsNullOrWhiteSpace(property.DotNetMemberName))
            {
                AddIfAbsent(properties, property.DotNetMemberName, property);
            }

            if (!string.IsNullOrWhiteSpace(property.SystemTextJsonPropertyName))
            {
                AddIfAbsent(properties, property.SystemTextJsonPropertyName, property);
            }
        }

        return properties;
    }

    private static void AddIfAbsent(Dictionary<string, SystemTextJsonPropertyDefinition> properties, string name, SystemTextJsonPropertyDefinition property)
    {
        if (!string.IsNullOrWhiteSpace(name) && !properties.ContainsKey(name))
        {
            properties.Add(name, property);
        }
    }

    private static SystemTextJsonPropertyDefinition? ResolveProperty(JsonPropertyInfo jsonProperty, Dictionary<string, SystemTextJsonPropertyDefinition> properties)
    {
        return jsonProperty.AttributeProvider is MemberInfo member && properties.TryGetValue(member.Name, out SystemTextJsonPropertyDefinition? propertyByMember)
            ? propertyByMember
            : properties.TryGetValue(jsonProperty.Name, out SystemTextJsonPropertyDefinition? propertyByCurrentName) ? propertyByCurrentName : null;
    }

    private static void CopyOptions(SystemTextJsonProjectionOptions source, SystemTextJsonProjectionOptions target)
    {
        target.ImportSystemTextJsonAttributes = source.ImportSystemTextJsonAttributes;
        target.UseJsonPropertyNameAsSerializationName = source.UseJsonPropertyNameAsSerializationName;
        target.UseJsonPropertyNameAsSemanticName = source.UseJsonPropertyNameAsSemanticName;
        target.PreserveUnsupportedConverterMetadata = source.PreserveUnsupportedConverterMetadata;
        target.PropertyNameSource = source.PropertyNameSource;
        target.Transformations = source.Transformations;
        target.PipelineOptions = source.PipelineOptions;
    }

    private sealed class SemanticTypeModelResolver(IJsonTypeInfoResolver baseResolver, IReadOnlyList<SystemTextJsonSemanticModel> models) : IJsonTypeInfoResolver
    {
        public SemanticTypeModelResolver Add(SystemTextJsonSemanticModel model)
        {
            return new(baseResolver, [.. models, model]);
        }

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions optionsFromSerializer)
        {
            JsonTypeInfo? typeInfo = baseResolver.GetTypeInfo(type, optionsFromSerializer);
            if (typeInfo is not null)
            {
                ApplyProjection(typeInfo, models);
            }

            return typeInfo;
        }
    }
}
