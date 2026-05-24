namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// Represents a reference to a type shape, either by identifier or as an inline definition.
/// Use <see cref="FromIdentifier"/> for named shapes registered in the model,
/// or <see cref="FromInline"/> for anonymous shapes embedded at the point of use.
/// </summary>
public sealed record ShapeRef
{
    /// <summary>
    /// Gets the inline shape, if this is an inline reference.
    /// </summary>
    public TypeShape? Inline { get; init; }

    /// <summary>
    /// Gets the shape identifier, if this is a named reference.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a named reference (not inline).
    /// </summary>
    public bool IsRef => Identifier is not null;

    /// <summary>
    /// Creates a shape reference that points to a named shape by identifier.
    /// </summary>
    public static ShapeRef FromIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        return new ShapeRef { Identifier = identifier };
    }

    /// <summary>
    /// Creates a shape reference that embeds an inline shape.
    /// </summary>
    public static ShapeRef FromInline(TypeShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        return new ShapeRef { Inline = shape };
    }

    /// <summary>
    /// Resolves this reference to a <see cref="TypeShape"/> using the given model.
    /// </summary>
    /// <param name="model">The model to resolve named references from.</param>
    /// <returns>The resolved type shape.</returns>
    public TypeShape Resolve(TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return Identifier is not null
            ? model.GetShape(Identifier)
            : Inline ?? throw new InvalidOperationException("ShapeRef has neither an identifier nor an inline shape.");
    }
}
