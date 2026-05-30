// CA1720 is disabled because projection option enum values intentionally use canonical type terms
// (for example Double/Decimal) that match documented mapping language in the Power BI/TOM spec.
#pragma warning disable CA1720
namespace SemanticTypeModel.PowerBI;

/// <summary>
/// Controls deterministic Power BI projection behavior.
/// </summary>
public sealed record PowerBiProjectionOptions
{
    /// <summary>
    /// Gets the default projection options.
    /// </summary>
    public static PowerBiProjectionOptions Default { get; } = new();

    /// <summary>
    /// Gets or sets the default naming policy for tables and columns.
    /// </summary>
    public PowerBiNamingPolicy NamingPolicy { get; set; } = PowerBiNamingPolicy.DisplayName;

    /// <summary>
    /// Gets or sets the default role for tables without explicit role metadata.
    /// </summary>
    public PowerBiTableRole DefaultTableRole { get; set; } = PowerBiTableRole.Unknown;

    /// <summary>
    /// Gets or sets the default numeric summarization behavior for non-key numeric columns.
    /// </summary>
    public PowerBiSummarization DefaultNumericSummarization { get; set; } = PowerBiSummarization.Sum;

    /// <summary>
    /// Gets or sets a value indicating whether key columns are hidden by default.
    /// </summary>
    public bool HideTechnicalKeys { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether relationship foreign-key columns are hidden by default.
    /// </summary>
    public bool HideForeignKeys { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether unsupported annotations remain on projected metadata.
    /// </summary>
    public bool IncludeUnsupportedAnnotations { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether incomplete relationships produce errors instead of warnings.
    /// </summary>
    public bool TreatRelationshipsAsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether unannotated object types can become Power BI tables.
    /// </summary>
    public bool ProjectUnannotatedObjectsAsTables { get; set; }

    /// <summary>
    /// Gets or sets how value objects are projected when encountered as nested properties.
    /// </summary>
    public ValueObjectProjectionMode ValueObjectProjectionMode { get; set; } = ValueObjectProjectionMode.Diagnose;

    /// <summary>
    /// Gets or sets how arrays, dictionaries, unions, and unsupported nested objects are handled.
    /// </summary>
    public UnsupportedPowerBiShapeBehavior UnsupportedShapeBehavior { get; set; } = UnsupportedPowerBiShapeBehavior.Diagnose;

    /// <summary>
    /// Gets or sets the enum projection behavior.
    /// </summary>
    public EnumProjectionMode EnumProjectionMode { get; set; } = EnumProjectionMode.Name;

    /// <summary>
    /// Gets or sets numeric projection behavior for general number scalars.
    /// </summary>
    public NumericProjectionMode NumericProjectionMode { get; set; } = NumericProjectionMode.DecimalWhenDefined;

    /// <summary>
    /// Gets or sets a value indicating whether hidden columns are included in projected output.
    /// </summary>
    public bool IncludeHiddenColumns { get; set; } = true;

    /// <summary>
    /// Gets or sets behavior when projected names collide.
    /// </summary>
    public NameCollisionBehavior NameCollisionBehavior { get; set; } = NameCollisionBehavior.Diagnose;

    /// <summary>
    /// Gets or sets a value indicating whether unsupported non-DAX computed expressions are preserved as measures.
    /// </summary>
    public bool PreserveUnsupportedMeasureExpressions { get; set; }

    /// <summary>
    /// Configures table and column naming policy.
    /// </summary>
    /// <param name="policy">The naming policy.</param>
    public void UseNamingPolicy(PowerBiNamingPolicy policy)
    {
        NamingPolicy = policy;
    }
}

/// <summary>
/// Defines how value-object-typed properties are represented.
/// </summary>
public enum ValueObjectProjectionMode
{
    /// <summary>Emit diagnostics and skip projection.</summary>
    Diagnose,

    /// <summary>Flatten nested scalar and enum properties into parent columns.</summary>
    Flatten,

    /// <summary>Serialize the nested value object to a JSON/string column.</summary>
    SerializeJson,
}

/// <summary>
/// Defines behavior for unsupported Power BI shapes.
/// </summary>
public enum UnsupportedPowerBiShapeBehavior
{
    /// <summary>Emit diagnostics and skip projection.</summary>
    Diagnose,

    /// <summary>Skip projection while emitting a warning diagnostic.</summary>
    IgnoreWithWarning,

    /// <summary>Serialize unsupported shapes into JSON/string columns.</summary>
    SerializeJson,
}

/// <summary>
/// Defines enum storage mode for projected columns.
/// </summary>
public enum EnumProjectionMode
{
    /// <summary>Store enum names.</summary>
    Name,

    /// <summary>Prefer enum display names when available.</summary>
    DisplayName,

    /// <summary>Use numeric storage when enum metadata supports numeric backing.</summary>
    NumericWhenAvailable,
}

/// <summary>
/// Defines how generic numeric scalars are projected.
/// </summary>
public enum NumericProjectionMode
{
    /// <summary>Map number scalars to double.</summary>
    Double,

    /// <summary>Map number scalars with precision metadata to decimal; otherwise double.</summary>
    DecimalWhenDefined,
}

/// <summary>
/// Defines behavior for duplicate projected names.
/// </summary>
public enum NameCollisionBehavior
{
    /// <summary>Emit diagnostics and skip duplicates.</summary>
    Diagnose,

    /// <summary>Deterministically append numeric suffixes.</summary>
    Suffix,
}
#pragma warning restore CA1720
