namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// The abstract base for all canonical type shapes in the semantic type model.
/// A type shape is the canonical, immutable representation of a type and its metadata.
/// </summary>
public abstract record TypeShape
{
    /// <summary>
    /// Gets the identifier for this shape. Named shapes are registered in the
    /// <see cref="TypeSchemaModel"/> by this identifier; anonymous shapes may have a null identifier.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// Gets the annotations attached to this shape.
    /// </summary>
    public IReadOnlyList<SchemaAnnotation> Annotations { get; init; } = [];

    /// <summary>
    /// Gets the constraint set for this shape.
    /// </summary>
    public ConstraintSet Constraints { get; init; } = ConstraintSet.Empty;
}
