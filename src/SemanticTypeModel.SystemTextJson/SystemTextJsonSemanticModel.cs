using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Represents the System.Text.Json domain semantic model used by resolver projection.
/// </summary>
public sealed record SystemTextJsonSemanticModel
{
    /// <summary>Gets the projected types keyed by canonical type id.</summary>
    public required IReadOnlyDictionary<TypeId, SystemTextJsonTypeDefinition> TypesById { get; init; }

    /// <summary>Gets diagnostics produced while deriving the domain semantic model.</summary>
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>Gets the selected property-name source used by the resolver projection.</summary>
    public required SemanticJsonPropertyNameSource PropertyNameSource { get; init; }

    /// <summary>Gets the transformation trace produced before domain model creation.</summary>
    public SemanticTransformationTrace Trace { get; init; } = new();

    /// <summary>Attempts to get a projected type by canonical type id.</summary>
    public SystemTextJsonTypeDefinition? TryGetType(TypeId id)
    {
        return TypesById.TryGetValue(id, out SystemTextJsonTypeDefinition? type) ? type : null;
    }
}

/// <summary>
/// Represents the System.Text.Json projection of a canonical object type.
/// </summary>
public sealed record SystemTextJsonTypeDefinition
{
    /// <summary>Gets the canonical type id.</summary>
    public required TypeId Id { get; init; }

    /// <summary>Gets the canonical display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets projected properties in deterministic order.</summary>
    public required IReadOnlyList<SystemTextJsonPropertyDefinition> Properties { get; init; }
}

/// <summary>
/// Represents a property after applying System.Text.Json projection metadata.
/// </summary>
public sealed record SystemTextJsonPropertyDefinition
{
    /// <summary>Gets the canonical property id.</summary>
    public required PropertyId Id { get; init; }

    /// <summary>Gets the canonical semantic property name.</summary>
    public required string SemanticName { get; init; }

    /// <summary>Gets the original CLR member name when available.</summary>
    public string? DotNetMemberName { get; init; }

    /// <summary>Gets the imported JsonPropertyNameAttribute value when available.</summary>
    public string? SystemTextJsonPropertyName { get; init; }

    /// <summary>Gets a value indicating whether the property is marked as extension data.</summary>
    public bool IsExtensionData { get; init; }

    /// <summary>Gets the projected JSON name when determinable before resolver matching.</summary>
    public string? ProjectedJsonName { get; init; }
}
