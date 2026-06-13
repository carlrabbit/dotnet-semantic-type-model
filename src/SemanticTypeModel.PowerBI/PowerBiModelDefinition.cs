// CA1720 is disabled because Power BI data-type enum members intentionally use canonical type names
// (for example String/Int64/Decimal) to align with projection terminology and spec contracts.
#pragma warning disable CA1720
using SemanticTypeModel.Abstractions.Canonical;

namespace SemanticTypeModel.PowerBI;

/// <summary>
/// Represents the Power BI projection result for a semantic type model.
/// </summary>
public sealed record PowerBiSemanticModel
{
    /// <summary>
    /// Gets the projected model name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets projected tables.
    /// </summary>
    public required IReadOnlyList<PowerBiTableDefinition> Tables { get; set; }

    /// <summary>
    /// Gets projected relationships.
    /// </summary>
    public required IReadOnlyList<PowerBiRelationshipDefinition> Relationships { get; set; }

    /// <summary>Gets explicit calculated tables.</summary>
    public IReadOnlyList<PowerBiCalculatedTableDefinition> CalculatedTables { get; set; } = [];

    /// <summary>
    /// Gets projection diagnostics.
    /// </summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; set; }
}

/// <summary>
/// Represents the legacy Power BI projection result for a semantic type model.
/// </summary>
public sealed record PowerBiProjectionModel
{
    /// <summary>Gets the projected model name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets projected tables.</summary>
    public required IReadOnlyList<PowerBiTableDefinition> Tables { get; set; }

    /// <summary>Gets projected relationships.</summary>
    public required IReadOnlyList<PowerBiRelationshipDefinition> Relationships { get; set; }

    /// <summary>Gets projection diagnostics.</summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; set; }

    /// <summary>Converts this projection result to the Power BI domain semantic model.</summary>
    public PowerBiSemanticModel ToSemanticModel()
    {
        return new PowerBiSemanticModel { Name = Name, Tables = Tables, Relationships = Relationships, Diagnostics = Diagnostics };
    }
}

/// <summary>
/// Represents a projected Power BI table.
/// </summary>
public sealed record PowerBiTableDefinition
{
    /// <summary>
    /// Gets table name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets optional display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets the Power BI table role.
    /// </summary>
    public PowerBiTableRole Role { get; init; } = PowerBiTableRole.Unknown;

    /// <summary>
    /// Gets a value indicating whether the table is hidden.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets the source semantic type id.
    /// </summary>
    public TypeId? SourceTypeId { get; init; }

    /// <summary>
    /// Gets projected columns.
    /// </summary>
    public required IReadOnlyList<PowerBiColumnDefinition> Columns { get; init; }

    /// <summary>
    /// Gets projected measures.
    /// </summary>
    public required IReadOnlyList<PowerBiMeasureDefinition> Measures { get; init; }

    /// <summary>Gets explicit hierarchies.</summary>
    public IReadOnlyList<PowerBiHierarchyDefinition> Hierarchies { get; init; } = [];

    /// <summary>
    /// Gets optional table description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets optional display folder.
    /// </summary>
    public string? DisplayFolder { get; set; }

    /// <summary>
    /// Gets carried annotations.
    /// </summary>
    public required AnnotationBag Annotations { get; init; }
}

/// <summary>
/// Represents a projected Power BI column.
/// </summary>
public sealed record PowerBiColumnDefinition
{
    /// <summary>
    /// Gets column name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets optional display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets Power BI data type.
    /// </summary>
    public required PowerBiDataType DataType { get; init; }

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
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets optional description.
    /// </summary>
    public string? Description { get; set; }

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
    public string? FormatString { get; set; }

    /// <summary>Gets optional sort-by-column metadata.</summary>
    public string? SortByColumn { get; init; }

    /// <summary>
    /// Gets carried annotations.
    /// </summary>
    public required AnnotationBag Annotations { get; init; }
}

/// <summary>
/// Represents a projected Power BI relationship.
/// </summary>
public sealed record PowerBiRelationshipDefinition
{
    /// <summary>
    /// Gets relationship name.
    /// </summary>
    public required string Name { get; set; }

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
    public required PowerBiRelationshipCardinality Cardinality { get; init; }

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
/// Represents a projected Power BI measure.
/// </summary>
public sealed record PowerBiMeasureDefinition
{
    /// <summary>
    /// Gets measure name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets optional display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets a value indicating whether the measure is hidden.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets measure expression.
    /// </summary>
    public required string Expression { get; set; }

    /// <summary>
    /// Gets expression language.
    /// </summary>
    public string ExpressionLanguage { get; set; } = "DAX";

    /// <summary>
    /// Gets optional format string.
    /// </summary>
    public string? FormatString { get; set; }

    /// <summary>
    /// Gets optional display folder.
    /// </summary>
    public string? DisplayFolder { get; set; }

    /// <summary>
    /// Gets optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets carried annotations.
    /// </summary>
    public AnnotationBag Annotations { get; set; } = new();
}


/// <summary>
/// Represents a projected Power BI calculated table.
/// </summary>
public sealed record PowerBiCalculatedTableDefinition
{
    /// <summary>Gets calculated table name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets DAX expression text. The expression is preserved, not parsed or validated.</summary>
    public required string Expression { get; set; }

    /// <summary>Gets expression language.</summary>
    public string ExpressionLanguage { get; set; } = "DAX";

    /// <summary>Gets optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets optional display folder.</summary>
    public string? DisplayFolder { get; set; }

    /// <summary>Gets a value indicating whether the calculated table is hidden.</summary>
    public bool IsHidden { get; set; }

    /// <summary>Gets carried annotations.</summary>
    public AnnotationBag Annotations { get; set; } = new();
}

/// <summary>Represents a basic explicit Power BI hierarchy.</summary>
public sealed record PowerBiHierarchyDefinition
{
    /// <summary>Gets hierarchy name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets ordered hierarchy levels.</summary>
    public required IReadOnlyList<PowerBiHierarchyLevelDefinition> Levels { get; init; }
}

/// <summary>Represents one level in a Power BI hierarchy.</summary>
public sealed record PowerBiHierarchyLevelDefinition
{
    /// <summary>Gets level name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets the column used by this level.</summary>
    public required string Column { get; init; }

    /// <summary>Gets deterministic level order.</summary>
    public required int Ordinal { get; init; }
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
/// Defines projected Power BI data types.
/// </summary>
public enum PowerBiDataType
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
public enum PowerBiRelationshipCardinality
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
