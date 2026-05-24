namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// Represents an object type in the canonical semantic type model.
/// An object shape defines its properties and whether additional properties are permitted.
/// </summary>
public sealed record ObjectShape : TypeShape
{
    /// <summary>
    /// Gets the properties defined on this object shape.
    /// </summary>
    public IReadOnlyList<PropertyShape> Properties { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether additional properties beyond those declared are permitted.
    /// </summary>
    public bool AdditionalPropertiesAllowed { get; init; }
}
