using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.Core.Transformation;

/// <summary>
/// Convenience APIs for transforming canonical semantic models.
/// </summary>
public static class SemanticModelTransformationExtensions
{
    /// <summary>
    /// Transforms a canonical semantic model with an explicitly configured pipeline.
    /// </summary>
    public static SemanticModelTransformationResult Transform(
        this TypeSchemaModel model,
        Action<SchemaTransformationPipeline> configure,
        SchemaPipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(configure);

        var pipeline = SchemaTransformationPipeline.Create();
        configure(pipeline);
        return pipeline.Run(model, options, cancellationToken);
    }
}
