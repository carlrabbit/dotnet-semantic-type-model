namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// Represents a dictionary (map) type in the canonical semantic type model.
/// A dictionary shape has string keys and an optional typed value shape.
/// </summary>
public sealed record DictionaryShape : TypeShape
{
    /// <summary>
    /// Gets the shape reference for the value type, or null if values are untyped.
    /// </summary>
    public ShapeRef? Values { get; init; }
}
