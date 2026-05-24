namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// Represents a scalar (primitive) type in the canonical semantic type model.
/// </summary>
public sealed record ScalarShape : TypeShape
{
    /// <summary>
    /// Gets the primitive kind of this scalar.
    /// </summary>
    public ScalarKind Kind { get; init; }

    /// <summary>
    /// Gets a value indicating whether this scalar accepts null values.
    /// </summary>
    public bool IsNullable { get; init; }
}
