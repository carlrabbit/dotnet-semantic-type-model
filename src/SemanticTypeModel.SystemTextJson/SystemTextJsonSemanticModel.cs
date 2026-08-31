using SemanticTypeModel.Abstractions.Model;
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

    /// <summary>Gets the projected Strong Scalar runtime mappings.</summary>
    public IReadOnlyList<SystemTextJsonStrongScalarDefinition> StrongScalars { get; init; } = [];

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

    /// <summary>Gets the imported object-level creation behavior, when declared.</summary>
    public string? ObjectCreationHandling { get; init; }

    /// <summary>Gets the imported object-level unmapped-member behavior, when declared.</summary>
    public string? UnmappedMemberHandling { get; init; }

    /// <summary>Gets a value indicating whether explicit STJ polymorphism metadata was present.</summary>
    public bool HasPolymorphism { get; init; }

    /// <summary>Gets a value indicating whether this type is a semantic Entity.</summary>
    public bool IsEntity { get; init; }
}

/// <summary>Describes a Strong Scalar mapping needed by the runtime projection.</summary>
public sealed record SystemTextJsonStrongScalarDefinition
{
    /// <summary>Gets the Strong Scalar CLR type identifier.</summary>
    public required TypeId Id { get; init; }
    /// <summary>Gets the underlying scalar CLR type identifier.</summary>
    public required TypeRef ValueType { get; init; }
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

    /// <summary>Gets a value indicating whether the member is ignored by STJ.</summary>
    public bool IsIgnored { get; init; }

    /// <summary>Gets the imported ignore condition.</summary>
    public string? IgnoreCondition { get; init; }

    /// <summary>Gets a value indicating whether the member is explicitly included.</summary>
    public bool IsIncluded { get; init; }

    /// <summary>Gets the imported converter metadata.</summary>
    public string? Converter { get; init; }

    /// <summary>Gets the imported number handling metadata.</summary>
    public string? NumberHandling { get; init; }

    /// <summary>Gets a value indicating whether the member is serializer-required.</summary>
    public bool IsRequired { get; init; }

    /// <summary>Gets the imported object creation handling metadata.</summary>
    public string? ObjectCreationHandling { get; init; }

    /// <summary>Gets the imported unmapped-member handling metadata.</summary>
    public string? UnmappedMemberHandling { get; init; }

    /// <summary>Gets a value indicating whether explicit STJ polymorphism metadata was present.</summary>
    public bool HasPolymorphism { get; init; }
}
