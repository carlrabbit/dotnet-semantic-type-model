namespace SemanticTypeModel.Abstractions.Contracts;

/// <summary>
/// Represents a reusable transformation that takes a <see cref="Model.TypeSchemaModel"/> and produces a new one.
/// Transformations are composable and suitable for use in both runtime and compile-time pipelines.
/// </summary>
public interface ISchemaTransformation
{
    /// <summary>
    /// Applies this transformation to the given model and returns the result.
    /// </summary>
    /// <param name="input">The source model to transform.</param>
    /// <returns>A new <see cref="Model.TypeSchemaModel"/> representing the transformed output.</returns>
    Model.TypeSchemaModel Transform(Model.TypeSchemaModel input);
}
