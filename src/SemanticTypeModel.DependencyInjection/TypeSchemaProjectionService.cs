using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Abstractions.Runtime;

namespace SemanticTypeModel.DependencyInjection;

internal sealed class TypeSchemaProjectionService<TProjection>(
    ITypeSchemaModelService modelService,
    IEnumerable<RegisteredTypeSchemaProjection<TProjection>> projections) : ITypeSchemaProjectionService<TProjection>
{
    private readonly IReadOnlyList<RegisteredTypeSchemaProjection<TProjection>> _projections = [.. projections];

    public async ValueTask<SchemaProjectionResult<TProjection>> ProjectAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modelService);

        if (_projections.Count == 0)
        {
            return new SchemaProjectionResult<TProjection>
            {
                Diagnostics =
                [
                    RuntimeDiagnostics.Error(
                        code: "STM3006",
                        message: "No semantic type model projection was registered for the requested projection result type.",
                        stage: SchemaDiagnosticStage.Projection),
                ],
            };
        }

        if (_projections.Count > 1)
        {
            return new SchemaProjectionResult<TProjection>
            {
                Diagnostics =
                [
                    RuntimeDiagnostics.Error(
                        code: "STM3007",
                        message: "Multiple semantic type model projections were registered for the requested projection result type. Register a single projection per result type.",
                        stage: SchemaDiagnosticStage.Projection),
                ],
            };
        }

        TypeSchemaModelResult modelResult = await modelService.GetModelAsync(cancellationToken).ConfigureAwait(false);
        if (modelResult.Model is null)
        {
            return new SchemaProjectionResult<TProjection>
            {
                Model = modelResult.Model,
                Diagnostics = modelResult.Diagnostics,
            };
        }

        if (modelResult.HasErrors)
        {
            List<SchemaDiagnostic> diagnostics = [.. modelResult.Diagnostics];
            RegisteredTypeSchemaProjection<TProjection> blockedProjection = _projections[0];
            diagnostics.Add(RuntimeDiagnostics.Error(
                code: "STM3008",
                message: "Projection was blocked because the canonical runtime model contains error diagnostics.",
                stage: SchemaDiagnosticStage.Projection,
                projectionTarget: blockedProjection.Target));

            return new SchemaProjectionResult<TProjection>
            {
                Model = modelResult.Model,
                IsProjectionBlocked = true,
                Diagnostics = diagnostics,
            };
        }

        RegisteredTypeSchemaProjection<TProjection> registeredProjection = _projections[0];
        List<SchemaDiagnostic> projectionDiagnostics = [.. modelResult.Diagnostics];
        SchemaProjectionContext context = new()
        {
            Target = registeredProjection.Target,
            Diagnostics = projectionDiagnostics,
        };

        try
        {
            TProjection projection = registeredProjection.Projection.Project(modelResult.Model, context);
            return new SchemaProjectionResult<TProjection>
            {
                Model = modelResult.Model,
                Projection = projection,
                HasProjection = true,
                Diagnostics = projectionDiagnostics,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            projectionDiagnostics.Add(RuntimeDiagnostics.Error(
                code: "STM3009",
                message: $"The semantic type model projection threw an exception: {exception.Message}",
                stage: SchemaDiagnosticStage.Projection,
                projectionTarget: registeredProjection.Target));

            return new SchemaProjectionResult<TProjection>
            {
                Model = modelResult.Model,
                Diagnostics = projectionDiagnostics,
            };
        }
    }
}
