// CA1720 is disabled because projection option enum values intentionally use canonical type terms
// (for example String/Numeric) that match the EF Core projection specification.
#pragma warning disable CA1720
namespace SemanticTypeModel.EFCore;

/// <summary>
/// Controls deterministic EF Core-like projection behavior.
/// </summary>
public sealed record EfCoreProjectionOptions
{
    /// <summary>
    /// Gets the default projection options.
    /// </summary>
    public static EfCoreProjectionOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether unannotated object types can become entity types.
    /// </summary>
    public bool ProjectUnannotatedObjectsAsEntities { get; init; }

    /// <summary>
    /// Gets a value indicating whether entity candidates may be projected without a primary key.
    /// </summary>
    public bool AllowKeylessEntities { get; init; }

    /// <summary>
    /// Gets how nested value objects are represented.
    /// </summary>
    public ValueObjectEfProjectionMode ValueObjectProjectionMode { get; init; } = ValueObjectEfProjectionMode.Diagnose;

    /// <summary>
    /// Gets how unsupported array, dictionary, union, and nested object shapes are handled.
    /// </summary>
    public UnsupportedEfShapeBehavior UnsupportedShapeBehavior { get; init; } = UnsupportedEfShapeBehavior.Diagnose;

    /// <summary>
    /// Gets how enum properties are stored.
    /// </summary>
    public EnumEfProjectionMode EnumProjectionMode { get; init; } = EnumEfProjectionMode.String;

    /// <summary>
    /// Gets how alternate, natural, and external keys are represented.
    /// </summary>
    public AlternateKeyProjectionMode AlternateKeyProjectionMode { get; init; } = AlternateKeyProjectionMode.AlternateKey;

    /// <summary>
    /// Gets a value indicating whether display names participate in table and column naming precedence.
    /// </summary>
    public bool PreferDisplayNamesForTableAndColumnNames { get; init; }

    /// <summary>
    /// Gets duplicate projected-name handling behavior.
    /// </summary>
    public NameCollisionBehavior NameCollisionBehavior { get; init; } = NameCollisionBehavior.Diagnose;

    /// <summary>Gets the default inheritance strategy used when canonical inheritance is present without EF-specific metadata.</summary>
    public EfCoreInheritanceStrategy DefaultInheritanceStrategy { get; init; } = EfCoreInheritanceStrategy.Unspecified;
}

/// <summary>
/// Defines how value-object-typed properties are represented.
/// </summary>
public enum ValueObjectEfProjectionMode
{
    /// <summary>
    /// Emit diagnostics and skip projection.
    /// </summary>
    Diagnose,

    /// <summary>
    /// Create an owned/complex-type-like entity definition.
    /// </summary>
    Owned,

    /// <summary>
    /// Flatten nested scalar members into the parent entity definition.
    /// </summary>
    Flatten,

    /// <summary>
    /// Serialize the value object as JSON/text.
    /// </summary>
    SerializeJson,
}

/// <summary>
/// Defines behavior for unsupported EF shapes.
/// </summary>
public enum UnsupportedEfShapeBehavior
{
    /// <summary>
    /// Emit diagnostics and skip projection.
    /// </summary>
    Diagnose,

    /// <summary>
    /// Skip projection while emitting a warning.
    /// </summary>
    IgnoreWithWarning,

    /// <summary>
    /// Serialize unsupported shapes as JSON/text.
    /// </summary>
    SerializeJson,
}

/// <summary>
/// Defines enum storage modes.
/// </summary>
public enum EnumEfProjectionMode
{
    /// <summary>
    /// Store enum values as strings.
    /// </summary>
    String,

    /// <summary>
    /// Store enum values using numeric backing where supported.
    /// </summary>
    Numeric,
}

/// <summary>
/// Defines how non-primary canonical keys are represented.
/// </summary>
public enum AlternateKeyProjectionMode
{
    /// <summary>
    /// Represent canonical alternate-like keys as EF alternate keys.
    /// </summary>
    AlternateKey,

    /// <summary>
    /// Represent canonical alternate-like keys as unique indexes.
    /// </summary>
    UniqueIndex,

    /// <summary>
    /// Preserve canonical alternate-like keys only through annotations.
    /// </summary>
    AnnotationOnly,
}

/// <summary>
/// Defines duplicate-name handling behavior.
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
