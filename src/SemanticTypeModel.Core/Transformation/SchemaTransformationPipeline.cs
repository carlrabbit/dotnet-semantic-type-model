using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.Core.Transformation;

/// <summary>
/// Executes deterministic schema transformations over a cloned working copy of the canonical model.
/// </summary>
public sealed class SchemaTransformationPipeline
{
    private readonly List<ISchemaTransformation> _transformations = [];

    /// <summary>
    /// Creates an empty transformation pipeline.
    /// </summary>
    public static SchemaTransformationPipeline Create()
    {
        return new SchemaTransformationPipeline();
    }

    /// <summary>
    /// Adds a transformation to the pipeline in execution order.
    /// </summary>
    public SchemaTransformationPipeline Use(ISchemaTransformation transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);
        _transformations.Add(transformation);
        return this;
    }

    /// <summary>
    /// Runs the configured transformations sequentially against an immutable working copy of the model.
    /// </summary>
    public async ValueTask<SchemaPipelineResult> RunAsync(
        TypeSchemaModel model,
        SchemaPipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        options ??= SchemaPipelineOptions.Default;

        TypeSchemaModelBuilder builder = new(TypeSchemaModelCloner.Clone(model));
        SchemaDiagnosticSink diagnostics = new(options.InitialDiagnostics, options.PromoteWarningsToErrors);

        foreach (ISchemaTransformation transformation in _transformations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SchemaTransformContext context = new()
            {
                Diagnostics = diagnostics,
                AnnotationPolicy = options.AnnotationPolicy,
                PipelineStage = transformation.GetType().Name,
                Services = options.Services,
            };

            await transformation.TransformAsync(builder, context, cancellationToken).ConfigureAwait(false);

            if (diagnostics.HasErrors && !options.ContinueOnError)
            {
                break;
            }
        }

        return new SchemaPipelineResult
        {
            Model = TypeSchemaModelCloner.Clone(builder.Build()),
            Diagnostics = [.. diagnostics.Diagnostics],
        };
    }
}
