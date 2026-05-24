namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// Represents an array type in the canonical semantic type model.
/// An array shape may optionally specify the type of its items.
/// </summary>
public sealed record ArrayShape : TypeShape
{
    /// <summary>
    /// Gets the shape reference for the item type, or null if items are untyped.
    /// </summary>
    public ShapeRef? Items { get; init; }
}
