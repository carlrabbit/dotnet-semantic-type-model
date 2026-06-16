using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.Core.Transformation;

/// <summary>
/// Represents a deterministic canonical semantic model transformation.
/// </summary>
public interface ISemanticModelTransformation
{
    /// <summary>Gets the stable transformation identifier.</summary>
    string Id { get; }

    /// <summary>Gets the deterministic display name.</summary>
    string DisplayName { get; }

    /// <summary>Transforms the immutable input model and returns the step result.</summary>
    SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context);
}

/// <summary>
/// Carries deterministic execution metadata for a transformation step.
/// </summary>
public sealed record SemanticModelTransformationContext
{
    /// <summary>Gets the transformation identifier for this step.</summary>
    public required string TransformationId { get; init; }

    /// <summary>Gets the deterministic pipeline run identifier.</summary>
    public string PipelineRunId { get; init; } = "default";

    /// <summary>Gets the mutable diagnostic sink for this step.</summary>
    public required SchemaDiagnosticSink Diagnostics { get; init; }

    /// <summary>Gets pipeline execution options.</summary>
    public SchemaPipelineOptions Options { get; init; } = SchemaPipelineOptions.Default;

    /// <summary>Gets annotation normalization policy.</summary>
    public AnnotationPolicy AnnotationPolicy => Options.AnnotationPolicy;

    /// <summary>Gets the cancellation token for this step.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Gets optional services explicitly supplied by the caller.</summary>
    public IServiceProvider? Services => Options.Services;
}

/// <summary>
/// Represents the result of a single transformation step.
/// </summary>
public sealed record SemanticModelTransformationStepResult
{
    /// <summary>Gets the transformed model, or the original model when unchanged.</summary>
    public required TypeSchemaModel Model { get; init; }

    /// <summary>Gets a deterministic summary of changes performed by the transformation.</summary>
    public IReadOnlyList<string> ChangeSummary { get; init; } = [];

    /// <summary>Gets a value indicating whether later transformations should continue.</summary>
    public bool Continue { get; init; } = true;

    /// <summary>Creates an unchanged transformation result.</summary>
    public static SemanticModelTransformationStepResult Unchanged(TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new SemanticModelTransformationStepResult { Model = model };
    }
}

/// <summary>
/// Represents the result of executing a transformation pipeline.
/// </summary>
public sealed record SemanticModelTransformationResult
{
    /// <summary>Gets the transformed canonical semantic model.</summary>
    public required TypeSchemaModel Model { get; init; }

    /// <summary>Gets accumulated diagnostics emitted during transformation.</summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }

    /// <summary>Gets the ordered deterministic transformation trace.</summary>
    public required SemanticTransformationTrace Trace { get; init; }

    /// <summary>Gets a value indicating whether any error diagnostic was emitted.</summary>
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error);
}

/// <summary>
/// Ordered deterministic trace of a transformation pipeline run.
/// </summary>
public sealed record SemanticTransformationTrace
{
    /// <summary>Gets the pipeline display name.</summary>
    public string PipelineName { get; init; } = "Configured";

    /// <summary>Gets ordered trace entries.</summary>
    public IReadOnlyList<SemanticTransformationTraceEntry> Entries { get; init; } = [];
}

/// <summary>
/// Deterministic trace entry for one transformation step.
/// </summary>
public sealed record SemanticTransformationTraceEntry
{
    /// <summary>Gets the one-based sequence index.</summary>
    public required int Sequence { get; init; }

    /// <summary>Gets the transformation identifier.</summary>
    public required string TransformationId { get; init; }

    /// <summary>Gets the deterministic display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the number of diagnostics emitted by this step.</summary>
    public required int DiagnosticCount { get; init; }

    /// <summary>Gets diagnostic codes emitted by this step in emission order.</summary>
    public IReadOnlyList<string> DiagnosticCodes { get; init; } = [];

    /// <summary>Gets optional deterministic change summaries.</summary>
    public IReadOnlyList<string> ChangeSummary { get; init; } = [];
}

/// <summary>
/// Shared result shape for domain semantic model derivation.
/// </summary>
/// <typeparam name="TDomainModel">The package-owned domain semantic model type.</typeparam>
public sealed record SemanticDerivationResult<TDomainModel>
{
    /// <summary>Gets the derived domain semantic model.</summary>
    public required TDomainModel Model { get; init; }

    /// <summary>Gets diagnostics emitted while deriving the domain model.</summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }

    /// <summary>Gets the deterministic transformation trace.</summary>
    public required SemanticTransformationTrace Trace { get; init; }

    /// <summary>Gets a value indicating whether any error diagnostic was emitted.</summary>
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error);
}
