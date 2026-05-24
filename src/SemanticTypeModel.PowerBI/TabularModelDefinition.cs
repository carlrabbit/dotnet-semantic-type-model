#pragma warning disable CA1720
using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.PowerBI;

/// <summary>
/// Represents the TOM-like projection result for a semantic type model.
/// </summary>
public sealed record TabularModelDefinition
{
    /// <summary>
    /// Gets the projected model name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets projected tables.
    /// </summary>
    public required IReadOnlyList<TabularTableDefinition> Tables { get; init; }

    /// <summary>
    /// Gets projected relationships.
    /// </summary>
    public required IReadOnlyList<TabularRelationshipDefinition> Relationships { get; init; }

    /// <summary>
    /// Gets projection diagnostics.
    /// </summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }
}

/// <summary>
/// Represents a projected tabular table.
/// </summary>
public sealed record TabularTableDefinition
{
    /// <summary>
    /// Gets table name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets projected columns.
    /// </summary>
    public required IReadOnlyList<TabularColumnDefinition> Columns { get; init; }

    /// <summary>
    /// Gets projected measures.
    /// </summary>
    public required IReadOnlyList<TabularMeasureDefinition> Measures { get; init; }

    /// <summary>
    /// Gets optional table description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets optional display folder.
    /// </summary>
    public string? DisplayFolder { get; init; }

    /// <summary>
    /// Gets carried annotations.
    /// </summary>
    public required AnnotationBag Annotations { get; init; }
}

/// <summary>
/// Represents a projected tabular column.
/// </summary>
public sealed record TabularColumnDefinition
{
    /// <summary>
    /// Gets column name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets tabular data type.
    /// </summary>
    public required TabularDataType DataType { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column is nullable.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column participates in a key.
    /// </summary>
    public bool IsKey { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column is hidden.
    /// </summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// Gets optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets optional data category.
    /// </summary>
    public string? DataCategory { get; init; }

    /// <summary>
    /// Gets optional format string.
    /// </summary>
    public string? FormatString { get; init; }

    /// <summary>
    /// Gets carried annotations.
    /// </summary>
    public required AnnotationBag Annotations { get; init; }
}

/// <summary>
/// Represents a projected tabular relationship.
/// </summary>
public sealed record TabularRelationshipDefinition
{
    /// <summary>
    /// Gets relationship name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets source table name.
    /// </summary>
    public required string FromTable { get; init; }

    /// <summary>
    /// Gets source column name.
    /// </summary>
    public required string FromColumn { get; init; }

    /// <summary>
    /// Gets destination table name.
    /// </summary>
    public required string ToTable { get; init; }

    /// <summary>
    /// Gets destination column name.
    /// </summary>
    public required string ToColumn { get; init; }

    /// <summary>
    /// Gets relationship cardinality.
    /// </summary>
    public required TabularRelationshipCardinality Cardinality { get; init; }

    /// <summary>
    /// Gets a value indicating whether this relationship is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Represents a projected tabular measure.
/// </summary>
public sealed record TabularMeasureDefinition
{
    /// <summary>
    /// Gets measure name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets measure expression.
    /// </summary>
    public required string Expression { get; init; }

    /// <summary>
    /// Gets expression language.
    /// </summary>
    public string ExpressionLanguage { get; init; } = "DAX";

    /// <summary>
    /// Gets optional format string.
    /// </summary>
    public string? FormatString { get; init; }

    /// <summary>
    /// Gets optional display folder.
    /// </summary>
    public string? DisplayFolder { get; init; }

    /// <summary>
    /// Gets optional description.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Defines projected tabular data types.
/// </summary>
public enum TabularDataType
{
    /// <summary>Boolean type.</summary>
    Boolean,

    /// <summary>String/text type.</summary>
    String,

    /// <summary>64-bit integer type.</summary>
    Int64,

    /// <summary>Double-precision numeric type.</summary>
    Double,

    /// <summary>Decimal numeric type.</summary>
    Decimal,

    /// <summary>Date type.</summary>
    Date,

    /// <summary>Time type.</summary>
    Time,

    /// <summary>Date-time type.</summary>
    DateTime,

    /// <summary>Binary payload type.</summary>
    Binary,
}

/// <summary>
/// Defines relationship cardinality in the projection model.
/// </summary>
public enum TabularRelationshipCardinality
{
    /// <summary>One-to-one.</summary>
    OneToOne,

    /// <summary>One-to-many.</summary>
    OneToMany,

    /// <summary>Many-to-one.</summary>
    ManyToOne,

    /// <summary>Many-to-many.</summary>
    ManyToMany,
}
#pragma warning restore CA1720
