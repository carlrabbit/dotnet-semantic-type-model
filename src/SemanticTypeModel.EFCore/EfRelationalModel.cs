#pragma warning disable CA1720
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.EFCore;

/// <summary>Represents the fixed relational projection of a semantic type model.</summary>
public sealed record EfRelationalModel
{
    /// <summary>Gets the source model identifier.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the projected semantic entities.</summary>
    public required IReadOnlyList<EfEntity> Entities { get; init; }
    /// <summary>Gets deterministic projection diagnostics.</summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }
}

/// <summary>Represents one semantic entity table.</summary>
public sealed record EfEntity
{
    /// <summary>Gets the semantic type identifier.</summary>
    public required string SemanticTypeId { get; init; }
    /// <summary>Gets the CLR entity type.</summary>
    public required Type ClrType { get; init; }
    /// <summary>Gets the table name.</summary>
    public required string Table { get; init; }
    /// <summary>Gets the semantic base entity identifier, when present.</summary>
    public string? BaseEntityId { get; init; }
    /// <summary>Gets the primary-key member names.</summary>
    public required IReadOnlyList<string> Key { get; init; }
    /// <summary>Gets scalar columns.</summary>
    public required IReadOnlyList<EfScalarColumn> ScalarColumns { get; init; }
    /// <summary>Gets JSON columns.</summary>
    public required IReadOnlyList<EfJsonColumn> JsonColumns { get; init; }
    /// <summary>Gets binary columns.</summary>
    public required IReadOnlyList<EfScalarColumn> BinaryColumns { get; init; }
}

/// <summary>Represents a scalar relational column.</summary>
public sealed record EfScalarColumn
{
    /// <summary>Gets the semantic property identifier.</summary>
    public required string PropertyId { get; init; }
    /// <summary>Gets the CLR member name.</summary>
    public required string MemberName { get; init; }
    /// <summary>Gets the column name.</summary>
    public required string ColumnName { get; init; }
    /// <summary>Gets the CLR property type.</summary>
    public required Type ClrType { get; init; }
    /// <summary>Gets the provider type.</summary>
    public required Type ProviderType { get; init; }
    /// <summary>Gets whether null is permitted.</summary>
    public bool IsNullable { get; init; }
    /// <summary>Gets the CLR type that actually declares the mapped member.</summary>
    public required Type DeclaringClrType { get; init; }
    /// <summary>Gets the CLR semantic entity whose table stores the column.</summary>
    public required Type StorageClrType { get; init; }
    /// <summary>Gets the semantic type that declares the projected property.</summary>
    public required string SemanticDeclaringTypeId { get; init; }
    /// <summary>Gets the semantic entity whose table stores the column.</summary>
    public required string StorageSemanticTypeId { get; init; }
}

/// <summary>Identifies the fixed JSON document shape.</summary>
public enum EfJsonShape
{
    /// <summary>A JSON object.</summary>
    Object,
    /// <summary>A JSON array.</summary>
    Array,
    /// <summary>A semantic extension-data object.</summary>
    ExtensionData,
}

/// <summary>Represents a JSON document column.</summary>
public sealed record EfJsonColumn
{
    /// <summary>Gets the semantic property identifier.</summary>
    public required string PropertyId { get; init; }
    /// <summary>Gets the CLR member name.</summary>
    public required string MemberName { get; init; }
    /// <summary>Gets the column name.</summary>
    public required string ColumnName { get; init; }
    /// <summary>Gets the JSON document shape.</summary>
    public required EfJsonShape JsonShape { get; init; }
    /// <summary>Gets the CLR value type.</summary>
    public required Type ValueType { get; init; }
    /// <summary>Gets whether null is permitted.</summary>
    public bool IsNullable { get; init; }
    /// <summary>Gets the CLR type that actually declares the mapped member.</summary>
    public required Type DeclaringClrType { get; init; }
    /// <summary>Gets the CLR semantic entity whose table stores the column.</summary>
    public required Type StorageClrType { get; init; }
    /// <summary>Gets the semantic type that declares the projected property.</summary>
    public required string SemanticDeclaringTypeId { get; init; }
    /// <summary>Gets the semantic entity whose table stores the column.</summary>
    public required string StorageSemanticTypeId { get; init; }
}


/// <summary>Represents deterministic diagnostics produced while applying a relational model.</summary>
public sealed record EfRelationalApplicationResult
{
    /// <summary>Gets application diagnostics.</summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }
}

/// <summary>Configures the opinionated relational projection.</summary>
public sealed class EfRelationalOptions
{
    /// <summary>Gets or sets the default relational schema.</summary>
    public string? DefaultSchema { get; set; }
}
