using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.EFCore;

/// <summary>
/// Configures EF Core domain semantic model derivation.
/// </summary>
public sealed class EfCoreDerivationOptions
{
    private bool _defaultTransformationsApplied;

    /// <summary>Gets the configurable transformation pipeline used before EF Core domain derivation.</summary>
    public SchemaTransformationPipeline Transformations { get; } = SchemaTransformationPipeline.Create();

    /// <summary>Gets or sets EF Core projection options applied after canonical transformations.</summary>
    public EfCoreProjectionOptions Projection { get; set; } = EfCoreProjectionOptions.Default;

    /// <summary>Gets EF Core envelope payload storage policy configuration.</summary>
    public EfCoreEnvelopeProjectionOptions Envelopes { get; } = new();

    /// <summary>Adds the repository-defined default canonical transformations.</summary>
    public EfCoreDerivationOptions UseDefaultTransformations()
    {
        if (!_defaultTransformationsApplied)
        {
            _ = Transformations.UseCoreDefaults();
            _defaultTransformationsApplied = true;
        }

        return this;
    }
}

/// <summary>
/// Provides EF Core domain semantic model derivation extensions.
/// </summary>
public static class EfCoreDerivationExtensions
{
    /// <summary>
    /// Derives an EF Core domain semantic model from the canonical semantic type model.
    /// </summary>
    public static SemanticDerivationResult<EfCoreSemanticModel> DeriveEfCoreModel(
        this TypeSchemaModel model,
        Action<EfCoreDerivationOptions>? configure = null,
        SchemaPipelineOptions? pipelineOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var options = new EfCoreDerivationOptions();
        _ = options.UseDefaultTransformations();
        configure?.Invoke(options);

        SemanticModelTransformationResult transformed = options.Transformations.Run(model, pipelineOptions, cancellationToken);
        var context = new SchemaProjectionContext { Target = ProjectionTarget.EfCore };
        EfCoreProjectionOptions projectionOptions = options.Projection with { EnvelopePolicies = options.Envelopes.Policies };
        EfModelDefinition projected = new EfCoreModelProjection(projectionOptions).Project(transformed.Model, context);
        IReadOnlyList<SchemaDiagnostic> diagnostics = [.. transformed.Diagnostics, .. projected.Diagnostics];

        return new SemanticDerivationResult<EfCoreSemanticModel>
        {
            Model = EfCoreSemanticModel.FromDefinition(projected with { Diagnostics = diagnostics }),
            Diagnostics = diagnostics,
            Trace = transformed.Trace,
        };
    }
}

/// <summary>
/// Stable EF Core domain derivation transformation that records the EF Core boundary in trace output.
/// </summary>
public sealed class EfCoreBoundaryTransformation : ISemanticModelTransformation
{
    /// <inheritdoc />
    public string Id => "efcore.boundary";

    /// <inheritdoc />
    public string DisplayName => "EF Core ModelBuilder Boundary";

    /// <inheritdoc />
    public SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        return new SemanticModelTransformationStepResult
        {
            Model = model,
            ChangeSummary = ["EF Core projection remains limited to domain semantic model derivation and provider-neutral ModelBuilder configuration."],
        };
    }
}
