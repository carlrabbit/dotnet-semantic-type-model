namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// Represents a union type in the canonical semantic type model.
/// A union shape captures <c>oneOf</c> semantics where a value may conform to any one of the listed options.
/// </summary>
public sealed record UnionShape : TypeShape
{
    /// <summary>
    /// Gets the shape references for each option in the union.
    /// </summary>
    public IReadOnlyList<ShapeRef> Options { get; init; } = [];
}
