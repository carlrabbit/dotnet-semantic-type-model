namespace SemanticTypeModel.Abstractions.Contracts;

/// <summary>
/// Represents a source that can produce a <see cref="Model.TypeSchemaModel"/>.
/// Implementations acquire and normalize schema data, producing the canonical runtime model.
/// </summary>
public interface ISchemaModelSource
{
    /// <summary>
    /// Loads and returns the canonical type schema model from this source.
    /// </summary>
    Model.TypeSchemaModel Load();
}
