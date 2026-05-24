namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// The canonical, immutable runtime representation of a semantic type model.
/// A <see cref="TypeSchemaModel"/> is the authoritative source of shapes, their relationships, and their metadata.
/// Once constructed it cannot be modified.
/// </summary>
public sealed class TypeSchemaModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="TypeSchemaModel"/>.
    /// </summary>
    /// <param name="shapes">The named shapes in this model.</param>
    /// <param name="rootIdentifier">The identifier of the root shape, or null if no root is declared.</param>
    public TypeSchemaModel(IReadOnlyDictionary<string, TypeShape> shapes, string? rootIdentifier)
    {
        ArgumentNullException.ThrowIfNull(shapes);

        Shapes = shapes;
        RootIdentifier = rootIdentifier;
    }

    /// <summary>
    /// Gets the identifier of the root shape, or null if no root is declared.
    /// </summary>
    public string? RootIdentifier { get; }

    /// <summary>
    /// Gets the root shape, or null if no root is declared.
    /// </summary>
    public TypeShape? Root => RootIdentifier is not null ? TryGetShape(RootIdentifier) : null;

    /// <summary>
    /// Gets the complete dictionary of named shapes in this model.
    /// </summary>
    public IReadOnlyDictionary<string, TypeShape> Shapes { get; }

    /// <summary>
    /// Returns the shape with the given identifier, or null if not found.
    /// </summary>
    public TypeShape? TryGetShape(string identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        return Shapes.TryGetValue(identifier, out TypeShape? shape) ? shape : null;
    }

    /// <summary>
    /// Returns the shape with the given identifier.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the identifier is not found in this model.</exception>
    public TypeShape GetShape(string identifier)
    {
        return TryGetShape(identifier) ?? throw new InvalidOperationException($"Shape '{identifier}' was not found in the model.");
    }

    /// <summary>
    /// Traverses all named and inline shapes in this model using a depth-first graph walk, visiting each named shape at most once.
    /// </summary>
    public IEnumerable<TypeShape> TraverseAll()
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<TypeShape>(Shapes.Values.Reverse());

        while (stack.Count > 0)
        {
            TypeShape shape = stack.Pop();

            if (shape.Identifier is not null && !visited.Add(shape.Identifier))
            {
                continue;
            }

            yield return shape;

            PushChildren(shape, stack);
        }
    }

    private void PushChildren(TypeShape shape, Stack<TypeShape> stack)
    {
        switch (shape)
        {
            case ObjectShape obj:
                foreach (PropertyShape? prop in obj.Properties.Reverse())
                {
                    PushRef(prop.Type, stack);
                }
                break;

            case ArrayShape array:
                PushRef(array.Items, stack);
                break;

            case DictionaryShape dictionary:
                PushRef(dictionary.Values, stack);
                break;

            case UnionShape union:
                foreach (ShapeRef? option in union.Options.Reverse())
                {
                    PushRef(option, stack);
                }
                break;

            default:
                break;
        }
    }

    private void PushRef(ShapeRef? shapeRef, Stack<TypeShape> stack)
    {
        if (shapeRef is null)
        {
            return;
        }

        if (shapeRef.Identifier is not null)
        {
            if (Shapes.TryGetValue(shapeRef.Identifier, out TypeShape? referenced))
            {
                stack.Push(referenced);
            }

            return;
        }

        if (shapeRef.Inline is not null)
        {
            stack.Push(shapeRef.Inline);
        }
    }
}
