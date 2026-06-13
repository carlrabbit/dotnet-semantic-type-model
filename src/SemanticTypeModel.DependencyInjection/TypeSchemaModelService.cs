using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Abstractions.Runtime;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.DependencyInjection;

internal sealed class TypeSchemaModelService : ITypeSchemaModelService, IDisposable
{
    private readonly IReadOnlyList<ITypeSchemaModelProvider> _providers;
    private readonly IReadOnlyList<ISchemaTransformation> _transformations;
    private readonly TypeSchemaRuntimeOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private TypeSchemaModelResult? _cachedResult;

    public TypeSchemaModelService(
        IEnumerable<ITypeSchemaModelProvider> providers,
        IEnumerable<ISchemaTransformation> transformations,
        TypeSchemaRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(transformations);
        ArgumentNullException.ThrowIfNull(options);

        _providers = [.. providers];
        _transformations = [.. transformations];
        _options = options;
    }

    public async ValueTask<TypeSchemaModelResult> GetModelAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.CacheModelResult)
        {
            return await BuildResultAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_cachedResult is not null)
        {
            return _cachedResult;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedResult is not null)
            {
                return _cachedResult;
            }

            TypeSchemaModelResult result = await BuildResultAsync(cancellationToken).ConfigureAwait(false);
            if (!result.HasErrors || _options.CacheFailureResults)
            {
                _cachedResult = result;
            }

            return result;
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    private async ValueTask<TypeSchemaModelResult> BuildResultAsync(CancellationToken cancellationToken)
    {
        if (_providers.Count == 0)
        {
            return new TypeSchemaModelResult
            {
                Diagnostics =
                [
                    RuntimeDiagnostics.Error(
                        code: "STM3001",
                        message: "No semantic type model provider was registered. Register a model instance, model factory, or provider type before requesting the runtime model.",
                        stage: SchemaDiagnosticStage.Transformation),
                ],
            };
        }

        if (_providers.Count > 1)
        {
            return new TypeSchemaModelResult
            {
                Diagnostics =
                [
                    RuntimeDiagnostics.Error(
                        code: "STM3002",
                        message: "Multiple semantic type model providers were registered. Register a single provider for each service collection.",
                        stage: SchemaDiagnosticStage.Transformation),
                ],
            };
        }

        TypeSchemaModelResult providerResult;
        try
        {
            providerResult = await _providers[0].GetModelAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new TypeSchemaModelResult
            {
                Diagnostics =
                [
                    RuntimeDiagnostics.Error(
                        code: "STM3003",
                        message: $"The semantic type model provider threw an exception: {exception.Message}",
                        stage: SchemaDiagnosticStage.Transformation),
                ],
            };
        }

        if (providerResult.Model is null)
        {
            return providerResult;
        }

        if (_transformations.Count == 0)
        {
            return providerResult;
        }

        try
        {
            var pipeline = SchemaTransformationPipeline.Create();
            foreach (ISchemaTransformation transformation in _transformations)
            {
                _ = pipeline.Use(transformation);
            }

            SchemaPipelineResult pipelineResult = await pipeline.RunAsync(
                providerResult.Model,
                new SchemaPipelineOptions
                {
                    ContinueOnError = _options.ContinueOnError,
                    PromoteWarningsToErrors = _options.PromoteWarningsToErrors,
                    InitialDiagnostics = providerResult.Diagnostics,
                },
                cancellationToken).ConfigureAwait(false);

            return new TypeSchemaModelResult
            {
                Model = pipelineResult.Model,
                Diagnostics = pipelineResult.Diagnostics,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            List<SchemaDiagnostic> diagnostics = [.. providerResult.Diagnostics];
            diagnostics.Add(RuntimeDiagnostics.Error(
                code: "STM3004",
                message: $"The semantic type model transformation pipeline threw an exception: {exception.Message}",
                stage: SchemaDiagnosticStage.Transformation));

            return new TypeSchemaModelResult
            {
                Model = providerResult.Model,
                Diagnostics = diagnostics,
            };
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
