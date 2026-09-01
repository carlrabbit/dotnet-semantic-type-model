using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SemanticTypeModel.EFCore;

/// <summary>Selects the semantic model manifest in the assembly containing <paramref name="markerType"/> for EF configuration generation.</summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class GenerateSemanticEfModelAttribute(Type markerType) : Attribute
{
    /// <summary>Gets the marker whose assembly owns the selected semantic model.</summary>
    public Type MarkerType { get; } = markerType;
}

/// <summary>Converter and comparer primitives used by generated EF configuration source.</summary>
public static class SemanticEfValueConverters
{
    /// <summary>Creates the URI-to-string converter used by generated scalar configuration.</summary>
    public static ValueConverter<Uri, string> Uri()
    {
        return new(value => value.ToString(), value => new Uri(value, UriKind.RelativeOrAbsolute));
    }

    /// <summary>Creates the nullable URI-to-string converter used by generated scalar configuration.</summary>
    public static ValueConverter<Uri?, string?> NullableUri()
    {
        return new(value => value == null ? null : value.ToString(), value => value == null ? null : new Uri(value, UriKind.RelativeOrAbsolute));
    }

    /// <summary>Creates the deterministic JSON converter used for an owned ValueKind column.</summary>
    public static ValueConverter<T, string> Json<T>(params Type[] wrapperTypes)
    {
        JsonSerializerOptions options = CreateJsonOptions(wrapperTypes);
        return new(
        value => JsonSerializer.Serialize(value, options),
        json => JsonSerializer.Deserialize<T>(json, options)!);
    }

    /// <summary>Creates the structural JSON comparer used for an owned ValueKind column.</summary>
    public static ValueComparer<T> JsonComparer<T>(params Type[] wrapperTypes)
    {
        JsonSerializerOptions options = CreateJsonOptions(wrapperTypes);
        return new(
        (left, right) => JsonSerializer.Serialize(left, options) == JsonSerializer.Serialize(right, options),
        value => JsonSerializer.Serialize(value, options).GetHashCode(StringComparison.Ordinal),
        value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, options), options)!);
    }

    private static JsonSerializerOptions CreateJsonOptions(IEnumerable<Type> wrapperTypes)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General);
        Type[] wrappers = [.. wrapperTypes.Distinct()];
        if (wrappers.Length > 0)
        {
            options.Converters.Insert(0, new SingleValueWrapperJsonConverterFactory(wrappers));
        }

        return options;
    }
}

internal sealed class SingleValueWrapperJsonConverterFactory(IEnumerable<Type> wrapperTypes) : JsonConverterFactory
{
    private readonly HashSet<Type> _wrapperTypes = [.. wrapperTypes];

    public override bool CanConvert(Type type)
    {
        return _wrapperTypes.Contains(type);
    }

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
    {
        PropertyInfo value = type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"Wrapper '{type}' does not expose a public Value property.");
        return (JsonConverter)Activator.CreateInstance(typeof(SingleValueWrapperJsonConverter<,>).MakeGenericType(type, value.PropertyType))!;
    }
}

internal sealed class SingleValueWrapperJsonConverter<TWrapper, TValue> : JsonConverter<TWrapper>
    where TWrapper : struct
{
    private static readonly ConstructorInfo Constructor = typeof(TWrapper).GetConstructor([typeof(TValue)])
        ?? throw new InvalidOperationException($"Wrapper '{typeof(TWrapper)}' has no matching constructor.");

    public override TWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        TValue value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
        return (TWrapper)Constructor.Invoke([value]);
    }

    public override void Write(Utf8JsonWriter writer, TWrapper value, JsonSerializerOptions options)
    {
        var underlying = (TValue)typeof(TWrapper).GetProperty("Value")!.GetValue(value)!;
        JsonSerializer.Serialize(writer, underlying, options);
    }
}
