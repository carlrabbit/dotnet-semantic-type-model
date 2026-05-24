namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// Represents an enumeration type in the canonical semantic type model.
/// An enum shape defines the closed set of allowed values.
/// </summary>
public sealed record EnumShape : TypeShape
{
    /// <summary>
    /// Gets the allowed string values for this enumeration.
    /// </summary>
    public IReadOnlyList<string> Values { get; init; } = [];
}
