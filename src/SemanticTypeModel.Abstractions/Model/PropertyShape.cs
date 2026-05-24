namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// Represents a property of an object type in the canonical semantic type model.
/// A property shape defines its name, type reference, nullability, and annotations.
/// </summary>
public sealed record PropertyShape
{
    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this property is required.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets a value indicating whether this property accepts null values.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets the shape reference for this property's type.
    /// </summary>
    public ShapeRef? Type { get; init; }

    /// <summary>
    /// Gets the annotations attached to this property.
    /// </summary>
    public IReadOnlyList<SchemaAnnotation> Annotations { get; init; } = [];
}
