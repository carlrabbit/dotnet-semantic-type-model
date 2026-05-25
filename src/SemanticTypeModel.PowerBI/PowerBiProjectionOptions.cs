// CA1720 is disabled because projection option enum values intentionally use canonical type terms
// (for example Double/Decimal) that match documented mapping language in the Power BI/TOM spec.
#pragma warning disable CA1720
namespace SemanticTypeModel.PowerBI;

/// <summary>
/// Controls deterministic Power BI / TOM-like projection behavior.
/// </summary>
public sealed record PowerBiProjectionOptions
{
    /// <summary>
    /// Gets the default projection options.
    /// </summary>
    public static PowerBiProjectionOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether unannotated object types can become tabular tables.
    /// </summary>
    public bool ProjectUnannotatedObjectsAsTables { get; init; }

    /// <summary>
    /// Gets how value objects are projected when encountered as nested properties.
    /// </summary>
    public ValueObjectProjectionMode ValueObjectProjectionMode { get; init; } = ValueObjectProjectionMode.Diagnose;

    /// <summary>
    /// Gets how arrays, dictionaries, unions, and unsupported nested objects are handled.
    /// </summary>
    public UnsupportedTabularShapeBehavior UnsupportedShapeBehavior { get; init; } = UnsupportedTabularShapeBehavior.Diagnose;

    /// <summary>
    /// Gets the enum projection behavior.
    /// </summary>
    public EnumProjectionMode EnumProjectionMode { get; init; } = EnumProjectionMode.Name;

    /// <summary>
    /// Gets numeric projection behavior for general number scalars.
    /// </summary>
    public NumericProjectionMode NumericProjectionMode { get; init; } = NumericProjectionMode.DecimalWhenDefined;

    /// <summary>
    /// Gets a value indicating whether hidden columns are included in projected output.
    /// </summary>
    public bool IncludeHiddenColumns { get; init; } = true;

    /// <summary>
    /// Gets behavior when projected names collide.
    /// </summary>
    public NameCollisionBehavior NameCollisionBehavior { get; init; } = NameCollisionBehavior.Diagnose;

    /// <summary>
    /// Gets a value indicating whether unsupported non-DAX computed expressions are preserved as measures.
    /// </summary>
    public bool PreserveUnsupportedMeasureExpressions { get; init; }
}

/// <summary>
/// Defines how value-object-typed properties are represented.
/// </summary>
public enum ValueObjectProjectionMode
{
    /// <summary>
    /// Emit diagnostics and skip projection.
    /// </summary>
    Diagnose,

    /// <summary>
    /// Flatten nested scalar and enum properties into parent columns.
    /// </summary>
    Flatten,

    /// <summary>
    /// Serialize the nested value object to a JSON/string column.
    /// </summary>
    SerializeJson,
}

/// <summary>
/// Defines behavior for unsupported tabular shapes.
/// </summary>
public enum UnsupportedTabularShapeBehavior
{
    /// <summary>
    /// Emit diagnostics and skip projection.
    /// </summary>
    Diagnose,

    /// <summary>
    /// Skip projection while emitting a warning diagnostic.
    /// </summary>
    IgnoreWithWarning,

    /// <summary>
    /// Serialize unsupported shapes into JSON/string columns.
    /// </summary>
    SerializeJson,
}

/// <summary>
/// Defines enum storage mode for projected columns.
/// </summary>
public enum EnumProjectionMode
{
    /// <summary>
    /// Store enum names.
    /// </summary>
    Name,

    /// <summary>
    /// Prefer enum display names when available.
    /// </summary>
    DisplayName,

    /// <summary>
    /// Use numeric storage when enum metadata supports numeric backing.
    /// </summary>
    NumericWhenAvailable,
}

/// <summary>
/// Defines how generic numeric scalars are projected.
/// </summary>
public enum NumericProjectionMode
{
    /// <summary>
    /// Map number scalars to double.
    /// </summary>
    Double,

    /// <summary>
    /// Map number scalars with precision metadata to decimal; otherwise double.
    /// </summary>
    DecimalWhenDefined,
}

/// <summary>
/// Defines behavior for duplicate projected names.
/// </summary>
public enum NameCollisionBehavior
{
    /// <summary>
    /// Emit diagnostics and skip duplicates.
    /// </summary>
    Diagnose,

    /// <summary>
    /// Deterministically append numeric suffixes.
    /// </summary>
    Suffix,
}
#pragma warning restore CA1720
