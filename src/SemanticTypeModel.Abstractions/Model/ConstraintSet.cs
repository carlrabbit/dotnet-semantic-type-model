namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// An immutable set of named validation constraints applied to a type shape.
/// </summary>
public sealed record ConstraintSet
{
    /// <summary>
    /// Gets a shared empty constraint set.
    /// </summary>
    public static readonly ConstraintSet Empty = new();

    /// <summary>
    /// Gets the constraint entries in this set.
    /// </summary>
    public IReadOnlyList<ConstraintEntry> Entries { get; init; } = [];
}
