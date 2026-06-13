using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SemanticTypeModel.Abstractions.Canonical;

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

        IJsonTypeInfoResolver baseResolver = options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.TypeInfoResolver = baseResolver.WithSemanticTypeModelJson(model, configure);
        return options;
    }
}
