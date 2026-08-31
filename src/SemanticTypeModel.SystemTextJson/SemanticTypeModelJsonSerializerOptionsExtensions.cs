using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Provides JsonSerializerOptions helpers for SemanticTypeModel System.Text.Json projection metadata.
/// </summary>
public static class SemanticTypeModelJsonSerializerOptionsExtensions
{
    /// <summary>
    /// Adds a semantic-model resolver to <see cref="JsonSerializerOptions.TypeInfoResolver"/>.
    /// </summary>
    public static JsonSerializerOptions AddSemanticTypeModelJson(
        this JsonSerializerOptions options,
        SystemTextJsonSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(model);

        AddStrongScalarConverters(options, model);
        IJsonTypeInfoResolver baseResolver = options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.TypeInfoResolver = baseResolver.WithSemanticTypeModelJson(model);
        return options;
    }

    /// <summary>
    /// Derives System.Text.Json projection metadata from the canonical semantic model and adds a semantic-model resolver to <see cref="JsonSerializerOptions.TypeInfoResolver"/>.
    /// </summary>
    public static JsonSerializerOptions AddSemanticTypeModelJson(
        this JsonSerializerOptions options,
        TypeSchemaModel model,
        Action<SystemTextJsonProjectionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(model);

        SystemTextJsonSemanticModel stjModel = model.DeriveSystemTextJsonModel(configure).Model;
        AddStrongScalarConverters(options, stjModel);
        IJsonTypeInfoResolver baseResolver = options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.TypeInfoResolver = baseResolver.WithSemanticTypeModelJson(stjModel);
        return options;
    }

    private static void AddStrongScalarConverters(JsonSerializerOptions options, SystemTextJsonSemanticModel model)
    {
        var mappings = new List<(Type Wrapper, Type Value)>();
        foreach (SystemTextJsonStrongScalarDefinition strongScalar in model.StrongScalars)
        {
            Type? wrapper = StrongScalarJsonConverterFactory.Resolve(strongScalar.Id.Value);
            Type? value = StrongScalarJsonConverterFactory.Resolve(strongScalar.ValueType.Id.Value);
            if (wrapper is not null && value is not null)
            {
                mappings.Add((wrapper, value));
            }
        }

        if (mappings.Count > 0)
        {
            options.Converters.Insert(0, new StrongScalarJsonConverterFactory(mappings));
        }
    }

}
