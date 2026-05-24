namespace SemanticTypeModel.Abstractions.Contracts;

/// <summary>
/// Represents a projection that derives a target representation from a <see cref="Model.TypeSchemaModel"/>.
/// Schema projections are the canonical mechanism for exporting to external formats such as JSON Schema,
/// OpenAPI, EF Core, and TypeScript.
/// </summary>
/// <typeparam name="T">The target projection output type.</typeparam>
public interface ISchemaProjection<T>
{
    /// <summary>
    /// Projects the given canonical model into the target representation.
    /// </summary>
    /// <param name="model">The canonical model to project.</param>
    /// <returns>The projected output.</returns>
    T Project(Model.TypeSchemaModel model);
}
