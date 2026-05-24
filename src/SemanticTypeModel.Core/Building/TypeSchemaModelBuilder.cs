using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.Core.Building;

/// <summary>
/// A mutable builder for constructing an immutable <see cref="TypeSchemaModel"/>.
/// Add all named shapes before calling <see cref="Build"/> to finalize the model.
/// The resulting model is immutable and cannot be modified after construction.
/// </summary>
public sealed class TypeSchemaModelBuilder
{
    private readonly Dictionary<string, TypeShape> _shapes = new(StringComparer.Ordinal);
    private string? _rootIdentifier;

    /// <summary>
    /// Registers a named shape in the model under the given identifier.
    /// If a shape with the same identifier already exists it is replaced.
    /// </summary>
    /// <param name="identifier">The unique identifier for this shape.</param>
    /// <param name="shape">The shape to register.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public TypeSchemaModelBuilder AddShape(string identifier, TypeShape shape)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(shape);

        _shapes[identifier] = shape with { Identifier = identifier };
        return this;
    }

    /// <summary>
    /// Sets the identifier of the root shape.
    /// The root shape must be registered via <see cref="AddShape"/> before calling <see cref="Build"/>.
    /// </summary>
    /// <param name="identifier">The identifier of the root shape.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public TypeSchemaModelBuilder SetRoot(string identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        _rootIdentifier = identifier;
        return this;
    }

    /// <summary>
    /// Builds and returns the immutable <see cref="TypeSchemaModel"/>.
    /// Validates that all shape references within the model resolve to registered shapes.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a shape reference cannot be resolved.</exception>
    public TypeSchemaModel Build()
    {
        if (_rootIdentifier is not null && !_shapes.ContainsKey(_rootIdentifier))
        {
            throw new InvalidOperationException($"Root identifier '{_rootIdentifier}' is not registered as a shape.");
        }

        ValidateRefs();

        return new TypeSchemaModel(
            new Dictionary<string, TypeShape>(_shapes, StringComparer.Ordinal),
            _rootIdentifier);
    }

    private void ValidateRefs()
    {
        foreach (TypeShape shape in _shapes.Values)
        {
            ValidateShape(shape);
        }
    }

    private void ValidateShape(TypeShape shape)
    {
        switch (shape)
        {
            case ObjectShape obj:
                foreach (PropertyShape property in obj.Properties)
                {
                    ValidateRef(property.Type);
                }
                break;

            case ArrayShape array:
                ValidateRef(array.Items);
                break;

            case DictionaryShape dictionary:
                ValidateRef(dictionary.Values);
                break;

            case UnionShape union:
                foreach (ShapeRef option in union.Options)
                {
                    ValidateRef(option);
                }
                break;

            default:
                break;
        }
    }

    private void ValidateRef(ShapeRef? shapeRef)
    {
        if (shapeRef is null)
        {
            return;
        }

        if (shapeRef.Identifier is not null && !_shapes.ContainsKey(shapeRef.Identifier))
        {
            throw new InvalidOperationException($"Shape reference '{shapeRef.Identifier}' cannot be resolved in this model.");
        }

        if (shapeRef.Inline is not null)
        {
            ValidateShape(shapeRef.Inline);
        }
    }
}
