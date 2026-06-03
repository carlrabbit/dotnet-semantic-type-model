using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Provides JsonSerializerOptions helpers for SemanticTypeModel annotations.
/// </summary>
public static class SemanticTypeModelJsonSerializerOptionsExtensions
{
    /// <summary>
    /// Adds a conservative semantic-model resolver to <see cref="JsonSerializerOptions.TypeInfoResolver"/>.
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
