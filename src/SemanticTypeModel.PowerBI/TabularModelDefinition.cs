// CA1720 is disabled because tabular data-type enum members intentionally use canonical type names
// (for example String/Int64/Decimal) to align with projection terminology and spec contracts.
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
    /// Gets optional display name.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the Power BI table role.
    /// </summary>
    public PowerBiTableRole Role { get; init; } = PowerBiTableRole.Unknown;

    /// <summary>
    /// Gets a value indicating whether the table is hidden.
    /// </summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// Gets the source semantic type id.
    /// </summary>
    public TypeId? SourceTypeId { get; init; }

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
    /// Gets optional display name.
    /// </summary>
    public string? DisplayName { get; init; }

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
    /// Gets default summarization behavior.
    /// </summary>
    public PowerBiSummarization Summarization { get; init; } = PowerBiSummarization.None;

    /// <summary>
    /// Gets the source semantic member id.
    /// </summary>
    public PropertyId? SourcePropertyId { get; init; }

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

    /// <summary>
    /// Gets the relationship filter direction.
    /// </summary>
    public PowerBiRelationshipDirection Direction { get; init; } = PowerBiRelationshipDirection.Single;

    /// <summary>
    /// Gets the source semantic relationship id.
    /// </summary>
    public RelationshipId? SourceRelationshipId { get; init; }
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
    /// Gets optional display name.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the measure is hidden.
    /// </summary>
    public bool IsHidden { get; init; }

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

    /// <summary>
    /// Gets carried annotations.
    /// </summary>
    public AnnotationBag Annotations { get; init; } = new();
}


/// <summary>
/// Defines supported Power BI table roles.
/// </summary>
public enum PowerBiTableRole
{
    /// <summary>Fact table.</summary>
    Fact,

    /// <summary>Dimension table.</summary>
    Dimension,

    /// <summary>Bridge table.</summary>
    Bridge,

    /// <summary>Degenerate dimension table.</summary>
    DegenerateDimension,

    /// <summary>Unknown table role.</summary>
    Unknown,
}

/// <summary>
/// Defines supported default summarization behaviors.
/// </summary>
public enum PowerBiSummarization
{
    /// <summary>No summarization.</summary>
    None,

    /// <summary>Sum values.</summary>
    Sum,

    /// <summary>Average values.</summary>
    Average,

    /// <summary>Minimum value.</summary>
    Min,

    /// <summary>Maximum value.</summary>
    Max,

    /// <summary>Count values.</summary>
    Count,

    /// <summary>Count distinct values.</summary>
    DistinctCount,
}

/// <summary>
/// Defines relationship filter direction metadata.
/// </summary>
public enum PowerBiRelationshipDirection
{
    /// <summary>Single-direction filtering.</summary>
    Single,

    /// <summary>Bi-directional filtering.</summary>
    Both,
}

/// <summary>
/// Defines table and column naming policy.
/// </summary>
public enum PowerBiNamingPolicy
{
    /// <summary>Prefer projection annotations, then display name, then canonical name.</summary>
    DisplayName,

    /// <summary>Prefer projection annotations, then canonical name.</summary>
    CanonicalName,
}

/// <summary>
/// Represents the hardened Power BI projection result.
/// </summary>
public sealed record PowerBiProjectionModel
{
    /// <summary>Gets projected tables.</summary>
    public required IReadOnlyList<TabularTableDefinition> Tables { get; init; }

    /// <summary>Gets projected relationships.</summary>
    public required IReadOnlyList<TabularRelationshipDefinition> Relationships { get; init; }

    /// <summary>Gets projection diagnostics.</summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }

    /// <summary>Creates a Power BI projection model from tabular metadata.</summary>
    /// <param name="tabular">The tabular projection.</param>
    /// <returns>The Power BI projection model.</returns>
    public static PowerBiProjectionModel FromTabular(TabularModelDefinition tabular)
    {
        ArgumentNullException.ThrowIfNull(tabular);
        return new PowerBiProjectionModel
        {
            Tables = tabular.Tables,
            Relationships = tabular.Relationships,
            Diagnostics = tabular.Diagnostics,
        };
    }
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
