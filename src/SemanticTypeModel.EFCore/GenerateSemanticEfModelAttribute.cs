using System.Text.Json;
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
    public static ValueConverter<T, string> Json<T>()
    {
        return new(
        value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<T>(json, (JsonSerializerOptions?)null)!);
    }

    /// <summary>Creates the structural JSON comparer used for an owned ValueKind column.</summary>
    public static ValueComparer<T> JsonComparer<T>()
    {
        return new(
        (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
        value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
        value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);
    }
}
