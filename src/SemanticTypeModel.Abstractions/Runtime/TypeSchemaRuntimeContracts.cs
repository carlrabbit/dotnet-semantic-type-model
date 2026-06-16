using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.Abstractions.Runtime;

/// <summary>
/// Represents the canonical runtime model result after provider acquisition and optional transformation.
/// </summary>
public sealed record TypeSchemaModelResult
{
    /// <summary>
    /// Gets the canonical model produced by the runtime provider pipeline.
    /// </summary>
    public TypeSchemaModel? Model { get; init; }

    /// <summary>
    /// Gets structured diagnostics emitted while acquiring or transforming the model.
    /// </summary>
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the result contains a model instance.
    /// </summary>
    public bool HasModel => Model is not null;

    /// <summary>
    /// Gets a value indicating whether the result contains any error diagnostics.
    /// </summary>
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error);
}

/// <summary>
/// Provides the canonical runtime model without exposing projection-specific behavior.
/// </summary>
public interface ITypeSchemaModelProvider
{
    /// <summary>
    /// Gets the canonical model result.
    /// </summary>
    ValueTask<TypeSchemaModelResult> GetModelAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides the cached runtime model view used by application code and projection services.
/// </summary>
public interface ITypeSchemaModelService
{
    /// <summary>
    /// Gets the canonical runtime model result after provider acquisition and configured transformations.
    /// </summary>
    ValueTask<TypeSchemaModelResult> GetModelAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the output of projecting the canonical runtime model into a target representation.
/// </summary>
/// <typeparam name="TProjection">The projected output type.</typeparam>
public sealed record SchemaProjectionResult<TProjection>
{
    /// <summary>
    /// Gets the canonical model that was supplied to the projection, when available.
    /// </summary>
    public TypeSchemaModel? Model { get; init; }

    /// <summary>
    /// Gets the projected output, when projection completed.
    /// </summary>
    public TProjection? Projection { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Projection"/> was produced.
    /// </summary>
    public bool HasProjection { get; init; }

    /// <summary>
    /// Gets a value indicating whether projection was blocked before projection execution.
    /// </summary>
    public bool IsProjectionBlocked { get; init; }

    /// <summary>
    /// Gets structured diagnostics emitted while acquiring, transforming, or projecting the model.
    /// </summary>
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the result contains any error diagnostics.
    /// </summary>
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error);
}

/// <summary>
/// Projects the canonical runtime model into a target result while preserving diagnostics.
/// </summary>
/// <typeparam name="TProjection">The projected output type.</typeparam>
public interface ITypeSchemaProjectionService<TProjection>
{
    /// <summary>
    /// Projects the canonical runtime model into the target representation.
    /// </summary>
    ValueTask<SchemaProjectionResult<TProjection>> ProjectAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Configures runtime model acquisition, transformation, and caching behavior.
/// </summary>
public sealed record TypeSchemaRuntimeOptions
{
    /// <summary>
    /// Gets the default runtime options.
    /// </summary>
    public static TypeSchemaRuntimeOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the model service caches the acquired and transformed result.
    /// </summary>
    public bool CacheModelResult { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether failed model results are cached when <see cref="CacheModelResult"/> is enabled.
    /// </summary>
    public bool CacheFailureResults { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether later transformations continue after an error diagnostic is emitted.
    /// </summary>
    public bool ContinueOnError { get; init; }

    /// <summary>
    /// Gets a value indicating whether warning diagnostics are promoted to errors during transformation execution.
    /// </summary>
    public bool PromoteWarningsToErrors { get; init; }
}
