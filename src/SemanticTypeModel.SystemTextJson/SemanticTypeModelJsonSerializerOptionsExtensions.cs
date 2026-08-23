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

        AddStrongScalarConverters(options, model);
        IJsonTypeInfoResolver baseResolver = options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.TypeInfoResolver = baseResolver.WithSemanticTypeModelJson(model, configure);
        return options;
    }

    private static void AddStrongScalarConverters(JsonSerializerOptions options, TypeSchemaModel model)
    {
        var mappings = new List<(Type Wrapper, Type Value)>();
        foreach (StrongScalarTypeDefinition strongScalar in model.Types.OfType<StrongScalarTypeDefinition>())
        {
            Type? wrapper = StrongScalarJsonConverterFactory.Resolve(strongScalar.Id.Value);
            Type? value = model.TryGetType(strongScalar.ValueType.Id) is ScalarTypeDefinition scalar
                ? StrongScalarJsonConverterFactory.Resolve(scalar.Id.Value) ?? ResolveScalarClrType(scalar)
                : null;
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

    private static Type? ResolveScalarClrType(ScalarTypeDefinition scalar)
    {
        return scalar.ScalarKind switch
        {
            ScalarKind.Boolean => typeof(bool),
            ScalarKind.String => typeof(string),
            ScalarKind.Integer => typeof(long),
            ScalarKind.Number => typeof(double),
            ScalarKind.Decimal => typeof(decimal),
            ScalarKind.Date => typeof(DateOnly),
            ScalarKind.Time => typeof(TimeOnly),
            ScalarKind.DateTime => typeof(DateTime),
            ScalarKind.DateTimeOffset => typeof(DateTimeOffset),
            ScalarKind.Duration => typeof(TimeSpan),
            ScalarKind.Guid => typeof(Guid),
            ScalarKind.Binary => typeof(byte[]),
            ScalarKind.Json or ScalarKind.Unknown => null,
            _ => null,
        };
    }
}
